import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom, switchMap, takeWhile, timer } from 'rxjs';
import { API } from '../api.const';
import { AddInvoiceItemRequest, Invoice, UpdateInvoiceItemRequest } from '../models/invoice';

@Injectable({ providedIn: 'root' })
export class InvoicesService {
  private readonly http = inject(HttpClient);

  getById = (id: number) => firstValueFrom(this.http.get<Invoice>(`${API}/notas/${id}`));

  create = () => firstValueFrom(this.http.post<Invoice>(`${API}/notas`, {}));

  remove = (id: number) => firstValueFrom(this.http.delete<void>(`${API}/notas/${id}`));

  addItem = (id: number, req: AddInvoiceItemRequest) =>
    firstValueFrom(this.http.post<Invoice>(`${API}/notas/${id}/itens`, req));

  updateItem = (id: number, itemId: number, req: UpdateInvoiceItemRequest) =>
    firstValueFrom(this.http.put<Invoice>(`${API}/notas/${id}/itens/${itemId}`, req));

  removeItem = (id: number, itemId: number) =>
    firstValueFrom(this.http.delete<Invoice>(`${API}/notas/${id}/itens/${itemId}`));

  print = (id: number) => firstValueFrom(this.http.post<Invoice>(`${API}/notas/${id}/impressao`, {}));

  track = (id: number) =>
    timer(1500, 1500).pipe(
      switchMap(() => this.http.get<Invoice>(`${API}/notas/${id}`)),
      takeWhile(nota => nota.printing, true)
    );
}
