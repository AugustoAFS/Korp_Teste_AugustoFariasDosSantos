import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LogoComponent } from '../../../design-system/logo/logo.component';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, LogoComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly email = signal('');
  protected readonly senha = signal('');
  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected preencher(): void {
    this.email.set('admin@admin.com');
    this.senha.set('Admin123!');
    void this.entrar();
  }

  protected async entrar(): Promise<void> {
    if (this.enviando()) return;

    this.enviando.set(true);
    this.erro.set(null);

    try {
      await this.auth.login(this.email(), this.senha());
      this.toast.ok(`Bem-vindo, ${this.auth.user()?.name ?? ''}.`);
      await this.router.navigate(['/painel']);
    } catch {
      this.erro.set('E-mail ou senha incorretos.');
    } finally {
      this.enviando.set(false);
    }
  }
}

export { LoginComponent as Login };
