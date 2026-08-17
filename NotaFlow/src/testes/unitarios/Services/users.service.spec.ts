import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { UsersService } from '../../../app/core/services/users.service';

describe('UsersService', () => {
  let usuarios: UsersService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    usuarios = TestBed.inject(UsersService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('criar envia nome email senha e perfis', async () => {
    const promessa = usuarios.create({
      name: 'Augusto',
      email: 'augusto@korp.com.br',
      password: 'Senha@123',
      roles: ['Gerente']
    });

    const req = http.expectOne('/api/v1/users');

    expect(req.request.method).toBe('POST');
    expect(req.request.body.email).toBe('augusto@korp.com.br');
    expect(req.request.body.roles).toEqual(['Gerente']);

    req.flush({ id: 1, name: 'Augusto', email: 'augusto@korp.com.br', active: true, roles: ['Gerente'] });
    await promessa;
  });

  it('trocar perfis usa PUT no recurso de papeis do usuario', async () => {
    const promessa = usuarios.replaceRoles(7, ['Administrador', 'Gerente']);
    const req = http.expectOne('/api/v1/users/7/roles');

    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ roles: ['Administrador', 'Gerente'] });

    req.flush(null);
    await promessa;
  });

  it('trocar perfis aceita lista vazia para tirar todos', async () => {
    const promessa = usuarios.replaceRoles(7, []);
    const req = http.expectOne('/api/v1/users/7/roles');

    expect(req.request.body).toEqual({ roles: [] });

    req.flush(null);
    await promessa;
  });

  it('erro de perfil inexistente sobe para a tela tratar', async () => {
    const promessa = usuarios.replaceRoles(7, ['Supervisor']);

    http.expectOne('/api/v1/users/7/roles').flush(
      { detail: 'O perfil Supervisor não existe.' },
      { status: 422, statusText: 'Unprocessable Entity' }
    );

    await expect(promessa).rejects.toBeDefined();
  });
});
