// src/app/modules/auth/login/login.component.ts
import { Component, signal, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

declare var grecaptcha: { render: (el: HTMLElement, opts: { sitekey: string; theme?: string }) => number; getResponse: (id?: number) => string; reset: (id?: number) => void };

const RECAPTCHA_CONTAINER_ID = 'recaptcha-login-container';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  private route = inject(ActivatedRoute);

  recaptchaSiteKey = signal(environment.recaptchaSiteKey || '');
  private recaptchaWidgetId: number | null = null;
  private recaptchaRendered = false;
  private recaptchaInitScheduled = false;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  loading = signal(false);
  error = signal('');
  showPass = signal(false);
  /** Afficher le formulaire de connexion (après clic sur le bouton Connexion). */
  showLoginForm = signal(false);
  /** Comptes démo : afficher le champ code. */
  showDemoCodeInput = signal(false);
  /** Comptes démo : code correct saisi → afficher les comptes. */
  demoUnlocked = signal(false);
  /** Code saisi pour débloquer les comptes démo (ngModel ou form). */
  demoCode = '';

  /** Code requis pour afficher les comptes démo (à modifier selon votre politique). */
  private readonly demoSecretCode = 'STB2025';

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router
  ) {
    effect(() => {
      if (!this.recaptchaSiteKey() || this.recaptchaWidgetId != null || this.recaptchaInitScheduled) return;
      this.recaptchaInitScheduled = true;
      const schedule = () => {
        const el = document.getElementById(RECAPTCHA_CONTAINER_ID);
        if (!el) return;
        if (el.querySelector('iframe')) return;
        const win = window as any;
        if (typeof win.grecaptcha !== 'undefined' && win.grecaptcha.render) {
          try {
            this.recaptchaWidgetId = win.grecaptcha.render(el, {
              sitekey: this.recaptchaSiteKey(),
              theme: 'light'
            });
            this.recaptchaRendered = true;
          } catch {
            this.recaptchaWidgetId = null;
          }
          return;
        }
        setTimeout(schedule, 150);
      };
      setTimeout(schedule, 350);
    });
  }

  ngOnInit() {
    if (this.auth.isAuthenticated()) {
      if (this.auth.isClient()) {
        this.router.navigate(['/client/dashboard']);
      } else if (this.auth.isResponsable()) {
        this.router.navigate(['/dashboard']);
      } else {
        this.router.navigate(['/complaints']);
      }
      return;
    }
    if (this.route.snapshot.queryParamMap.get('form') === '1') {
      this.showLoginForm.set(true);
    }
    this.route.queryParamMap.subscribe(params => {
      if (params.get('form') === '1') this.showLoginForm.set(true);
    });
  }

  openLoginForm() {
    this.showLoginForm.set(true);
  }

  private resetRecaptcha() {
    if (this.recaptchaWidgetId != null && typeof (window as any).grecaptcha !== 'undefined') {
      try {
        (window as any).grecaptcha.reset(this.recaptchaWidgetId);
      } catch {}
    }
  }

  backToWelcome() {
    this.showLoginForm.set(false);
    this.error.set('');
    this.demoUnlocked.set(false);
    this.showDemoCodeInput.set(false);
    this.demoCode = '';
    this.recaptchaRendered = false;
    this.recaptchaWidgetId = null;
    this.recaptchaInitScheduled = false;
  }

  toggleDemoCodeInput() {
    this.showDemoCodeInput.set(!this.showDemoCodeInput());
    this.demoCode = '';
  }

  checkDemoCode() {
    if (this.demoCode.trim() === this.demoSecretCode) {
      this.demoUnlocked.set(true);
      this.showDemoCodeInput.set(false);
      this.demoCode = '';
    }
  }

  submit() {
    if (this.form.invalid) return;
    const recaptchaToken = this.recaptchaSiteKey() && this.recaptchaWidgetId != null
      ? (window as any).grecaptcha?.getResponse?.(this.recaptchaWidgetId) ?? ''
      : '';
    this.loading.set(true);
    this.error.set('');

    const payload = { ...this.form.value, recaptchaToken } as any;
    this.auth.login(payload).subscribe({
      next: (res) => {
        if (this.auth.isClient()) {
          this.router.navigate(['/client/dashboard']);
        } else if (this.auth.isResponsable()) {
          this.router.navigate(['/dashboard']);
        } else {
          this.router.navigate(['/complaints']);
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.resetRecaptcha();

        if (err.status === 0) {
          this.error.set('Impossible de contacter le serveur. Veuillez vérifier que le backend est démarré.');
          return;
        }

        if (err.status === 401) {
          this.error.set('Identifiants incorrects (Email ou Mot de passe).');
          return;
        }

        if (err.status === 403 && err.error?.code === 'EMAIL_NOT_VERIFIED') {
          this.router.navigate(['/auth/verify-email'], { queryParams: { email: err.error?.email ?? this.form.value.email } });
          return;
        }

        if (err.status === 400 && (err.error?.code === 'RECAPTCHA_REQUIRED' || err.error?.code === 'RECAPTCHA_FAILED')) {
          this.error.set(err.error?.message || 'Vérification de sécurité requise.');
          return;
        }

        this.error.set(err.error?.message || 'Une erreur est survenue lors de la connexion.');
      },
      complete: () => this.loading.set(false)
    });
  }

  fillLogin(email: string, password: string) {
    this.form.setValue({ email, password });
    this.error.set('');
  }

  goToSignup(e: Event) {
    e.preventDefault();
    this.router.navigate(['/auth/signup']);
  }

  onGoogle() {
    this.error.set('');
    this.loading.set(true);
    window.location.href = this.auth.getGoogleLoginUrl();
  }
}
