import { Component, OnInit, OnDestroy, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  standalone: true,
  selector: 'app-landing',
  imports: [CommonModule, RouterModule],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss']
})
export class LandingComponent implements OnInit, OnDestroy {
  auth = inject(AuthService);
  private router = inject(Router);
  readonly currentYear = new Date().getFullYear();
  /** Heure affichée côté TN (timer numérique) */
  currentTime = signal('');
  private timerId: ReturnType<typeof setInterval> | null = null;

  ngOnInit() {
    const update = () => {
      const d = new Date();
      this.currentTime.set(
        d.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false })
      );
    };
    update();
    this.timerId = setInterval(update, 1000);

    // Auto-redirect if already logged in
    if (this.auth.isAuthenticated()) {
      if (this.auth.isClient()) {
        this.router.navigate(['/client/dashboard']);
      } else if (this.auth.isResponsable()) {
        this.router.navigate(['/dashboard']);
      } else {
        this.router.navigate(['/complaints']);
      }
    }
  }

  ngOnDestroy() {
    if (this.timerId) clearInterval(this.timerId);
  }
  /** URL de l'image de fond du hero (milieu) — chargée depuis le template pour affichage fiable. */
  readonly heroBgImage = 'url(/assets/hero-bank.png)';
  /** IDs alignés avec l'API / depot. image = photo du bouton. */
  readonly complaintTypes = [
    { id: 1, name: 'Carte Bancaire', type: 'card' },
    { id: 3, name: 'Compte Bancaire', type: 'account' },
    { id: 5, name: 'Virement', type: 'transfer' },
    { id: 2, name: 'Crédit & Prêts', type: 'loan' },
    { id: 4, name: 'Autres...', type: 'other' }
  ];
  readonly steps = [
    { num: 1, title: 'Déposer', desc: 'Remplir le formulaire', icon: '📝' },
    { num: 2, title: 'Traitement', desc: 'Analyse par STB', icon: '⚙️' },
    { num: 3, title: 'Suivi', desc: 'Suivre en temps réel', icon: '💻' },
    { num: 4, title: 'Résolution', desc: 'Réponse de la Banque', icon: '✅' }
  ];
  readonly supportPhone = '71 110 000';
  readonly supportEmail = 'support@stb.com.tn';

  logout() {
    this.auth.logout();
  }
}
