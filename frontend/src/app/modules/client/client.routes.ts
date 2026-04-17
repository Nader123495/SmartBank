import { Routes } from '@angular/router';

export const CLIENT_ROUTES: Routes = [
  { path: 'depot', loadComponent: () => import('./depot/depot.component').then(m => m.DepotComponent) },
  { path: 'suivi', loadComponent: () => import('./suivi/suivi.component').then(m => m.SuiviComponent) }
];
