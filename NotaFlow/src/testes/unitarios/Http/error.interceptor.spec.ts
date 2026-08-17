import '@angular/compiler';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { errorInterceptor } from '../../../app/core/interceptors/error.interceptor';
import { ToastService } from '../../../app/core/services/toast.service';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let controle: HttpTestingController;
  let avisos: ToastService;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting()
      ]
    });

    http = TestBed.inject(HttpClient);
    controle = TestBed.inject(HttpTestingController);
    avisos = TestBed.inject(ToastService);
  });

  const falhar = async (status: number, corpo: object | null = null, url = '/api/v1/notas') => {
    const promessa = new Promise<void>(resolve => {
      http.get(url).subscribe({ error: () => resolve(), next: () => resolve() });
    });

    controle.expectOne(url).flush(corpo, { status, statusText: 'erro' });
    await promessa;
  };

  const ultimo = () => avisos.toasts().at(-1);

  it('sem conexao avisa que o servidor esta fora', async () => {
    await falhar(0);

    expect(ultimo()?.kind).toBe('bad');
    expect(ultimo()?.text).toContain('Sem conexão');
  });

  it('401 fora do login manda o usuario para a tela de entrada', async () => {
    const router = TestBed.inject(Router);
    const navegou = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    await falhar(401);

    expect(ultimo()?.text).toContain('sessão expirou');
    expect(navegou).toHaveBeenCalledWith(['/entrar']);
  });

  it('401 no proprio login nao avisa sessao expirada', async () => {
    await falhar(401, null, '/api/v1/auth/login');

    expect(avisos.toasts()).toHaveLength(0);
  });

  it('403 mostra o detalhe do servidor', async () => {
    await falhar(403, { detail: 'Seu perfil não permite esta ação.' });

    expect(ultimo()?.kind).toBe('bad');
    expect(ultimo()?.text).toBe('Seu perfil não permite esta ação.');
  });

  it('503 explica que a nota continua aberta', async () => {
    await falhar(503, { detail: 'Serviço indisponível. Sua nota continua aberta.' });

    expect(ultimo()?.text).toContain('continua aberta');
  });

  it('429 avisa para aguardar', async () => {
    await falhar(429, { detail: 'Muitas requisições. Aguarde alguns instantes.' });

    expect(ultimo()?.kind).toBe('warn');
  });

  it('500 mostra o traceId para o usuario relatar', async () => {
    await falhar(500, { traceId: '00-abc-123' });

    expect(ultimo()?.text).toContain('00-abc-123');
  });

  it('erro de negocio mostra o detalhe como aviso', async () => {
    await falhar(409, { detail: 'Esta nota já foi impressa.' });

    expect(ultimo()?.kind).toBe('warn');
    expect(ultimo()?.text).toBe('Esta nota já foi impressa.');
  });

  it('erro sem detalhe conhecido nao inventa mensagem', async () => {
    await falhar(418);

    expect(avisos.toasts()).toHaveLength(0);
  });

  it('o erro continua subindo para quem chamou tratar', async () => {
    let capturou = false;

    const promessa = new Promise<void>(resolve => {
      http.get('/api/v1/notas').subscribe({
        error: () => {
          capturou = true;
          resolve();
        }
      });
    });

    controle.expectOne('/api/v1/notas').flush(null, { status: 500, statusText: 'erro' });
    await promessa;

    expect(capturou).toBe(true);
  });
});
