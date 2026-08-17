import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ConfirmService } from '../../core/services/confirm.service';

@Component({
  selector: 'app-confirm',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (confirm.request(); as pedido) {
      <div class="veu" (click)="confirm.answer(false)"></div>
      <div class="caixa" role="dialog" aria-modal="true" [attr.aria-label]="pedido.title">
        <h2>{{ pedido.title }}</h2>
        <p>{{ pedido.text }}</p>
        <div class="acoes-confirm">
          <button type="button" class="cancelar" (click)="confirm.answer(false)">Cancelar</button>
          <button
            type="button"
            [class.perigo]="pedido.destructive"
            [class.confirmar]="!pedido.destructive"
            (click)="confirm.answer(true)">
            {{ pedido.action }}
          </button>
        </div>
      </div>
    }
  `,
  styles: `
    :host { display: contents; }
    .veu { position: fixed; inset: 0; z-index: 950; background: rgb(11 18 32 / 50%); }
    .caixa {
      position: fixed;
      z-index: 951;
      inset: auto var(--s-2) var(--s-2);
      background: var(--surface);
      border: var(--d-bw) solid var(--d-edge);
      border-radius: var(--d-radius);
      padding: var(--s-3);
      display: flex; flex-direction: column; gap: var(--s-1);
      box-shadow: 0 20px 48px -20px rgb(11 18 32 / 60%);
      animation: sobe var(--dur-2) var(--ease-out);
    }
    @media (min-width: 600px) {
      .caixa { inset: 50% auto auto 50%; transform: translate(-50%, -50%); width: 400px; }
      @keyframes sobe { from { opacity: 0; } to { opacity: 1; } }
    }
    h2 { margin: 0; font-size: var(--t-lg); font-family: var(--d-display); }
    p { margin: 0; font-size: var(--t-sm); color: var(--fg-muted); }
    .acoes-confirm { display: flex; justify-content: flex-end; gap: var(--s-1); margin-top: var(--s-2); }
    .acoes-confirm button {
      min-height: var(--s-6);
      padding: 0 var(--s-3);
      border: 1px solid var(--d-edge);
      border-radius: var(--d-radius);
      background: transparent;
      color: var(--fg);
      cursor: pointer;
      font: 700 var(--t-sm)/1 var(--font-sans);
      transition: filter var(--dur-1) var(--ease-out);
    }
    .acoes-confirm button:hover { filter: brightness(1.1); }
    .acoes-confirm button.confirmar {
      background: var(--d-solid);
      border-color: var(--d-solid);
      color: #fff;
    }
    .acoes-confirm button.perigo {
      background: var(--bad);
      border-color: var(--bad);
      color: #fff;
    }
    @keyframes sobe { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: none; } }
  `
})
export class ConfirmComponent {
  protected readonly confirm = inject(ConfirmService);
}

export { ConfirmComponent as Confirm };
