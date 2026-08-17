import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API } from '../api.const';
import { CreateProductRequest, Product, UpdateProductRequest } from '../models/product';

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly http = inject(HttpClient);

  create = (req: CreateProductRequest) =>
    firstValueFrom(this.http.post<Product>(`${API}/produtos`, req));

  update = (id: string, req: UpdateProductRequest) =>
    firstValueFrom(this.http.put<Product>(`${API}/produtos/${id}`, req));

  remove = (id: string) =>
    firstValueFrom(this.http.delete<void>(`${API}/produtos/${id}`));
}
