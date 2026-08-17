import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-erro',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="tela">
      <div class="palco">
        @switch (codigo()) {
          @case ('404') {
            <svg viewBox="0 0 128 88" fill="none" aria-hidden="true">
              <rect x="30" y="8" width="58" height="72" rx="2" fill="var(--d-paper)" stroke="var(--d-accent)" stroke-width="1.5" />
              <path d="M72 8v12h16" stroke="var(--d-accent)" stroke-width="1.5" stroke-linejoin="round" />
              <path d="M40 36h38M40 45h38M40 54h22" stroke="var(--d-accent)" stroke-width="1.5" stroke-linecap="round" opacity=".38" />
              <circle cx="90" cy="64" r="16" fill="var(--d-paper)" stroke="var(--d-accent)" stroke-width="1.5" />
              <path d="m84 58 12 12M96 58l-12 12" stroke="var(--d-accent)" stroke-width="2" stroke-linecap="round" />
            </svg>
          }
          @case ('403') {
            <svg viewBox="0 0 128 88" fill="none" aria-hidden="true">
              <rect x="48" y="40" width="34" height="32" rx="3" fill="none" stroke="var(--bad)" stroke-width="2" />
              <path d="M54 40v-8a11 11 0 0 1 22 0v8" stroke="var(--bad)" stroke-width="2" stroke-linecap="round" />
              <circle cx="65" cy="55" r="3.5" fill="var(--bad)" />
            </svg>
          }
          @default {
            <svg viewBox="0 0 128 88" fill="none" aria-hidden="true">
              <path d="M26 40a44 44 0 0 1 76 0" stroke="var(--fg-muted)" stroke-width="2.5" stroke-linecap="round" opacity=".26" />
              <path d="M40 54a27 27 0 0 1 48 0" stroke="var(--fg-muted)" stroke-width="2.5" stroke-linecap="round" opacity=".5" />
              <path d="M55 66a12 12 0 0 1 18 0" stroke="var(--fg-muted)" stroke-width="2.5" stroke-linecap="round" />
              <circle cx="64" cy="76" r="3.5" fill="var(--fg-muted)" />
              <path d="m32 18 64 62" stroke="var(--bad)" stroke-width="3" stroke-linecap="round" />
            </svg>
          }
        }
      </div>

      <p class="cod">{{ codigo() }}</p>
      <h1>{{ titulo() }}</h1>
      <p class="txt">{{ texto() }}</p>

      <a class="primario" [routerLink]="destino()">{{ acao() }}</a>
    </div>
  `,
  styles: `
    .tela {
      min-height: 60vh;
      display: flex; flex-direction: column; align-items: center; justify-content: center;
      gap: var(--s-1); text-align: center; padding: var(--s-4) var(--s-2);
    }
    .palco {
      width: 100%; max-width: 260px; padding: var(--s-3);
      background: var(--d-tint); border-radius: var(--d-radius); margin-bottom: var(--s-2);
      svg { width: 100%; height: auto; }
    }
    .cod {
      margin: 0; font: 700 11px/1 var(--font-mono);
      letter-spacing: .18em; text-transform: uppercase; color: var(--d-ink);
    }
    h1 { margin: 0; font-family: var(--d-display); font-size: var(--t-xl); }
    .txt { margin: 0; max-width: 46ch; font-size: var(--t-sm); color: var(--fg-muted); }
    .primario { margin-top: var(--s-2); text-decoration: none; display: inline-flex; align-items: center; }
  `
})
export class ErroComponent {
  readonly codigo = input('404');
  readonly titulo = input('Página não encontrada');
  readonly texto = input('O endereço não existe ou o item foi removido.');
  readonly acao = input('Voltar ao painel');
  readonly destino = input('/painel');
}

export { ErroComponent as Erro };
