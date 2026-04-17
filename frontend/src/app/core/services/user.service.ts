import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { UserListItem, UserStats, RoleOption, AgencyOption } from '../models';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly API = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getRoles(): Observable<RoleOption[]> {
    return this.http.get<RoleOption[]>(`${this.API}/roles`);
  }

  getAgencies(): Observable<AgencyOption[]> {
    return this.http.get<AgencyOption[]>(`${this.API}/agencies`);
  }

  getStats(): Observable<UserStats> {
    return this.http.get<UserStats>(`${this.API}/stats`);
  }

  getAll(opts: { search?: string; roleId?: number; agencyId?: number }): Observable<UserListItem[]> {
    let params = new HttpParams();
    Object.entries(opts).forEach(([key, val]) => {
      if (val !== undefined && val !== null && val !== '')
        params = params.set(key, String(val));
    });
    return this.http.get<UserListItem[]>(this.API, { params });
  }

  toggleStatus(userId: number): Observable<{ isActive: boolean }> {
    return this.http.post<{ isActive: boolean }>(`${this.API}/${userId}/toggle-status`, {});
  }
}
