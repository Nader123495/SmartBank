import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SlaTemplate {
  id: number;
  type: string;
  slaHours: number;
  priority: 'Basse' | 'Moyenne' | 'Haute' | 'Critique';
  description: string;
  isActive: boolean;
  escalationLevels: EscalationLevel[];
}

export interface EscalationLevel {
  level: number;
  name: string;
  minutesThreshold: number;
  targetRole: string;
  channels: string[];
  requiredAction: string;
}

@Injectable({
  providedIn: 'root'
})
export class SlaService {
  private apiUrl = `${environment.apiUrl}/api/sla`;

  constructor(private http: HttpClient) {}

  // === TEMPLATE ENDPOINTS ===
  getSlaTemplates(): Observable<SlaTemplate[]> {
    return this.http.get<SlaTemplate[]>(`${this.apiUrl}/templates`);
  }

  getSlaTemplate(id: number): Observable<SlaTemplate> {
    return this.http.get<SlaTemplate>(`${this.apiUrl}/templates/${id}`);
  }

  createSlaTemplate(template: Partial<SlaTemplate>): Observable<SlaTemplate> {
    return this.http.post<SlaTemplate>(`${this.apiUrl}/templates`, template);
  }

  updateSlaTemplate(id: number, template: Partial<SlaTemplate>): Observable<SlaTemplate> {
    return this.http.put<SlaTemplate>(`${this.apiUrl}/templates/${id}`, template);
  }

  deleteSlaTemplate(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/templates/${id}`);
  }

  // === ESCALATION ENDPOINTS ===
  addEscalationLevel(templateId: number, escalation: Partial<EscalationLevel>): Observable<SlaTemplate> {
    return this.http.post<SlaTemplate>(
      `${this.apiUrl}/templates/${templateId}/escalations`,
      escalation
    );
  }

  updateEscalationLevel(
    templateId: number,
    level: number,
    escalation: Partial<EscalationLevel>
  ): Observable<SlaTemplate> {
    return this.http.put<SlaTemplate>(
      `${this.apiUrl}/templates/${templateId}/escalations/${level}`,
      escalation
    );
  }

  deleteEscalationLevel(templateId: number, level: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/templates/${templateId}/escalations/${level}`
    );
  }

  // === COMPLAINT SLA ENDPOINTS ===
  getComplaintSla(complaintId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/complaints/${complaintId}`);
  }

  // === STATISTICS ===
  getSlaStatistics(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/statistics`);
  }

  getSlaStatisticsByType(type: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/statistics/type/${type}`);
  }

  // === IMPORT/EXPORT ===
  exportConfiguration(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export`, { responseType: 'blob' });
  }

  importConfiguration(data: any[]): Observable<any> {
    return this.http.post(`${this.apiUrl}/import`, { templates: data });
  }

  // === VERIFICATION ===
  verifySlaCompliance(complaintId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/verify/${complaintId}`);
  }

  // === ESCALATION SIMULATION ===
  simulateEscalation(complaintId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/simulate-escalation/${complaintId}`, {});
  }
}
