import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { API_BASE_URLS } from '../../config/api-base-urls';
import { ApiError } from '../../http/problem-details';
import { problemDetailsInterceptor } from '../../http/problem-details.interceptor';
import { InvoicesApi } from './invoices.api';

describe('InvoicesApi', () => {
  let api: InvoicesApi;
  let backend: HttpTestingController;

  const empty = { invoices: [], page: 1, rows: 10, totalCount: 0, totalPages: 0 };

  function expectList() {
    return backend.expectOne(
      (candidate) => candidate.url === 'http://localhost:3001/invoices',
    );
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([problemDetailsInterceptor])),
        provideHttpClientTesting(),
        {
          provide: API_BASE_URLS,
          useValue: { stock: 'http://localhost:3000', invoicing: 'http://localhost:3001' },
        },
      ],
    });

    api = TestBed.inject(InvoicesApi);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('reads the invoicing base URL, not the stock one', () => {
    api.list({ rows: 10 }).subscribe();

    expectList().flush(empty);
  });

  it('sends the status filter the print screen needs', () => {
    api.list({ rows: 10, status: 'Open' }).subscribe();

    const request = expectList();

    expect(request.request.params.get('status')).toBe('Open');
    request.flush(empty);
  });

  it('sends paging and descending sort', () => {
    api.list({ rows: 25, page: 2, orderBy: 'Number', asc: false }).subscribe();

    const request = expectList();

    expect(request.request.params.get('rows')).toBe('25');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('orderBy')).toBe('Number');
    expect(request.request.params.get('asc')).toBe('false');
    request.flush(empty);
  });

  it('sends an exact number filter', () => {
    api.list({ rows: 10, number: 42 }).subscribe();

    const request = expectList();

    expect(request.request.params.get('number')).toBe('42');
    request.flush(empty);
  });

  it('omits every optional filter that was not supplied', () => {
    api.list({ rows: 10 }).subscribe();

    const request = expectList();

    expect(request.request.params.has('status')).toBe(false);
    expect(request.request.params.has('number')).toBe(false);
    expect(request.request.params.has('orderBy')).toBe(false);
    expect(request.request.params.has('asc')).toBe(false);
    expect(request.request.params.has('page')).toBe(false);
    request.flush(empty);
  });

  it('surfaces a rejected enum as an ApiError with no field to blame', async () => {
    const failure = new Promise<unknown>((resolve) =>
      api.list({ rows: 10 }).subscribe({ error: resolve }),
    );

    expectList().flush(
      { type: '…15.5.1', title: 'Bad Request', status: 400, traceId: '00-abc-def-00' },
      { status: 400, statusText: 'Bad Request' },
    );

    const error = (await failure) as ApiError;

    expect(error).toBeInstanceOf(ApiError);
    expect(error.kind).toBe('badRequest');
    expect(error.hasFieldErrors).toBe(false);
  });
});
