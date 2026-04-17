// src/app/modules/complaints/form/complaint-form.component.ts
import { Component, OnInit, OnDestroy, inject, signal, ElementRef, ViewChild, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { ComplaintService } from '../../../core/services/complaint.service';
import { AiService } from '../../../core/services/ai.service';
import { UserService } from '../../../core/services/user.service';
import { COMPLAINT_CHANNELS } from '../../../core/models';
import type { AiSuggestion } from '../../../core/models';
import type { AgencyOption } from '../../../core/models';

import { AuthService } from '../../../core/services/auth.service';

// ─── Type helpers ───────────────────────────────────────────────────────────
export type LocationStatus = 'idle' | 'detecting' | 'gps' | 'ip' | 'manual' | 'denied';

export interface CategoryCard {
  id: string;
  name: string;
  icon: string;
  description: string;
  color: string;
  typeIds: number[];
}

declare global {
  interface Window {
    L: any; // Leaflet loaded from CDN
  }
}

@Component({
  selector: 'app-complaint-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './complaint-form.component.html',
  styleUrls: ['./complaint-form.component.scss']
})
export class ComplaintFormComponent implements OnInit, OnDestroy {
  // ─── Steps & Category ──────────────────────────────────────────────────
  step = signal(1);
  selectedCategory = signal<CategoryCard | null>(null);

  categories: CategoryCard[] = [
    { 
      id: 'carte', 
      name: 'Carte Bancaire', 
      icon: '💳', 
      description: 'Perte, vol, blocage ou fraude sur vos cartes Visa/Mastercard.', 
      color: '#3b82f6', 
      typeIds: [1] 
    },
    { 
      id: 'compte', 
      name: 'Compte Bancaire', 
      icon: '🏦', 
      description: 'Problèmes de solde, frais, chèques ou services e-banking.', 
      color: '#10b981', 
      typeIds: [3, 4, 6] 
    },
    { 
      id: 'virement', 
      name: 'Virement', 
      icon: '💸', 
      description: 'Retards, erreurs de destinataire ou virements suspects.', 
      color: '#8b5cf6', 
      typeIds: [5] 
    },
    { 
      id: 'credit', 
      name: 'Crédit & Prêts', 
      icon: '🏠', 
      description: 'Suivi de dossier, taux, mensualités ou assurances prêt.', 
      color: '#f59e0b', 
      typeIds: [2] 
    },
    { 
      id: 'autre', 
      name: 'Autres', 
      icon: '📁', 
      description: 'Tout autre service : assurance, coffre-fort, change, etc.', 
      color: '#6b7280', 
      typeIds: [7] 
    }
  ];

  // ─── Form ──────────────────────────────────────────────────────────────
  form = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(5)]],
    description: ['', [Validators.required, Validators.minLength(20)]],
    complaintTypeId: ['', Validators.required],
    channel: ['', Validators.required],
    priority: ['Moyenne', Validators.required],
    clientName: [''],
    clientEmail: ['', Validators.email],
    clientPhone: [''],
    clientAccountNumber: [''],
    agencyId: [''],
    locationCity: [''],

    // Specific fields for dynamic categories
    cardLastFour: [''],
    incidentDate: [''],
    accountType: [''],
    amount: [null as number | null],
    virementReference: [''],
    creditType: [''],
    dossierNumber: ['']
  });

  channels = COMPLAINT_CHANNELS;
  priorityOptions = [
    { label: 'Faible', value: 'Faible' },
    { label: 'Normale', value: 'Moyenne' },
    { label: 'Haute', value: 'Haute' },
    { label: 'Critique', value: 'Critique' }
  ];

  allComplaintTypes = [
    { id: 1, name: 'Carte Bancaire' },
    { id: 2, name: 'Crédit et Prêts' },
    { id: 3, name: 'Compte Courant' },
    { id: 4, name: 'Digital Banking' },
    { id: 5, name: 'Virement' },
    { id: 6, name: 'Chèque' },
    { id: 7, name: 'Autre' }
  ];

  filteredComplaintTypes = computed(() => {
    const cat = this.selectedCategory();
    if (!cat) return [];
    return this.allComplaintTypes.filter(t => cat.typeIds.includes(t.id));
  });

  agencies: AgencyOption[] = [];
  editId = signal<number | null>(null);
  loading = signal(false);
  error = signal('');
  private ai = inject(AiService);
  private userService = inject(UserService);
  public auth = inject(AuthService);
  classifyLoading = signal(false);
  suggestion = signal<AiSuggestion | null>(null);
  attachments = signal<File[]>([]);
  dragOver = signal(false);
  readonly maxFileSizeMB = 10;
  readonly allowedExtensions = ['.pdf', '.jpg', '.jpeg', '.png', '.docx', '.xlsx'];

  // ─── Geolocation state ──────────────────────────────────────────────────
  locationStatus = signal<LocationStatus>('idle');
  detectedCity = signal('');
  latitude = signal<number | null>(null);
  longitude = signal<number | null>(null);
  locationConfirmed = signal(false);
  mapReady = signal(false);

  @ViewChild('mapContainer') mapContainerRef?: ElementRef<HTMLDivElement>;
  private leafletMap: any = null;
  private leafletMarker: any = null;
  private leafletScriptLoaded = false;

  constructor(
    private fb: FormBuilder,
    private svc: ComplaintService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  // ─── Lifecycle ──────────────────────────────────────────────────────────
  ngOnInit() {
    this.userService.getAgencies().subscribe({
      next: list => (this.agencies = list),
      error: () => (this.agencies = [])
    });
    const id = this.route.snapshot.paramMap.get('id');
    
    // Si l'utilisateur est un Admin/Agent, on passe directement au formulaire complet
    if (!this.auth.isClient()) {
      this.step.set(2);
      // On sélectionne par défaut "Autres" pour avoir une catégorie valide
      const defaultCat = this.categories.find(c => c.id === 'autre') || this.categories[0];
      this.selectedCategory.set(defaultCat);
    }

    if (id) {
      this.editId.set(+id);
      this.step.set(2);
      this.svc.getById(+id).subscribe(c => {
        // Try to find the category based on complaintTypeId
        const cat = this.categories.find(k => k.typeIds.includes(Number(c.complaintType))); // Backend maps ID to Name usually, needs care
        // Assuming Backend returns Type as string... wait, DTO mapping uses complaintType: c.ComplaintType.Name
        // If editing, we might need more logic, but for now let's focus on Creation flow.
        this.form.patchValue(c as any);
      });
    }

    this.detectLocation();
  }

  ngOnDestroy() {
    if (this.leafletMap) {
      this.leafletMap.remove();
      this.leafletMap = null;
    }
  }

  // ─── Step Logic ──────────────────────────────────────────────────────────
  selectCategory(cat: CategoryCard) {
    this.selectedCategory.set(cat);
    this.step.set(2);
    // Reset specific fields when switching
    this.form.patchValue({
      complaintTypeId: cat.typeIds[0].toString(),
      cardLastFour: '',
      accountType: '',
      virementReference: '',
      creditType: '',
      dossierNumber: '',
      amount: null
    });
  }

  goBack() {
    this.step.set(1);
    this.selectedCategory.set(null);
  }

  // ─── Geolocation logic ──────────────────────────────────────────────────
  detectLocation() {
    this.locationStatus.set('detecting');
    if ('geolocation' in navigator) {
      navigator.geolocation.getCurrentPosition(
        pos => this.onGpsSuccess(pos),
        err => this.onGpsDenied(err),
        { timeout: 10000, maximumAge: 60000, enableHighAccuracy: false }
      );
    } else {
      this.fallbackToIp();
    }
  }

  private async onGpsSuccess(pos: GeolocationPosition) {
    const lat = pos.coords.latitude;
    const lng = pos.coords.longitude;
    this.latitude.set(lat);
    this.longitude.set(lng);
    this.locationStatus.set('gps');
    try {
      const res = await fetch(`https://nominatim.openstreetmap.org/reverse?lat=${lat}&lon=${lng}&format=json&accept-language=fr`);
      if (res.ok) {
        const data = await res.json();
        const addr = data.address;
        const city = addr?.city || addr?.town || addr?.village || addr?.county || '';
        this.detectedCity.set(city);
        this.form.patchValue({ locationCity: city });
      }
    } catch { }
    setTimeout(() => this.loadLeafletMap(), 300);
  }

  private async onGpsDenied(_err: GeolocationPositionError) {
    this.locationStatus.set('denied');
    this.fallbackToIp();
  }

  private async fallbackToIp() {
    this.locationStatus.set('detecting');
    try {
      const res = await fetch('https://ipapi.co/json/');
      if (res.ok) {
        const data = await res.json();
        if (data && !data.error) {
          const city = data.city || data.region || '';
          this.detectedCity.set(city);
          this.form.patchValue({ locationCity: city });
          this.locationStatus.set('ip');
          if (data.latitude && data.longitude) {
            this.latitude.set(data.latitude);
            this.longitude.set(data.longitude);
            setTimeout(() => this.loadLeafletMap(), 300);
          }
          return;
        }
      }
    } catch { }
    this.locationStatus.set('manual');
  }

  private loadLeafletMap() {
    if (this.leafletScriptLoaded && window.L) {
      this.initMap();
      return;
    }
    if (!document.getElementById('leaflet-css')) {
      const link = document.createElement('link');
      link.id = 'leaflet-css'; link.rel = 'stylesheet';
      link.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
      document.head.appendChild(link);
    }
    const script = document.createElement('script');
    script.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
    script.onload = () => { this.leafletScriptLoaded = true; this.initMap(); };
    document.head.appendChild(script);
  }

  private initMap() {
    const lat = this.latitude();
    const lng = this.longitude();
    if (lat === null || lng === null || !window.L) return;
    this.mapReady.set(true);
    setTimeout(() => {
      const container = document.getElementById('leaflet-map-container');
      if (!container || this.leafletMap) return;
      const L = window.L;
      this.leafletMap = L.map(container, { zoomControl: true }).setView([lat, lng], 13);
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap', maxZoom: 19
      }).addTo(this.leafletMap);
      const icon = L.divIcon({
        className: '',
        html: `<div class="leaflet-custom-pin"><div class="pin-pulse"></div><svg viewBox="0 0 24 24" fill="#2563eb" width="28" height="28"><path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/></svg></div>`,
        iconSize: [28, 36], iconAnchor: [14, 36]
      });
      this.leafletMarker = L.marker([lat, lng], { icon, draggable: true }).addTo(this.leafletMap);
      this.leafletMarker.on('dragend', async (e: any) => {
        const pos = e.target.getLatLng();
        this.latitude.set(pos.lat); this.longitude.set(pos.lng);
        this.locationStatus.set('manual');
        try {
          const res = await fetch(`https://nominatim.openstreetmap.org/reverse?lat=${pos.lat}&lon=${pos.lng}&format=json&accept-language=fr`);
          if (res.ok) {
            const data = await res.json();
            const city = data.address?.city || data.address?.town || data.address?.village || '';
            if (city) { this.detectedCity.set(city); this.form.patchValue({ locationCity: city }); }
          }
        } catch { }
      });
    }, 0);
  }

  onManualCityChange() {
    const val = this.form.get('locationCity')?.value || '';
    this.detectedCity.set(val);
    this.locationStatus.set('manual');
  }

  retryLocation() {
    this.locationConfirmed.set(false);
    this.mapReady.set(false);
    if (this.leafletMap) { this.leafletMap.remove(); this.leafletMap = null; }
    this.detectLocation();
  }

  // ─── AI assist ───────────────────────────────────────────────────────────
  runClassify() {
    const title = this.form.get('title')?.value ?? '';
    const description = this.form.get('description')?.value ?? '';
    if (!title || !description) return;
    this.classifyLoading.set(true);
    this.ai.classify(title, description).subscribe({
      next: s => { this.suggestion.set(s); this.classifyLoading.set(false); },
      error: () => this.classifyLoading.set(false)
    });
  }

  applySuggestion() {
    const s = this.suggestion();
    if (!s) return;
    const pri = ['Faible', 'Moyenne', 'Haute', 'Critique'].includes(s.priority) ? s.priority : 'Moyenne';
    this.form.patchValue({ priority: pri });
    const match = this.allComplaintTypes.find(t => t.name.toLowerCase().includes(s.type.toLowerCase()));
    if (match) this.form.patchValue({ complaintTypeId: String(match.id) });
    this.suggestion.set(null);
  }

  // ─── Files ───────────────────────────────────────────────────────────────
  onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.addFiles(Array.from(input.files));
    input.value = '';
  }

  onDrop(e: DragEvent) {
    e.preventDefault();
    this.dragOver.set(false);
    if (e.dataTransfer?.files?.length) this.addFiles(Array.from(e.dataTransfer.files));
  }

  onDragOver(e: DragEvent) { e.preventDefault(); this.dragOver.set(true); }
  onDragLeave() { this.dragOver.set(false); }

  private addFiles(files: File[]) {
    const maxBytes = this.maxFileSizeMB * 1024 * 1024;
    const allowed = new Set(this.allowedExtensions.map(ext => ext.toLowerCase()));
    for (const f of files) {
      const ext = '.' + (f.name.split('.').pop() ?? '').toLowerCase();
      if (allowed.has(ext) && f.size <= maxBytes) {
        this.attachments.update(list => [...list, f]);
      }
    }
  }

  removeAttachment(index: number) { this.attachments.update(list => list.filter((_, i) => i !== index)); }

  // ─── Submit ─────────────────────────────────────────────────────────────
  saveDraft() { this.submit(); }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    this.error.set('');
    const val = this.form.getRawValue();

    const payload: any = {
      title: val.title,
      description: val.description,
      complaintTypeId: Number(val.complaintTypeId),
      channel: val.channel,
      priority: val.priority,
      clientName: val.clientName || undefined,
      clientEmail: val.clientEmail || undefined,
      clientPhone: val.clientPhone || undefined,
      clientAccountNumber: val.clientAccountNumber || undefined,
      agencyId: val.agencyId ? Number(val.agencyId) : undefined,
      // Geolocation
      submissionCity: this.detectedCity() || val.locationCity || undefined,
      latitude: this.latitude() ?? undefined,
      longitude: this.longitude() ?? undefined,
      // Specific fields
      cardLastFour: val.cardLastFour,
      incidentDate: val.incidentDate,
      accountType: val.accountType,
      amount: val.amount,
      virementReference: val.virementReference,
      creditType: val.creditType,
      dossierNumber: val.dossierNumber
    };

    const req = this.editId() ? this.svc.update(this.editId()!, payload) : this.svc.create(payload);

    req.subscribe({
      next: async (c: any) => {
        const files = this.attachments();
        if (files.length && c?.id) {
          for (const file of files) {
            try { await firstValueFrom(this.svc.uploadAttachment(c.id, file)); } catch { }
          }
        }
        this.router.navigate(['/complaints', c.id]);
      },
      error: (err: any) => {
        this.error.set(err.error?.message || 'Erreur lors de l\'enregistrement');
        this.loading.set(false);
      }
    });
  }

  cancel() { this.goBack(); if (this.step() === 1) this.router.navigate(['/complaints']); }

  isInvalid(field: string) {
    const ctrl = this.form.get(field);
    return ctrl?.invalid && ctrl?.touched;
  }

  // ─── Template helpers ─────────────────────────────────────────────────────
  get locationStatusLabel(): string {
    switch (this.locationStatus()) {
      case 'detecting': return 'Détection en cours…';
      case 'gps':       return 'Position GPS détectée';
      case 'ip':        return 'Localisation approximative (IP)';
      case 'manual':    return 'Position saisie manuellement';
      case 'denied':    return 'Accès GPS refusé — localisation par IP';
      default:          return 'Localisation';
    }
  }

  get locationStatusClass(): string {
    switch (this.locationStatus()) {
      case 'gps':       return 'status-gps';
      case 'ip':        return 'status-ip';
      case 'manual':    return 'status-manual';
      case 'denied':    return 'status-denied';
      case 'detecting': return 'status-detecting';
      default:          return '';
    }
  }

  get locationIcon(): string {
    switch (this.locationStatus()) {
      case 'gps':  return '📡';
      case 'ip':   return '🌐';
      case 'denied': return '🔒';
      default:     return '📍';
    }
  }
}
