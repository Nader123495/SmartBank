import { Injectable, signal } from '@angular/core';
import { AiConversationHistory } from '../models';

@Injectable({ providedIn: 'root' })
export class AiContextService {
  complaintId = signal<number | null>(null);
  panel = signal<'chat' | 'trends' | null>(null);
  history = signal<AiConversationHistory[] | null>(null);

  setComplaint(id: number | null): void {
    this.complaintId.set(id);
  }

  openPanel(kind: 'chat' | 'trends'): void {
    this.panel.set(kind);
  }

  closePanel(): void {
    this.panel.set(null);
  }
}
