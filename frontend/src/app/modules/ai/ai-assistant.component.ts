import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiService } from '../../core/services/ai.service';
import { AiContextService } from '../../core/services/ai-context.service';
import { AuthService } from '../../core/services/auth.service';
import { AiMessage, AiChatRequest } from '../../core/models';

@Component({
  standalone: true,
  selector: 'app-ai-assistant',
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-assistant.component.html',
  styleUrls: ['./ai-assistant.component.scss']
})
export class AiAssistantComponent {
  private ai = inject(AiService);
  private aiContext = inject(AiContextService);
  private auth = inject(AuthService);

  open = computed(() => this.aiContext.panel() !== null);
  message = signal('');
  messages = signal<AiMessage[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  /** Pièce jointe pour le prochain envoi (photo ou PDF) */
  attachment = signal<{ contentBase64: string; mimeType: string; fileName: string } | null>(null);
  readonly maxAttachmentSizeBytes = 6 * 1024 * 1024; // 6 Mo
  readonly acceptTypes = 'image/*,.pdf';

  close() {
    this.aiContext.closePanel();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (file.size > this.maxAttachmentSizeBytes) {
      this.error.set('Fichier trop volumineux (max 6 Mo).');
      input.value = '';
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      const data = reader.result as string;
      const base64 = data.includes(',') ? data.split(',')[1] : data;
      this.attachment.set({ contentBase64: base64, mimeType: file.type || 'application/octet-stream', fileName: file.name });
      this.error.set(null);
    };
    reader.readAsDataURL(file);
    input.value = '';
  }

  removeAttachment() {
    this.attachment.set(null);
  }

  send() {
    const text = this.message().trim();
    if ((!text && !this.attachment()) || this.loading()) return;
    const displayText = text || `[Fichier joint : ${this.attachment()?.fileName ?? 'pièce jointe'}]`;
    this.message.set('');
    this.error.set(null);
    const userMsg: AiMessage = { role: 'user', content: displayText };
    this.messages.update(msgs => [...msgs, userMsg]);
    const att = this.attachment();
    this.attachment.set(null);
    this.loading.set(true);

    const complaintId = this.aiContext.complaintId();
    const history = this.messages();

    const body: AiChatRequest = {
      message: text || displayText,
      complaintId: complaintId ?? undefined,
      conversationHistory: history.slice(0, -1)
    };
    if (att) {
      body.attachmentBase64 = att.contentBase64;
      body.attachmentMimeType = att.mimeType;
      body.attachmentFileName = att.fileName;
    }

    this.ai.chat(body).subscribe({
      next: res => {
        this.messages.update(msgs => [...msgs, { role: 'assistant', content: res.reply }]);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.message || err?.message || 'Erreur assistant.');
        this.loading.set(false);
      }
    });
  }
}
