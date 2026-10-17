import { HttpResourceRef, httpResource } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { ApiResponse, apiUrl } from '../../../core/api/api-response';
import { AuthService } from '../../../core/auth/auth';
import { Item } from '../item';

/**
 * Items API for the signed-in user (UI-TSX's ItemClient). Reads use httpResource, so loading,
 * value and error are signals and the list re-fetches when the user or item type changes.
 */
@Injectable({ providedIn: 'root' })
export class ItemApi {
  private readonly auth = inject(AuthService);

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
}
