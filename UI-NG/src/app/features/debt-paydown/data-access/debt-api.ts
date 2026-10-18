import { HttpClient, HttpResourceRef, httpResource } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiResponse, apiUrl } from '../../../core/api/api-response';
import { Debt, DebtRequest } from '../debt-paydown.models';

/** Debt Paydown's tracked debts. The API takes the owner from the access token. */
@Injectable({ providedIn: 'root' })
export class DebtApi {
  private readonly http = inject(HttpClient);

  /** The signed-in user's tracked debts. Must be called from an injection context. */
  list(): HttpResourceRef<ApiResponse<Debt[]> | undefined> {
    return httpResource<ApiResponse<Debt[]>>(() => apiUrl('/debts'));
  }

  create(request: DebtRequest): Observable<ApiResponse<Debt>> {
    return this.http.post<ApiResponse<Debt>>(apiUrl('/debts'), request);
  }

  update(id: number, request: DebtRequest): Observable<ApiResponse<Debt>> {
    return this.http.put<ApiResponse<Debt>>(apiUrl(`/debts/${id}`), request);
  }

  remove(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(apiUrl(`/debts/${id}`));
  }
}
