import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { SlaService } from '../../../services/sla.service';
import { ToastrService } from 'ngx-toastr';

interface SlaTemplate {
  id: number;
  type: string;
  slaHours: number;
  priority: 'Basse' | 'Moyenne' | 'Haute' | 'Critique';
  description: string;
  isActive: boolean;
  escalationLevels: EscalationLevel[];
}

interface EscalationLevel {
  level: number;
  name: string;
  minutesThreshold: number;
  targetRole: string;
  channels: string[];
  requiredAction: string;
}

@Component({
  selector: 'app-sla-configuration-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './sla-configuration-tab.component.html',
  styleUrls: ['./sla-configuration-tab.component.scss']
})
export class SlaConfigurationTabComponent implements OnInit, OnDestroy {
  // === STATE ===
  slaTemplates: SlaTemplate[] = [];
  selectedTemplate: SlaTemplate | null = null;
  isLoading = false;
  isFormOpen = false;
  isEscalationFormOpen = false;
  
  // === FORMS ===
  templateForm: FormGroup;
  escalationForm: FormGroup;
  
  // === PRIVATE ===
  private destroy$ = new Subject<void>();
  
  // === DROPDOWNS DATA ===
  complaintTypes = [
    'Carte Bancaire',
    'Virement',
    'Digital Banking',
    'Compte',
    'Crédit',
    'Autre'
  ];
  
  priorityLevels = ['Basse', 'Moyenne', 'Haute', 'Critique'];
  targetRoles = ['Agent', 'Manager Agence', 'Responsable Département', 'Direction Générale'];
  channelOptions = ['Email', 'SMS', 'Push Notification', 'Appel Téléphonique'];

  constructor(
    private slaService: SlaService,
    private fb: FormBuilder,
    private toastr: ToastrService
  ) {
    this.templateForm = this.createTemplateForm();
    this.escalationForm = this.createEscalationForm();
  }

