import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API } from '../api.const';
import { Session } from '../models/user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly _user = signal<Session | null>(null);

  readonly user = this._user.asReadonly();
  readonly authenticated = computed(() => this._user() !== null);
  readonly roles = computed(() => this._user()?.roles ?? []);
  readonly admin = computed(() => this.roles().includes('Administrador'));
  readonly manager = computed(() => this.admin() || this.roles().includes('Gerente'));

  async login(email: string, password: string): Promise<void> {
    await firstValueFrom(this.http.post(`${API}/auth/login`, { email, password }));
    await this.loadSession();
  }

  async logout(): Promise<void> {
    await firstValueFrom(this.http.post(`${API}/auth/logout`, {}));
    this._user.set(null);
  }

  async loadSession(): Promise<void> {
    try {
      const session = await firstValueFrom(this.http.get<Session>(`${API}/auth/me`));
      this._user.set(session);
    } catch {
      this._user.set(null);
    }
  }
}
