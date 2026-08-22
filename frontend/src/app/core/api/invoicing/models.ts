import { PagedResponse } from '../paging';

export const INVOICE_STATUSES = ['Open', 'Processing', 'Closed'] as const;

export type InvoiceStatus = (typeof INVOICE_STATUSES)[number];

export interface UnitOfInvoice {
  readonly id: string;
  readonly number: number;
  readonly status: InvoiceStatus;
  readonly createdAt: string;
  readonly updatedAt: string;
  readonly itemCount: number;
  readonly totalQuantity: number;
}

export interface ListInvoicesResponse extends PagedResponse {
  readonly invoices: readonly UnitOfInvoice[];
}

export const INVOICE_ORDER_BY = ['Number', 'CreatedAt', 'UpdatedAt', 'Status'] as const;

export type InvoiceOrderBy = (typeof INVOICE_ORDER_BY)[number];

export interface ListInvoicesQuery {
  readonly rows: number;
  readonly page?: number;
  readonly number?: number;
  readonly orderBy?: InvoiceOrderBy;
  readonly asc?: boolean;
  readonly status?: InvoiceStatus;
}

export interface InvoiceLine {
  readonly productId: string;
  readonly productCode: string;
  readonly description: string;
  readonly quantity: number;
}

export interface CreateInvoiceRequest {
  readonly items: readonly InvoiceLine[];
}

export interface CreateInvoiceResponse {
  readonly id: string;
  readonly number: number;
  readonly status: InvoiceStatus;
  readonly createdAt: string;
  readonly items: readonly InvoiceLine[];
}

export const INSUFFICIENT_STOCK = 'insufficient_stock';

export interface InvoiceFailureLine {
  readonly productId: string;
  readonly requested: number;
  readonly available: number;
}

export interface InvoiceDetail {
  readonly id: string;
  readonly number: number;
  readonly status: InvoiceStatus;
  readonly createdAt: string;
  readonly updatedAt: string;
  readonly closedAt: string | null;
  readonly failureReason: string | null;
  readonly failureCode: string | null;
  readonly failureLines: readonly InvoiceFailureLine[];
  readonly items: readonly InvoiceLine[];
}

export interface PrintInvoiceResponse {
  readonly id: string;
  readonly number: number;
  readonly status: InvoiceStatus;
  readonly updatedAt: string;
}
