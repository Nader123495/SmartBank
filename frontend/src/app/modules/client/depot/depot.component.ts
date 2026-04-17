// Dépôt de réclamation par un client (sans authentification)
import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ClientComplaintService } from '../../../core/services/complaint.service';
import { COMPLAINT_CHANNELS } from '../../../core/models';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-depot',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './depot.component.html',
  styleUrls: ['./depot.component.scss']
})
export class DepotComponent {
  private fb = inject(FormBuilder);
  private clientSvc = inject(ClientComplaintService);
  private route = inject(ActivatedRoute);
  public auth = inject(AuthService);

  step = signal<number>(1);
  selectedCategory = signal<string | null>(null);

  form = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(5)]],
    description: ['', [Validators.required, Validators.minLength(20)]],
    complaintTypeId: [1, Validators.required],
    channel: ['E-Banking', Validators.required],
    priority: ['Moyenne', Validators.required],
    clientName: ['', Validators.required],
    clientEmail: ['', [Validators.required, Validators.email]],
    clientPhone: [''],
    clientAccountNumber: [''],
    clientGovernorate: ['', Validators.required],

    // Specific fields
    cardLastFour: ['', [Validators.pattern(/^\d{4}$/)]],
    incidentDate: [''],
    accountType: ['Compte Courant'],
    amount: [null as number | null],
    virementReference: [''],
    creditType: ['Prêt Immobilier'],
    dossierNumber: ['']
  });

  channels = COMPLAINT_CHANNELS;
  priorityOptions = [
    { label: 'Faible', value: 'Faible' },
    { label: 'Normale', value: 'Moyenne' },
    { label: 'Haute', value: 'Haute' },
    { label: 'Critique', value: 'Critique' }
  ];
  governorates = [
    'Ariana', 'Béja', 'Ben Arous', 'Bizerte', 'Gabès', 'Gafsa',
    'Jendouba', 'Kairouan', 'Kasserine', 'Kébili', 'Kef', 'Mahdia',
    'Manouba', 'Médenine', 'Monastir', 'Nabeul', 'Sfax', 'Sidi Bouzid',
    'Siliana', 'Sousse', 'Tataouine', 'Tozeur', 'Tunis', 'Zaghouan'
  ];
  complaintTypes = [
    { id: 1, name: 'Carte Bancaire' },
    { id: 2, name: 'Crédit et Prêts' },
    { id: 3, name: 'Compte Courant' },
    { id: 4, name: 'Digital Banking' },
    { id: 5, name: 'Virement' },
    { id: 6, name: 'Chèque' },
    { id: 7, name: 'Autre' }
  ];

  loading = signal(false);
  error = signal('');
  successRef = signal<string | null>(null);
  successEmail = signal<string | null>(null);
  copyDone = signal(false);

  categories = [
    { name: 'Carte Bancaire', icon: 'credit-card', color: '#1A56DB', typeIds: [1, 4] },
    { name: 'Compte Bancaire', icon: 'wallet', color: '#10b981', typeIds: [3, 6] },
    { name: 'Virement', icon: 'send', color: '#f59e0b', typeIds: [5] },
    { name: 'Crédit & Prêts', icon: 'home', color: '#8b5cf6', typeIds: [2] },
    { name: 'Autres', icon: 'more-horizontal', color: '#64748b', typeIds: [7] }
  ];

  filteredComplaintTypes() {
    const cat = this.selectedCategory();
    if (!cat) return [];
    
    // Hardcoded mapping based on common banking needs for STB
    const mappings: Record<string, any[]> = {
      'Carte Bancaire': [
        { id: 1, name: 'Problème de Carte (Avalée, bloquée...)' },
        { id: 4, name: 'Digital Banking - 100% Mobile' },
        { id: 7, name: 'Autre (Carte)' }
      ],
      'Compte Bancaire': [
        { id: 3, name: 'Compte Courant & Gestion' },
        { id: 6, name: 'Chèques & Chéquiers' },
        { id: 7, name: 'Autre (Compte)' }
      ],
      'Virement': [
        { id: 5, name: 'Virement non reçu / bloqué' },
        { id: 7, name: 'Autre (Virement)' }
      ],
      'Crédit & Prêts': [
        { id: 2, name: 'Crédit Immobilier / Consommation' },
        { id: 7, name: 'Autre (Crédit)' }
      ],
      'Autres': [
        { id: 7, name: 'Réclamation Générale' }
      ]
    };
    return mappings[cat] || this.complaintTypes;
  }

  selectCategory(name: string) {
    this.selectedCategory.set(name);
    const types = this.filteredComplaintTypes();
    if (types.length > 0) {
      this.form.patchValue({ complaintTypeId: types[0].id });
    }
    this.step.set(2);
  }

  resetSelection() {
    this.step.set(1);
    this.selectedCategory.set(null);
    this.form.reset({
      channel: 'E-Banking',
      priority: 'Moyenne',
      accountType: 'Compte Courant',
      creditType: 'Prêt Immobilier'
    });
    // Restore auth info if any
    const user = this.auth.currentUser();
    if (user) {
      this.form.patchValue({
        clientName: user.fullName,
        clientEmail: user.email
      });
    }
  }

  constructor() {
    this.route.queryParams.subscribe(q => {
      const typeId = q['typeId'];
      if (typeId != null && typeId !== '') {
        const id = Number(typeId);
        if (id >= 1 && id <= 7) this.form.patchValue({ complaintTypeId: id });
      }
    });

    // Pré-remplissage si déjà connecté
    const user = this.auth.currentUser();
    if (user) {
      this.form.patchValue({
        clientName: user.fullName,
        clientEmail: user.email
      });
    }
  }

  isInvalid(name: string) {
    const c = this.form.get(name);
    return c && c.invalid && (c.dirty || c.touched);
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set('');
    this.successRef.set(null);
    this.successEmail.set(null);

    const v = this.form.getRawValue();
    const dto = {
      title: v.title!,
      description: v.description!,
      complaintTypeId: Number(v.complaintTypeId) || 1,
      channel: v.channel!,
      priority: v.priority!,
      clientName: v.clientName || undefined,
      clientEmail: v.clientEmail!,
      clientPhone: v.clientPhone || undefined,
      clientAccountNumber: v.clientAccountNumber || undefined,
      clientGovernorate: v.clientGovernorate || undefined,
      // Specific fields
      cardLastFour: v.cardLastFour || undefined,
      incidentDate: v.incidentDate ? new Date(v.incidentDate) : undefined,
      accountType: v.accountType || undefined,
      amount: v.amount != null ? Number(v.amount) : undefined,
      virementReference: v.virementReference || undefined,
      creditType: v.creditType || undefined,
      dossierNumber: v.dossierNumber || undefined
    };

    this.clientSvc.depot(dto).subscribe({
      next: (res) => {
        this.successRef.set(res.reference);
        this.successEmail.set(this.form.get('clientEmail')?.value ?? null);
        this.loading.set(false);
      },
      error: (err) => {
        const body = err.error;
        let msg = body?.message ?? body?.detail ?? '';
        if (!msg && typeof body === 'object' && body?.errors) {
          const first = Object.values(body.errors as Record<string, string[]>)[0];
          msg = Array.isArray(first) ? first[0] : String(first);
        }
        if (!msg) {
          if (err.status === 0) {
          msg = `Impossible de joindre le serveur. Démarrez l'API backend (dossier backend/SmartBank.API) avec : dotnet run. URL utilisée : ${environment.apiUrl}`;
        }
          else if (err.status === 404) msg = 'Service de dépôt introuvable.';
          else msg = err.message || 'Erreur lors de l\'enregistrement.';
        }
        this.error.set(msg);
        this.loading.set(false);
      }
    });
  }

  copyReference() {
    const ref = this.successRef();
    if (!ref) return;
    if (navigator.clipboard?.writeText) {
      navigator.clipboard.writeText(ref).then(() => {
        this.copyDone.set(true);
        setTimeout(() => this.copyDone.set(false), 2000);
      });
    }
  }
}
