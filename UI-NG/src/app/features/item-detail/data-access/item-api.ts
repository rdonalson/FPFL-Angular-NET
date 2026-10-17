import { HttpClient, HttpResourceRef, httpResource } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiResponse, apiUrl } from '../../../core/api/api-response';
import { AuthService } from '../../../core/auth/auth';
import { Item, ItemRequest } from '../item';

/**
 * Items API for the signed-in user (UI-TSX's ItemClient). Reads use httpResource, so loading,
 * value and error are signals; writes are plain HttpClient calls.
 */
@Injectable({ providedIn: 'root' })
export class ItemApi {
  private readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);

  /**
   * The current user's items of one type (1 = credits, 2 = debits).
   * Must be called from an injection context (e.g. a component field initializer).
   */
  listForCurrentUser(itemType: () => number): HttpResourceRef<ApiResponse<Item[]> | undefined> {
    return httpResource<ApiResponse<Item[]>>(() => {
      const userId = this.auth.session()?.userId;
      return userId ? apiUrl(`/items/${encodeURIComponent(userId)}/${itemType()}`) : undefined;
    });
  }

  /** One item, or no request at all while `id` is undefined (e.g. on the "new" page). */
  getById(id: () => number | undefined): HttpResourceRef<ApiResponse<Item> | undefined> {
    return httpResource<ApiResponse<Item>>(() => {
      const value = id();
      return value ? apiUrl(`/items/${value}`) : undefined;
    });
  }

  create(request: ItemRequest): Observable<ApiResponse<Item>> {
    return this.http.post<ApiResponse<Item>>(apiUrl('/items'), request);
  }

  update(id: number, request: ItemRequest): Observable<ApiResponse<Item>> {
    return this.http.put<ApiResponse<Item>>(apiUrl(`/items/${id}`), request);
  }

  remove(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(apiUrl(`/items/${id}`));
  }
}
