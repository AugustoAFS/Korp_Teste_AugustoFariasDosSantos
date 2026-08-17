import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ProductsService } from '../../../app/core/services/products.service';

const produto = {
  id: 'abc',
  code: 'PAR-M8',
  description: 'Parafuso sextavado M8',
  balance: 10,
  active: true
};

describe('ProductsService', () => {
  let produtos: ProductsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    produtos = TestBed.inject(ProductsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('criar envia codigo descricao e saldo inicial', async () => {
    const promessa = produtos.create({ code: 'PAR-M8', description: 'Parafuso sextavado M8', balance: 10 });
    const req = http.expectOne('/api/v1/produtos');

    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      code: 'PAR-M8',
      description: 'Parafuso sextavado M8',
      balance: 10
    });

    req.flush(produto);
    expect((await promessa).code).toBe('PAR-M8');
  });

  it('atualizar usa PUT no produto pelo id', async () => {
    const promessa = produtos.update('abc', {
      code: 'PAR-M10',
      description: 'Parafuso M10',
      active: false
    });

    const req = http.expectOne('/api/v1/produtos/abc');

    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ code: 'PAR-M10', description: 'Parafuso M10', active: false });

    req.flush({ ...produto, code: 'PAR-M10', active: false });
    expect((await promessa).active).toBe(false);
  });

  it('atualizar nao envia saldo porque quem manda no estoque e o servidor', async () => {
    const promessa = produtos.update('abc', {
      code: 'PAR-M8',
      description: 'Parafuso',
      active: true
    });

    const req = http.expectOne('/api/v1/produtos/abc');

    expect(req.request.body).not.toHaveProperty('balance');

    req.flush(produto);
    await promessa;
  });

  it('excluir usa DELETE no produto pelo id', async () => {
    const promessa = produtos.remove('abc');
    const req = http.expectOne('/api/v1/produtos/abc');

    expect(req.request.method).toBe('DELETE');

    req.flush(null);
    await promessa;
  });

  it('erro do servidor sobe para a tela tratar', async () => {
    const promessa = produtos.remove('abc');

    http.expectOne('/api/v1/produtos/abc').flush(
      { detail: 'Zere o saldo do produto antes de excluí-lo.' },
      { status: 422, statusText: 'Unprocessable Entity' }
    );

    await expect(promessa).rejects.toBeDefined();
  });
});
