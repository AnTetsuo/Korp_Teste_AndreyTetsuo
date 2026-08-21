export const MIN_PAGE_SIZE = 5;

export const PAGE_SIZE_OPTIONS: readonly number[] = [10, 25, 50];

export interface PagedResponse {
  readonly page: number;
  readonly rows: number;
  readonly totalCount: number;
  readonly totalPages: number;
}
