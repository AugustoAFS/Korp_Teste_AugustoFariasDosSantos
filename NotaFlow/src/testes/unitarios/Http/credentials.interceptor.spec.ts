import '@angular/compiler';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { credentialsInterceptor } from '../../../app/core/interceptors/credentials.interceptor';

describe('credentialsInterceptor', () => {
  let http: HttpClient;
  let controle: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([credentialsInterceptor])),
        provideHttpClientTesting()
      ]
    });

    http = TestBed.inject(HttpClient);
    controle = TestBed.inject(HttpTestingController);
  });

  it('toda requisicao leva o cookie de sessao', () => {
    http.get('/api/v1/notas').subscribe();

    expect(controle.expectOne('/api/v1/notas').request.withCredentials).toBe(true);
  });

  it('mutacao tambem leva o cookie', () => {
    http.post('/api/v1/notas', {}).subscribe();

    expect(controle.expectOne('/api/v1/notas').request.withCredentials).toBe(true);
  });

  it('o resto da requisicao nao e alterado', () => {
    http.post('/api/v1/notas/1/itens', { productId: 'abc', quantity: 2 }).subscribe();

    const req = controle.expectOne('/api/v1/notas/1/itens');

    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ productId: 'abc', quantity: 2 });
  });
});
