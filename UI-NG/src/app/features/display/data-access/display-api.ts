import { HttpResourceRef, httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { ApiResponse, apiUrl } from '../../../core/api/api-response';
import { LedgerDay, LedgerRequest } from '../display.models';

/** The forecast ledger (UI-TSX's displayApi). */
@Injectable({ providedIn: 'root' })
export class DisplayApi {
  /**
   * POST /display/ledger whenever `request()` changes; nothing is sent while it returns undefined.
   * Must be called from an injection context.
   */
  ledger(request: () => LedgerRequest | undefined): HttpResourceRef<ApiResponse<LedgerDay[]> | undefined> {
    return httpResource<ApiResponse<LedgerDay[]>>(() => {
      const body = request();
      return body ? { url: apiUrl('/display/ledger'), method: 'POST', body } : undefined;
    });
  }
}
