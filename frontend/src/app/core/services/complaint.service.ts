// src/app/core/services/complaint.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  Complaint, ComplaintFilter, PagedResult, DashboardStats, Notification, ClientComplaintView
} from '../models';

@Injectable({ providedIn: 'root' })
export class ComplaintService {
  private readonly API = `${environment.apiUrl}/complaints`;

  constructor(private http: HttpClient) {}

  getAll(filter: ComplaintFilter) {
    let params = new HttpParams();
    Object.entries(filter).forEach(([key, val]) => {
      if (val !== undefined && val !== null && val !== '')
        params = params.set(key, String(val));
    });
    return this.http.get<PagedResult<Complaint>>(this.API, { params });
  }

  getById(id: number) {
    return this.http.get<Complaint>(`${this.API}/${id}`);
  }

  create(dto: Partial<Complaint>) {
    return this.http.post<Complaint>(this.API, dto);
  }

  update(id: number, dto: Partial<Complaint>) {
    return this.http.put<Complaint>(`${this.API}/${id}`, dto);
  }

  assign(id: number, dto: { agentId: number; notes?: string }) {
    return this.http.post(`${this.API}/${id}/assign`, dto);
  }

  changeStatus(id: number, dto: {
    newStatus: string;
    comment?: string;
    resolutionNote?: string;
    rejectionReason?: string;
  }) {
    return this.http.post(`${this.API}/${id}/status`, dto);
  }

  addComment(id: number, dto: { content: string; isInternal: boolean }) {
    return this.http.post(`${this.API}/${id}/comments`, dto);
  }

  autoAssign(id: number) {
    return this.http.post(`${this.API}/${id}/auto-assign`, {});
  }

  uploadAttachment(id: number, file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post(`${this.API}/${id}/attachments`, form);
  }
}

/** Service portail client : dépôt et suivi sans authentification. */
@Injectable({ providedIn: 'root' })
export class ClientComplaintService {
  private readonly API = `${environment.apiUrl}/client`;

  constructor(private http: HttpClient) {}

  depot(dto: {
    title: string;
    description: string;
    complaintTypeId: number;
    channel: string;
    priority: string;
    clientName?: string;
    clientEmail: string;
    clientPhone?: string;
    clientAccountNumber?: string;
    agencyId?: number;
  }) {
    return this.http.post<{ reference: string; id: number; message: string }>(`${this.API}/depot`, dto);
  }

  suivi(reference: string, email: string) {
    return this.http.get<ClientComplaintView>(`${this.API}/suivi`, {
      params: { reference, email }
    });
  }

  addComment(dto: { reference: string; email: string; content: string }) {
    return this.http.post<{ message: string }>(`${this.API}/suivi/comment`, dto);
  }
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private http: HttpClient) {}
  getStats() {
    return this.http.get<DashboardStats>(`${environment.apiUrl}/dashboard/stats`);
  }
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  constructor(private http: HttpClient) {}
  getAll() {
    return this.http.get<Notification[]>(`${environment.apiUrl}/notifications`);
  }
  markRead(id: number) {
    return this.http.patch(`${environment.apiUrl}/notifications/${id}/read`, {});
  }
}
