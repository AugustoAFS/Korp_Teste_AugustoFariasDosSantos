import { ChangeDetectionStrategy, Component, effect, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Product } from '../../../core/models/product';

export interface ProdutoFormData {
  code: string;
  description: string;
  balance: number;
  active: boolean;
}

@Component({
  selector: 'app-produto-form',
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './produto-form.component.html',
  styleUrl: './produto-form.component.scss'
})
export class ProdutoFormComponent {
  readonly produto = input<Product | null>(null);
  readonly salvando = input<boolean>(false);

  readonly cancelar = output<void>();
  readonly salvar = output<ProdutoFormData>();

  protected code = '';
  protected description = '';
  protected balance = 0;
  protected active = true;

  constructor() {
    effect(() => {
      const p = this.produto();
      if (p) {
        this.code = p.code;
        this.description = p.description;
        this.balance = p.balance;
        this.active = p.active;
      } else {
        this.code = '';
        this.description = '';
        this.balance = 0;
        this.active = true;
      }
    });
  }

  protected aoSubmeter(): void {
    this.salvar.emit({
      code: this.code,
      description: this.description,
      balance: this.balance,
      active: this.active
    });
  }
}
