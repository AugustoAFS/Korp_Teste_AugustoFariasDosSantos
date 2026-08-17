import '@angular/compiler';
import { TestBed } from '@angular/core/testing';
import { ToastService } from '../../../app/core/services/toast.service';

describe('ToastService', () => {
  let avisos: ToastService;

  beforeEach(() => {
    TestBed.resetTestingModule();
    avisos = TestBed.inject(ToastService);
  });

  it('comeca sem nenhum aviso', () => {
    expect(avisos.toasts()).toEqual([]);
  });

  it('cada tipo de aviso guarda o proprio texto', () => {
    avisos.ok('Produto salvo.');
    avisos.warn('Verifique a quantidade.');
    avisos.bad('Serviço indisponível.');

    expect(avisos.toasts().map(aviso => aviso.kind)).toEqual(['ok', 'warn', 'bad']);
    expect(avisos.toasts()[0].text).toBe('Produto salvo.');
  });

  it('cada aviso recebe um identificador proprio', () => {
    avisos.ok('primeiro');
    avisos.ok('segundo');

    const [um, dois] = avisos.toasts();

    expect(um.id).not.toBe(dois.id);
  });

  it('dispensar remove apenas o aviso pedido', () => {
    avisos.ok('fica');
    avisos.bad('sai');

    const alvo = avisos.toasts().find(aviso => aviso.text === 'sai')!;
    avisos.dismiss(alvo.id);

    expect(avisos.toasts().map(aviso => aviso.text)).toEqual(['fica']);
  });

  it('aviso some sozinho depois do tempo do tipo', () => {
    vi.useFakeTimers();

    avisos.ok('Produto salvo.');
    expect(avisos.toasts()).toHaveLength(1);

    vi.advanceTimersByTime(4000);
    expect(avisos.toasts()).toHaveLength(0);

    vi.useRealTimers();
  });

  it('erro fica mais tempo na tela que um sucesso', () => {
    vi.useFakeTimers();

    avisos.ok('sucesso');
    avisos.bad('erro');

    vi.advanceTimersByTime(4000);

    expect(avisos.toasts().map(aviso => aviso.text)).toEqual(['erro']);

    vi.advanceTimersByTime(3000);
    expect(avisos.toasts()).toHaveLength(0);

    vi.useRealTimers();
  });
});
