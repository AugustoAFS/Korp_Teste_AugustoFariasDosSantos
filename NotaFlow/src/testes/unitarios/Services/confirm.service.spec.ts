import '@angular/compiler';
import { TestBed } from '@angular/core/testing';
import { ConfirmService } from '../../../app/core/services/confirm.service';

const pedido = {
  title: 'Excluir produto',
  text: 'Esta ação não pode ser desfeita.',
  action: 'Excluir',
  destructive: true
};

describe('ConfirmService', () => {
  let confirmar: ConfirmService;

  beforeEach(() => {
    TestBed.resetTestingModule();
    confirmar = TestBed.inject(ConfirmService);
  });

  it('comeca sem nenhum pedido aberto', () => {
    expect(confirmar.request()).toBeNull();
  });

  it('perguntar abre o pedido com o texto para a tela exibir', () => {
    void confirmar.ask(pedido);

    expect(confirmar.request()?.title).toBe('Excluir produto');
    expect(confirmar.request()?.action).toBe('Excluir');
    expect(confirmar.request()?.destructive).toBe(true);
  });

  it('confirmar resolve como verdadeiro e fecha o pedido', async () => {
    const resposta = confirmar.ask(pedido);

    confirmar.answer(true);

    expect(await resposta).toBe(true);
    expect(confirmar.request()).toBeNull();
  });

  it('cancelar resolve como falso e fecha o pedido', async () => {
    const resposta = confirmar.ask(pedido);

    confirmar.answer(false);

    expect(await resposta).toBe(false);
    expect(confirmar.request()).toBeNull();
  });

  it('responder sem pedido aberto nao explode', () => {
    expect(() => confirmar.answer(true)).not.toThrow();
  });

  it('um segundo pedido depois do primeiro resolvido funciona normalmente', async () => {
    const primeiro = confirmar.ask(pedido);
    confirmar.answer(true);
    await primeiro;

    const segundo = confirmar.ask({ ...pedido, title: 'Excluir nota' });
    expect(confirmar.request()?.title).toBe('Excluir nota');

    confirmar.answer(false);
    expect(await segundo).toBe(false);
  });
});
