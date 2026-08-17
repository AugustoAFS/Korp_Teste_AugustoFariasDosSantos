import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { API } from '../../core/api.const';
import { PagedResult } from '../../core/models/paged-result';
import { Product } from '../../core/models/product';
import { AuthService } from '../../core/services/auth.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { ProductsService } from '../../core/services/products.service';
import { ToastService } from '../../core/services/toast.service';
import { EmptyStateComponent } from '../../design-system/empty-state/empty-state.component';
import { SkeletonDirective } from '../../design-system/skeleton/skeleton.directive';
import { ProdutoFormComponent, ProdutoFormData } from './produto-form/produto-form.component';

@Component({
  selector: 'app-produtos',
  imports: [FormsModule, EmptyStateComponent, SkeletonDirective, ProdutoFormComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './produtos.component.html'
})
export class ProdutosComponent {
  private readonly service = inject(ProductsService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  protected readonly auth = inject(AuthService);

  protected readonly linhas = computed<readonly (Product | undefined)[]>(() =>
    this.dados.isLoading() ? Array.from({ length: 4 }) : (this.dados.value()?.items ?? [])
  );

  protected readonly pagina = signal(1);
  protected readonly termo = signal('');
  protected readonly busca = signal('');

  protected readonly dados = httpResource<PagedResult<Product>>(() => ({
    url: `${API}/produtos`,
    params: { page: this.pagina(), size: 20, ...(this.busca() ? { search: this.busca() } : {}) }
  }));

  protected readonly aberto = signal(false);
  protected readonly editando = signal<Product | null>(null);
  protected readonly salvando = signal(false);

  protected buscar = () => (this.pagina.set(1), this.busca.set(this.termo()));

  protected novo = () => {
    this.editando.set(null);
    this.aberto.set(true);
  };

  protected editar = (p: Product) => {
    this.editando.set(p);
    this.aberto.set(true);
  };

  protected salvarForm = async (formData: ProdutoFormData) => {
    if (this.salvando()) return;
    this.salvando.set(true);

    const { code, description, balance, active } = formData;
    const alvo = this.editando();

    try {
      alvo
        ? await this.service.update(alvo.id, { code, description, active })
        : await this.service.create({ code, description, balance });

      this.toast.ok(`Produto ${code} ${alvo ? 'atualizado' : 'cadastrado'}.`);
      this.aberto.set(false);
      this.dados.reload();
    } catch {
      this.salvando.set(false);
      return;
    }

    this.salvando.set(false);
  };

  protected excluir = async (p: Product) => {
    const ok = await this.confirm.ask({
      title: 'Excluir produto',
      text: `${p.code} — ${p.description}. Produto com saldo em estoque não pode ser excluído.`,
      action: 'Excluir produto',
      destructive: true
    });

    if (!ok) return;

    try {
      await this.service.remove(p.id);
      this.toast.ok(`Produto ${p.code} excluído.`);
      this.dados.reload();
    } catch {
      return;
    }
  };
}

export { ProdutosComponent as Produtos };
