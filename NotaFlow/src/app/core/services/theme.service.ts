import { Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark' | 'sistema';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private static readonly StorageKey = 'notaflow.tema';

  private readonly _theme = signal<Theme>(this.read());

  readonly theme = this._theme.asReadonly();

  constructor() {
    this.apply(this._theme());
  }

  set(theme: Theme): void {
    this._theme.set(theme);
    this.apply(theme);

    if (theme === 'sistema') localStorage.removeItem(ThemeService.StorageKey);
    else localStorage.setItem(ThemeService.StorageKey, theme);
  }

  private apply(theme: Theme): void {
    const root = document.documentElement;

    if (theme === 'sistema') root.removeAttribute('data-theme');
    else root.setAttribute('data-theme', theme);
  }

  private read(): Theme {
    const saved = localStorage.getItem(ThemeService.StorageKey);

    return saved === 'light' || saved === 'dark' ? saved : 'sistema';
  }
}
