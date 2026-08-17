import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-logo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg [attr.width]="tamanho()" [attr.height]="tamanho()" viewBox="0 0 32 32"
         role="img" aria-label="Emissor NF">
      <rect x="1" y="1" width="14" height="30" rx="2" fill="#C2410C" />
      <rect x="17" y="1" width="14" height="30" rx="2" fill="#1E40AF" />
      <g fill="none" stroke="#fff" stroke-width="1.8" stroke-linejoin="round">
        <path d="M4.5 12.5 8 10.5l3.5 2v7L8 21.5l-3.5-2z" />
        <path d="M4.5 12.5 8 14.5l3.5-2M8 14.5v7" />
        <path d="M20.5 10h4.5l3 3v9h-7.5z" />
        <path d="M25 10v3h3" />
      </g>
    </svg>
  `,
  styles: `
    :host { display: inline-flex; line-height: 0; }
    svg { display: block; }
  `
})
export class LogoComponent {
  readonly tamanho = input(24);
}
