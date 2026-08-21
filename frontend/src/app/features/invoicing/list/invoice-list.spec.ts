import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { API_BASE_URLS } from '../../../core/config/api-base-urls';
import { InvoiceList } from './invoice-list';

describe('InvoiceList', () => {
  let backend: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvoiceList],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: API_BASE_URLS,
          useValue: { stock: 'http://localhost:3000', invoicing: 'http://localhost:3001' },
        },
      ],
    }).compileComponents();

    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  function render() {
    const fixture = TestBed.createComponent(InvoiceList);
    fixture.detectChanges();

    backend
      .expectOne((request) => request.url === 'http://localhost:3001/invoices')
      .flush({ invoices: [], page: 1, rows: 10, totalCount: 0, totalPages: 0 });

    fixture.detectChanges();

    return fixture.nativeElement as HTMLElement;
  }

  it('resolves the Nova nota button through RouterLink, not as an inert attribute', () => {
    const anchor = render().querySelector('a[routerLink]');

    expect(anchor).not.toBeNull();
    expect(anchor?.getAttribute('href')).toBe('/notas/nova');
  });
});
