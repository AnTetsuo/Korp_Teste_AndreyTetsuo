import { PagedResponse } from '../paging';

export interface UnitOfProduct {
  readonly id: string;
  readonly description: string;
  readonly productCode: string;
  readonly dateCreated: string;
  readonly dateModified: string;
  readonly stock: number;
}

export interface ListProductsResponse extends PagedResponse {
  readonly products: readonly UnitOfProduct[];
}

export const PRODUCT_ORDER_BY = ['Description', 'ProductCode', 'CreatedAt', 'UpdatedAt'] as const;

export type ProductOrderBy = (typeof PRODUCT_ORDER_BY)[number];

export interface ListProductsQuery {
  readonly rows: number;
  readonly page?: number;
  readonly searchTerm?: string;
  readonly orderBy?: ProductOrderBy;
  readonly asc?: boolean;
}

export interface CreateProductRequest {
  readonly productCode: string;
  readonly description: string;
  readonly initialQuantity: number;
}

export interface CreateProductResponse {
  readonly id: string;
  readonly productCode: string;
  readonly description: string;
  readonly quantity: number;
}
