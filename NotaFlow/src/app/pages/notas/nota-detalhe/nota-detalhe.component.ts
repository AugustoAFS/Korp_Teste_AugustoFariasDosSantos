import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { API } from '../../../core/api.const';
import { InterpretedItem, Invoice } from '../../../core/models/invoice';
import { PagedResult } from '../../../core/models/paged-result';
import { Product } from '../../../core/models/product';
import { ConfirmService } from '../../../core/services/confirm.service';
import { InvoicesService } from '../../../core/services/invoices.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-nota-detalhe',
  imports: [DatePipe, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './nota-detalhe.component.html',
  styleUrl: './nota-detalhe.component.scss'
})
export class NotaDetalheComponent {
  readonly id = input.required({ transform: Number });

  private readonly service = inject(InvoicesService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly router = inject(Router);
  private readonly destroy = inject(DestroyRef);

  protected readonly nota = signal<Invoice | null>(null);
  protected readonly ocupado = signal(false);

  protected readonly carga = httpResource<Invoice>(() => `${API}/notas/${this.id()}`);

  protected readonly termo = signal('');
  protected readonly quantidade = signal(1);
  protected readonly escolhido = signal<Product | null>(null);

  protected readonly listaAberta = signal(false);

  protected readonly pedidoIa = signal('');
  protected readonly interpretando = signal(false);
  protected readonly sugestoes = signal<readonly InterpretedItem[]>([]);
  protected readonly naoReconhecidos = signal<readonly string[]>([]);
  protected readonly iaIndisponivel = signal(false);
  protected readonly baixando = signal(false);

  protected readonly catalogo = httpResource<PagedResult<Product>>(() => ({
    url: `${API}/produtos`,
    params: { page: 1, size: 20, ...(this.termo().length >= 2 ? { search: this.termo() } : {}) }
  }));

  protected readonly disponiveis = computed(() => {
    const naNota = new Set((this.atual()?.items ?? []).map(item => item.productId));

    return (this.catalogo.value()?.items ?? []).filter(p => p.active && !naNota.has(p.id));
  });

  protected readonly atual = computed(() => this.nota() ?? this.carga.value() ?? null);
  protected readonly editavel = computed(() => this.atual()?.editable ?? false);
  protected readonly imprimindo = computed(() => this.atual()?.printing ?? false);

  protected readonly estado = computed(() => {
    const n = this.atual();
    if (!n) return { rotulo: '', classe: '' };
    if (n.status === 'Closed') return { rotulo: 'Fechada', classe: 'ok' };
    if (n.printing) return { rotulo: 'Processando', classe: 'andamento' };
    if (n.lastError) return { rotulo: 'Pendente', classe: 'ruim' };
    return { rotulo: 'Aberta', classe: 'off' };
  });

  protected abrirLista = () => this.listaAberta.set(true);

  protected fecharLista = () => setTimeout(() => this.listaAberta.set(false), 120);

  protected digitou = (valor: string) => {
    this.escolhido.set(null);
    this.termo.set(valor);
    this.listaAberta.set(true);
  };

  protected escolher = (p: Product) => {
    this.escolhido.set(p);
    this.termo.set('');
    this.listaAberta.set(false);
  };

  protected baixarPdf = async () => {
    if (this.baixando()) return;

    this.baixando.set(true);

    try {
      const arquivo = await this.service.pdf(this.id());
      const url = URL.createObjectURL(arquivo);
      const link = document.createElement('a');

      link.href = url;
      link.download = `nota-fiscal-${String(this.atual()?.number ?? this.id()).padStart(7, '0')}.pdf`;
      link.click();

      URL.revokeObjectURL(url);
    } finally {
      this.baixando.set(false);
    }
  };

  protected interpretar = async () => {
    const frase = this.pedidoIa().trim();
    if (frase.length < 3 || this.interpretando()) return;

    this.interpretando.set(true);
    this.sugestoes.set([]);
    this.naoReconhecidos.set([]);

    try {
      const resultado = await this.service.interpret(this.id(), frase);

      this.sugestoes.set(resultado.items);
      this.naoReconhecidos.set(resultado.unresolved);

      if (resultado.items.length === 0 && resultado.unresolved.length === 0)
        this.toast.warn('Não identifiquei nenhum produto do catálogo nesse pedido.');
    } catch {
      this.iaIndisponivel.set(true);
    } finally {
      this.interpretando.set(false);
    }
  };

  protected descartarSugestoes = () => {
    this.sugestoes.set([]);
    this.naoReconhecidos.set([]);
    this.pedidoIa.set('');
  };

  protected aceitarSugestao = async (sugestao: InterpretedItem) => {
    if (this.ocupado()) return;

    this.ocupado.set(true);

    try {
      this.nota.set(await this.service.addItem(
        this.id(), { productId: sugestao.productId, quantity: sugestao.quantity }));

      this.sugestoes.update(lista => lista.filter(item => item.productId !== sugestao.productId));
      this.toast.ok(`${sugestao.productCode} adicionado.`);
    } finally {
      this.ocupado.set(false);
    }
  };

  protected adicionar = async () => {
    const produto = this.escolhido();
    if (!produto || this.ocupado()) return;

    this.ocupado.set(true);

    try {
      this.nota.set(await this.service.addItem(this.id(), { productId: produto.id, quantity: this.quantidade() }));
      this.toast.ok(`${produto.code} adicionado.`);
      this.escolhido.set(null);
      this.quantidade.set(1);
    } finally {
      this.ocupado.set(false);
    }
  };

  protected alterar = async (itemId: number, valor: number) => {
    if (valor < 1 || this.ocupado()) return;
    this.ocupado.set(true);

    try {
      this.nota.set(await this.service.updateItem(this.id(), itemId, { quantity: valor }));
    } finally {
      this.ocupado.set(false);
    }
  };

  protected remover = async (itemId: number, codigo: string) => {
    const ok = await this.confirm.ask({
      title: 'Remover item', text: `${codigo} sai desta nota.`, action: 'Remover item', destructive: true
    });

    if (!ok) return;
    this.ocupado.set(true);

    try {
      this.nota.set(await this.service.removeItem(this.id(), itemId));
      this.toast.ok('Item removido.');
    } finally {
      this.ocupado.set(false);
    }
  };

  protected fechar = async () => {
    if (this.ocupado()) return;
    this.ocupado.set(true);

    try {
      this.nota.set(await this.service.print(this.id()));
      this.acompanhar();
    } finally {
      this.ocupado.set(false);
    }
  };

  private acompanhar = () =>
    this.service.track(this.id())
      .pipe(takeUntilDestroyed(this.destroy))
      .subscribe(nota => {
        this.nota.set(nota);

        if (nota.printing) return;

        nota.status === 'Closed'
          ? this.toast.ok(`Nota ${nota.number} fechada. O estoque foi baixado.`)
          : this.toast.bad(nota.lastError ?? 'O fechamento não foi concluído.');
      });

  protected excluirNota = async () => {
    const n = this.atual();
    if (!n || this.ocupado()) return;

    const ok = await this.confirm.ask({
      title: `Excluir a nota ${n.number}?`,
      text: 'A nota e seus itens saem da listagem. Nota já fechada não pode ser excluída.',
      action: 'Excluir nota',
      destructive: true
    });

    if (!ok) return;
    this.ocupado.set(true);

    try {
      await this.service.remove(this.id());
      this.toast.ok(`Nota ${n.number} excluída.`);
      await this.router.navigate(['/notas']);
    } finally {
      this.ocupado.set(false);
    }
  };
}
