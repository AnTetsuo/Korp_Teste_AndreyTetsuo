import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { provideRouter } from '@angular/router';

import { API_BASE_URLS } from '../../../core/config/api-base-urls';
import { InvoiceDetail as Invoice, InvoiceStatus } from '../../../core/api/invoicing/models';
import { problemDetailsInterceptor } from '../../../core/http/problem-details.interceptor';
import { GRACE_TICKS, POLL_INTERVAL_MS, InvoiceDetail } from './invoice-detail';

const ID = '01a0263a-4083-7839-800b-26e01bd0c7b0';
const URL = `http://localhost:3001/invoices/${ID}`;
const REASON = 'Insufficient balance: 2 available, 999 requested.';
const PT_REASON = 'BHS112613-3: 2 em estoque, 3 solicitadas.';

function rejected(): Partial<Invoice> {
  return {
    status: 'Open',
    failureReason: REASON,
    failureCode: 'insufficient_stock',
    failureLines: [{ productId: 'p-1', requested: 3, available: 2 }],
  };
}

function invoice(overrides: Partial<Invoice> = {}): Invoice {
  return {
    id: ID,
    number: 42,
    status: 'Open',
    createdAt: '2026-08-21T21:29:06Z',
    updatedAt: '2026-08-21T21:29:06Z',
    closedAt: null,
    failureReason: null,
    failureCode: null,
    failureLines: [],
    items: [
      { productId: 'p-1', productCode: 'BHS112613-3', description: 'Alavanca Direita', quantity: 3 },
      { productId: 'p-2', productCode: 'ATL0138', description: 'Alavanca Direita', quantity: 2 },
    ],
    ...overrides,
  };
}

