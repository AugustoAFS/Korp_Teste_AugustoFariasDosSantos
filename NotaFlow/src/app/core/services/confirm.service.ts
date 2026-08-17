import { Injectable, signal } from '@angular/core';

export interface ConfirmRequest {
  readonly title: string;
  readonly text: string;
  readonly action: string;
  readonly destructive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private readonly _request = signal<ConfirmRequest | null>(null);
  private resolver: ((ok: boolean) => void) | null = null;

  readonly request = this._request.asReadonly();

  ask(request: ConfirmRequest): Promise<boolean> {
    this._request.set(request);

    return new Promise<boolean>(resolve => (this.resolver = resolve));
  }

  answer(ok: boolean): void {
    this._request.set(null);
    this.resolver?.(ok);
    this.resolver = null;
  }
}
