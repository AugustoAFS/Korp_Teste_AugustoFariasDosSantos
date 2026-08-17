import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TourStep } from '../models/tour-step';

@Injectable({ providedIn: 'root' })
export class TourService {
  private static readonly StorageKey = 'notaflow.tours-vistos';

  private readonly router = inject(Router);

  private readonly _steps = signal<readonly TourStep[]>([]);
  private readonly _index = signal(0);
  private readonly _tour = signal<string | null>(null);

  readonly running = computed(() => this._tour() !== null);
  readonly step = computed<TourStep | null>(() => this._steps()[this._index()] ?? null);
  readonly index = computed(() => this._index() + 1);
  readonly total = computed(() => this._steps().length);
  readonly first = computed(() => this._index() === 0);
  readonly last = computed(() => this._index() === this._steps().length - 1);

  start = async (tour: string, steps: readonly TourStep[]) => {
    if (steps.length === 0) return;

    this._steps.set(steps);
    this._tour.set(tour);
    await this.ir(0);
  };

  startOnce = async (tour: string, steps: readonly TourStep[]) => {
    if (!this.seen(tour)) await this.start(tour, steps);
  };

  next = async () => (this.last() ? this.finish() : this.ir(this._index() + 1));

  back = async () => {
    if (!this.first()) await this.ir(this._index() - 1);
  };

  skip = () => this.close();

  finish = () => this.close();

  seen = (tour: string) => this.read().includes(tour);

  forget = () => localStorage.removeItem(TourService.StorageKey);

  private ir = async (indice: number) => {
    const destino = this._steps()[indice]?.route;

    if (destino && !this.router.url.startsWith(destino)) {
      try {
        await this.router.navigateByUrl(destino);
      } catch {
        this._index.set(indice);
        return;
      }
    }

    this._index.set(indice);
  };

  private close = () => {
    const tour = this._tour();

    if (tour !== null) this.remember(tour);

    this._tour.set(null);
    this._steps.set([]);
    this._index.set(0);
  };

  private remember = (tour: string) => {
    const seen = this.read();

    if (!seen.includes(tour)) {
      localStorage.setItem(TourService.StorageKey, JSON.stringify([...seen, tour]));
    }
  };

  private read = (): readonly string[] => {
    try {
      const raw = localStorage.getItem(TourService.StorageKey);
      const parsed: unknown = raw === null ? [] : JSON.parse(raw);

      return Array.isArray(parsed) ? parsed.filter(item => typeof item === 'string') : [];
    } catch {
      return [];
    }
  };
}
