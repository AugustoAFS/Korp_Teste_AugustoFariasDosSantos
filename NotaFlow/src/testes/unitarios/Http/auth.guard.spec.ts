import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, UrlTree } from '@angular/router';
import { authGuard, managerGuard } from '../../../app/core/guards/auth.guard';
import { AuthService } from '../../../app/core/services/auth.service';

describe('guards de rota', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    });

    http = TestBed.inject(HttpTestingController);
  });

  const autenticar = async (roles: string[]) => {
    const auth = TestBed.inject(AuthService);
    const carga = auth.loadSession();
    http.expectOne('/api/v1/auth/me').flush({ name: 'Augusto', email: 'a@b.c', roles });
    await carga;
  };

  const rodar = (guard: typeof authGuard) =>
    TestBed.runInInjectionContext(() => guard(null!, null!)) as boolean | UrlTree;

  describe('authGuard', () => {
    it('sem sessao redireciona para a tela de entrada', () => {
      const resultado = rodar(authGuard);

      expect(resultado).toBeInstanceOf(UrlTree);
      expect(String(resultado)).toContain('/entrar');
    });

    it('com sessao libera a rota', async () => {
      await autenticar(['Funcionario']);

      expect(rodar(authGuard)).toBe(true);
    });
  });

  describe('managerGuard', () => {
    it('sem sessao redireciona para a entrada e nao para sem-acesso', () => {
      const resultado = rodar(managerGuard);

      expect(String(resultado)).toContain('/entrar');
    });

    it('funcionario autenticado e mandado para sem-acesso', async () => {
      await autenticar(['Funcionario']);

      expect(String(rodar(managerGuard))).toContain('/sem-acesso');
    });

    it('gerente passa', async () => {
      await autenticar(['Gerente']);

      expect(rodar(managerGuard)).toBe(true);
    });

    it('administrador tambem passa', async () => {
      await autenticar(['Administrador']);

      expect(rodar(managerGuard)).toBe(true);
    });
  });
});
