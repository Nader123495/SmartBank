// src/app/modules/complaints/complaints.routes.ts
import { Routes } from '@angular/router';
export const COMPLAINTS_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./list/complaints-list.component').then(m => m.ComplaintsListComponent) },
  { path: 'new', loadComponent: () => import('./form/complaint-form.component').then(m => m.ComplaintFormComponent) },
  { path: ':id', loadComponent: () => import('./detail/complaint-detail.component').then(m => m.ComplaintDetailComponent) },
  { path: ':id/edit', loadComponent: () => import('./form/complaint-form.component').then(m => m.ComplaintFormComponent) }
];
