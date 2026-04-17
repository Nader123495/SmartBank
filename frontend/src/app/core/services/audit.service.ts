import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuditFilter, AuditPageResult } from '../models';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly API = `${environment.apiUrl}/audit`;

  constructor(private http: HttpClient) {}

  getAll(filter: AuditFilter): Observable<AuditPageResult> {
    let params = new HttpParams();
    Object.entries(filter).forEach(([key, val]) => {
      if (val !== undefined && val !== null && val !== '')
        params = params.set(key, String(val));
    });
    return this.http.get<AuditPageResult>(this.API, { params });
  }
}
