import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { Footer } from './core/layout/footer/footer';
import { Topbar } from './core/layout/topbar/topbar';

/**
 * Application shell: top bar, routed page content, footer.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Topbar, Footer],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
