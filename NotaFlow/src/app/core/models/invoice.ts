export type InvoiceStatus = 'Open' | 'Closed';

export type InvoiceSituation = 'Open' | 'Printing' | 'Pending' | 'Closed';

export interface InvoiceItem {
  readonly id: number;
  readonly productId: string;
  readonly productCode: string;
  readonly productDescription: string;
  readonly quantity: number;
}

export interface Invoice {
  readonly id: number;
  readonly number: number;
  readonly status: InvoiceStatus;
  readonly issuedByUserName: string;
  readonly createdAt: string;
  readonly closedAt: string | null;
  readonly processingId: string | null;

  readonly printing: boolean;
  readonly editable: boolean;

  readonly lastError: string | null;
  readonly rejectionExplanation: string | null;
  readonly items: readonly InvoiceItem[];
}

export interface AddInvoiceItemRequest {
  readonly productId: string;
  readonly quantity: number;
}

export interface UpdateInvoiceItemRequest {
  readonly quantity: number;
}

export interface InterpretedItem {
  readonly productId: string;
  readonly productCode: string;
  readonly productDescription: string;
  readonly quantity: number;
  readonly alreadyInInvoice: boolean;
}

export interface InterpretationResult {
  readonly items: readonly InterpretedItem[];
  readonly unresolved: readonly string[];
}
