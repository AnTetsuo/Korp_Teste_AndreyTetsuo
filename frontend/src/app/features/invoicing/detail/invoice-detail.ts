import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import {
  Observable,
  catchError,
  concat,
  concatWith,
  defer,
  finalize,
  map,
  of,
  startWith,
  switchMap,
  take,
  takeWhile,
  tap,
  timer,
} from 'rxjs';

import { InvoicesApi } from '../../../core/api/invoicing/invoices.api';
import { InvoiceDetail as Invoice } from '../../../core/api/invoicing/models';
import { ApiError } from '../../../core/http/problem-details';
import { InvoiceStatusPipe } from '../../../shared/invoice-status.pipe';

export const POLL_INTERVAL_MS = 1000;
export const GRACE_TICKS = 10;

type DetailState =
  | { readonly status: 'loading' }
  | { readonly status: 'ready'; readonly invoice: Invoice; readonly watching: boolean }
  | { readonly status: 'error'; readonly error: ApiError };

@Component({
  selector: 'korp-invoice-detail',
  imports: [
    DatePipe,
    RouterLink,
    InvoiceStatusPipe,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  templateUrl: './invoice-detail.html',
  styleUrl: './invoice-detail.scss',
})
export class InvoiceDetail {
  private readonly api = inject(InvoicesApi);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly id = input.required<string>();

  protected readonly displayedColumns = ['productCode', 'description', 'quantity'] as const;

  protected readonly printing = signal(false);

  private readonly printedId = signal<string | null>(null);
  private readonly reloadToken = signal(0);

  private readonly request = computed(() => ({ token: this.reloadToken(), id: this.id() }));

  protected readonly state = toSignal(
    toObservable(this.request).pipe(
      switchMap(({ id }) =>
        this.watch(id).pipe(
          catchError((error: unknown) =>
            of<DetailState>({ status: 'error', error: error as ApiError }),
          ),
          startWith<DetailState>({ status: 'loading' }),
          tap((state) => this.announce(state)),
          finalize(() => this.printing.set(false)),
        ),
      ),
      takeUntilDestroyed(),
    ),
    { initialValue: { status: 'loading' } as DetailState },
  );

  protected readonly totalQuantity = computed(() => {
    const current = this.state();

    return current.status === 'ready'
      ? current.invoice.items.reduce((sum, item) => sum + item.quantity, 0)
      : 0;
  });

  protected canPrint(invoice: Invoice): boolean {
    return invoice.status === 'Open';
  }

  protected print(invoice: Invoice): void {
    if (this.printing() || !this.canPrint(invoice)) {
      return;
    }

    this.printing.set(true);

    this.api
      .print(invoice.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.printedId.set(invoice.id);
          this.reloadToken.update((token) => token + 1);
        },
        error: (error: unknown) => {
          const apiError = error as ApiError;

          if (apiError.status === 409) {
            this.snackBar.open('Esta nota já está sendo impressa.', 'Fechar', { duration: 4000 });
            this.printedId.set(invoice.id);
            this.reloadToken.update((token) => token + 1);
            return;
          }

          this.printing.set(false);
          this.snackBar.open(apiError.message, 'Fechar', { duration: 6000 });
        },
      });
  }

  protected retry(): void {
    this.reloadToken.update((token) => token + 1);
  }

  private announce(state: DetailState): void {
    if (state.status !== 'ready' || state.watching) {
      return;
    }

    if (state.invoice.status === 'Processing' || this.printedId() !== state.invoice.id) {
      return;
    }

    this.printedId.set(null);

    if (state.invoice.status === 'Closed') {
      this.snackBar.open(
        `Nota fiscal ${state.invoice.number} impressa com sucesso.`,
        'Fechar',
        { duration: 6000, panelClass: 'korp-snack-success' },
      );
    }
  }

  private watch(id: string): Observable<DetailState> {
    return timer(0, POLL_INTERVAL_MS).pipe(
      switchMap(() => this.api.get(id)),
      takeWhile((invoice) => invoice.status === 'Processing', true),
      switchMap((invoice) =>
        invoice.status === 'Open' && invoice.failureReason !== null
          ? this.settleAfterFailure(id, invoice)
          : of<DetailState>({ status: 'ready', invoice, watching: false }),
      ),
    );
  }

  private settleAfterFailure(id: string, reopened: Invoice): Observable<DetailState> {
    let latest = reopened;

    return concat(of(reopened), this.gracePolls(id).pipe(tap((invoice) => (latest = invoice))))
      .pipe(map((invoice): DetailState => ({ status: 'ready', invoice, watching: true })))
      .pipe(
        concatWith(
          defer(() => of<DetailState>({ status: 'ready', invoice: latest, watching: false })),
        ),
      );
  }

  private gracePolls(id: string): Observable<Invoice> {
    return timer(POLL_INTERVAL_MS, POLL_INTERVAL_MS).pipe(
      take(GRACE_TICKS),
      switchMap(() => this.api.get(id)),
      takeWhile((invoice) => invoice.status !== 'Closed', true),
    );
  }
}
