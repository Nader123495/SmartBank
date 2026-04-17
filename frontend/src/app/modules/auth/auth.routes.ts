// src/app/modules/auth/auth.routes.ts
import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  { path: 'login', loadComponent: () => import('./login/login.component').then(m => m.LoginComponent) },
  { path: 'signup', loadComponent: () => import('./signup/signup.component').then(m => m.SignupComponent) },
  { path: 'verify-email', loadComponent: () => import('./verify-email/verify-email.component').then(m => m.VerifyEmailComponent) },
  { path: 'forgot-password', loadComponent: () => import('./forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent) },
  { path: 'google-callback', loadComponent: () => import('./google-callback/google-callback.component').then(m => m.GoogleCallbackComponent) },
  { path: '', redirectTo: 'login', pathMatch: 'full' }
];

export default AUTH_ROUTES;
