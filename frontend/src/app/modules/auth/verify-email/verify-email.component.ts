import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  standalone: true,
  selector: 'app-verify-email',
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './verify-email.component.html',
  styleUrls: ['./verify-email.component.scss']
})
export class VerifyEmailComponent implements OnInit {
  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    code: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6), Validators.pattern(/^\d{6}$/)]]
  });

  loading = signal(false);
  error = signal('');
  info = signal('');
  verified = signal(false); // New signal to track success without login

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    const email = this.route.snapshot.queryParamMap.get('email');
    if (email) this.form.patchValue({ email });
  }

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set('');
    this.info.set('');
    const email = this.form.value.email!;
    const code = this.form.value.code!;
    this.auth.verifyEmail(email, code).subscribe({
      next: (res) => {
        if (res.accessToken) {
          if (this.auth.isClient()) {
            this.router.navigate(['/client/dashboard']);
          } else {
            this.router.navigate(['/dashboard']);
          }
        } else {
          this.loading.set(false);
          this.verified.set(true);
          this.info.set('Email vérifié avec succès ! Votre compte est maintenant en attente de validation par un administrateur.');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message || 'Code invalide ou expiré.');
      }
    });
  }

  resend() {
    const email = this.form.value.email;
    if (!email) {
      this.error.set("Veuillez d'abord saisir votre adresse e-mail.");
      return;
    }
    this.loading.set(true);
    this.error.set('');
    this.info.set('');
    this.auth.resendVerification(email).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.info.set(res.message || 'Un nouveau code a été généré.');
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message || "Impossible de renvoyer le code de vérification.");
      }
    });
  }
}
