import { HttpClient, HttpResourceRef, httpResource } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiResponse, apiUrl } from '../../../core/api/api-response';
import { ItemType } from '../item-type';

/**
 * Time periods API. Reads use httpResource (signals); changes (admin only) are plain
 * HttpClient calls, and the page reloads the list after each one.
 */
@Injectable({ providedIn: 'root' })
export class ItemTypesApi {
  private readonly http = inject(HttpClient);

  /** Must be called from an injection context (e.g. a component field initializer). */
  list(): HttpResourceRef<ApiResponse<ItemType[]> | undefined> {
    return httpResource<ApiResponse<ItemType[]>>(() => apiUrl('/item-types'));
  }

  create(itemType: ItemType): Observable<ApiResponse<ItemType>> {
    return this.http.post<ApiResponse<ItemType>>(apiUrl('/item-types'), itemType);
  }

  /** Renames and, when `changes.id` differs from `id`, changes the id too. */
  update(id: number, changes: ItemType): Observable<ApiResponse<ItemType>> {
    return this.http.put<ApiResponse<ItemType>>(apiUrl(`/item-types/${id}`), changes);
  }

  remove(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(apiUrl(`/item-types/${id}`));
  }
}
