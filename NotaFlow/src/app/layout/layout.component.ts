import { afterNextRender, ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TOUR_INICIAL } from '../core/tour-inicial';
import { AuthService } from '../core/services/auth.service';
import { ThemeService } from '../core/services/theme.service';
import { TourService } from '../core/services/tour.service';
import { LogoComponent } from '../design-system/logo/logo.component';

interface Destino {
  readonly rota: string;
  readonly rotulo: string;
  readonly curto: string;
  readonly icone: string;
  readonly gerente?: boolean;
}

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LogoComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent {
  private readonly router = inject(Router);

  constructor() {
    afterNextRender(() => void this.tour.startOnce('inicial', TOUR_INICIAL));
  }

  protected readonly auth = inject(AuthService);
  protected readonly theme = inject(ThemeService);
  protected readonly tour = inject(TourService);

  protected readonly recolhida = signal(false);

  private readonly destinos: readonly Destino[] = [
    { rota: '/painel', rotulo: 'Painel', curto: 'Painel', icone: 'painel' },
    { rota: '/produtos', rotulo: 'Produtos', curto: 'Prod', icone: 'caixa' },
    { rota: '/notas', rotulo: 'Notas fiscais', curto: 'Notas', icone: 'documento' },
    { rota: '/usuarios', rotulo: 'Usuários', curto: 'Users', icone: 'pessoa', gerente: true }
  ];

  protected readonly visiveis = computed(() =>
    this.destinos.filter(destino => !destino.gerente || this.auth.manager())
  );

  protected readonly escuro = signal(this.theme.theme() === 'dark');

  protected alternarTema(): void {
    this.escuro.update(atual => !atual);
    this.theme.set(this.escuro() ? 'dark' : 'light');
  }

  protected async sair(): Promise<void> {
    await this.auth.logout();
    await this.router.navigate(['/entrar']);
  }

  protected reverTour(): void {
    this.tour.forget();
    void this.tour.start('inicial', TOUR_INICIAL);
  }
}
