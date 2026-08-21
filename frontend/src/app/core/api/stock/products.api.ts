import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URLS } from '../../config/api-base-urls';
import {
  CreateProductRequest,
  CreateProductResponse,
  ListProductsQuery,
  ListProductsResponse,
} from './models';

@Injectable({ providedIn: 'root' })
export class ProductsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URLS).stock;

  list(query: ListProductsQuery): Observable<ListProductsResponse> {
    let params = new HttpParams().set('rows', query.rows);

    if (query.page !== undefined) {
      params = params.set('page', query.page);
    }

    if (query.orderBy !== undefined) {
      params = params.set('orderBy', query.orderBy);
    }

    if (query.asc !== undefined) {
      params = params.set('asc', query.asc);
    }

    const searchTerm = query.searchTerm?.trim();

    if (searchTerm !== undefined && searchTerm.length > 0) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<ListProductsResponse>(`${this.baseUrl}/products`, { params });
  }

  create(request: CreateProductRequest): Observable<CreateProductResponse> {
    return this.http.post<CreateProductResponse>(`${this.baseUrl}/products`, request);
  }
}
