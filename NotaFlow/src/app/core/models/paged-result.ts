export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly size: number;
  readonly total: number;
  readonly totalPages: number;
}
