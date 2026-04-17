import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { UserService } from '../../../core/services/user.service';
import { UserListItem, UserStats, RoleOption, AgencyOption } from '../../../core/models';

@Component({
  standalone: true,
  selector: 'app-admin-users',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss']
})
export class UsersComponent implements OnInit {
  private userService = inject(UserService);

  stats: UserStats | null = null;
  roles: RoleOption[] = [];
  agencies: AgencyOption[] = [];
  users: UserListItem[] = [];
  filter: { search: string; roleId: number | null; agencyId: number | null } = {
    search: '',
    roleId: null,
    agencyId: null
  };
  loading = false;
  error = '';

  /** Cartes rôles pour le résumé (Admin, Agents, Clients) */
  get roleCards(): { key: string; label: string; icon: string; count: number }[] {
    const byRole = this.stats?.byRole ?? [];
    const order: { key: string; label: string; icon: string }[] = [
      { key: 'admin', label: 'Administrateurs', icon: '👑' },
      { key: 'agent', label: 'Agents', icon: '👤' },
      { key: 'client', label: 'Clients', icon: '🏠' }
    ];
    return order.map(({ key, label, icon }) => ({
      key,
      label,
      icon,
      count: byRole.find(x => this.getRoleKey(x.roleName) === key)?.count ?? 0
    }));
  }

  ngOnInit() {
    this.loadRoles();
    this.loadAgencies();
    this.loadStats();
    this.loadUsers();
  }

  loadRoles() {
    this.userService.getRoles().subscribe({
      next: (list) => (this.roles = list),
      error: () => (this.roles = [])
    });
  }

  loadAgencies() {
    this.userService.getAgencies().subscribe({
      next: (list) => (this.agencies = list),
      error: () => (this.agencies = [])
    });
  }

  loadStats() {
    this.userService.getStats().subscribe({
      next: (s) => (this.stats = s),
      error: () => (this.stats = null)
    });
  }

  loadUsers() {
    this.loading = true;
    this.error = '';
    this.userService.getAll({
      search: this.filter.search || undefined,
      roleId: this.filter.roleId ?? undefined,
      agencyId: this.filter.agencyId ?? undefined
    }).subscribe({
      next: (list) => {
        this.users = list;
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.error?.message || err?.message || 'Erreur lors du chargement.';
        this.loading = false;
      }
    });
  }

  getInitials(u: UserListItem): string {
    const f = (u.firstName || '').trim();
    const l = (u.lastName || '').trim();
    if (f && l) return (f[0] + l[0]).toUpperCase();
    if (u.fullName) return u.fullName.slice(0, 2).toUpperCase();
    return (u.email || '?').slice(0, 2).toUpperCase();
  }

  /** Correspondance stricte au nom de rôle API (évite admin ⊂ administratif, etc.). */
  private normalizedRole(roleName: string): string {
    return (roleName || '').trim().toLowerCase();
  }

  getRoleTitle(roleName: string): string {
    const r = this.normalizedRole(roleName);
    if (r === 'admin') return 'Administrateur';
    if (r === 'responsable') return 'Administrateur'; // ancien libellé base
    if (r === 'agent') return 'Agent réclamations';
    if (r === 'client') return 'Client';
    return roleName || '—';
  }

  /** Clé pour les styles (admin, responsable, agent, client) */
  getRoleKey(roleName: string): string {
    const r = this.normalizedRole(roleName);
    if (r === 'admin') return 'admin';
    if (r === 'responsable') return 'admin'; // fusionné visuellement avec Admin
    if (r === 'agent') return 'agent';
    if (r === 'client') return 'client';
    return 'other';
  }

  getRoleIcon(roleName: string): string {
    const k = this.getRoleKey(roleName);
    if (k === 'admin') return '👑';
    if (k === 'client') return '🏠';
    return '👤';
  }

  getRoleBadgeLabel(roleName: string): string {
    const r = this.normalizedRole(roleName);
    if (r === 'admin') return 'Admin';
    if (r === 'responsable') return 'Admin';
    if (r === 'agent') return 'Agent';
    if (r === 'client') return 'Client';
    return roleName || '—';
  }

  isAgent(roleName: string): boolean {
    return this.normalizedRole(roleName) === 'agent';
  }

  openNewUser() {
    // TODO: ouvrir modal ou route /admin/users/new
  }

  formatLastLogin(lastLogin?: string): string {
    if (!lastLogin) return '—';
    const d = new Date(lastLogin);
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);
    const dDate = new Date(d.getFullYear(), d.getMonth(), d.getDate());
    const h = d.getHours().toString().padStart(2, '0');
    const m = d.getMinutes().toString().padStart(2, '0');
    if (dDate.getTime() === today.getTime()) return `Aujourd'hui ${h}:${m}`;
    if (dDate.getTime() === yesterday.getTime()) return `Hier ${h}:${m}`;
    const day = d.getDate().toString().padStart(2, '0');
    const month = (d.getMonth() + 1).toString().padStart(2, '0');
    return `${day}/${month} ${h}:${m}`;
  }

  editUser(u: UserListItem) {
    // TODO: navigate to edit or open modal
  }

  getFallbackAvatar(userId?: number, gender?: string): string {
    if (!userId) return 'assets/images/avatars/av-1.png';
    
    if (gender === 'Male') {
      return (userId % 2 === 0) ? 'assets/images/avatars/av-1.png' : 'assets/images/avatars/av-3.png';
    } else if (gender === 'Female') {
      return (userId % 2 === 0) ? 'assets/images/avatars/av-2.png' : 'assets/images/avatars/av-4.png';
    }

    const index = (userId % 4) + 1;
    return `assets/images/avatars/av-${index}.png`;
  }

  toggleUserStatus(u: UserListItem) {

    this.userService.toggleStatus(u.id).subscribe({
      next: (res) => {
        u.isActive = res.isActive;
        this.loadStats(); // Recharger les compteurs roles
      },
      error: (err) => {
        this.error = err?.error?.message || 'Échec du changement de statut.';
      }
    });
  }
}
