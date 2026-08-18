import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { API } from '../../core/api.const';
import { Invoice, InvoiceSituation } from '../../core/models/invoice';
import { PagedResult } from '../../core/models/paged-result';
import { ConfirmService } from '../../core/services/confirm.service';
import { InvoicesService } from '../../core/services/invoices.service';
import { ToastService } from '../../core/services/toast.service';
import { EmptyStateComponent } from '../../design-system/empty-state/empty-state.component';
import { SkeletonDirective } from '../../design-system/skeleton/skeleton.directive';

@Component({
  selector: 'app-notas',
  imports: [DatePipe, RouterLink, EmptyStateComponent, SkeletonDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './notas.component.html'
})
export class NotasComponent {
  private readonly service = inject(InvoicesService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly confirm = inject(ConfirmService);

  protected readonly linhas = computed<readonly (Invoice | undefined)[]>(() =>
    this.dados.isLoading() ? Array.from({ length: 4 }) : (this.dados.value()?.items ?? [])
  );

  protected readonly pagina = signal(1);
  protected readonly situacao = signal<InvoiceSituation | ''>('');
  protected readonly criando = signal(false);

  protected readonly dados = httpResource<PagedResult<Invoice>>(() => ({
    url: `${API}/notas`,
    params: { page: this.pagina(), size: 20, ...(this.situacao() ? { situation: this.situacao() } : {}) }
  }));

  protected filtrar = (valor: string) => (this.pagina.set(1), this.situacao.set(valor as InvoiceSituation | ''));

  protected estado = (n: Invoice) =>
    n.status === 'Closed' ? 'Fechada'
      : n.printing ? 'Processando'
        : n.lastError ? 'Pendente'
          : 'Aberta';

  protected classe = (n: Invoice) =>
    n.status === 'Closed' ? 'ok'
      : n.printing ? 'andamento'
        : n.lastError ? 'ruim'
          : 'off';

  protected nova = async () => {
    if (this.criando()) return;
    this.criando.set(true);

    try {
      const nota = await this.service.create();
      this.toast.ok(`Nota ${nota.number} aberta.`);
      await this.router.navigate(['/notas', nota.id]);
    } catch {
      this.criando.set(false);
    }
  };
  protected excluir = async (n: Invoice) => {
    const ok = await this.confirm.ask({
      title: `Excluir a nota ${n.number}?`,
      text: 'A nota sai da listagem. Só é possível excluir nota que ainda não foi fechada.',
      action: 'Excluir nota',
      destructive: true
    });

    if (!ok) return;

    try {
      await this.service.remove(n.id);
      this.toast.ok(`Nota ${n.number} excluída.`);
      this.dados.reload();
    } catch {
      return;
    }
  };
}

export { NotasComponent as Notas };
