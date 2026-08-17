import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '../../../app/core/services/auth.service';

const sessao = (roles: string[]) => ({ name: 'Augusto', email: 'augusto@korp.com.br', roles });

describe('AuthService', () => {
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  const carregar = async (roles: string[]) => {
    const promessa = auth.loadSession();
    http.expectOne('/api/v1/auth/me').flush(sessao(roles));
    await promessa;
  };

  it('comeca sem sessao', () => {
    expect(auth.authenticated()).toBe(false);
    expect(auth.user()).toBeNull();
    expect(auth.roles()).toEqual([]);
  });

  it('carrega a sessao do servidor', async () => {
    await carregar(['Funcionario']);

    expect(auth.authenticated()).toBe(true);
    expect(auth.user()?.email).toBe('augusto@korp.com.br');
  });

  it('sessao ausente deixa o usuario deslogado sem lancar erro', async () => {
    const promessa = auth.loadSession();
    http.expectOne('/api/v1/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });
    await promessa;

    expect(auth.authenticated()).toBe(false);
  });

  it('login autentica e em seguida carrega a sessao', async () => {
    const promessa = auth.login('augusto@korp.com.br', 'Senha@123');

    const login = http.expectOne('/api/v1/auth/login');
    expect(login.request.method).toBe('POST');
    expect(login.request.body).toEqual({ email: 'augusto@korp.com.br', password: 'Senha@123' });
    login.flush(null);

    await Promise.resolve();

    http.expectOne('/api/v1/auth/me').flush(sessao(['Funcionario']));
    await promessa;

    expect(auth.authenticated()).toBe(true);
  });

  it('logout limpa a sessao local', async () => {
    await carregar(['Funcionario']);

    const promessa = auth.logout();
    http.expectOne('/api/v1/auth/logout').flush(null);
    await promessa;

    expect(auth.authenticated()).toBe(false);
    expect(auth.user()).toBeNull();
  });

  describe('permissoes derivadas', () => {
    it('funcionario nao e gerente nem administrador', async () => {
      await carregar(['Funcionario']);

      expect(auth.admin()).toBe(false);
      expect(auth.manager()).toBe(false);
    });

    it('gerente tem permissao de gerente mas nao de administrador', async () => {
      await carregar(['Gerente']);

      expect(auth.admin()).toBe(false);
      expect(auth.manager()).toBe(true);
    });

    it('administrador tambem tem a permissao de gerente', async () => {
      await carregar(['Administrador']);

      expect(auth.admin()).toBe(true);
      expect(auth.manager()).toBe(true);
    });

    it('roles expoe os perfis crus para exibicao', async () => {
      await carregar(['Administrador', 'Gerente']);

      expect(auth.roles()).toEqual(['Administrador', 'Gerente']);
    });
  });
});
