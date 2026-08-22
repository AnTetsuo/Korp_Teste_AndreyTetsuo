import { INSUFFICIENT_STOCK, InvoiceDetail } from '../core/api/invoicing/models';

export interface PrintFailureLine {
  readonly productId: string;
  readonly productCode: string;
  readonly requested: number;
  readonly available: number;
  readonly text: string;
}

export interface PrintFailure {
  readonly headline: string;
  readonly lines: readonly PrintFailureLine[];
  readonly rawReason: string | null;
  readonly productIds: ReadonlySet<string>;
}

export function describePrintFailure(invoice: InvoiceDetail): PrintFailure | null {
  if (invoice.failureReason === null || invoice.failureReason.length === 0) {
    return null;
  }

  const byProductId = new Map(invoice.items.map((item) => [item.productId, item]));

  const lines = invoice.failureLines.map((line): PrintFailureLine => {
    const productCode = byProductId.get(line.productId)?.productCode ?? line.productId;

    return {
      productId: line.productId,
      productCode,
      requested: line.requested,
      available: line.available,
      text: `${productCode}: ${line.available} em estoque, ${line.requested} solicitada${
        line.requested === 1 ? '' : 's'
      }.`,
    };
  });

  return {
    headline: headlineFor(invoice.failureCode, lines.length),
    lines,
    rawReason: lines.length === 0 ? invoice.failureReason : null,
    productIds: new Set(lines.map((line) => line.productId)),
  };
}

function headlineFor(code: string | null, lineCount: number): string {
  if (code !== INSUFFICIENT_STOCK || lineCount === 0) {
    return 'O estoque recusou esta nota.';
  }

  return lineCount === 1
    ? 'O estoque não tem saldo suficiente para um item desta nota.'
    : `O estoque não tem saldo suficiente para ${lineCount} itens desta nota.`;
}
