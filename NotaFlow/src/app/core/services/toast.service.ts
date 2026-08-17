import { Injectable, signal } from '@angular/core';

export type ToastKind = 'ok' | 'warn' | 'bad';

export interface Toast {
  readonly id: number;
  readonly kind: ToastKind;
  readonly text: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private static readonly Duration: Readonly<Record<ToastKind, number>> = {
    ok: 4000,
    warn: 5000,
    bad: 7000
  };

  private sequence = 0;

  private readonly _toasts = signal<readonly Toast[]>([]);

  readonly toasts = this._toasts.asReadonly();

  ok(text: string): void { this.push('ok', text); }
  warn(text: string): void { this.push('warn', text); }
  bad(text: string): void { this.push('bad', text); }

  dismiss(id: number): void {
    this._toasts.update(list => list.filter(toast => toast.id !== id));
  }

  private push(kind: ToastKind, text: string): void {
    const id = ++this.sequence;

    this._toasts.update(list => [...list, { id, kind, text }]);

    setTimeout(() => this.dismiss(id), ToastService.Duration[kind]);
  }
}
