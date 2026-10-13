import { HttpClient, HttpResourceRef, httpResource } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiResponse, apiUrl } from '../../../core/api/api-response';
import { TimePeriod } from '../time-period';

/**
 * Time periods API. Reads use httpResource (signals); changes (admin only) are plain
 * HttpClient calls, and the page reloads the list after each one.
 */
@Injectable({ providedIn: 'root' })
export class TimePeriodsApi {
  private readonly http = inject(HttpClient);

  /** Must be called from an injection context (e.g. a component field initializer). */
  list(): HttpResourceRef<ApiResponse<TimePeriod[]> | undefined> {
    return httpResource<ApiResponse<TimePeriod[]>>(() => apiUrl('/time-periods'));
  }

  create(timePeriod: TimePeriod): Observable<ApiResponse<TimePeriod>> {
    return this.http.post<ApiResponse<TimePeriod>>(apiUrl('/time-periods'), timePeriod);
  }

  /** Renames and, when `changes.id` differs from `id`, changes the id too. */
  update(id: number, changes: TimePeriod): Observable<ApiResponse<TimePeriod>> {
    return this.http.put<ApiResponse<TimePeriod>>(apiUrl(`/time-periods/${id}`), changes);
  }

  remove(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(apiUrl(`/time-periods/${id}`));
  }
}
