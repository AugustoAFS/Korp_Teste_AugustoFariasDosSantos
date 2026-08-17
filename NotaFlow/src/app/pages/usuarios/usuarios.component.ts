import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { API } from '../../core/api.const';
import { PagedResult } from '../../core/models/paged-result';
import { ROLES, User } from '../../core/models/user';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { UsersService } from '../../core/services/users.service';
import { EmptyStateComponent } from '../../design-system/empty-state/empty-state.component';
import { SkeletonDirective } from '../../design-system/skeleton/skeleton.directive';

@Component({
  selector: 'app-usuarios',
  imports: [FormsModule, EmptyStateComponent, SkeletonDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './usuarios.component.html',
  styleUrl: './usuarios.component.scss'
})
export class UsuariosComponent {
  private readonly service = inject(UsersService);
  private readonly toast = inject(ToastService);

  protected readonly auth = inject(AuthService);
  protected readonly perfis = ROLES;

  protected readonly linhas = computed<readonly (User | undefined)[]>(() =>
    this.dados.isLoading() ? Array.from({ length: 3 }) : (this.dados.value()?.items ?? [])
  );

  protected readonly termo = signal('');
  protected readonly busca = signal('');

  protected readonly dados = httpResource<PagedResult<User>>(() => ({
    url: `${API}/users`,
    params: { page: 1, size: 50, ...(this.busca() ? { search: this.busca() } : {}) }
  }));

  protected readonly aberto = signal(false);
  protected readonly salvando = signal(false);
  protected readonly form = signal({ name: '', email: '', password: '', roles: ['Funcionario'] as string[] });

  protected readonly editando = signal<User | null>(null);
  protected readonly perfisEdicao = signal<string[]>([]);

  protected campo = (chave: 'name' | 'email' | 'password', valor: string) =>
    this.form.update(atual => ({ ...atual, [chave]: valor }));

  protected alternarPerfil = (perfil: string) =>
    this.form.update(atual => ({
      ...atual,
      roles: atual.roles.includes(perfil) ? atual.roles.filter(r => r !== perfil) : [...atual.roles, perfil]
    }));

  protected alternarEdicao = (perfil: string) =>
    this.perfisEdicao.update(atual =>
      atual.includes(perfil) ? atual.filter(r => r !== perfil) : [...atual, perfil]
    );

  protected novo = () => {
    this.form.set({ name: '', email: '', password: '', roles: ['Funcionario'] });
    this.aberto.set(true);
  };

  protected editarPerfis = (u: User) => {
    this.editando.set(u);
    this.perfisEdicao.set([...u.roles]);
  };

  protected criar = async () => {
    if (this.salvando()) return;
    this.salvando.set(true);

    try {
      const criado = await this.service.create(this.form());
      this.toast.ok(`${criado.name} cadastrado.`);
      this.aberto.set(false);
      this.dados.reload();
    } finally {
      this.salvando.set(false);
    }
  };

  protected salvarPerfis = async () => {
    const alvo = this.editando();
    if (!alvo || this.salvando()) return;

    this.salvando.set(true);

    try {
      await this.service.replaceRoles(alvo.id, this.perfisEdicao());
      this.toast.ok(`Perfis de ${alvo.name} atualizados.`);
      this.editando.set(null);
      this.dados.reload();
    } finally {
      this.salvando.set(false);
    }
  };
}

export { UsuariosComponent as Usuarios };
