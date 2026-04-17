// src/app/modules/complaints/detail/complaint-detail.component.ts
import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ComplaintService } from '../../../core/services/complaint.service';
import { AuthService } from '../../../core/services/auth.service';
import { AiService } from '../../../core/services/ai.service';
import { AiContextService } from '../../../core/services/ai-context.service';
import { Complaint, COMPLAINT_STATUSES } from '../../../core/models';

@Component({
  selector: 'app-complaint-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './complaint-detail.component.html',
  styleUrls: ['./complaint-detail.component.scss']
})
export class ComplaintDetailComponent implements OnInit, OnDestroy {
  private ai = inject(AiService);
  private aiContext = inject(AiContextService);

  complaint = signal<Complaint | null>(null);
  loading = signal(true);
  loadError = signal<string | null>(null);
  showStatusModal = signal(false);
  showCommentModal = signal(false);
  showAssignModal = signal(false);
  showDraftModal = signal(false);
  draftText = signal('');
  draftLoading = signal(false);

  newStatus = '';
  statusComment = '';
  resolutionNote = '';
  rejectionReason = '';
  newComment = '';
  commentInternal = true;
  assignAgentId = '';
  statusError = signal('');

  statuses = COMPLAINT_STATUSES;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private svc: ComplaintService,
    public auth: AuthService
  ) {}

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.aiContext.setComplaint(id);
    this.loadComplaint(id);
  }

  ngOnDestroy() {
    this.aiContext.setComplaint(null);
  }

  loadComplaint(id: number) {
    this.loadError.set(null);
    this.svc.getById(id).subscribe({
      next: c => {
        this.complaint.set(c);
        this.loading.set(false);
        // Ouvrir directement le modal "Changer le statut" si on arrive depuis la liste avec ?action=status
        if (this.route.snapshot.queryParamMap.get('action') === 'status') {
          this.showStatusModal.set(true);
          this.router.navigate([], { queryParams: {}, queryParamsHandling: '', replaceUrl: true });
        }
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err.error?.detail ?? err.error?.message ?? err.message ?? 'Impossible de charger la réclamation.';
        this.loadError.set(msg);
      }
    });
  }

  submitStatus() {
    this.statusError.set('');
    const c = this.complaint()!;
    this.svc.changeStatus(c.id, {
      newStatus: this.newStatus,
      comment: this.statusComment,
      resolutionNote: this.resolutionNote,
      rejectionReason: this.rejectionReason
    }).subscribe({
      next: () => {
        this.closeStatusModal();
        this.loadComplaint(c.id);
      },
      error: (err: any) => {
        this.statusError.set(err.error?.message || err.message || 'Erreur lors du changement de statut.');
      }
    });
  }

  submitComment() {
    const c = this.complaint()!;
    this.svc.addComment(c.id, { content: this.newComment, isInternal: this.commentInternal })
      .subscribe(() => {
        this.newComment = '';
        this.showCommentModal.set(false);
        this.loadComplaint(c.id);
      });
  }

  submitAssign() {
    const c = this.complaint()!;
    this.svc.assign(c.id, { agentId: +this.assignAgentId })
      .subscribe(() => {
        this.showAssignModal.set(false);
        this.loadComplaint(c.id);
      });
  }

  openStatusModal() {
    this.newStatus = '';
    this.statusComment = '';
    this.resolutionNote = '';
    this.rejectionReason = '';
    this.statusError.set('');
    this.showStatusModal.set(true);
  }

  closeStatusModal() {
    this.showStatusModal.set(false);
  }

  autoAssign() {
    const c = this.complaint()!;
    this.svc.autoAssign(c.id).subscribe(() => this.loadComplaint(c.id));
  }

  priorityClass(p: string) {
    return { 'Critique': 'badge-critique', 'Haute': 'badge-haute', 'Moyenne': 'badge-moyenne', 'Faible': 'badge-faible' }[p] ?? '';
  }

  statusClass(s: string) {
    return {
      'Nouvelle': 'badge-nouvelle', 'Assignée': 'badge-assignee', 'En cours': 'badge-encours',
      'Validation': 'badge-validation', 'Clôturée': 'badge-cloturee', 'Rejetée': 'badge-rejetee'
    }[s ?? ''] ?? '';
  }

  formatFileSize(bytes?: number) {
    if (!bytes) return '';
    if (bytes < 1024) return `${bytes} o`;
    if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} Ko`;
    return `${(bytes / 1048576).toFixed(1)} Mo`;
  }

  generateDraft() {
    const c = this.complaint();
    if (!c) return;
    this.showDraftModal.set(true);
    this.draftLoading.set(true);
    this.draftText.set('');
    this.ai.draftResponse(c.id).subscribe({
      next: draft => {
        this.draftText.set(draft);
        this.draftLoading.set(false);
      },
      error: () => this.draftLoading.set(false)
    });
  }

  copyDraft() {
    const text = this.draftText();
    if (text && navigator.clipboard) navigator.clipboard.writeText(text);
  }

  sendDraftAsComment() {
    const text = this.draftText().trim();
    if (!text) return;
    this.newComment = text;
    this.commentInternal = false;
    this.showDraftModal.set(false);
    this.showCommentModal.set(true);
  }
}
