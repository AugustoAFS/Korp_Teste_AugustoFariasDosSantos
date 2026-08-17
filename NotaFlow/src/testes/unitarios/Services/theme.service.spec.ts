import '@angular/compiler';
import { TestBed } from '@angular/core/testing';
import { ThemeService } from '../../../app/core/services/theme.service';

const Chave = 'notaflow.tema';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    TestBed.resetTestingModule();
  });

  const criar = () => TestBed.inject(ThemeService);

  it('sem preferencia salva comeca no tema do sistema', () => {
    expect(criar().theme()).toBe('sistema');
  });

  it('tema do sistema nao marca o atributo e deixa o css decidir', () => {
    criar();

    expect(document.documentElement.hasAttribute('data-theme')).toBe(false);
  });

  it('escolher claro marca o atributo e persiste', () => {
    const tema = criar();

    tema.set('light');

    expect(tema.theme()).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(localStorage.getItem(Chave)).toBe('light');
  });

  it('escolher escuro marca o atributo e persiste', () => {
    const tema = criar();

    tema.set('dark');

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem(Chave)).toBe('dark');
  });

  it('voltar para sistema limpa a preferencia e o atributo', () => {
    const tema = criar();
    tema.set('dark');

    tema.set('sistema');

    expect(document.documentElement.hasAttribute('data-theme')).toBe(false);
    expect(localStorage.getItem(Chave)).toBeNull();
  });

  it('preferencia salva e restaurada no proximo carregamento', () => {
    localStorage.setItem(Chave, 'dark');

    expect(criar().theme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('valor invalido no armazenamento cai para o sistema', () => {
    localStorage.setItem(Chave, 'roxo');

    expect(criar().theme()).toBe('sistema');
  });
});
