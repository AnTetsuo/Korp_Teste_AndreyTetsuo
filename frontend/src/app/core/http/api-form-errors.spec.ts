import { FormBuilder, Validators } from '@angular/forms';

import { apiErrorsOf, applyApiErrors, clearApiError } from './api-form-errors';
import { ApiError } from './problem-details';

function validationError(fieldErrors: Record<string, readonly string[]>): ApiError {
  return new ApiError(400, 'validation', 'Verifique os campos destacados.', null, fieldErrors, null);
}

describe('applyApiErrors', () => {
  const build = () =>
    new FormBuilder().nonNullable.group({
      productCode: ['', Validators.required],
      description: ['ok'],
    });

  it('puts a server message on the matching control', () => {
    const form = build();

    const unmatched = applyApiErrors(
      form,
      validationError({ productCode: ['Product code already exists.'] }),
    );

    expect(apiErrorsOf(form.controls.productCode)).toEqual(['Product code already exists.']);
    expect(form.controls.productCode.touched).toBe(true);
    expect(unmatched).toEqual([]);
  });

  it('returns messages for fields the form does not have', () => {
    const form = build();

    const unmatched = applyApiErrors(form, validationError({ body: ['A request body is required.'] }));

    expect(unmatched).toEqual(['A request body is required.']);
  });

  it('keeps client-side validators alongside the server message', () => {
    const form = build();

    applyApiErrors(form, validationError({ productCode: ['Product code is required.'] }));

    expect(form.controls.productCode.hasError('required')).toBe(true);
    expect(apiErrorsOf(form.controls.productCode)).toEqual(['Product code is required.']);
  });

  it('clears only the server message, leaving client validators intact', () => {
    const form = build();
    applyApiErrors(form, validationError({ productCode: ['Product code is required.'] }));

    clearApiError(form.controls.productCode);

    expect(apiErrorsOf(form.controls.productCode)).toEqual([]);
    expect(form.controls.productCode.hasError('required')).toBe(true);
  });

  it('leaves a control valid when the server message was its only error', () => {
    const form = build();
    applyApiErrors(form, validationError({ description: ['Description is required.'] }));

    clearApiError(form.controls.description);

    expect(form.controls.description.errors).toBeNull();
  });

  it('reports no messages for a control that has none', () => {
    expect(apiErrorsOf(build().controls.description)).toEqual([]);
    expect(apiErrorsOf(null)).toEqual([]);
  });
});
