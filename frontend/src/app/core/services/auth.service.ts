// src/app/core/services/auth.service.ts
import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap, catchError } from 'rxjs/operators';
import { of, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse, RegisterRequest, RegisterResponse, UserProfile } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API = environment.apiUrl;

  currentUser = signal<UserProfile | null>(null);
  isAuthenticated = signal(false);

  constructor(private http: HttpClient, private router: Router) {
    this.loadFromStorage();
  }

  private loadFromStorage() {
    const user = localStorage.getItem('smartbank_user');
    const token = localStorage.getItem('smartbank_token');
    if (user && token && !this.isTokenExpired(token)) {
      this.currentUser.set(JSON.parse(user));
      this.isAuthenticated.set(true);
    } else if (token && this.isTokenExpired(token)) {
      this.clearSession();
    }
  }

  /** Décode base64url (JWT) en chaîne. */
  private b64UrlDecode(str: string): string {
    let base64 = str.replace(/-/g, '+').replace(/_/g, '/');
    const pad = base64.length % 4;
    if (pad) base64 += '===='.slice(0, 4 - pad);
    return atob(base64);
  }

  /** Vérifie si le JWT est expiré (marge 10 s). */
  isTokenExpired(token: string | null): boolean {
    if (!token) return true;
    try {
      const payload = JSON.parse(this.b64UrlDecode(token.split('.')[1]));
      const exp = payload.exp as number;
      return !exp || exp * 1000 < Date.now() + 10000;
    } catch {
      return true;
    }
  }

  private setSession(res: LoginResponse) {
    localStorage.setItem('smartbank_token', res.accessToken);
    localStorage.setItem('smartbank_refresh', res.refreshToken);
    localStorage.setItem('smartbank_user', JSON.stringify(res.user));
    this.currentUser.set(res.user);
    this.isAuthenticated.set(true);
  }

  /** URL du backend pour lancer le flux OAuth Google (redirection navigateur). */
  getGoogleLoginUrl(): string {
    const base = this.API.replace(/\/api\/?$/, '');
    return `${base}/api/auth/google`;
  }

  /** Enregistre la session après retour du callback Google (token, refresh, user dans le hash). */
  setSessionFromGoogle(params: { token: string; refresh: string; user: string }): void {
    const user = JSON.parse(decodeURIComponent(params.user)) as UserProfile;
    localStorage.setItem('smartbank_token', params.token);
    localStorage.setItem('smartbank_refresh', params.refresh);
    localStorage.setItem('smartbank_user', JSON.stringify(user));
    this.currentUser.set(user);
    this.isAuthenticated.set(true);
  }

  login(dto: LoginRequest) {
    return this.http.post<LoginResponse>(`${this.API}/auth/login`, dto).pipe(
      tap(res => this.setSession(res)),
      catchError(err => {
        // Propagation des erreurs structurées pour que le composant puisse afficher le bon message
        return throwError(() => err);
      })
    );
  }

  register(dto: RegisterRequest) {
    return this.http.post<RegisterResponse>(`${this.API}/auth/register`, dto);
  }

  verifyEmail(email: string, code: string) {
    return this.http.post<LoginResponse>(`${this.API}/auth/verify-email`, { email, code }).pipe(
      tap(res => this.setSession(res))
    );
  }

  resendVerification(email: string) {
    return this.http.post<{ message: string }>(`${this.API}/auth/resend-verification`, { email });
  }

  forgotPassword(email: string) {
    return this.http.post<{ message: string }>(`${this.API}/auth/forgot-password`, { email });
  }

  resetPassword(email: string, code: string, newPassword: string) {
    return this.http.post<{ message: string }>(`${this.API}/auth/reset-password`, { email, code, newPassword });
  }

  getProfile() {
    return this.http.get<UserProfile>(`${this.API}/auth/profile`);
  }

  updateProfile(data: { 
    firstName?: string; 
    lastName?: string; 
    fullName?: string; 
    avatarUrl?: string; 
    gender?: string;
    phoneNumber?: string;
    accountNumber?: string;
  }) {
    return this.http.put<UserProfile>(`${this.API}/auth/profile`, data).pipe(
      tap(profile => {
        this.currentUser.set(profile);
        localStorage.setItem('smartbank_user', JSON.stringify(profile));
      })
    );
  }

  changePassword(currentPassword: string, newPassword: string) {
    return this.http.post<{ message: string }>(`${this.API}/auth/change-password`, {
      currentPassword,
      newPassword
    });
  }

  /** Déconnecte côté serveur puis vide la session locale. */
  logout() {
    this.http.post(`${this.API}/auth/logout`, {}).pipe(
      catchError(() => of(null))
    ).subscribe(() => {
      this.clearSession();
    });
  }

  /** Vide la session locale sans appeler le backend (ex. après 401). */
  clearSession() {
    localStorage.removeItem('smartbank_token');
    localStorage.removeItem('smartbank_refresh');
    localStorage.removeItem('smartbank_user');
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    this.router.navigate(['/auth/login']);
  }

  getToken(): string | null {
    return localStorage.getItem('smartbank_token');
  }

  hasRole(role: string): boolean {
    return this.currentUser()?.role === role;
  }

  hasPermission(perm: string): boolean {
    const perms = this.currentUser()?.permissions ?? [];
    return perms.includes('all') || perms.includes(perm);
  }

  /** Administrateur : pilotage complet (dashboard, assignation, zone admin, utilisateurs, audit). */
  isAdmin = () => this.hasRole('Admin');

  /** Responsable (Chef d'agence) : gestion d'agence et statistiques. */
  isResponsable = () => this.hasRole('Admin') || this.hasRole('Responsable');

  /** Agent : traitement de réclamations. */
  isAgent = () => this.hasRole('Agent');

  /** Client : accès portail client. */
  isClient = () => this.hasRole('Client');
}
