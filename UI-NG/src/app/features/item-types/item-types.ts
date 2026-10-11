import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';

import { ApiError, errorMessage } from '../../core/api/api-response';
import { ItemTypesApi } from './data-access/item-types-api';

/**
 * Lists item types from API-NET (GET /item-types).
 */
@Component({
  selector: 'app-item-types',
  imports: [RouterLink, ButtonModule, MessageModule, TableModule],
  templateUrl: './item-types.html',
  styleUrl: './item-types.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ItemTypes {
  protected readonly itemTypes = inject(ItemTypesApi).list();

  protected readonly rows = computed(() => this.itemTypes.value()?.data ?? []);

  protected readonly error = computed(() => {
    const error = this.itemTypes.error();
    if (!error) {
      return null;
    }
    return {
      message: errorMessage(error),
      correlationId: error instanceof ApiError ? error.correlationId : '',
    };
  });
}
