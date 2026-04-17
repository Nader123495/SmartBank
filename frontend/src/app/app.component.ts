// src/app/app.component.ts
import { Component, computed, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd, NavigationStart } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from './core/services/auth.service';
import { ThemeService } from './core/services/theme.service';
import { AiAssistantComponent } from './modules/ai/ai-assistant.component';
import { AiContextService } from './core/services/ai-context.service';
import { NotificationService } from './core/services/complaint.service';
import type { Notification } from './core/models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule, AiAssistantComponent],
  template: `
    @if (showLayout()) {
      <aside class="sidebar">
        <a routerLink="/dashboard" class="sb-brand">
          <img src="assets/images/smartbank-logo.png" alt="Smart Bank" class="sb-logo-img" />
          <span class="sb-subtitle">Gestion des réclamations</span>
        </a>

        <nav class="sb-nav">
          <div class="sb-section">Navigation</div>
          @if (auth.isResponsable()) {
            <a routerLink="/dashboard" routerLinkActive="active" class="sb-link">
              <span class="ic">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="7" height="9" x="3" y="3" rx="1"/><rect width="7" height="5" x="14" y="3" rx="1"/><rect width="7" height="9" x="14" y="12" rx="1"/><rect width="7" height="5" x="3" y="16" rx="1"/></svg>
              </span>
              Tableau de bord
            </a>
          }
          <a routerLink="/complaints" routerLinkActive="active" [routerLinkActiveOptions]="{exact:false}" class="sb-link">
            <span class="ic">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/><rect x="8" y="2" width="8" height="4" rx="1" ry="1"/><path d="M9 14h6"/><path d="M9 18h6"/><path d="M9 10h6"/></svg>
            </span>
            Réclamations
          </a>
          <a routerLink="/complaints/new" routerLinkActive="active" class="sb-link">
            <span class="ic">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><line x1="12" y1="8" x2="12" y2="16"/><line x1="8" y1="12" x2="16" y2="12"/></svg>
            </span>
            Nouvelle réclamation
          </a>
          @if (auth.isResponsable()) {
            <div class="sb-section">Administration</div>
            @if (auth.isAdmin()) {
              <a routerLink="/admin/users" routerLinkActive="active" class="sb-link">
                <span class="ic">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>
                </span>
                Utilisateurs
              </a>
            }
            <a routerLink="/admin/agencies" routerLinkActive="active" class="sb-link">
              <span class="ic">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="3" y1="21" x2="21" y2="21"/><line x1="3" y1="7" x2="21" y2="7"/><path d="M4 7v14"/><path d="M9 7v14"/><path d="M15 7v14"/><path d="M20 7v14"/><path d="M10.22 2.72a2 2 0 0 1 3.56 0L19 7H5l5.22-4.28z"/></svg>
              </span>
              Agences
            </a>
            <a routerLink="/admin/sla" routerLinkActive="active" class="sb-link">
              <span class="ic">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
              </span>
              Configuration SLA
            </a>
          }

          @if (auth.isAdmin()) {
            <a routerLink="/admin/audit" routerLinkActive="active" class="sb-link">
              <span class="ic">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
              </span>
              Journal d'audit
            </a>
          }
        </nav>

        <div class="sb-foot">
          @if (auth.currentUser()) {
            <a routerLink="/profile" class="sb-user">
              <img 
                [src]="auth.currentUser()!.avatarUrl || getFallbackAvatar(auth.currentUser()!.role)" 
                [alt]="auth.currentUser()!.fullName" 
                class="sb-avatar-img" 
              />
              <div>
                <div class="sb-uname">{{ auth.currentUser()!.fullName }}</div>
                <div class="sb-urole">{{ auth.currentUser()!.role }}</div>
              </div>
            </a>
          }
          <button type="button" class="sb-logout" (click)="auth.logout()">
            <svg class="ic-logout" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="width: 16px; height: 16px;"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>
            Déconnexion
          </button>
        </div>
      </aside>


      <div class="main-wrap">
        <header class="topbar">
          <div class="topbar-brand">
            <span class="topbar-title"><strong>Tableau de bord</strong> — Système de gestion des réclamations</span>
          </div>
          <div class="topbar-actions">
            <button
              type="button"
              class="topbar-icon-btn theme-toggle"
              [attr.aria-label]="theme.isDark() ? 'Passer au mode clair' : 'Passer au mode sombre'"
              (click)="theme.toggle()"
              [title]="theme.isDark() ? 'Mode clair' : 'Mode sombre'"
            >
              @if (theme.isDark()) {
                <svg class="topbar-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>
              } @else {
                <svg class="topbar-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
              }
            </button>
            <button
              type="button"
              class="topbar-icon-btn notif-btn notif-btn-chat"
              aria-label="Assistant IA"
              (click)="aiContext.openPanel('chat')"
            >
              <svg class="topbar-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                <path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z"/>
              </svg>
            </button>
            <div class="notif-wrap">
              <button type="button" class="topbar-icon-btn notif-btn" aria-label="Notifications" (click)="toggleNotifPanel()">
                <svg class="topbar-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/></svg>
                @if (unreadCount() > 0) {
                  <span class="notif-badge">{{ unreadCount() > 99 ? '99+' : unreadCount() }}</span>
                }
              </button>
              @if (notifPanelOpen()) {
                <div class="notif-panel" (click)="$event.stopPropagation()">
                  <div class="notif-panel-header">
                    <span>Notifications</span>
                    <button type="button" class="notif-panel-close" aria-label="Fermer" (click)="notifPanelOpen.set(false)">×</button>
                  </div>
                  <div class="notif-panel-body">
                    @if (notifLoading()) {
                      <p class="notif-empty">Chargement…</p>
                    } @else if (notifError()) {
                      <p class="notif-error">{{ notifError() }}</p>
                      <button type="button" class="btn btn-sm btn-outline" (click)="loadNotifications()">Réessayer</button>
                    } @else if (notifications().length === 0) {
                      <p class="notif-empty">Aucune notification.</p>
                    } @else {
                      @for (n of notifications(); track n.id) {
                        @if (n.complaintId) {
                          <a [routerLink]="['/complaints', n.complaintId]" class="notif-item" [class.read]="n.isRead"
                            (click)="markNotifRead(n); notifPanelOpen.set(false)">
                            <div class="notif-item-title">{{ n.title }}</div>
                            <div class="notif-item-msg">{{ n.message }}</div>
                            <div class="notif-item-time">{{ n.createdAt | date:'dd/MM/yyyy HH:mm' }}</div>
                          </a>
                        } @else {
                          <div class="notif-item" [class.read]="n.isRead" (click)="markNotifRead(n)">
                            <div class="notif-item-title">{{ n.title }}</div>
                            <div class="notif-item-msg">{{ n.message }}</div>
                            <div class="notif-item-time">{{ n.createdAt | date:'dd/MM/yyyy HH:mm' }}</div>
                          </div>
                        }
                      }
                    }
                  </div>
                </div>
              }
            </div>
          </div>
        </header>
        <div class="page-body">
          <router-outlet />
        </div>
      </div>
      <app-ai-assistant />
    } @else {
      <router-outlet />
    }
  `
})
export class AppComponent {
  isPublicRoute = signal(false);
  private notifService = inject(NotificationService);

