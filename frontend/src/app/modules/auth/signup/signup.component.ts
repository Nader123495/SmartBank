import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { UserService } from '../../../core/services/user.service';
import { AgencyOption } from '../../../core/models';

@Component({
  standalone: true,
  selector: 'app-signup',
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.scss']
})
export class SignupComponent implements OnInit {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private userService = inject(UserService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  isAgentMode = signal(false);
  governorates = [
    "Ariana", "Béja", "Ben Arous", "Bizerte", "Gabès", "Gafsa", "Jendouba",
    "Kairouan", "Kasserine", "Kébili", "Le Kef", "Mahdia", "La Manouba",
    "Médenine", "Monastir", "Nabeul", "Sfax", "Sidi Bouzid", "Siliana",
    "Sousse", "Tataouine", "Tozeur", "Tunis", "Zaghouan"
  ];

  allAgencies = signal<AgencyOption[]>([]);
  filteredAgencies = signal<AgencyOption[]>([]);

  form = this.fb.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
    acceptTerms: [false, Validators.requiredTrue],
    // Agent specific
    professionalId: [''],
    governorate: [''],
    agencyId: [null as number | null],
    gender: ['Male', Validators.required],
    // Common — required for all accounts
    city: ['', Validators.required]
  });

  loading = signal(false);
  error = signal('');
  success = signal('');
  showPass = signal(false);
  showPass2 = signal(false);

  ngOnInit() {
    const email = this.route.snapshot.queryParamMap.get('email');
    if (email) {
      this.form.patchValue({ email });
    }

    // Load agencies
    this.userService.getAgencies().subscribe(list => {
      this.allAgencies.set(list);
    });

    // Handle governorate change to filter agencies
    this.form.get('governorate')?.valueChanges.subscribe(gov => {
      if (gov) {
        this.filteredAgencies.set(this.allAgencies().filter(a => a.governorate === gov || !a.governorate));
      } else {
        this.filteredAgencies.set(this.allAgencies());
      }
    });
  }

  toggleMode(isAgent: boolean) {
    this.isAgentMode.set(isAgent);
    if (isAgent) {
      this.form.get('professionalId')?.setValidators([Validators.required]);
      this.form.get('governorate')?.setValidators([Validators.required]);
      this.form.get('agencyId')?.setValidators([Validators.required]);
    } else {
      this.form.get('professionalId')?.clearValidators();
      this.form.get('governorate')?.clearValidators();
      this.form.get('agencyId')?.clearValidators();
    }
    this.form.get('professionalId')?.updateValueAndValidity();
    this.form.get('governorate')?.updateValueAndValidity();
    this.form.get('agencyId')?.updateValueAndValidity();
  }

  goLogin(): void {
    this.router.navigate(['/auth/login']);
  }

  onGoogle(): void {
    this.error.set('');
    this.loading.set(true);
    window.location.href = this.auth.getGoogleLoginUrl();
  }

  submit() {
    if (this.form.invalid || this.form.value.password !== this.form.value.confirmPassword) {
      this.error.set('Vérifiez le formulaire (les mots de passe doivent correspondre).');
      return;
    }

    this.error.set('');
    this.success.set('');
    this.loading.set(true);

    const value = this.form.value;
    const parts = (value.fullName || '').trim().split(/\s+/);
    const firstName = parts[0] || '';
    const lastName = parts.slice(1).join(' ') || firstName;

    this.auth.register({
      firstName,
      lastName,
      email: value.email!,
      password: value.password!,
      isAgentRequest: this.isAgentMode(),
      professionalId: value.professionalId ?? undefined,
      governorate: value.governorate ?? undefined,
      agencyId: value.agencyId ?? undefined,
      gender: value.gender ?? 'Male',
      city: value.city ?? undefined
    }).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.requiresVerification && res.email) {
          this.success.set('Compte créé. Veuillez vérifier votre e-mail avec le code envoyé.');
          this.router.navigate(['/auth/verify-email'], { queryParams: { email: res.email } });
        } else if (this.isAgentMode()) {
          this.success.set('Demande d\'inscription agent envoyée. Un administrateur doit valider votre compte.');
          this.form.reset();
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message || 'Erreur lors de la création du compte.');
      }
    });
  }
}

