import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { API_BASE_URLS } from '../../../core/config/api-base-urls';
import { InvoiceDetail as Invoice, InvoiceStatus } from '../../../core/api/invoicing/models';
import { problemDetailsInterceptor } from '../../../core/http/problem-details.interceptor';
import { InvoiceDetail } from './invoice-detail';

const ID = '01a0263a-4083-7839-800b-26e01bd0c7b0';

function invoice(overrides: Partial<Invoice> = {}): Invoice {
  return {
    id: ID,
    number: 42,
    status: 'Open',
    createdAt: '2026-08-21T21:29:06Z',
    updatedAt: '2026-08-21T21:29:06Z',
    closedAt: null,
    failureReason: null,
    items: [
      { productId: 'p-1', productCode: 'BHS112613-3', description: 'Alavanca Direita', quantity: 3 },
      { productId: 'p-2', productCode: 'ATL0138', description: 'Alavanca Direita', quantity: 2 },
    ],
    ...overrides,
  };
}

describe('InvoiceDetail', () => {
  let backend: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvoiceDetail],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([problemDetailsInterceptor])),
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

  function render(body: Invoice | null, status = 200): ComponentFixture<InvoiceDetail> {
    const fixture = TestBed.createComponent(InvoiceDetail);
    fixture.componentRef.setInput('id', ID);
    fixture.detectChanges();

    const request = backend.expectOne(`http://localhost:3001/invoices/${ID}`);

    if (body === null) {
      request.flush(
        { title: 'Not Found', status, detail: 'Invoice not found.' },
        { status, statusText: 'Not Found' },
      );
    } else {
      request.flush(body);
    }

    fixture.detectChanges();

    return fixture;
  }

  function text(fixture: ComponentFixture<InvoiceDetail>): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  function printButton(fixture: ComponentFixture<InvoiceDetail>): HTMLButtonElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector('button[mat-flat-button]');
  }

  it('requests the invoice named by the route input', () => {
    render(invoice());
  });

  it('renders the number, the items and the totals', () => {
    const content = text(render(invoice()));

    expect(content).toContain('Nota fiscal 42');
    expect(content).toContain('BHS112613-3');
    expect(content).toContain('ATL0138');
    expect(content).toContain('2 item(ns)');
    expect(content).toContain('5 unidade(s)');
  });

  it.each<[InvoiceStatus, string]>([
    ['Open', 'Aberta'],
    ['Processing', 'Processando'],
    ['Closed', 'Fechada'],
  ])('renders %s as %s', (status, label) => {
    expect(text(render(invoice({ status })))).toContain(label);
  });

  it('enables Imprimir only while the invoice is open', () => {
    expect(printButton(render(invoice({ status: 'Open' })))?.disabled).toBe(false);
  });

  it.each<InvoiceStatus>(['Processing', 'Closed'])('disables Imprimir when %s', (status) => {
    expect(printButton(render(invoice({ status })))?.disabled).toBe(true);
  });

  it('explains that Processing resolves without the user acting', () => {
    expect(text(render(invoice({ status: 'Processing' })))).toContain('muda sozinha');
  });

  it('shows the reason a failed print left behind', () => {
    const content = text(
      render(invoice({ status: 'Open', failureReason: 'Insufficient balance for product X.' })),
    );

    expect(content).toContain('Insufficient balance for product X.');
  });

  it('shows closedAt only once the invoice is closed', () => {
    expect(text(render(invoice({ status: 'Open' })))).toContain('—');
    expect(
      text(render(invoice({ status: 'Closed', closedAt: '2026-08-21T22:00:00Z' }))),
    ).toContain('21/08/2026');
  });

  it('offers a way back when the invoice does not exist', () => {
    const content = text(render(null, 404));

    expect(content).toContain('Invoice not found.');
    expect(content).toContain('Tentar novamente');
  });
});
