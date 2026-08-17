import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { TourService } from '../../../app/core/services/tour.service';
import { TOUR_INICIAL } from '../../../app/core/tour-inicial';
import { ErroComponent as Erro } from '../../../app/pages/erro/erro.component';
import { CoachmarkComponent as Coachmark } from '../../../app/design-system/coachmark/coachmark.component';

const rotas = [
  { path: 'painel', component: Erro },
  { path: 'produtos', component: Erro },
  { path: 'notas', component: Erro },
  { path: 'usuarios', component: Erro },
  { path: '**', component: Erro }
];

describe('tour', () => {
  let tour: TourService;
  let router: Router;

  beforeEach(async () => {
    TestBed.resetTestingModule();

    await TestBed.configureTestingModule({
      providers: [provideRouter(rotas), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    tour = TestBed.inject(TourService);
    router = TestBed.inject(Router);
    tour.forget();

    await router.navigateByUrl('/painel');
  });

  it('avanca pelos 9 passos sem se encerrar no meio', async () => {
    await tour.start('teste', TOUR_INICIAL);

    expect(tour.running()).toBe(true);
    expect(tour.index()).toBe(1);

    for (let passo = 2; passo <= TOUR_INICIAL.length; passo++) {
      await tour.next();

      expect(tour.running(), `sumiu no passo ${passo}`).toBe(true);
      expect(tour.index(), `indice errado no passo ${passo}`).toBe(passo);
      expect(tour.step(), `passo ${passo} sem conteudo`).toBeTruthy();
    }

    await tour.next();
    expect(tour.running()).toBe(false);
  });

  it('navega para a rota de cada passo', async () => {
    await tour.start('teste', TOUR_INICIAL);

    for (const passo of TOUR_INICIAL) {
      if (passo.route) {
        expect(router.url, `passo ${passo.id} deveria estar em ${passo.route}`).toBe(passo.route);
      }

      if (!tour.last()) await tour.next();
    }
  });

  it('volta sem sair do tour', async () => {
    await tour.start('teste', TOUR_INICIAL);
    await tour.next();
    await tour.next();

    expect(tour.index()).toBe(3);

    await tour.back();
    expect(tour.running()).toBe(true);
    expect(tour.index()).toBe(2);
  });

  it('o balao continua na tela ao clicar em Proximo', async () => {
    const fixture = TestBed.createComponent(Coachmark);
    fixture.detectChanges();

    await tour.start('teste', TOUR_INICIAL);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const raiz = fixture.nativeElement as HTMLElement;
    expect(raiz.querySelector('.balao'), 'balao nao apareceu').toBeTruthy();

    const proximo = raiz.querySelector<HTMLButtonElement>('.primario')!;
    proximo.click();

    await fixture.whenStable();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(tour.running(), 'tour encerrou ao clicar em Proximo').toBe(true);
    expect(tour.index(), 'nao avancou').toBe(2);
    expect(raiz.querySelector('.balao'), 'balao sumiu apos Proximo').toBeTruthy();
  });

  it('clicar no veu NAO encerra o tour', async () => {
    const fixture = TestBed.createComponent(Coachmark);
    fixture.detectChanges();

    await tour.start('teste', TOUR_INICIAL);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const veu = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.veu')!;
    veu.click();

    await fixture.whenStable();
    fixture.detectChanges();

    expect(tour.running(), 'o veu encerrou o tour').toBe(true);
    expect(tour.index()).toBe(1);
  });

  it('Proximo avanca os 9 passos clicando no botao', async () => {
    const fixture = TestBed.createComponent(Coachmark);
    fixture.detectChanges();

    await tour.start('teste', TOUR_INICIAL);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const raiz = fixture.nativeElement as HTMLElement;

    for (let esperado = 2; esperado <= TOUR_INICIAL.length; esperado++) {
      raiz.querySelector<HTMLButtonElement>('.primario')!.click();

      await fixture.whenStable();
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(tour.running(), `sumiu ao ir para o passo ${esperado}`).toBe(true);
      expect(tour.index(), `nao avancou para o passo ${esperado}`).toBe(esperado);
      expect(raiz.querySelector('.balao'), `balao sumiu no passo ${esperado}`).toBeTruthy();
    }

    raiz.querySelector<HTMLButtonElement>('.primario')!.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(tour.running(), 'Concluir deveria encerrar').toBe(false);
  });
});
