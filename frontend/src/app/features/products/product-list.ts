import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { catchError, map, of, startWith, switchMap } from 'rxjs';

import { ProductsApi } from '../../core/api/stock/products.api';
import { PAGE_SIZE_OPTIONS } from '../../core/api/paging';
import {
  ListProductsQuery,
  ListProductsResponse,
  ProductOrderBy,
} from '../../core/api/stock/models';
import { ApiError } from '../../core/http/problem-details';

type ListState =
  | { readonly status: 'loading' }
  | { readonly status: 'ready'; readonly response: ListProductsResponse }
  | { readonly status: 'error'; readonly error: ApiError };

const COLUMN_TO_ORDER_BY: Readonly<Record<string, ProductOrderBy>> = {
  productCode: 'ProductCode',
  description: 'Description',
  dateCreated: 'CreatedAt',
  dateModified: 'UpdatedAt',
};

@Component({
  selector: 'korp-product-list',
  imports: [
    DatePipe,
    RouterLink,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
})
export class ProductList {
  private readonly api = inject(ProductsApi);

  protected readonly pageSizeOptions = PAGE_SIZE_OPTIONS;
  protected readonly displayedColumns = [
    'productCode',
    'description',
    'stock',
    'dateCreated',
  ] as const;

  protected readonly searchTerm = signal('');
  protected readonly page = signal(1);
  protected readonly rows = signal(PAGE_SIZE_OPTIONS[0]);
  protected readonly orderBy = signal<ProductOrderBy>('ProductCode');
  protected readonly asc = signal(true);

  private readonly reloadToken = signal(0);

  private readonly request = computed<{ token: number; query: ListProductsQuery }>(() => ({
    token: this.reloadToken(),
    query: {
      rows: this.rows(),
      page: this.page(),
      orderBy: this.orderBy(),
      asc: this.asc(),
      searchTerm: this.searchTerm(),
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

  protected onSearch(term: string): void {
    this.page.set(1);
    this.searchTerm.set(term);
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
