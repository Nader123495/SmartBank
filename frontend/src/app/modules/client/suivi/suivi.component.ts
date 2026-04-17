// Suivi d'une réclamation par référence + email (sans authentification)
import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ClientComplaintService } from '../../../core/services/complaint.service';
import type { ClientComplaintView } from '../../../core/models';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/services/auth.service';
@Component({
  selector: 'app-suivi',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './suivi.component.html',
  styleUrls: ['./suivi.component.scss']
})
export class SuiviComponent {
  private fb = inject(FormBuilder);
  private clientSvc = inject(ClientComplaintService);
  private route = inject(ActivatedRoute);
  public auth = inject(AuthService);

  searchForm = this.fb.group({
    reference: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]]
  });

  commentForm = this.fb.group({
    content: ['', [Validators.required, Validators.minLength(3)]]
  });

  loading = signal(false);
  loadingComment = signal(false);
  error = signal('');
  complaint = signal<ClientComplaintView | null>(null);
  searched = signal(false);
  copyDone = signal(false);

  ngOnInit() {
    const ref = this.route.snapshot.queryParamMap.get('reference');
    const email = this.route.snapshot.queryParamMap.get('email');
    if (ref) this.searchForm.patchValue({ reference: ref });
    if (email) this.searchForm.patchValue({ email });

    // Pré-remplissage si déjà connecté
    const user = this.auth.currentUser();
    if (user && !this.searchForm.get('email')?.value) {
      this.searchForm.patchValue({ email: user.email });
    }

    if (ref && email) this.search();
  }

  search() {
    if (this.searchForm.invalid) {
      this.searchForm.markAllAsTouched();
      return;
    }
    const { reference, email } = this.searchForm.getRawValue();
    if (!reference || !email) return;
    this.loading.set(true);
    this.error.set('');
    this.complaint.set(null);
    this.searched.set(true);

    this.clientSvc.suivi(reference, email).subscribe({
      next: (c) => {
        this.complaint.set(c);
        this.error.set('');
      },
      error: (err) => {
        const body = err.error;
        let msg = body?.message ?? body?.detail ?? '';
        if (err.status === 404)
          msg = msg || 'Aucune réclamation trouvée pour cette référence et cet email.';
        if (!msg) {
          if (err.status === 0)
            msg = `Impossible de joindre le serveur. Démarrez l'API backend (dossier backend/SmartBank.API) avec : dotnet run. URL : ${environment.apiUrl}`;
          else if (err.status === 404) msg = 'Aucune réclamation trouvée pour cette référence et cet email.';
          else msg = err.message || 'Erreur lors du chargement.';
        }
        // Pour 500, afficher le détail technique si présent (aide au diagnostic)
        if (err.status === 500 && body?.detail && typeof body.detail === 'string')
          msg = msg + ' — ' + body.detail;
        this.error.set(msg);
        this.complaint.set(null);
        this.loading.set(false);
      },
      complete: () => this.loading.set(false)
    });
  }

  addComment() {
    const c = this.complaint();
    if (!c || this.commentForm.invalid) return;
    const content = this.commentForm.get('content')?.value?.trim();
    if (!content) return;
    const { reference, email } = this.searchForm.getRawValue();
    if (!reference || !email) return;

    this.loadingComment.set(true);
    this.clientSvc.addComment({ reference, email, content }).subscribe({
      next: () => {
        this.commentForm.reset();
        this.search();
        this.loadingComment.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Impossible d\'ajouter le commentaire.');
        this.loadingComment.set(false);
      }
    });
  }

  formatDate(s: string) {
    if (!s) return '—';
    const d = new Date(s);
    return d.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  /** Classe CSS pour le badge statut (ex. "En cours" → "status-en-cours"). */
  statusClass(status: string): string {
    if (!status) return 'status-';
    return 'status-' + status.toLowerCase().replace(/\s+/g, '-');
  }

  /** Retourne l'index de l'étape actuelle pour la timeline (1, 2 ou 3). */
  getTimelineStep(status: string): number {
    if (!status) return 0;
    const s = status.toLowerCase();
    if (s === 'nouvelle' || s === 'assignée') return 1;
    if (s === 'en cours' || s === 'validation') return 2;
    if (s === 'clôturée' || s === 'rejetée') return 3;
    return 1;
  }

  copyReference(ref: string) {
    if (!ref || !navigator.clipboard?.writeText) return;
    navigator.clipboard.writeText(ref).then(() => {
      this.copyDone.set(true);
      setTimeout(() => this.copyDone.set(false), 2000);
    });
  }
}