  ngOnInit(): void {
    this.loadSlaTemplates();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // === LOAD DATA ===
  loadSlaTemplates(): void {
    this.isLoading = true;
    this.slaService.getSlaTemplates()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.slaTemplates = data;
          if (this.slaTemplates.length > 0) {
            this.selectTemplate(this.slaTemplates[0]);
          }
          this.isLoading = false;
        },
        error: (err) => {
          this.toastr.error('Erreur lors du chargement des templates SLA');
          this.isLoading = false;
        }
      });
  }

  // === TEMPLATE SELECTION ===
  selectTemplate(template: SlaTemplate): void {
    this.selectedTemplate = template;
    this.populateTemplateForm(template);
  }

  // === FORM CREATION ===
  private createTemplateForm(): FormGroup {
    return this.fb.group({
      id: [null],
      type: ['', Validators.required],
      slaHours: ['', [Validators.required, Validators.min(1), Validators.max(720)]],
      priority: ['', Validators.required],
      description: ['', Validators.required],
      isActive: [true]
    });
  }

  private createEscalationForm(): FormGroup {
    return this.fb.group({
      level: ['', [Validators.required, Validators.min(1), Validators.max(5)]],
      name: ['', Validators.required],
      minutesThreshold: ['', [Validators.required, Validators.min(0)]],
      targetRole: ['', Validators.required],
      channels: [[], Validators.required],
      requiredAction: ['', Validators.required]
    });
  }

  // === TEMPLATE FORM HANDLERS ===
  openTemplateForm(template?: SlaTemplate): void {
    if (template) {
      this.populateTemplateForm(template);
    } else {
      this.templateForm.reset({ isActive: true });
    }
    this.isFormOpen = true;
  }

  closeTemplateForm(): void {
    this.isFormOpen = false;
    this.templateForm.reset();
  }

  private populateTemplateForm(template: SlaTemplate): void {
    this.templateForm.patchValue({
      id: template.id,
      type: template.type,
      slaHours: template.slaHours,
      priority: template.priority,
      description: template.description,
      isActive: template.isActive
    });
  }

  saveTemplate(): void {
    if (this.templateForm.invalid) {
      this.toastr.error('Veuillez remplir tous les champs requis');
      return;
    }

    const templateData = this.templateForm.value;
    this.isLoading = true;

    const request = templateData.id 
      ? this.slaService.updateSlaTemplate(templateData.id, templateData)
      : this.slaService.createSlaTemplate(templateData);

    request.pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastr.success('Template SLA sauvegardé avec succès');
          this.closeTemplateForm();
          this.loadSlaTemplates();
        },
        error: () => {
          this.toastr.error('Erreur lors de la sauvegarde du template');
          this.isLoading = false;
        }
      });
  }

  deleteTemplate(id: number): void {
    if (confirm('Êtes-vous sûr de vouloir supprimer ce template SLA ?')) {
      this.slaService.deleteSlaTemplate(id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.toastr.success('Template supprimé avec succès');
            this.loadSlaTemplates();
          },
          error: () => this.toastr.error('Erreur lors de la suppression')
        });
    }
  }

  // === ESCALATION FORM HANDLERS ===
  openEscalationForm(): void {
    if (!this.selectedTemplate) {
      this.toastr.warning('Sélectionnez un template SLA d\'abord');
      return;
    }
    this.escalationForm.reset();
    this.isEscalationFormOpen = true;
  }

  closeEscalationForm(): void {
    this.isEscalationFormOpen = false;
    this.escalationForm.reset();
  }

  addEscalationLevel(): void {
    if (this.escalationForm.invalid || !this.selectedTemplate) {
      this.toastr.error('Veuillez remplir tous les champs');
      return;
    }

    const escalationData = this.escalationForm.value;
    this.isLoading = true;

    this.slaService.addEscalationLevel(this.selectedTemplate.id, escalationData)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastr.success('Niveau d\'escalade ajouté avec succès');
          this.closeEscalationForm();
          this.loadSlaTemplates();
        },
        error: () => {
          this.toastr.error('Erreur lors de l\'ajout du niveau');
          this.isLoading = false;
        }
      });
  }

  deleteEscalationLevel(templateId: number, level: number): void {
    if (confirm('Supprimer ce niveau d\'escalade ?')) {
      this.slaService.deleteEscalationLevel(templateId, level)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.toastr.success('Niveau supprimé');
            this.loadSlaTemplates();
          },
          error: () => this.toastr.error('Erreur lors de la suppression')
        });
    }
  }

  // === UTILITY ===
  getPriorityClass(priority: string): string {
    const classes: Record<string, string> = {
      'Basse': 'priority-low',
      'Moyenne': 'priority-medium',
      'Haute': 'priority-high',
      'Critique': 'priority-critical'
    };
    return classes[priority] || '';
  }

  getStatusClass(isActive: boolean): string {
    return isActive ? 'status-active' : 'status-inactive';
  }

  exportConfiguration(): void {
    const dataStr = JSON.stringify(this.slaTemplates, null, 2);
    const dataUri = 'data:application/json;charset=utf-8,' + encodeURIComponent(dataStr);
    const exportFileDefaultName = `sla-config-${new Date().toISOString().split('T')[0]}.json`;
    
    const linkElement = document.createElement('a');
    linkElement.setAttribute('href', dataUri);
    linkElement.setAttribute('download', exportFileDefaultName);
    linkElement.click();
    
    this.toastr.success('Configuration exportée avec succès');
  }

  importConfiguration(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e: any) => {
      try {
        const imported = JSON.parse(e.target.result);
        this.slaService.importConfiguration(imported)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: () => {
              this.toastr.success('Configuration importée avec succès');
              this.loadSlaTemplates();
            },
            error: () => this.toastr.error('Erreur lors de l\'import')
          });
      } catch {
        this.toastr.error('Format JSON invalide');
      }
    };
    reader.readAsText(file);
  }
}
