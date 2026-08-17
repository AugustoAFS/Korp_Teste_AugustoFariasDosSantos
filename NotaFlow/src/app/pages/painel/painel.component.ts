import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { API } from '../../core/api.const';
import { Invoice } from '../../core/models/invoice';
import { PagedResult } from '../../core/models/paged-result';
import { Product } from '../../core/models/product';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-painel',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './painel.component.html',
  styleUrl: './painel.component.scss'
})
export class PainelComponent {
  protected readonly auth = inject(AuthService);

  protected readonly produtos = httpResource<PagedResult<Product>>(
    () => ({ url: `${API}/produtos`, params: { page: 1, size: 100 } })
  );

  protected readonly notas = httpResource<PagedResult<Invoice>>(
    () => ({ url: `${API}/notas`, params: { page: 1, size: 100 } })
  );

  protected readonly carregando = computed(() => this.produtos.isLoading() || this.notas.isLoading());

  private readonly listaProdutos = computed(() => this.produtos.value()?.items ?? []);
  private readonly listaNotas = computed(() => this.notas.value()?.items ?? []);

  protected readonly totalProdutos = computed(() => this.produtos.value()?.total ?? 0);
  protected readonly totalNotas = computed(() => this.notas.value()?.total ?? 0);
  protected readonly saldoTotal = computed(() => this.listaProdutos().reduce((s, p) => s + p.balance, 0));
  protected readonly baixos = computed(() => this.listaProdutos().filter(p => p.active && p.balance <= 10));

  protected readonly fechadas = computed(() => this.listaNotas().filter(n => n.status === 'Closed').length);
  protected readonly processando = computed(() => this.listaNotas().filter(n => n.printing).length);
  protected readonly pendentes = computed(
    () => this.listaNotas().filter(n => n.status === 'Open' && !n.printing && n.lastError).length
  );
  protected readonly abertas = computed(
    () => this.listaNotas().length - this.fechadas() - this.processando() - this.pendentes()
  );

  protected readonly barras = computed(() => {
    const total = Math.max(1, this.listaNotas().length);
    const linha = (rotulo: string, valor: number, classe: string) =>
      ({ rotulo, valor, classe, largura: Math.round((valor / total) * 100) });

    return [
      linha('Fechadas', this.fechadas(), 'ok'),
      linha('Abertas', this.abertas(), 'off'),
      linha('Processando', this.processando(), 'andamento'),
      linha('Pendentes', this.pendentes(), 'ruim')
    ];
  });

  protected readonly ultimas = computed(() => this.listaNotas().slice(0, 5));
}

export { PainelComponent as Painel };
