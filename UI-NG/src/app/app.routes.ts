import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth-guard';

/**
 * Top-level routes. Every page is lazy-loaded with loadComponent (no NgModules).
 */
export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'Home | FPFL',
    loadComponent: () => import('./features/home/home').then((m) => m.Home),
  },
  {
    path: 'item-types',
    title: 'Item Types | FPFL',
    canActivate: [authGuard],
    loadComponent: () => import('./features/item-types/item-types').then((m) => m.ItemTypes),
  },
  {
    path: '**',
    title: 'Page not found | FPFL',
    loadComponent: () => import('./core/system/not-found/not-found').then((m) => m.NotFound),
  },
];
