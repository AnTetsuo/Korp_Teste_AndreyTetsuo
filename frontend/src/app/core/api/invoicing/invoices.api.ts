import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URLS } from '../../config/api-base-urls';
import { ListInvoicesQuery, ListInvoicesResponse } from './models';

@Injectable({ providedIn: 'root' })
export class InvoicesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URLS).invoicing;

  list(query: ListInvoicesQuery): Observable<ListInvoicesResponse> {
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

    if (query.status !== undefined) {
      params = params.set('status', query.status);
    }

    if (query.number !== undefined) {
      params = params.set('number', query.number);
    }

    return this.http.get<ListInvoicesResponse>(`${this.baseUrl}/invoices`, { params });
  }
}
