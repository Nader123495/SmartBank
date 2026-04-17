import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuditService } from '../../../core/services/audit.service';
import { AuditLog, AuditFilter, AuditPageResult, AUDIT_ACTIONS } from '../../../core/models';

@Component({
  standalone: true,
  selector: 'app-admin-audit',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './audit.component.html',
  styleUrls: ['./audit.component.scss']
})
export class AuditComponent implements OnInit {
  private audit = inject(AuditService);

  readonly auditActions = AUDIT_ACTIONS;
  filter: AuditFilter = { page: 1, pageSize: 20 };
  items: AuditLog[] = [];
  totalCount = 0;
  totalPages = 0;
  page = 1;
  loading = false;
  error = '';

  ngOnInit() {
    this.load(1);
  }

  load(p: number) {
    this.page = p;
    this.filter.page = p;
    this.loading = true;
    this.error = '';
    this.audit.getAll(this.filter).subscribe({
      next: (res: AuditPageResult) => {
        this.items = res.items ?? [];
        this.totalCount = res.totalCount ?? 0;
        this.totalPages = res.totalPages ?? 1;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message || err?.message || 'Erreur lors du chargement de l\'audit.';
      }
    });
  }

  clearFilters() {
    this.filter.search = '';
    this.filter.action = '';
    this.filter.from = undefined;
    this.filter.to = undefined;
    this.load(1);
  }

  /** Clé CSS pour la couleur du badge (ESCALADE=rouge, COMMENT=vert, etc.) */
  getActionBadgeClass(action: string): string {
    const a = (action || '').toUpperCase();
    if (a === 'ESCALADE' || a === 'ESCALATION') return 'escalade';
    if (a === 'COMMENT' || a === 'CREATE' || a === 'LOGIN' || a === 'REGISTER') return 'green';
    if (a === 'STATUS') return 'blue';
    if (a === 'ASSIGN') return 'purple';
    if (a === 'LOGOUT') return 'gray';
    return 'default';
  }

  /** Numéros de page à afficher (1, 2, 3, ..., totalPages) */
  get paginationPages(): number[] {
    const total = this.totalPages;
    const current = this.page;
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
    const pages: number[] = [];
    if (current <= 4) {
      pages.push(1, 2, 3, 4, 5, -1, total);
    } else if (current >= total - 3) {
      pages.push(1, -1, total - 4, total - 3, total - 2, total - 1, total);
    } else {
      pages.push(1, -1, current - 1, current, current + 1, -1, total);
    }
    return pages.filter((p, i, arr) => p !== -1 || arr[i - 1] !== -1);
  }

  getInitials(log: AuditLog): string {
    if (log.userName?.trim()) {
      const parts = log.userName.trim().split(/\s+/);
      if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
      return log.userName.slice(0, 2).toUpperCase();
    }
    return 'SYS';
  }

  isSystem(log: AuditLog): boolean {
    return !log.userId && !log.userName && !log.userEmail;
  }

  exportCsv() {
    const headers = ['#', 'Date & Heure', 'Utilisateur', 'Action', 'Entité', 'Détail', 'IP'];
    const rows = this.items.map((log, i) => {
      const index = (this.page - 1) * this.filter.pageSize + i + 1;
      const user = this.isSystem(log) ? 'Système' : (log.userName || '') + (log.userEmail ? ' (' + log.userEmail + ')' : '');
      return [index, log.createdAt, user, log.action, log.entity ?? '', log.detail ?? '', log.ipAddress ?? ''];
    });
    const csv = [headers.join(';'), ...rows.map(r => r.map(c => `"${String(c).replace(/"/g, '""')}"`).join(';'))].join('\n');
    const blob = new Blob(['\ufeff' + csv], { type: 'text/csv;charset=utf-8' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `audit-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(a.href);
  }

  trackByIndex(i: number) {
    return i;
  }
}
