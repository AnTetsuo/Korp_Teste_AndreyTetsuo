import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';

import { ProductsApi } from '../../core/api/stock/products.api';
import { apiErrorsOf, applyApiErrors, clearApiError } from '../../core/http/api-form-errors';
import { ApiError, describeForSupport } from '../../core/http/problem-details';

@Component({
  selector: 'korp-product-create',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: './product-create.html',
  styleUrl: './product-create.scss',
})
export class ProductCreate {
  private readonly api = inject(ProductsApi);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    productCode: ['', Validators.required],
    description: ['', Validators.required],
    initialQuantity: [0, [Validators.required, Validators.min(0)]],
  });

  protected readonly saving = signal(false);

  constructor() {
    const controls: AbstractControl[] = Object.values(this.form.controls);

    for (const control of controls) {
      control.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => clearApiError(control));
    }
  }

  protected serverMessages(field: string): readonly string[] {
    return apiErrorsOf(this.form.get(field));
  }

  protected submit(): void {
    if (this.saving()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    this.api
      .create(this.form.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (product) => {
          this.snackBar.open(`Produto ${product.productCode} cadastrado.`, 'Fechar', {
            duration: 4000,
          });
          void this.router.navigate(['/produtos']);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          const apiError = error as ApiError;
          const unmatched = applyApiErrors(this.form, apiError);

          this.snackBar.open(
            unmatched.length > 0 ? unmatched.join(' ') : describeForSupport(apiError),
            'Fechar',
            { duration: 6000 },
          );
        },
      });
  }
}
