export interface Product {
  readonly id: string;
  readonly code: string;
  readonly description: string;
  readonly balance: number;
  readonly active: boolean;
}

export interface CreateProductRequest {
  readonly code: string;
  readonly description: string;
  readonly balance: number;
}

export interface UpdateProductRequest {
  readonly code: string;
  readonly description: string;
  readonly active: boolean;
}
