import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Top bar: avatar + app title (links home) and a link to the source on GitHub.
 */
@Component({
  selector: 'app-topbar',
  imports: [RouterLink],
  templateUrl: './topbar.html',
  styleUrl: './topbar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Topbar {
  protected readonly title = 'Financial Planner Forecast Ledger';
  protected readonly repoUrl = 'https://github.com/rdonalson/FPFL-Angular-NET';
}
