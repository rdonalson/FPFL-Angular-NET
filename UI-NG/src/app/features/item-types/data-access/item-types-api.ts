import { HttpResourceRef, httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { ApiResponse, apiUrl } from '../../../core/api/api-response';
import { ItemType } from '../item-type';

/**
 * Item types API. Uses httpResource (signals) instead of the old NgRx actions/effects/reducer:
 * loading, value and error come back as signals, and requests go through the interceptors.
 */
@Injectable({ providedIn: 'root' })
export class ItemTypesApi {
  /** Must be called from an injection context (e.g. a component field initializer). */
  list(): HttpResourceRef<ApiResponse<ItemType[]> | undefined> {
    return httpResource<ApiResponse<ItemType[]>>(() => apiUrl('/item-types'));
  }
}
