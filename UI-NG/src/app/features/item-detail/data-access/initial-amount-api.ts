import { HttpClient, HttpResourceRef, httpResource } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiResponse, apiUrl } from '../../../core/api/api-response';
import { AuthService } from '../../../core/auth/auth';
import { InitialAmount } from '../item';

/** The signed-in user's starting balance (UI-TSX's initialAmountApi). */
@Injectable({ providedIn: 'root' })
export class InitialAmountApi {
  private readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);

  /**
   * The current user's initial amount. The API creates $0.00 the first time, so this never 404s.
   * Must be called from an injection context.
   */
  current(): HttpResourceRef<ApiResponse<InitialAmount> | undefined> {
    return httpResource<ApiResponse<InitialAmount>>(() => {
      const userId = this.auth.session()?.userId;
      return userId ? apiUrl(`/initial-amount/${encodeURIComponent(userId)}`) : undefined;
    });
  }

  update(id: number, amount: number): Observable<ApiResponse<InitialAmount>> {
    return this.http.put<ApiResponse<InitialAmount>>(apiUrl(`/initial-amount/${id}`), { amount });
  }
}
