import '@angular/compiler';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TourStep } from '../../../app/core/models/tour-step';
import { TourService } from '../../../app/core/services/tour.service';

const passos: readonly TourStep[] = [
  { id: 'um', title: 'Primeiro', text: 'Começo' },
  { id: 'dois', title: 'Segundo', text: 'Meio' },
  { id: 'tres', title: 'Terceiro', text: 'Fim' }
] as readonly TourStep[];

describe('TourService', () => {
  let tour: TourService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });

    tour = TestBed.inject(TourService);
  });

  it('comeca parado', () => {
    expect(tour.running()).toBe(false);
    expect(tour.step()).toBeNull();
  });

  it('iniciar posiciona no primeiro passo', async () => {
    await tour.start('inicial', passos);

    expect(tour.running()).toBe(true);
    expect(tour.step()?.id).toBe('um');
    expect(tour.index()).toBe(1);
    expect(tour.total()).toBe(3);
    expect(tour.first()).toBe(true);
    expect(tour.last()).toBe(false);
  });

  it('lista vazia nao inicia tour nenhum', async () => {
    await tour.start('inicial', []);

    expect(tour.running()).toBe(false);
  });

  it('proximo avanca um passo por vez', async () => {
    await tour.start('inicial', passos);

    await tour.next();
    expect(tour.step()?.id).toBe('dois');

    await tour.next();
    expect(tour.step()?.id).toBe('tres');
    expect(tour.last()).toBe(true);
  });

  it('proximo no ultimo passo encerra o tour', async () => {
    await tour.start('inicial', passos);
    await tour.next();
    await tour.next();

    await tour.next();

    expect(tour.running()).toBe(false);
  });

  it('voltar retrocede sem passar do primeiro', async () => {
    await tour.start('inicial', passos);
    await tour.next();

    await tour.back();
    expect(tour.step()?.id).toBe('um');

    await tour.back();
    expect(tour.step()?.id).toBe('um');
  });

  it('pular encerra o tour imediatamente', async () => {
    await tour.start('inicial', passos);

    tour.skip();

    expect(tour.running()).toBe(false);
  });

  describe('memoria de tour visto', () => {
    it('tour inedito ainda nao foi visto', () => {
      expect(tour.seen('inicial')).toBe(false);
    });

    it('encerrar marca o tour como visto', async () => {
      await tour.start('inicial', passos);
      tour.skip();

      expect(tour.seen('inicial')).toBe(true);
    });

    it('startOnce nao repete um tour ja visto', async () => {
      await tour.start('inicial', passos);
      tour.skip();

      await tour.startOnce('inicial', passos);

      expect(tour.running()).toBe(false);
    });

    it('startOnce roda um tour inedito', async () => {
      await tour.startOnce('inicial', passos);

      expect(tour.running()).toBe(true);
    });

    it('esquecer permite ver o tour de novo', async () => {
      await tour.start('inicial', passos);
      tour.skip();

      tour.forget();

      expect(tour.seen('inicial')).toBe(false);
    });
  });
});
