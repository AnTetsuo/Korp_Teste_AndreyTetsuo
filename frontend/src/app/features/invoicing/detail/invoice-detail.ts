import { DatePipe } from '@angular/common';
import { Component, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { catchError, map, of, startWith, switchMap } from 'rxjs';

import { InvoicesApi } from '../../../core/api/invoicing/invoices.api';
import { InvoiceDetail as Invoice } from '../../../core/api/invoicing/models';
import { ApiError } from '../../../core/http/problem-details';
import { InvoiceStatusPipe } from '../../../shared/invoice-status.pipe';

type DetailState =
  | { readonly status: 'loading' }
  | { readonly status: 'ready'; readonly invoice: Invoice }
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

  readonly id = input.required<string>();

  protected readonly displayedColumns = ['productCode', 'description', 'quantity'] as const;

  private readonly reloadToken = signal(0);

  private readonly request = computed(() => ({ token: this.reloadToken(), id: this.id() }));

  protected readonly state = toSignal(
    toObservable(this.request).pipe(
      switchMap(({ id }) =>
        this.api.get(id).pipe(
          map((invoice): DetailState => ({ status: 'ready', invoice })),
          catchError((error: unknown) =>
            of<DetailState>({ status: 'error', error: error as ApiError }),
          ),
          startWith<DetailState>({ status: 'loading' }),
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

  protected retry(): void {
    this.reloadToken.update((token) => token + 1);
  }
}
