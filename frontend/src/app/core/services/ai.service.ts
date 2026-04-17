import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AiChatRequest,
  AiChatResponse,
  AiSuggestion,
  AiConversationHistory
} from '../models';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AiService {
  private readonly API = `${environment.apiUrl}/ai`;

  constructor(private http: HttpClient) {}

  chat(body: AiChatRequest): Observable<AiChatResponse> {
    return this.http.post<AiChatResponse>(`${this.API}/chat`, body);
  }

  classify(title: string, description: string): Observable<AiSuggestion> {
    return this.http.post<AiSuggestion>(`${this.API}/classify`, { title, description });
  }

  draftResponse(complaintId: number): Observable<string> {
    return this.http
      .post<{ draft: string }>(`${this.API}/draft-response`, { complaintId })
      .pipe(map(r => r.draft));
  }

  summarize(complaintId: number): Observable<string> {
    return this.http
      .post<{ summary: string }>(`${this.API}/summarize/${complaintId}`, {})
      .pipe(map(r => r.summary));
  }

  history(userId: number): Observable<AiConversationHistory[]> {
    return this.http.get<AiConversationHistory[]>(`${this.API}/history/${userId}`);
  }

  canRequest(): Observable<{ can: boolean }> {
    return this.http.get<{ can: boolean }>(`${this.API}/can-request`);
  }
}
