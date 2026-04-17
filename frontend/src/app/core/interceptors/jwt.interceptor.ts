// src/app/core/interceptors/jwt.interceptor.ts
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = localStorage.getItem('smartbank_token');
  if (token && !auth.isTokenExpired(token)) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }
  return next(req);
};

/** En cas de 401 (token expiré ou invalide), déconnecte et redirige vers la page de connexion. */
export const unauthorizedInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const url = req.url;
  const isAuthEndpoint = url.includes('/auth/login') || url.includes('/auth/register') || url.includes('/auth/forgot-password') || url.includes('/auth/verify-email') || url.includes('/auth/reset-password') || url.includes('/auth/resend-verification') || url.includes('/users/agencies');
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && !isAuthEndpoint) {
        auth.clearSession();
      }
      return throwError(() => err);
    })
  );
};

// src/app/core/guards/auth.guard.ts
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = localStorage.getItem('smartbank_token');
  if (token && auth.isTokenExpired(token)) {
    auth.clearSession();
    return false;
  }
  if (auth.isAuthenticated()) return true;
  router.navigate(['/auth/login']);
  return false;
};

/** Accès Administration (Responsable ou Admin) : agences, config SLA, etc. */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isResponsable()) return true;
  router.navigate(['/dashboard']);
  return false;
};

/** Accès réservé à l’Admin uniquement (UML : Gérer les utilisateurs, Audit). */
export const adminOnlyGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAdmin()) return true;
  router.navigate(['/dashboard']);
  return false;
};

/** Tableau de bord : Responsable ou Admin uniquement (UML). Agent → redirigé vers réclamations. Client → redirigé vers dashboard client. */
export const dashboardGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isResponsable()) return true;
  if (auth.isClient()) {
    router.navigate(['/client/dashboard']);
    return false;
  }
  router.navigate(['/complaints']);
  return false;
};

/** Empêche le Client d'accéder aux vues internes (réclamations globales). */
export const staffGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isClient()) return true;
  router.navigate(['/client/dashboard']);
  return false;
};
