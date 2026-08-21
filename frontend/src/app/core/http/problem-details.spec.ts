import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { problemDetailsInterceptor } from './problem-details.interceptor';
import { API_ERROR_MESSAGES, ApiError, toApiError } from './problem-details';

function errorResponse(status: number, body: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: body, url: 'http://localhost:3000/products' });
}

describe('toApiError', () => {
  it('maps a validation problem to per-field messages', () => {
    const error = toApiError(
      errorResponse(400, {
        type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: {
          productCode: ['Product code is required.'],
          initialQuantity: ['Quantity must be a whole number.'],
        },
      }),
    );

    expect(error.kind).toBe('validation');
    expect(error.hasFieldErrors).toBe(true);
    expect(error.errorsFor('productCode')).toEqual(['Product code is required.']);
    expect(error.errorsFor('initialQuantity')).toEqual(['Quantity must be a whole number.']);
    expect(error.message).toBe(API_ERROR_MESSAGES.validation);
  });

  it('keeps every message when a field has more than one', () => {
    const error = toApiError(
      errorResponse(400, { errors: { description: ['Required.', 'Too long.'] } }),
    );

    expect(error.errorsFor('description')).toEqual(['Required.', 'Too long.']);
  });

  it('returns an empty list for a field that has no error', () => {
    const error = toApiError(errorResponse(400, { errors: { productCode: ['Required.'] } }));

    expect(error.errorsFor('description')).toEqual([]);
  });

  it('treats a bare 400 from binding as badRequest, not validation', () => {
    const error = toApiError(
      errorResponse(400, {
        type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
        title: 'Bad Request',
        status: 400,
      }),
    );

    expect(error.kind).toBe('badRequest');
    expect(error.hasFieldErrors).toBe(false);
    expect(error.message).toBe(API_ERROR_MESSAGES.badRequest);
  });

  it('survives a 400 with no body at all', () => {
    const error = toApiError(errorResponse(400, null));

    expect(error.kind).toBe('badRequest');
    expect(error.message).toBe(API_ERROR_MESSAGES.badRequest);
  });

  it('prefers the detail on a 404', () => {
    const error = toApiError(errorResponse(404, { title: 'Not Found', detail: 'Invoice not found.' }));

    expect(error.kind).toBe('notFound');
    expect(error.detail).toBe('Invoice not found.');
    expect(error.message).toBe('Invoice not found.');
  });

  it('falls back to a written message when a 404 carries no detail', () => {
    const error = toApiError(errorResponse(404, { title: 'Not Found' }));

    expect(error.message).toBe(API_ERROR_MESSAGES.notFound);
  });

  it('maps a plain conflict, as a double-clicked print produces', () => {
    const error = toApiError(
      errorResponse(409, { title: 'Conflict', detail: 'Invoice is not open.' }),
    );

    expect(error.kind).toBe('conflict');
    expect(error.message).toBe('Invoice is not open.');
  });

  it('maps a conflict that carries field errors as a validation failure', () => {
    const error = toApiError(
      errorResponse(409, {
        detail: 'One or more validation errors occurred.',
        errors: { productCode: ['Product code already exists.'] },
      }),
    );

    expect(error.kind).toBe('validation');
    expect(error.errorsFor('productCode')).toEqual(['Product code already exists.']);
  });

  it('maps a 500 to server', () => {
    const error = toApiError(errorResponse(500, { title: 'An unexpected error occurred.' }));

    expect(error.kind).toBe('server');
    expect(error.message).toBe(API_ERROR_MESSAGES.server);
  });

  it('maps an unreachable service to network', () => {
    const error = toApiError(errorResponse(0, null));

    expect(error.kind).toBe('network');
    expect(error.status).toBe(0);
    expect(error.message).toBe(API_ERROR_MESSAGES.network);
  });

  it('parses a problem body that arrived as text', () => {
    const error = toApiError(
      errorResponse(400, JSON.stringify({ errors: { productCode: ['Required.'] } })),
    );

    expect(error.kind).toBe('validation');
    expect(error.errorsFor('productCode')).toEqual(['Required.']);
  });

  it('ignores an unparseable body rather than throwing', () => {
    const error = toApiError(errorResponse(400, '<html>gateway error</html>'));

    expect(error.kind).toBe('badRequest');
  });

  it('ignores non-string entries inside errors', () => {
    const error = toApiError(
      errorResponse(400, { errors: { productCode: [42, 'Required.'], description: [7] } }),
    );

    expect(error.errorsFor('productCode')).toEqual(['Required.']);
    expect(error.errorsFor('description')).toEqual([]);
  });
});