  notifPanelOpen = signal(false);
  notifLoading = signal(false);
  notifError = signal<string | null>(null);
  notifications = signal<Notification[]>([]);
  unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);

  constructor(
    public auth: AuthService,
    public theme: ThemeService,
    public aiContext: AiContextService,
    router: Router
  ) {
    const checkIsPublic = (url: string) => {
      const u = url.split('?')[0].split('#')[0];
      return u === '/' || u.startsWith('/auth') || u.startsWith('/depot') || u.startsWith('/suivi') || u.startsWith('/client/dashboard');
    };

    this.isPublicRoute.set(checkIsPublic(router.url));
    router.events.pipe(filter(e => e instanceof NavigationStart)).subscribe((e: any) => {
      this.isPublicRoute.set(checkIsPublic(e.url));
    });
    router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe((e: any) => {
      this.isPublicRoute.set(checkIsPublic(e.urlAfterRedirects || e.url));
    });
  }

  showLayout = computed(() => {
    const isAuth = this.auth.isAuthenticated();
    const isStaff = this.auth.isAdmin() || this.auth.isResponsable() || this.auth.isAgent();
    return isAuth && isStaff && !this.isPublicRoute();
  });

  getFallbackAvatar(role?: string): string {
    const r = (role || '').toUpperCase();
    if (r.includes('ADMIN')) return 'assets/images/avatars/admin-default.png';
    if (r.includes('AGENT')) return 'assets/images/avatars/agent-default.png';
    return 'assets/images/avatars/client-default.png';
  }
  toggleNotifPanel() {

    const willOpen = !this.notifPanelOpen();
    this.notifPanelOpen.set(willOpen);
    this.notifError.set(null);
    if (willOpen) this.loadNotifications();
  }

  loadNotifications() {
    this.notifError.set(null);
    this.notifLoading.set(true);
    this.notifService.getAll().subscribe({
      next: list => {
        this.notifications.set(Array.isArray(list) ? list : []);
        this.notifLoading.set(false);
      },
      error: (err) => {
        this.notifications.set([]);
        this.notifLoading.set(false);
        const msg = err?.error?.message || err?.message || err?.status === 401
          ? 'Session expirée. Reconnectez-vous.'
          : 'Impossible de charger les notifications.';
        this.notifError.set(msg);
      }
    });
  }

  markNotifRead(n: Notification) {
    if (n.isRead) return;
    this.notifService.markRead(n.id).subscribe({
      next: () => this.notifications.update(list =>
        list.map(x => x.id === n.id ? { ...x, isRead: true } : x))
    });
  }
}
