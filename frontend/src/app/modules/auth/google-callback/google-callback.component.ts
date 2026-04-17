import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  standalone: true,
  selector: 'app-google-callback',
  imports: [CommonModule, RouterModule],
  template: `
    <div class="callback-wrap">
      @if (error) {
        <div class="callback-error">{{ error }}</div>
        <a [routerLink]="['/auth/login']">Retour à la connexion</a>
      } @else {
        <p>Connexion en cours…</p>
      }
    </div>
  `,
  styles: [`
    .callback-wrap {
      max-width: 400px;
      margin: 3rem auto;
      padding: 1.5rem;
      text-align: center;
    }
    .callback-error {
      color: var(--red-600);
      margin-bottom: 1rem;
    }
  `]
})
export class GoogleCallbackComponent implements OnInit {
  private auth = inject(AuthService);
  private router = inject(Router);

  error = '';

  ngOnInit() {
    const hash = window.location.hash?.slice(1) || '';
    const params = new URLSearchParams(hash);
    const token = params.get('token');
    const refresh = params.get('refresh');
    const user = params.get('user');
    if (token && refresh && user) {
      try {
        this.auth.setSessionFromGoogle({ token, refresh, user });
        this.router.navigate(['/dashboard']);
      } catch (e) {
        this.error = 'Session invalide. Veuillez réessayer.';
      }
    } else {
      this.error = 'Paramètres de connexion Google manquants.';
    }
  }
}
