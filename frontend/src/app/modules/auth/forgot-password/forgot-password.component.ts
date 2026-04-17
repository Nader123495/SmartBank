import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  standalone: true,
  selector: 'app-forgot-password',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  private auth = inject(AuthService);

  step: 1 | 2 = 1;
  email = '';
  code = '';
  newPassword = '';
  loading = false;
  message = '';
  error = '';

  sendCode() {
    this.error = '';
    this.message = '';
    if (!this.email.trim()) {
      this.error = 'Indiquez votre adresse email.';
      return;
    }
    this.loading = true;
    this.auth.forgotPassword(this.email.trim()).subscribe({
      next: () => {
        this.message = 'Un code de réinitialisation a été envoyé à votre email.';
        this.step = 2;
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.error?.message || err?.message || 'Erreur lors de l\'envoi du code.';
        this.loading = false;
      }
    });
  }

  resetPassword() {
    this.error = '';
    if (!this.code.trim() || !this.newPassword) {
      this.error = 'Renseignez le code et le nouveau mot de passe.';
      return;
    }
    if (this.newPassword.length < 6) {
      this.error = 'Le mot de passe doit contenir au moins 6 caractères.';
      return;
    }
    this.loading = true;
    this.auth.resetPassword(this.email.trim(), this.code.trim(), this.newPassword).subscribe({
      next: () => {
        this.message = 'Mot de passe mis à jour. Vous pouvez vous connecter.';
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.error?.message || err?.message || 'Erreur lors de la réinitialisation.';
        this.loading = false;
      }
    });
  }
}
