// src/app/modules/dashboard/dashboard.component.ts
import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DashboardService } from '../../core/services/complaint.service';
import { DashboardStats } from '../../core/models';
import { AuthService } from '../../core/services/auth.service';
import { AiContextService } from '../../core/services/ai-context.service';
import { TunisiaMapComponent, type GovernorateComplaintData } from './tunisia-map/tunisia-map.component';

type PieChartDimension = 'status' | 'type' | 'priority' | 'agency';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, TunisiaMapComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  stats = signal<DashboardStats | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  /** Dimension affichée dans le pie chart (choix dynamique) */
  pieChartDimension = signal<PieChartDimension>('status');

  readonly pieChartOptions: { value: PieChartDimension; label: string }[] = [
    { value: 'status', label: 'Par statut' },
    { value: 'type', label: 'Par type' },
    { value: 'priority', label: 'Par priorité' },
    { value: 'agency', label: 'Par agence' }
  ];

  constructor(
    public auth: AuthService,
    private dashService: DashboardService,
    public aiContext: AiContextService
  ) {}

  ngOnInit() {
    this.loadStats();
  }

  loadStats() {
    this.loading.set(true);
    this.error.set(null);
    this.dashService.getStats().subscribe({
      next: data => {
        this.stats.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || err?.message || 'Impossible de charger le tableau de bord.');
      }
    });
  }

  get slaPercent() {
    return Math.round((this.stats()?.slaComplianceRate ?? 0) * 100);
  }

  getMaxTrend(): number {
    const trend = this.stats()?.dailyTrend ?? [];
    let max = 1;
    trend.forEach(d => { max = Math.max(max, d.created, d.closed); });
    return max;
  }

  hasCriticalPriority(): boolean {
    return (this.stats()?.byPriority ?? []).some(x => x.priority === 'Critique' && x.count > 0);
  }

  priorityColor(p: string) {
    return { 'Critique': '#dc2626', 'Haute': '#f97316', 'Moyenne': '#eab308', 'Faible': '#22c55e' }[p] ?? '#64748b';
  }

  statusColor(s: string) {
    const map: Record<string, string> = {
      'Nouvelle': '#3b82f6', 'Assignée': '#8b5cf6', 'En cours': '#f59e0b',
      'Validation': '#06b6d4', 'Clôturée': '#22c55e', 'Rejetée': '#ef4444'
    };
    return map[s] ?? '#64748b';
  }

  /** Palette pour type / agence (couleurs fixes par index) */
  private static readonly PIE_PALETTE = ['#1a56db', '#8b5cf6', '#0891b2', '#059669', '#d97706', '#dc2626', '#7c3aed', '#0d9488', '#ca8a04', '#e11d48'];

  colorForLabel(dim: PieChartDimension, label: string, index: number): string {
    if (dim === 'status') return this.statusColor(label);
    if (dim === 'priority') return this.priorityColor(label);
    return DashboardComponent.PIE_PALETTE[index % DashboardComponent.PIE_PALETTE.length];
  }

  setPieChartDimension(dim: PieChartDimension) {
    this.pieChartDimension.set(dim);
  }

  /** Données brutes selon la dimension sélectionnée */
  get pieChartRawData(): { label: string; count: number }[] {
    const s = this.stats();
    const dim = this.pieChartDimension();
    if (!s) return [];
    if (dim === 'status') return (s.byStatus ?? []).map(x => ({ label: x.status, count: x.count }));
    if (dim === 'type') return (s.byType ?? []).map(x => ({ label: x.type || 'Autre', count: x.count }));
    if (dim === 'priority') return (s.byPriority ?? []).map(x => ({ label: x.priority, count: x.count }));
    if (dim === 'agency') return (s.byAgency ?? []).map(x => ({ label: x.agency || 'Non affecté', count: x.count }));
    return [];
  }

  /** Segments pour le pie chart (dynamique selon la dimension) */
  get pieChartSegments(): { color: string; startPercent: number; endPercent: number; label: string; count: number }[] {
    const s = this.stats();
    const raw = this.pieChartRawData;
    const total = raw.reduce((sum, x) => sum + x.count, 0);
    if (!raw.length || total === 0) return [];
    const dim = this.pieChartDimension();
    let acc = 0;
    return raw.map((item, i) => {
      const pct = (item.count / total) * 100;
      const start = acc;
      acc += pct;
      return {
        color: this.colorForLabel(dim, item.label, i),
        startPercent: start,
        endPercent: acc,
        label: item.label,
        count: item.count
      };
    });
  }

  /** Conic gradient pour le pie chart (dynamique) */
  get pieChartConicGradient(): string {
    const segs = this.pieChartSegments;
    if (!segs.length) return 'var(--gray-200)';
    return segs.map(seg => `${seg.color} ${seg.startPercent}% ${seg.endPercent}%`).join(', ');
  }

  /** Conic gradient avec espaces entre les segments (effet plus lisible) */
  get pieChartConicGradientWithGaps(): string {
    const segs = this.pieChartSegments;
    if (!segs.length) return 'var(--gray-200)';
    const gap = 1.2;
    const totalGap = segs.length * gap;
    const available = 100 - totalGap;
    const total = this.pieChartTotal;
    if (total === 0) return 'var(--gray-200)';
    const parts: string[] = [];
    let pos = 0;
    segs.forEach((seg, i) => {
      const segWidth = (seg.count / total) * available;
      const segEnd = pos + segWidth;
      parts.push(`${seg.color} ${pos}% ${segEnd}%`);
      if (i < segs.length - 1) {
        parts.push(`#fff ${segEnd}% ${segEnd + gap}%`);
        pos = segEnd + gap;
      }
    });
    return parts.join(', ');
  }

  get pieChartTotal(): number {
    return this.pieChartRawData.reduce((sum, x) => sum + x.count, 0);
  }

  get pieChartTitle(): string {
    const opt = this.pieChartOptions.find(o => o.value === this.pieChartDimension());
    return opt ? `Répartition ${opt.label.toLowerCase()}` : 'Répartition';
  }

  /** Régions Tunisie pour la carte choroplèthe (agences → région) */
  private static readonly REGION_KEYS = ['tunis', 'nabeul', 'sousse', 'sfax', 'gabes'] as const;
  private static readonly REGION_LABELS: Record<string, string> = { tunis: 'Tunis', nabeul: 'Nabeul', sousse: 'Sousse', sfax: 'Sfax', gabes: 'Gabès' };
  private static readonly REGION_MATCH: Record<string, string[]> = {
    tunis: ['tunis', 'ariana', 'ben arous', 'manouba', 'bizerte'],
    nabeul: ['nabeul', 'zaghouan'],
    sousse: ['sousse', 'monastir', 'mahdia'],
    sfax: ['sfax'],
    gabes: ['gabès', 'gabes', 'médenine', 'medenine', 'tataouine', 'kebili', 'tozeur', 'gafsa']
  };

  /** Agrège byAgency par région et retourne count + niveau de couleur */
  get tunisiaRegionData(): { key: string; label: string; count: number; color: string; colorClass: 'high' | 'moderate' | 'low' }[] {
    const byAgency = this.stats()?.byAgency ?? [];
    const map: Record<string, number> = { tunis: 0, nabeul: 0, sousse: 0, sfax: 0, gabes: 0 };
    for (const a of byAgency) {
      const name = (a.agency || '').toLowerCase();
      let found = false;
      for (const key of DashboardComponent.REGION_KEYS) {
        const terms = DashboardComponent.REGION_MATCH[key];
        if (terms.some(t => name.includes(t))) { map[key] += a.count; found = true; break; }
      }
      if (!found) map.tunis += a.count;
    }
    const counts = Object.values(map);
    const totalCount = counts.reduce((a, b) => a + b, 0);
    const max = Math.max(1, ...counts);
    const result: { key: string; label: string; count: number; color: string; colorClass: 'high' | 'moderate' | 'low' }[] = [];
    for (const key of DashboardComponent.REGION_KEYS) {
      const count = map[key];
      let colorClass: 'high' | 'moderate' | 'low' = 'low';
      if (totalCount > 0 && max > 0) {
        if (count >= max * 0.5) colorClass = 'high';
        else if (count >= max * 0.25) colorClass = 'moderate';
      }
      const color = totalCount === 0 ? '#e2e8f0' : (colorClass === 'high' ? '#dc2626' : colorClass === 'moderate' ? '#ea580c' : '#16a34a');
      result.push({
        key,
        label: DashboardComponent.REGION_LABELS[key] || key,
        count,
        color,
        colorClass
      });
    }
    return result;
  }

  getChoroplethColor(regionKey: string): string {
    const r = this.tunisiaRegionData.find(x => x.key === regionKey);
    return r?.color ?? '#e2e8f0';
  }

  getRegionCount(regionKey: string): number {
    const r = this.tunisiaRegionData.find(x => x.key === regionKey);
    return r?.count ?? 0;
  }

  /** Données pour la carte Leaflet : contour Tunisie (Tunisie = total) + régions si GeoJSON gouvernorats */
  get tunisiaMapComplaintData(): GovernorateComplaintData {
    const data = this.tunisiaRegionData;
    const keyToGeoName: Record<string, string> = {
      tunis: 'Tunis',
      nabeul: 'Nabeul',
      sousse: 'Sousse',
      sfax: 'Sfax',
      gabes: 'Gabes'
    };
    const acc = data.reduce((a, r) => {
      const geoName = keyToGeoName[r.key];
      if (geoName) a[geoName] = r.count;
      return a;
    }, {} as GovernorateComplaintData);
    acc['Tunisie'] = this.stats()?.totalComplaints ?? 0;
    return acc;
  }
}
