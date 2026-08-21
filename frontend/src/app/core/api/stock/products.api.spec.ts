import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { API_BASE_URLS } from '../../config/api-base-urls';
import { ApiError } from '../../http/problem-details';
import { problemDetailsInterceptor } from '../../http/problem-details.interceptor';
import { MIN_PAGE_SIZE, PAGE_SIZE_OPTIONS } from '../paging';
import { ProductsApi } from './products.api';

describe('ProductsApi', () => {
  let api: ProductsApi;
  let backend: HttpTestingController;

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

    api = TestBed.inject(ProductsApi);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('sends paging and sorting to the stock service', () => {
    api.list({ rows: 25, page: 3, orderBy: 'Description', asc: false }).subscribe();

    const request = backend.expectOne(
      (candidate) => candidate.url === 'http://localhost:3000/products',
    );

    expect(request.request.params.get('rows')).toBe('25');
    expect(request.request.params.get('page')).toBe('3');
    expect(request.request.params.get('orderBy')).toBe('Description');
    expect(request.request.params.get('asc')).toBe('false');
    request.flush({ products: [], page: 3, rows: 25, totalCount: 0, totalPages: 0 });
  });

  it('omits an empty search term rather than sending a blank one', () => {
    api.list({ rows: 10, searchTerm: '   ' }).subscribe();

    const request = backend.expectOne(
      (candidate) => candidate.url === 'http://localhost:3000/products',
    );

    expect(request.request.params.has('searchTerm')).toBe(false);
    request.flush({ products: [], page: 1, rows: 10, totalCount: 0, totalPages: 0 });
  });

  it('trims a search term before sending it', () => {
    api.list({ rows: 10, searchTerm: '  parafuso  ' }).subscribe();

    const request = backend.expectOne(
      (candidate) => candidate.url === 'http://localhost:3000/products',
    );

    expect(request.request.params.get('searchTerm')).toBe('parafuso');
    request.flush({ products: [], page: 1, rows: 10, totalCount: 0, totalPages: 0 });
  });

  it('posts a new product to the stock service', () => {
    api
      .create({ productCode: 'P-1', description: 'Parafuso', initialQuantity: 4 })
      .subscribe();

    const request = backend.expectOne('http://localhost:3000/products');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      productCode: 'P-1',
      description: 'Parafuso',
      initialQuantity: 4,
    });
    request.flush({ id: 'x', productCode: 'P-1', description: 'Parafuso', quantity: 4 }, {
      status: 201,
      statusText: 'Created',
    });
  });

  it('surfaces a duplicate code as an ApiError carrying the field message', async () => {
    const failure = new Promise<unknown>((resolve) =>
      api
        .create({ productCode: 'P-1', description: 'Parafuso', initialQuantity: 0 })
        .subscribe({ error: resolve }),
    );

    backend.expectOne('http://localhost:3000/products').flush(
      {
        title: 'Conflict',
        detail: 'One or more validation errors occurred.',
        errors: { productCode: ['Product code already exists.'] },
      },
      { status: 409, statusText: 'Conflict' },
    );

    const error = (await failure) as ApiError;

    expect(error).toBeInstanceOf(ApiError);
    expect(error.kind).toBe('validation');
    expect(error.errorsFor('productCode')).toEqual(['Product code already exists.']);
  });

  it('never offers a page size the services would reject', () => {
    expect(Math.min(...PAGE_SIZE_OPTIONS)).toBeGreaterThanOrEqual(MIN_PAGE_SIZE);
  });
});
