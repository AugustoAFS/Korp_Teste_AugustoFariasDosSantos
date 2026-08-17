import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Type } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppComponent as App } from '../../../app/app.component';
import { AuthService } from '../../../app/core/services/auth.service';
import { TOUR_INICIAL } from '../../../app/core/tour-inicial';
import { LoginComponent as Login } from '../../../app/pages/auth/login/login.component';
import { ErroComponent as Erro } from '../../../app/pages/erro/erro.component';
import { NotaDetalheComponent as NotaDetalhe } from '../../../app/pages/notas/nota-detalhe/nota-detalhe.component';
import { NotasComponent as Notas } from '../../../app/pages/notas/notas.component';
import { PainelComponent as Painel } from '../../../app/pages/painel/painel.component';
import { ProdutosComponent as Produtos } from '../../../app/pages/produtos/produtos.component';
import { UsuariosComponent as Usuarios } from '../../../app/pages/usuarios/usuarios.component';
import { LayoutComponent } from '../../../app/layout/layout.component';
import { routes } from '../../../app/app.routes';

const vazio = { items: [], page: 1, size: 20, total: 0, totalPages: 0 };

const nota = {
  id: 1, number: 1042, status: 'Open', issuedByUserName: 'Augusto',
  createdAt: new Date().toISOString(), closedAt: null, processingId: null,
  printing: false, editable: true, lastError: null,
  items: [{ id: 1, productId: 'a', productCode: 'P-1', productDescription: 'Caneta', quantity: 2 }]
};

const admin = { name: 'Administrador', email: 'admin@admin.com', roles: ['Administrador'] };

const http = () => TestBed.inject(HttpTestingController);

const montar = async <T>(tipo: Type<T>, entradas: Record<string, unknown> = {}, sessao = false) => {
  TestBed.resetTestingModule();

  await TestBed.configureTestingModule({
    imports: [tipo],
    providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()]
  }).compileComponents();

  if (sessao) {
    const carga = TestBed.inject(AuthService).loadSession();
    http().expectOne('/api/v1/auth/me').flush(admin);
    await carga;
  }

  const fixture: ComponentFixture<T> = TestBed.createComponent(tipo);

  for (const [chave, valor] of Object.entries(entradas)) fixture.componentRef.setInput(chave, valor);

  fixture.detectChanges();
  await Promise.resolve();
  fixture.detectChanges();
  return fixture;
};

const responder = async (fixture: ComponentFixture<unknown>, corpo: object) => {
  for (const req of http().match(() => true)) req.flush(corpo);

  await Promise.resolve();
  fixture.detectChanges();
  await Promise.resolve();
  fixture.detectChanges();
};

const texto = (fixture: ComponentFixture<unknown>) =>
  (fixture.nativeElement as HTMLElement).textContent ?? '';

describe('telas', () => {
  it('login renderiza as credenciais de teste', async () => {
    expect(texto(await montar(Login))).toContain('admin@admin.com');
  });

  it('layout renderiza a navegacao', async () => {
    const f = await montar(LayoutComponent);
    expect((f.nativeElement as HTMLElement).querySelector('nav')).toBeTruthy();
  });

  it('produtos mostra o estado vazio quando nao ha catalogo', async () => {
    const f = await montar(Produtos);
    await responder(f, vazio);
    expect(texto(f)).toContain('Nenhum produto ainda');
  });

  it('notas mostra o estado vazio quando nao ha notas', async () => {
    const f = await montar(Notas);
    await responder(f, vazio);
    expect(texto(f)).toContain('Nenhuma nota ainda');
  });

  it('detalhe da nota lista os itens', async () => {
    const f = await montar(NotaDetalhe, { id: 1 });
    await responder(f, nota);
    expect(texto(f)).toContain('1042');
    expect(texto(f)).toContain('P-1');
  });

  it('painel agrega produtos e notas', async () => {
    const f = await montar(Painel);
    await responder(f, vazio);
    expect(texto(f)).toContain('Últimas notas');
  });

  it('toda ancora declarada no tour existe na tela correspondente', async () => {
    const telas: Record<string, Type<unknown>> = {
      '/painel': Painel,
      '/produtos': Produtos,
      '/notas': Notas,
      '/usuarios': Usuarios
    };

    const doLayout = ['#nav-painel', '#nav-caixa', '#nav-documento', '#nav-pessoa'];

    const layout = await montar(LayoutComponent, {}, true);

    for (const passo of TOUR_INICIAL) {
      if (!passo.anchor) continue;

      if (doLayout.includes(passo.anchor)) {
        expect(
          (layout.nativeElement as HTMLElement).querySelector(passo.anchor),
          `${passo.anchor} (passo "${passo.id}") sumiu do layout`
        ).toBeTruthy();
        continue;
      }

      const tela = passo.route ? telas[passo.route] : undefined;
      if (!tela) continue;

      const f = await montar(tela, {}, true);
      await responder(f, vazio);

      expect(
        (f.nativeElement as HTMLElement).querySelector(passo.anchor),
        `${passo.anchor} (passo "${passo.id}") sumiu de ${passo.route}`
      ).toBeTruthy();
    }
  });
});