describe('InvoiceDetail', () => {
  let backend: HttpTestingController;
  let fixture: ComponentFixture<InvoiceDetail> | null = null;

  beforeEach(async () => {
    vi.useFakeTimers();

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

  afterEach(() => {
    fixture?.destroy();
    fixture = null;
    backend.verify();
    vi.useRealTimers();
  });

  async function start(): Promise<ComponentFixture<InvoiceDetail>> {
    fixture = TestBed.createComponent(InvoiceDetail);
    fixture.componentRef.setInput('id', ID);
    fixture.detectChanges();
    await vi.advanceTimersByTimeAsync(0);

    return fixture;
  }

  function answer(body: Invoice): void {
    backend.expectOne(URL).flush(body);
    fixture?.detectChanges();
  }

  async function open(body: Invoice): Promise<ComponentFixture<InvoiceDetail>> {
    const created = await start();
    answer(body);

    return created;
  }

  async function tick(): Promise<void> {
    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS);
  }

  function text(): string {
    return (fixture?.nativeElement as HTMLElement).textContent ?? '';
  }

  function printButton(): HTMLButtonElement | null {
    return (fixture?.nativeElement as HTMLElement).querySelector('button[mat-flat-button]');
  }

  it('renders the number, the items and the totals', async () => {
    await open(invoice());

    expect(text()).toContain('Nota fiscal 42');
    expect(text()).toContain('BHS112613-3');
    expect(text()).toContain('5 unidade(s)');
  });

  it.each<[InvoiceStatus, string]>([
    ['Open', 'Aberta'],
    ['Closed', 'Fechada'],
  ])('renders %s as %s and stops after one request', async (status, label) => {
    await open(invoice({ status }));

    expect(text()).toContain(label);

    await tick();
    backend.verify();
  });

  it('enables Imprimir only while the invoice is open', async () => {
    await open(invoice({ status: 'Open' }));

    expect(printButton()?.disabled).toBe(false);
  });

  it.each<InvoiceStatus>(['Processing', 'Closed'])('disables Imprimir when %s', async (status) => {
    await open(invoice({ status }));

    expect(printButton()?.disabled).toBe(true);
  });

  it('shows closedAt only once the invoice is closed', async () => {
    await open(invoice({ status: 'Closed', closedAt: '2026-08-21T22:00:00Z' }));

    expect(text()).toContain('21/08/2026');
  });

  it('offers a way back when the invoice does not exist', async () => {
    await start();
    backend
      .expectOne(URL)
      .flush({ title: 'Not Found', detail: 'Invoice not found.' }, { status: 404, statusText: 'Not Found' });
    fixture?.detectChanges();

    expect(text()).toContain('Invoice not found.');
    expect(text()).toContain('Tentar novamente');
  });

  it('keeps polling while Processing and stops as soon as it closes', async () => {
    await open(invoice({ status: 'Processing' }));

    expect(text()).toContain('Processando');
    expect(text()).toContain('muda sozinha');

    await tick();
    answer(invoice({ status: 'Processing' }));

    expect(text()).toContain('Processando');

    await tick();
    answer(invoice({ status: 'Closed', closedAt: '2026-08-21T22:00:00Z' }));

    expect(text()).toContain('Fechada');

    await tick();
    backend.verify();
  });

  it('softens the copy during the grace window, then reveals the reason', async () => {
    await open(invoice(rejected()));

    expect(text()).toContain('Ainda confirmando com o estoque');
    expect(text()).not.toContain(PT_REASON);

    for (let i = 0; i < GRACE_TICKS; i++) {
      await tick();
      answer(invoice(rejected()));
    }

    expect(text()).toContain(PT_REASON);
    expect(text()).not.toContain('Ainda confirmando com o estoque');

    await tick();
    backend.verify();
  });

  it('ends the grace window early when a late confirmation closes the invoice', async () => {
    await open(invoice(rejected()));

    await tick();
    answer(invoice({ status: 'Closed', closedAt: '2026-08-21T22:00:00Z' }));

    expect(text()).toContain('Fechada');
    expect(text()).not.toContain(PT_REASON);

    await tick();
    backend.verify();
  });

  it('does not wait around for a rejection that carries no reason', async () => {
    await open(invoice({ status: 'Open', failureReason: null }));

    expect(text()).not.toContain('Ainda confirmando');

    await tick();
    backend.verify();
  });

  it('starts watching after a successful print', async () => {
    await open(invoice({ status: 'Open' }));

    printButton()?.click();

    backend
      .expectOne({ url: `${URL}/print`, method: 'POST' })
      .flush({ id: ID, number: 42, status: 'Processing', updatedAt: '2026-08-21T22:00:00Z' }, {
        status: 202,
        statusText: 'Accepted',
      });

    fixture?.detectChanges();
    await vi.advanceTimersByTimeAsync(0);
    answer(invoice({ status: 'Processing' }));

    expect(text()).toContain('Processando');
  });

  it('announces success only once the printed invoice closes', async () => {
    await open(invoice({ status: 'Open' }));
    const snackBar = vi.spyOn(TestBed.inject(MatSnackBar), 'open');

    printButton()?.click();
    backend
      .expectOne({ url: `${URL}/print`, method: 'POST' })
      .flush({ id: ID, number: 42, status: 'Processing', updatedAt: '2026-08-21T22:00:00Z' }, {
        status: 202,
        statusText: 'Accepted',
      });

    fixture?.detectChanges();
    await vi.advanceTimersByTimeAsync(0);
    answer(invoice({ status: 'Processing' }));

    expect(snackBar).not.toHaveBeenCalled();

    await tick();
    answer(invoice({ status: 'Closed', closedAt: '2026-08-21T22:00:00Z' }));

    expect(snackBar).toHaveBeenCalledOnce();
    expect(snackBar.mock.calls[0]?.[0]).toContain('impressa com sucesso');
  });

  it('stays quiet when an already-closed invoice is merely opened', async () => {
    const snackBar = vi.spyOn(TestBed.inject(MatSnackBar), 'open');

    await open(invoice({ status: 'Closed', closedAt: '2026-08-21T22:00:00Z' }));

    expect(snackBar).not.toHaveBeenCalled();
  });

  it('does not claim success when the print was rejected', async () => {
    await open(invoice({ status: 'Open' }));
    const snackBar = vi.spyOn(TestBed.inject(MatSnackBar), 'open');

    printButton()?.click();
    backend
      .expectOne({ url: `${URL}/print`, method: 'POST' })
      .flush({ id: ID, number: 42, status: 'Processing', updatedAt: '2026-08-21T22:00:00Z' }, {
        status: 202,
        statusText: 'Accepted',
      });

    fixture?.detectChanges();
    await vi.advanceTimersByTimeAsync(0);
    answer(invoice(rejected()));

    for (let i = 0; i < GRACE_TICKS; i++) {
      await tick();
      answer(invoice(rejected()));
    }

    expect(snackBar).not.toHaveBeenCalled();
    expect(text()).toContain(PT_REASON);
  });

  it('marks only the failing row, and never shows a raw product id', async () => {
    await open(invoice(rejected()));

    for (let i = 0; i < GRACE_TICKS; i++) {
      await tick();
      answer(invoice(rejected()));
    }

    const rows = (fixture?.nativeElement as HTMLElement).querySelectorAll('tr[mat-row]');

    expect(rows.length).toBe(2);
    expect(rows[0]?.classList.contains('failed')).toBe(true);
    expect(rows[1]?.classList.contains('failed')).toBe(false);
    expect(text()).not.toContain('p-1');
    expect(text()).toContain('BHS112613-3');

    await tick();
    backend.verify();
  });

  it('treats a 409 as a normal outcome and watches anyway', async () => {
    await open(invoice({ status: 'Open' }));

    printButton()?.click();

    backend
      .expectOne({ url: `${URL}/print`, method: 'POST' })
      .flush({ title: 'Conflict', detail: 'Invoice is not open.' }, {
        status: 409,
        statusText: 'Conflict',
      });

    fixture?.detectChanges();
    await vi.advanceTimersByTimeAsync(0);
    answer(invoice({ status: 'Processing' }));

    expect(text()).toContain('Processando');
  });
});
