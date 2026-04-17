// src/app/modules/admin/admin.routes.ts
import { Routes } from '@angular/router';
import { adminOnlyGuard } from '../../core/interceptors/jwt.interceptor';

export const ADMIN_ROUTES: Routes = [
  { path: 'users', canActivate: [adminOnlyGuard], loadComponent: () => import('./users/users.component').then(m => m.UsersComponent) },
  { path: 'audit', canActivate: [adminOnlyGuard], loadComponent: () => import('./audit/audit.component').then(m => m.AuditComponent) },
  { path: '', redirectTo: 'users', pathMatch: 'full' }
];
