import { InvoiceDetail } from '../core/api/invoicing/models';
import { describePrintFailure } from './print-failure';

function invoice(overrides: Partial<InvoiceDetail> = {}): InvoiceDetail {
  return {
    id: 'i-1',
    number: 19,
    status: 'Open',
    createdAt: '2026-08-21T00:00:00Z',
    updatedAt: '2026-08-21T00:00:00Z',
    closedAt: null,
    failureReason: null,
    failureCode: null,
    failureLines: [],
    items: [
      { productId: 'p-1', productCode: 'PARAF-12', description: 'Parafuso', quantity: 999 },
      { productId: 'p-2', productCode: 'ATL0138', description: 'Alavanca', quantity: 4 },
    ],
    ...overrides,
  };
}

describe('describePrintFailure', () => {
  it('is silent when nothing failed', () => {
    expect(describePrintFailure(invoice())).toBeNull();
  });

  it('names the product by its code, never by its id', () => {
    const failure = describePrintFailure(
      invoice({
        failureReason: 'Stock cannot satisfy every line of this invoice. p-1: Insufficient…',
        failureCode: 'insufficient_stock',
        failureLines: [{ productId: 'p-1', requested: 999, available: 2 }],
      }),
    );

    expect(failure?.headline).toBe('O estoque não tem saldo suficiente para um item desta nota.');
    expect(failure?.lines[0]?.text).toBe('PARAF-12: 2 em estoque, 999 solicitadas.');
    expect(failure?.lines[0]?.text).not.toContain('p-1');
    expect(failure?.rawReason).toBeNull();
  });

  it('pluralises the headline and singularises a quantity of one', () => {
    const failure = describePrintFailure(
      invoice({
        failureReason: 'rejected',
        failureCode: 'insufficient_stock',
        failureLines: [
          { productId: 'p-1', requested: 999, available: 2 },
          { productId: 'p-2', requested: 1, available: 0 },
        ],
      }),
    );

    expect(failure?.headline).toBe('O estoque não tem saldo suficiente para 2 itens desta nota.');
    expect(failure?.lines[1]?.text).toBe('ATL0138: 0 em estoque, 1 solicitada.');
  });

  it('marks exactly the products that failed', () => {
    const failure = describePrintFailure(
      invoice({
        failureReason: 'rejected',
        failureCode: 'insufficient_stock',
        failureLines: [{ productId: 'p-1', requested: 999, available: 2 }],
      }),
    );

    expect(failure?.productIds.has('p-1')).toBe(true);
    expect(failure?.productIds.has('p-2')).toBe(false);
  });

  it('falls back to the id when the line is not on the invoice', () => {
    const failure = describePrintFailure(
      invoice({
        failureReason: 'rejected',
        failureCode: 'insufficient_stock',
        failureLines: [{ productId: 'ghost', requested: 5, available: 0 }],
      }),
    );

    expect(failure?.lines[0]?.productCode).toBe('ghost');
  });

  it('keeps the original reason when an older invoice carries no structured lines', () => {
    const failure = describePrintFailure(
      invoice({ failureReason: 'Stock rejected this invoice.', failureCode: null }),
    );

    expect(failure?.headline).toBe('O estoque recusou esta nota.');
    expect(failure?.rawReason).toBe('Stock rejected this invoice.');
    expect(failure?.lines).toEqual([]);
  });
});
