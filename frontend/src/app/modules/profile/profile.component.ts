import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  standalone: true,
  selector: 'app-profile',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  auth = inject(AuthService);
  router = inject(Router);
  theme = inject(ThemeService);

  activeTab = signal<'general' | 'security'>('general');
  firstName = '';
  lastName = '';
  fullName = '';
  email = '';
  gender = 'Male';
  phoneNumber = '';
  accountNumber = '';
  agency = '';
  avatarUrl = signal<string | null>(null);
  
  // 2FA Mock
  is2FAEnabled = signal(false);

  loading = signal(false);
  error = signal('');
  success = signal('');

  // Changement mot de passe
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  passwordLoading = signal(false);
  passwordError = signal('');
  passwordSuccess = signal('');

  user = computed(() => this.auth.currentUser());

  ngOnInit() {
    const u = this.auth.currentUser();
    if (u) {
      this.firstName = u.firstName || '';
      this.lastName = u.lastName || '';
      this.fullName = u.fullName;
      this.email = u.email;
      this.avatarUrl.set(u.avatarUrl || null);
      this.gender = u.gender || 'Male';
      this.phoneNumber = u.phoneNumber || '';
      this.accountNumber = u.accountNumber || '';
      this.agency = u.agency || 'Non assignée';
    }
  }

  setTab(tab: 'general' | 'security') {
    this.activeTab.set(tab);
    this.error.set('');
    this.success.set('');
    this.passwordError.set('');
    this.passwordSuccess.set('');
  }

  getInitials(): string {
    const fn = this.firstName.charAt(0) || '';
    const ln = this.lastName.charAt(0) || '';
    return (fn + ln).toUpperCase() || 'U';
  }

  getFallbackAvatar(): string {
    const role = (this.user()?.role || '').toUpperCase();
    if (role.includes('ADMIN')) return 'assets/images/avatars/admin-default.png';
    if (role.includes('AGENT')) return 'assets/images/avatars/agent-default.png';
    return 'assets/images/avatars/client-default.png';
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || !file.type.startsWith('image/')) return;
    const reader = new FileReader();
    reader.onload = () => {
      let dataUrl = reader.result as string;
      this.saveAvatar(dataUrl);
    };
    reader.readAsDataURL(file);
    input.value = '';
  }

  private saveAvatar(dataUrl: string) {
    this.error.set('');
    this.loading.set(true);
    this.auth.updateProfile({ avatarUrl: dataUrl }).subscribe({
      next: (p) => {
        this.avatarUrl.set(p.avatarUrl || null);
        this.loading.set(false);
        this.success.set('Photo mise à jour avec succès.');
        setTimeout(() => this.success.set(''), 3000);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Erreur lors de la mise à jour de la photo.');
      }
    });
  }

  saveProfile() {
    this.error.set('');
    this.loading.set(true);
    
    const updateData = {
      firstName: this.firstName,
      lastName: this.lastName,
      fullName: `${this.firstName} ${this.lastName}`,
      gender: this.gender,
      phoneNumber: this.phoneNumber,
      accountNumber: this.accountNumber
    };

    this.auth.updateProfile(updateData as any).subscribe({
      next: (u) => {
        this.loading.set(false);
        this.fullName = u.fullName;
        this.success.set('Profil mis à jour avec succès.');
        setTimeout(() => this.success.set(''), 3000);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Échec de la mise à jour du profil.');
      }
    });
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }

  savePassword() {
    this.passwordError.set('');
    this.passwordSuccess.set('');
    if (!this.currentPassword || !this.newPassword) {
      this.passwordError.set('Veuillez remplir tous les champs.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.passwordError.set('La confirmation ne correspond pas.');
      return;
    }
    this.passwordLoading.set(true);
    this.auth.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.passwordLoading.set(false);
        this.currentPassword = this.newPassword = this.confirmPassword = '';
        this.passwordSuccess.set('Mot de passe modifié avec succès.');
        setTimeout(() => this.passwordSuccess.set(''), 3000);
      },
      error: () => {
        this.passwordLoading.set(false);
        this.passwordError.set('Mot de passe actuel incorrect.');
      }
    });
  }
}
