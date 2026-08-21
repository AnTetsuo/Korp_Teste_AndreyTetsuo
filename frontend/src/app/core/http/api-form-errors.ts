import { AbstractControl, FormGroup } from '@angular/forms';

import { ApiError } from './problem-details';

export const API_ERROR_KEY = 'api';

export function applyApiErrors(form: FormGroup, error: ApiError): readonly string[] {
  const unmatched: string[] = [];

  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    const control = form.get(field);

    if (control === null) {
      unmatched.push(...messages);
      continue;
    }

    control.setErrors({ ...(control.errors ?? {}), [API_ERROR_KEY]: messages });
    control.markAsTouched();
  }

  return unmatched;
}

export function clearApiErrors(form: FormGroup): void {
  for (const control of Object.values(form.controls)) {
    clearApiError(control);
  }
}

export function apiErrorsOf(control: AbstractControl | null): readonly string[] {
  const messages = control?.errors?.[API_ERROR_KEY];

  return Array.isArray(messages) ? (messages as readonly string[]) : [];
}

export function clearApiError(control: AbstractControl): void {
  if (control.errors === null || !(API_ERROR_KEY in control.errors)) {
    return;
  }

  const remaining = { ...control.errors };
  delete remaining[API_ERROR_KEY];

  control.setErrors(Object.keys(remaining).length > 0 ? remaining : null);
}
