import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { TourService } from '../../core/services/tour.service';

interface Posicao {
  readonly top: number;
  readonly left: number;
}

@Component({
  selector: 'app-coachmark',
  templateUrl: './coachmark.html',
  styleUrl: './coachmark.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CoachmarkComponent {
  private static readonly Largura = 380;
  private static readonly Folga = 16;

  protected readonly tour = inject(TourService);
  protected readonly posicao = signal<Posicao | null>(null);

  constructor() {
    effect(onCleanup => {
      const seletor = this.tour.step()?.anchor;

      this.posicao.set(null);

      if (!seletor) return;

      let alvo: HTMLElement | null = null;
      let tentativas = 0;
      let agendado = 0;

      const reposicionar = () => {
        if (alvo) this.posicao.set(this.calcular(alvo));
      };

      const procurar = () => {
        const achado = document.querySelector<HTMLElement>(seletor);

        if (achado?.offsetParent) {
          alvo = achado;
          alvo.classList.add('tour-alvo');
          alvo.scrollIntoView({ behavior: 'smooth', block: 'center' });
          agendado = window.setTimeout(reposicionar, 320);
          return;
        }

        if (++tentativas < 40) agendado = window.setTimeout(procurar, 50);
      };

      agendado = window.setTimeout(procurar, 0);
      window.addEventListener('resize', reposicionar);
      window.addEventListener('scroll', reposicionar, true);

      onCleanup(() => {
        window.clearTimeout(agendado);
        window.removeEventListener('resize', reposicionar);
        window.removeEventListener('scroll', reposicionar, true);
        alvo?.classList.remove('tour-alvo');
      });
    });
  }

  private calcular(alvo: HTMLElement): Posicao | null {
    if (window.innerWidth < 768) return null;

    const area = alvo.getBoundingClientRect();
    const folga = CoachmarkComponent.Folga;
    const largura = CoachmarkComponent.Largura;

    const cabeAbaixo = area.bottom + folga + 220 < window.innerHeight;
    const top = cabeAbaixo ? area.bottom + folga : Math.max(folga, area.top - 220 - folga);

    const direita = area.left + largura + folga < window.innerWidth;
    const left = direita
      ? Math.max(folga, area.left)
      : Math.max(folga, window.innerWidth - largura - folga);

    return { top, left };
  }
}

export { CoachmarkComponent as Coachmark };
