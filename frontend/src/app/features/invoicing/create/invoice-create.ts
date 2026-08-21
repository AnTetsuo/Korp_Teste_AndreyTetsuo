import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { Router, RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, map, of, switchMap } from 'rxjs';

import { InvoicesApi } from '../../../core/api/invoicing/invoices.api';
import { InvoiceLine } from '../../../core/api/invoicing/models';
import { UnitOfProduct } from '../../../core/api/stock/models';
import { ProductsApi } from '../../../core/api/stock/products.api';
import { ApiError } from '../../../core/http/problem-details';

@Component({
  selector: 'korp-invoice-create',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatAutocompleteModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatProgressBarModule,
  ],
  templateUrl: './invoice-create.html',
  styleUrl: './invoice-create.scss',
})
export class InvoiceCreate {
  private readonly products = inject(ProductsApi);
  private readonly invoices = inject(InvoicesApi);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly displayedColumns = [
    'productCode',
    'description',
    'quantity',
    'actions',
  ] as const;

  protected readonly search = new FormControl<string | UnitOfProduct>('', { nonNullable: true });
  protected readonly quantity = new FormControl(1, { nonNullable: true });

  protected readonly selected = signal<UnitOfProduct | null>(null);
  protected readonly lines = signal<readonly InvoiceLine[]>([]);
  protected readonly searching = signal(false);
  protected readonly saving = signal(false);

  protected readonly totalQuantity = computed(() =>
    this.lines().reduce((sum, line) => sum + line.quantity, 0),
  );

  protected readonly options = signal<readonly UnitOfProduct[]>([]);

  constructor() {
    this.search.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((term) => {
          if (typeof term !== 'string' || term.trim().length === 0) {
            return of<readonly UnitOfProduct[]>([]);
          }

          this.searching.set(true);

          return this.products
            .list({ rows: 10, searchTerm: term, orderBy: 'ProductCode', asc: true })
            .pipe(
              map((response) => response.products),
              catchError(() => of<readonly UnitOfProduct[]>([])),
            );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((products) => {
        this.searching.set(false);
        this.options.set(products);
      });
  }

  protected displayProduct(value: UnitOfProduct | string | null): string {
    return typeof value === 'string' || value === null ? (value ?? '') : value.productCode;
  }

  protected onSelected(event: MatAutocompleteSelectedEvent): void {
    this.selected.set(event.option.value as UnitOfProduct);
  }

  protected addLine(): void {
    const product = this.selected();
    const quantity = this.quantity.value;

    if (product === null) {
      this.snackBar.open('Escolha um produto antes de adicionar.', 'Fechar', { duration: 4000 });
      return;
    }

    if (!Number.isInteger(quantity) || quantity <= 0) {
      this.snackBar.open('A quantidade deve ser um inteiro maior que zero.', 'Fechar', {
        duration: 4000,
      });
      return;
    }

    if (this.lines().some((line) => line.productId === product.id)) {
      this.snackBar.open(
        `${product.productCode} já está na nota. Remova a linha para alterar a quantidade.`,
        'Fechar',
        { duration: 5000 },
      );
      return;
    }

    this.lines.update((current) => [
      ...current,
      {
        productId: product.id,
        productCode: product.productCode,
        description: product.description,
        quantity,
      },
    ]);

    this.selected.set(null);
    this.options.set([]);
    this.search.setValue('');
    this.quantity.setValue(1);
  }

  protected removeLine(productId: string): void {
    this.lines.update((current) => current.filter((line) => line.productId !== productId));
  }

  protected submit(): void {
    if (this.saving()) {
      return;
    }

    if (this.lines().length === 0) {
      this.snackBar.open('Adicione ao menos um produto à nota.', 'Fechar', { duration: 4000 });
      return;
    }

    this.saving.set(true);

    this.invoices
      .create({ items: this.lines() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (invoice) => {
          this.snackBar.open(`Nota fiscal ${invoice.number} criada.`, 'Fechar', { duration: 5000 });
          void this.router.navigate(['/notas']);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          const apiError = error as ApiError;
          const messages = Object.values(apiError.fieldErrors).flat();

          this.snackBar.open(
            messages.length > 0 ? messages.join(' ') : apiError.message,
            'Fechar',
            { duration: 8000 },
          );
        },
      });
  }
}
