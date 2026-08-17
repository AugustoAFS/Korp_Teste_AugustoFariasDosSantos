import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-toast',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="pilha" role="status" aria-live="polite">
      @for (t of toasts.toasts(); track t.id) {
        <div class="toast" [class]="t.kind">
          <span class="txt">{{ t.text }}</span>
          <button type="button" (click)="toasts.dismiss(t.id)" aria-label="Fechar aviso">&times;</button>
        </div>
      }
    </div>
  `,
  styles: `
    .pilha {
      position: fixed;
      inset: auto var(--s-2) var(--s-2);
      z-index: 900;
      display: flex;
      flex-direction: column;
      gap: var(--s-1);
      pointer-events: none;
    }
    @media (min-width: 768px) {
      .pilha { inset: auto var(--s-3) var(--s-3) auto; max-width: 400px; }
    }
    .toast {
      pointer-events: auto;
      display: flex;
      align-items: flex-start;
      gap: var(--s-1);
      padding: var(--s-2);
      background: var(--surface);
      border: 1px solid var(--line);
      border-left: 3px solid var(--fg-subtle);
      border-radius: var(--s-half);
      box-shadow: 0 12px 32px -14px rgb(11 18 32 / 45%);
      animation: entra var(--dur-3) var(--ease-out);
    }
    .toast.ok   { border-left-color: var(--ok); }
    .toast.warn { border-left-color: var(--warn); }
    .toast.bad  { border-left-color: var(--bad); }
    .txt { flex: 1; font-size: var(--t-sm); }
    button {
      border: 0; background: transparent; cursor: pointer;
      color: var(--fg-subtle); font-size: var(--t-lg); line-height: 1;
      min-width: var(--s-3); min-height: var(--s-3);
    }
    @keyframes entra { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: none; } }
  `
})
export class ToastComponent {
  protected readonly toasts = inject(ToastService);
}

export { ToastComponent as Toast };
