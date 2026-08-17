import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Invoice } from '../../../app/core/models/invoice';
import { InvoicesService } from '../../../app/core/services/invoices.service';

const nota = (printing: boolean) => ({
  id: 1,
  number: 1042,
  status: printing ? 'Open' : 'Closed',
  issuedByUserName: 'Augusto',
  createdAt: new Date().toISOString(),
  closedAt: null,
  processingId: printing ? 'abc' : null,
  printing,
  editable: !printing,
  lastError: null,
  items: []
}) as unknown as Invoice;

describe('InvoicesService', () => {
  let notas: InvoicesService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    notas = TestBed.inject(InvoicesService);
    http = TestBed.inject(HttpTestingController);
  });

  describe('operacoes da nota', () => {
    it('criar usa POST em /notas', async () => {
      const promessa = notas.create();
      const req = http.expectOne('/api/v1/notas');

      expect(req.request.method).toBe('POST');
      req.flush(nota(false));
      await promessa;
    });

    it('imprimir chama o endpoint de impressao', async () => {
      const promessa = notas.print(1);
      const req = http.expectOne('/api/v1/notas/1/impressao');

      expect(req.request.method).toBe('POST');
      req.flush(nota(true));
      await promessa;
    });

    it('remover item devolve a nota inteira e nao vazio', async () => {
      const promessa = notas.removeItem(1, 9);
      const req = http.expectOne('/api/v1/notas/1/itens/9');

      expect(req.request.method).toBe('DELETE');
      req.flush(nota(false));

      expect((await promessa).number).toBe(1042);
    });

    it('adicionar item envia produto e quantidade', async () => {
      const promessa = notas.addItem(1, { productId: 'abc', quantity: 3 });
      const req = http.expectOne('/api/v1/notas/1/itens');

      expect(req.request.body).toEqual({ productId: 'abc', quantity: 3 });
      req.flush(nota(false));
      await promessa;
    });
  });

  describe('acompanhamento da impressao', () => {
    it('so comeca a perguntar depois do primeiro intervalo', () => {
      vi.useFakeTimers();
      const recebidas: Invoice[] = [];
      const inscricao = notas.track(1).subscribe(valor => recebidas.push(valor));

      http.expectNone('/api/v1/notas/1');

      vi.advanceTimersByTime(1500);
      http.expectOne('/api/v1/notas/1').flush(nota(false));

      expect(recebidas).toHaveLength(1);

      inscricao.unsubscribe();
      vi.useRealTimers();
    });

    it('continua perguntando enquanto a nota estiver imprimindo', () => {
      vi.useFakeTimers();
      const recebidas: Invoice[] = [];
      const inscricao = notas.track(1).subscribe(valor => recebidas.push(valor));

      vi.advanceTimersByTime(1500);
      http.expectOne('/api/v1/notas/1').flush(nota(true));

      vi.advanceTimersByTime(1500);
      http.expectOne('/api/v1/notas/1').flush(nota(true));

      expect(recebidas).toHaveLength(2);

      inscricao.unsubscribe();
      vi.useRealTimers();
    });

    it('para sozinho quando a impressao termina', () => {
      vi.useFakeTimers();
      let completou = false;
      notas.track(1).subscribe({ complete: () => (completou = true) });

      vi.advanceTimersByTime(1500);
      http.expectOne('/api/v1/notas/1').flush(nota(true));

      vi.advanceTimersByTime(1500);
      http.expectOne('/api/v1/notas/1').flush(nota(false));

      expect(completou).toBe(true);

      vi.advanceTimersByTime(3000);
      http.expectNone('/api/v1/notas/1');
      vi.useRealTimers();
    });

    it('emite tambem o valor final para a tela mostrar o desfecho', () => {
      vi.useFakeTimers();
      const recebidas: Invoice[] = [];
      notas.track(1).subscribe(valor => recebidas.push(valor));

      vi.advanceTimersByTime(1500);
      http.expectOne('/api/v1/notas/1').flush(nota(true));

      vi.advanceTimersByTime(1500);
      http.expectOne('/api/v1/notas/1').flush(nota(false));

      expect(recebidas.at(-1)?.printing).toBe(false);
      vi.useRealTimers();
    });
  });
});