describe('toApiError, against payloads captured from a live ASP.NET host', () => {
  it('classifies every shape the two APIs actually emit', () => {
    const captured = {
      validation: {
        status: 400,
        body: {
          type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: {
            productCode: ['Product code is required.'],
            initialQuantity: ['Quantity must be a whole number.'],
          },
          traceId: '00-5d22c10accc359e55135eac0c18f7559-db02bcea8d515d05-00',
        },
      },
      binding: {
        status: 400,
        body: {
          type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
          title: 'Bad Request',
          status: 400,
          traceId: '00-71b006928ea652829d165078da424ba7-1c94d4fe8d26e801-00',
        },
      },
      notFound: {
        status: 404,
        body: {
          type: 'https://tools.ietf.org/html/rfc9110#section-15.5.5',
          title: 'Not Found',
          status: 404,
          detail: 'Product not found.',
          traceId: '00-f40523613ef27f5e414e8618b4bd9f20-8de48e6ad40035f5-00',
        },
      },
      conflictWithErrors: {
        status: 409,
        body: {
          type: 'https://tools.ietf.org/html/rfc9110#section-15.5.10',
          title: 'Conflict',
          status: 409,
          detail: 'One or more validation errors occurred.',
          errors: { productCode: ['Product code already exists.'] },
          traceId: '00-ec0308c1f0b69279c53634e86b567437-18256150780f78b7-00',
        },
      },
      conflictPlain: {
        status: 409,
        body: {
          type: 'https://tools.ietf.org/html/rfc9110#section-15.5.10',
          title: 'Conflict',
          status: 409,
          detail: 'Invoice is not open.',
          traceId: '00-06c7ef0356484913b36d84d4f04f477f-9a692c523586ed8d-00',
        },
      },
      serverError: {
        status: 500,
        body: {
          type: 'https://tools.ietf.org/html/rfc9110#section-15.6.1',
          title: 'An error occurred while processing your request.',
          status: 500,
          detail: 'An unexpected error occurred.',
          traceId: '00-45132b79b30c2b183aaf5a38491fe735-a136e27faef3a44f-00',
        },
      },
    };

    const kinds = Object.fromEntries(
      Object.entries(captured).map(([name, { status, body }]) => [
        name,
        toApiError(errorResponse(status, body)).kind,
      ]),
    );

    expect(kinds).toEqual({
      validation: 'validation',
      binding: 'badRequest',
      notFound: 'notFound',
      conflictWithErrors: 'validation',
      conflictPlain: 'conflict',
      serverError: 'server',
    });
  });

  it('keeps the traceId so a failure can be found in the service logs', () => {
    const error = toApiError(
      errorResponse(404, {
        title: 'Not Found',
        detail: 'Product not found.',
        traceId: '00-f40523613ef27f5e414e8618b4bd9f20-8de48e6ad40035f5-00',
      }),
    );

    expect(error.traceId).toBe('00-f40523613ef27f5e414e8618b4bd9f20-8de48e6ad40035f5-00');
  });

  it('leaves traceId null when the payload has none', () => {
    expect(toApiError(errorResponse(0, null)).traceId).toBeNull();
  });
});

describe('problemDetailsInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([problemDetailsInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('replaces HttpErrorResponse with ApiError', async () => {
    const failure = new Promise<unknown>((resolve) =>
      http.get('/products').subscribe({ error: resolve }),
    );

    backend
      .expectOne('/products')
      .flush({ errors: { productCode: ['Product code is required.'] } }, {
        status: 400,
        statusText: 'Bad Request',
      });

    const error = await failure;

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).errorsFor('productCode')).toEqual(['Product code is required.']);
  });

  it('leaves a successful response alone', async () => {
    const value = new Promise<unknown>((resolve) => http.get('/products').subscribe(resolve));

    backend.expectOne('/products').flush({ products: [] });

    expect(await value).toEqual({ products: [] });
  });
});
