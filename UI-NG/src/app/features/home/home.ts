import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { GalleriaModule } from 'primeng/galleria';

import { AuthService } from '../../core/auth/auth';
import { AuthUi } from '../../core/auth/auth-ui';
import { GALLERY_IMAGES, GalleryImage } from './gallery-images';

/**
 * Landing page: auto-cycling photo Galleria beside a summary of what the app does,
 * with sign-in buttons (signed out) or links into the app (signed in).
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
}
