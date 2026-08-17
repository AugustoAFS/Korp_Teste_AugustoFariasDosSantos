import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API } from '../api.const';
import { CreateUserRequest, User } from '../models/user';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);

  create = (req: CreateUserRequest) => firstValueFrom(this.http.post<User>(`${API}/users`, req));

  replaceRoles = (id: number, roles: readonly string[]) =>
    firstValueFrom(this.http.put<void>(`${API}/users/${id}/roles`, { roles }));
}
