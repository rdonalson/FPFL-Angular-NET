import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { GalleriaModule } from 'primeng/galleria';

import { AuthService } from '../../core/auth/auth';
import { AuthUi } from '../../core/auth/auth-ui';
import { GALLERY_IMAGES, GalleryImage } from './gallery-images';

type Tone = 'brand' | 'credit' | 'debit' | 'neutral';

interface QuickAction {
  label: string;
  description: string;
  icon: string;
  to: string;
  tone: Tone;
}

/** Everyone (signed-in pages; the auth guard opens Login first when signed out). */
const USER_ACTIONS: readonly QuickAction[] = [
  {
    label: 'Display',
    description: 'Chart and ledger for any date range',
    icon: 'pi pi-chart-line',
    to: '/display',
    tone: 'brand',
  },
  {
    label: 'Initial Amount',
    description: 'The balance the forecast starts from',
    icon: 'pi pi-wallet',
    to: '/initial-amount',
    tone: 'brand',
  },
  {
    label: 'Credits',
    description: 'Paychecks and other income',
    icon: 'pi pi-arrow-down-left',
    to: '/credits',
    tone: 'credit',
  },
  {
    label: 'Debits',
    description: 'Bills and recurring expenses',
    icon: 'pi pi-arrow-up-right',
    to: '/debits',
    tone: 'debit',
  },
  {
    label: 'Debt Paydown',
    description: 'Put part of each credit toward a debt',
    icon: 'pi pi-credit-card',
    to: '/debt-paydown',
    tone: 'neutral',
  },
];

/** ROLE_ADMIN only (matches the sidebar's Admin section). */
const ADMIN_ACTIONS: readonly Pick<QuickAction, 'label' | 'icon' | 'to'>[] = [
  { label: 'Manage Item Types', icon: 'pi pi-tags', to: '/item-types' },
  { label: 'Manage Time Periods', icon: 'pi pi-calendar', to: '/time-periods' },
  { label: 'System Status', icon: 'pi pi-server', to: '/status' },
];

/**
 * Landing page ("Polished Indigo"): a hero card with the photo Galleria, quick-action cards,
 * and an Admin row for administrators.
 */
@Component({
  selector: 'app-home',
  imports: [GalleriaModule, ButtonModule, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {
  protected readonly auth = inject(AuthService);
  protected readonly ui = inject(AuthUi);

  // Galleria's [value] input is a mutable any[], so hand it a copy of the readonly list.
  protected readonly images: GalleryImage[] = [...GALLERY_IMAGES];
  protected readonly guideUrl = 'media/fpfl-user-guide.pdf';

  protected readonly userActions = USER_ACTIONS;
  protected readonly adminActions = ADMIN_ACTIONS;

  /** "Good morning, Rick" for signed-in users. */
  protected readonly greeting = computed(() => {
    const hour = new Date().getHours();
    const part = hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening';
    const first = this.auth.session()?.first?.trim();
    return first ? `${part}, ${first}` : part;
  });
}
