// src/app/app.routes.ts
import { Routes } from '@angular/router';
import { authGuard, adminGuard, adminOnlyGuard, dashboardGuard, staffGuard } from './core/interceptors/jwt.interceptor';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./modules/landing/landing.component').then(m => m.LandingComponent) },
  {
    path: 'depot',
    canActivate: [authGuard],
    loadComponent: () => import('./modules/client/depot/depot.component').then(m => m.DepotComponent)
  },
  {
    path: 'suivi',
    canActivate: [authGuard],
    loadComponent: () => import('./modules/client/suivi/suivi.component').then(m => m.SuiviComponent)
  },
  {
    path: 'auth',
    loadChildren: () => import('./modules/auth/auth.routes').then(m => m.default || m.AUTH_ROUTES)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard, dashboardGuard],
    loadChildren: () => import('./modules/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES)
  },
  {
    path: 'complaints',
    canActivate: [authGuard, staffGuard],
    children: [
      { path: '', loadComponent: () => import('./modules/complaints/list/complaints-list.component').then(m => m.ComplaintsListComponent) },
      { path: 'new', loadComponent: () => import('./modules/complaints/form/complaint-form.component').then(m => m.ComplaintFormComponent) },
      { path: ':id', loadComponent: () => import('./modules/complaints/detail/complaint-detail.component').then(m => m.ComplaintDetailComponent) },
      { path: ':id/edit', loadComponent: () => import('./modules/complaints/form/complaint-form.component').then(m => m.ComplaintFormComponent) }
    ]
  },
  {
    path: 'client/dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./modules/client/dashboard/dashboard.component').then(m => m.ClientDashboardComponent)
  },
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    loadChildren: () => import('./modules/admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('./modules/profile/profile.component').then(m => m.ProfileComponent)
  },
  { path: '**', redirectTo: '/auth/login' }
];
