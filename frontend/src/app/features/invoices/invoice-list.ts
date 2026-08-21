import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { catchError, map, of, startWith, switchMap } from 'rxjs';

import { InvoicesApi } from '../../core/api/invoicing/invoices.api';
import {
  INVOICE_STATUSES,
  InvoiceOrderBy,
  InvoiceStatus,
  ListInvoicesQuery,
  ListInvoicesResponse,
} from '../../core/api/invoicing/models';
import { PAGE_SIZE_OPTIONS } from '../../core/api/paging';
import { ApiError } from '../../core/http/problem-details';
import { InvoiceStatusPipe } from '../../shared/invoice-status.pipe';

type ListState =
  | { readonly status: 'loading' }
  | { readonly status: 'ready'; readonly response: ListInvoicesResponse }
  | { readonly status: 'error'; readonly error: ApiError };

const COLUMN_TO_ORDER_BY: Readonly<Record<string, InvoiceOrderBy>> = {
  number: 'Number',
  status: 'Status',
  createdAt: 'CreatedAt',
  updatedAt: 'UpdatedAt',
};

const ORDER_BY_TO_COLUMN: Readonly<Record<InvoiceOrderBy, string>> = {
  Number: 'number',
  Status: 'status',
  CreatedAt: 'createdAt',
  UpdatedAt: 'updatedAt',
};

@Component({
  selector: 'korp-invoice-list',
  imports: [
    DatePipe,
    InvoiceStatusPipe,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  templateUrl: './invoice-list.html',
  styleUrl: './invoice-list.scss',
})
export class InvoiceList {
  private readonly api = inject(InvoicesApi);

  protected readonly pageSizeOptions = PAGE_SIZE_OPTIONS;
  protected readonly statuses = INVOICE_STATUSES;
  protected readonly displayedColumns = [
    'number',
    'status',
    'itemCount',
    'totalQuantity',
    'createdAt',
  ] as const;

  protected readonly status = signal<InvoiceStatus | null>(null);
  protected readonly number = signal<number | null>(null);
  protected readonly page = signal(1);
  protected readonly rows = signal(PAGE_SIZE_OPTIONS[0]);
  protected readonly orderBy = signal<InvoiceOrderBy>('Number');
  protected readonly asc = signal(false);

  private readonly reloadToken = signal(0);

  private readonly request = computed<{ token: number; query: ListInvoicesQuery }>(() => ({
    token: this.reloadToken(),
    query: {
      rows: this.rows(),
      page: this.page(),
      orderBy: this.orderBy(),
      asc: this.asc(),
      status: this.status() ?? undefined,
      number: this.number() ?? undefined,
    },
  }));

  protected readonly state = toSignal(
    toObservable(this.request).pipe(
      switchMap(({ query }) =>
        this.api.list(query).pipe(
          map((response): ListState => ({ status: 'ready', response })),
          catchError((error: unknown) =>
            of<ListState>({ status: 'error', error: error as ApiError }),
          ),
          startWith<ListState>({ status: 'loading' }),
        ),
      ),
      takeUntilDestroyed(),
    ),
    { initialValue: { status: 'loading' } as ListState },
  );

  protected readonly totalCount = computed(() => {
    const state = this.state();

    return state.status === 'ready' ? state.response.totalCount : 0;
  });

  protected readonly sortColumn = computed(() => ORDER_BY_TO_COLUMN[this.orderBy()]);

  protected onStatus(status: InvoiceStatus | null): void {
    this.page.set(1);
    this.status.set(status);
  }

  protected onNumber(raw: string): void {
    const parsed = Number.parseInt(raw, 10);

    this.page.set(1);
    this.number.set(Number.isInteger(parsed) && parsed > 0 ? parsed : null);
  }

  protected onSort(sort: Sort): void {
    const mapped = COLUMN_TO_ORDER_BY[sort.active];

    if (mapped === undefined || sort.direction === '') {
      return;
    }

    this.page.set(1);
    this.orderBy.set(mapped);
    this.asc.set(sort.direction === 'asc');
  }

  protected onPage(event: PageEvent): void {
    this.rows.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }

  protected retry(): void {
    this.reloadToken.update((token) => token + 1);
  }
}
