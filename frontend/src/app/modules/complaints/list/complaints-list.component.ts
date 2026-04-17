// src/app/modules/complaints/list/complaints-list.component.ts
import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ComplaintService } from '../../../core/services/complaint.service';
import { AuthService } from '../../../core/services/auth.service';
import { Complaint, ComplaintFilter, PagedResult, COMPLAINT_STATUSES, COMPLAINT_PRIORITIES, COMPLAINT_CHANNELS } from '../../../core/models';

@Component({
  selector: 'app-complaints-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './complaints-list.component.html',
  styleUrls: ['./complaints-list.component.scss']
})
export class ComplaintsListComponent implements OnInit {
  result = signal<PagedResult<Complaint> | null>(null);
  loading = signal(true);
  loadError = signal<string | null>(null);

  filter: ComplaintFilter = {
    page: 1,
    pageSize: 20,
    sortBy: 'CreatedAt',
    sortDir: 'desc'
  };

  statuses = COMPLAINT_STATUSES;
  priorities = COMPLAINT_PRIORITIES;
  channels = COMPLAINT_CHANNELS;

  private auth = inject(AuthService);
  constructor(private svc: ComplaintService) {}

  ngOnInit() { this.load(); }

  load() {
    const token = localStorage.getItem('smartbank_token');
    if (!token || this.auth.isTokenExpired(token)) {
      this.loading.set(false);
      this.result.set({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
      this.auth.clearSession();
      return;
    }
    this.loadError.set(null);
    this.loading.set(true);
    this.svc.getAll(this.filter).subscribe({
      next: data => { this.result.set(data); this.loading.set(false); },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 401) {
          this.result.set({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
          return;
        }
        this.loadError.set(err.error?.detail ?? err.error?.message ?? err.message ?? 'Erreur de chargement.');
        this.result.set({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
      }
    });
  }

  search() { this.filter.page = 1; this.load(); }
  clearFilters() {
    this.filter = { page: 1, pageSize: 20, sortBy: 'CreatedAt', sortDir: 'desc' };
    this.load();
  }

  goPage(p: number) { this.filter.page = p; this.load(); }

  /** Retourne la classe badge Figma pour la priorité */
  priorityClass(p: string): string {
    return { 'Critique': 'badge-critique', 'Haute': 'badge-haute', 'Moyenne': 'badge-moyenne', 'Faible': 'badge-faible' }[p] ?? '';
  }

  /** Retourne la classe badge Figma pour le statut */
  statusClass(s: string): string {
    return {
      'Nouvelle': 'badge-nouvelle', 'Assignée': 'badge-assignee', 'En cours': 'badge-encours',
      'Validation': 'badge-validation', 'Clôturée': 'badge-cloturee', 'Rejetée': 'badge-rejetee'
    }[s] ?? '';
  }

  /** Afficher le bouton "Changer le statut" si la réclamation n'est pas clôturée/rejetée */
  canChangeStatus(status: string | undefined): boolean {
    if (!status) return true;
    const s = status.trim();
    return s !== 'Clôturée' && s !== 'Rejetée';
  }

  sortBy(col: string) {
    if (this.filter.sortBy === col) {
      this.filter.sortDir = this.filter.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.filter.sortBy = col;
      this.filter.sortDir = 'desc';
    }
    this.load();
  }

  get pages() {
    const total = this.result()?.totalPages ?? 0;
    return Array.from({ length: total }, (_, i) => i + 1);
  }
}
