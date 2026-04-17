import { Component, OnInit, signal, inject, computed, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { UserProfile } from '../../../core/models';
import { AuthService } from '../../../core/services/auth.service';

interface MyComplaint {
  id: number;
  reference: string;
  title: string;
  complaintType: string;
  status: string;
  priority: string;
  clientGovernorate?: string;
  hasRating: boolean;
  createdAt: string;
  closedAt?: string;
}

@Component({
  selector: 'app-client-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class ClientDashboardComponent implements OnInit, OnDestroy {
  private http = inject(HttpClient);
  private authSvc = inject(AuthService);
  private router = inject(Router);

  loading = signal(true);
  error = signal('');
  complaints = signal<MyComplaint[]>([]);
  stats = signal({ total: 0, active: 0, resolved: 0 });
  user = signal<UserProfile | null>(null);
  readonly now = new Date();

  // UI State
  currentTime = signal<string>('');
  showProfileMenu = signal(false);
  private timerId: any;

  // Computed Stats
  resolutionRate = computed(() => {
    const s = this.stats();
    if (s.total === 0) return 0;
    return Math.round((s.resolved / s.total) * 100);
  });

  initials = computed(() => {
    const name = this.user()?.fullName;
    if (!name) return 'U';
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  });

  // Rating modal state
  ratingComplaint = signal<MyComplaint | null>(null);
  rating = { satisfaction: 0, speed: 0, quality: 0, feedback: '' };
  ratingLoading = signal(false);
  ratingError = signal('');
  ratingSuccess = signal(false);

  ngOnInit() {
    this.user.set(this.authSvc.currentUser());
    this.loadData();
    this.startClock();
  }

  ngOnDestroy() {
    if (this.timerId) clearInterval(this.timerId);
  }

  private startClock() {
    const update = () => {
      const now = new Date();
      this.currentTime.set(now.toLocaleTimeString('fr-FR', { 
        hour: '2-digit', 
        minute: '2-digit',
        weekday: 'long',
        day: 'numeric',
        month: 'long'
      }));
    };
    update();
    this.timerId = setInterval(update, 60000);
  }

  loadData() {
    this.loading.set(true);
    this.error.set('');

    this.http.get<any>(`${environment.apiUrl}/client-portal/my-complaints`).subscribe({
      next: (res) => {
        this.complaints.set(res.complaints ?? []);
        this.stats.set(res.stats ?? { total: 0, active: 0, resolved: 0 });
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Erreur lors du chargement de vos réclamations.');
        this.loading.set(false);
      }
    });
  }

  toggleProfileMenu() {
    this.showProfileMenu.set(!this.showProfileMenu());
  }

  logout() {
    this.authSvc.logout();
    this.router.navigate(['/auth/login']);
  }

  getStatusIcon(status: string): string {
    switch (status) {
      case 'Nouvelle': return '🆕';
      case 'Assignée': return '👤';
      case 'En cours': return '⚙️';
      case 'Validation': return '👀';
      case 'Clôturée': return '✅';
      case 'Rejetée': return '❌';
      default: return '📄';
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Nouvelle': return 'status-new';
      case 'Assignée': return 'status-assigned';
      case 'En cours': return 'status-progress';
      case 'Clôturée': return 'status-closed';
      case 'Rejetée': return 'status-rejected';
      default: return '';
    }
  }

  isClosed(status: string): boolean {
    return status === 'Clôturée' || status === 'Rejetée';
  }

  formatDate(s: string) {
    if (!s) return '—';
    return new Date(s).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  // ──── Rating ────
  openRating(c: MyComplaint) {
    this.ratingComplaint.set(c);
    this.rating = { satisfaction: 0, speed: 0, quality: 0, feedback: '' };
    this.ratingError.set('');
    this.ratingSuccess.set(false);
  }

  closeRating() {
    this.ratingComplaint.set(null);
  }

  stars(n: number): number[] {
    return Array.from({ length: n }, (_, i) => i + 1);
  }

  setRating(field: 'satisfaction' | 'speed' | 'quality', value: number) {
    this.rating[field] = value;
  }

  submitRating() {
    if (!this.rating.satisfaction || !this.rating.speed || !this.rating.quality) {
      this.ratingError.set('Veuillez attribuer une note pour chaque critère.');
      return;
    }
    this.ratingLoading.set(true);
    this.ratingError.set('');
    const c = this.ratingComplaint();
    if (!c) return;

    this.http.post(`${environment.apiUrl}/client-portal/rate`, {
      complaintId: c.id,
      satisfactionRating: this.rating.satisfaction,
      speedRating: this.rating.speed,
      qualityRating: this.rating.quality,
      feedback: this.rating.feedback || null
    }).subscribe({
      next: () => {
        this.ratingLoading.set(false);
        this.ratingSuccess.set(true);
        this.complaints.update(list =>
          list.map(x => x.id === c.id ? { ...x, hasRating: true } : x)
        );
        setTimeout(() => this.closeRating(), 2000);
      },
      error: (err) => {
        this.ratingLoading.set(false);
        this.ratingError.set(err?.error?.message || 'Erreur lors de l\'envoi.');
      }
    });
  }
}
