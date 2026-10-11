import { ChangeDetectionStrategy, Component } from '@angular/core';
import { GalleriaModule } from 'primeng/galleria';

import { GALLERY_IMAGES, GalleryImage } from './gallery-images';

/**
 * Landing page: auto-cycling photo Galleria beside a summary of what the app does.
 * Same behavior as the old Angular 16 home page, without NgModules, subscriptions or MSAL.
 */
@Component({
  selector: 'app-home',
  imports: [GalleriaModule],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {
  // Galleria's [value] input is a mutable any[], so hand it a copy of the readonly list.
  protected readonly images: GalleryImage[] = [...GALLERY_IMAGES];
  protected readonly guideUrl = 'media/financial-planner-guide.pdf';
}
