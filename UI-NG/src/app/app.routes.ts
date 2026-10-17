import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth-guard';
import { ITEM_TYPE } from './features/item-detail/item';

/**
 * Top-level routes. Every page is lazy-loaded with loadComponent (no NgModules).
 * Route `data` is bound to component inputs (withComponentInputBinding), e.g. ItemList's itemType.
 */
export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'Home | FPFL',
    loadComponent: () => import('./features/home/home').then((m) => m.Home),
  },
  {
    path: 'credits',
    title: 'Credits | FPFL',
    canActivate: [authGuard],
    data: { itemType: ITEM_TYPE.credit },
    loadComponent: () => import('./features/item-detail/item-list/item-list').then((m) => m.ItemList),
  },
  {
    path: 'debits',
    title: 'Debits | FPFL',
    canActivate: [authGuard],
    data: { itemType: ITEM_TYPE.debit },
    loadComponent: () => import('./features/item-detail/item-list/item-list').then((m) => m.ItemList),
  },
  {
    path: 'item-types',
    title: 'Item Types | FPFL',
    canActivate: [authGuard],
    loadComponent: () => import('./features/item-types/item-types').then((m) => m.ItemTypes),
  },
  {
    path: 'time-periods',
    title: 'Time Periods | FPFL',
    canActivate: [authGuard],
    loadComponent: () => import('./features/time-periods/time-periods').then((m) => m.TimePeriods),
  },
  {
    path: '**',
    title: 'Page not found | FPFL',
    loadComponent: () => import('./core/system/not-found/not-found').then((m) => m.NotFound),
  },
];
