// Tunisia map component — Leaflet + GeoJSON choropleth for complaint dashboard
import {
  Component,
  Input,
  OnDestroy,
  AfterViewInit,
  ViewChild,
  ElementRef,
  inject,
  signal,
  computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import type { Map, GeoJSON as GeoJSONLayer, Layer } from 'leaflet';
import * as L from 'leaflet';

export interface GovernorateComplaintData {
  [governorateName: string]: number;
}

const DEFAULT_COMPLAINT_DATA: GovernorateComplaintData = {
  Tunis: 120,
  Sfax: 80,
  Sousse: 45,
  Nabeul: 30,
  Gabes: 25
};

const COLORS = {
  high: '#dc2626',
  moderate: '#ea580c',
  low: '#16a34a',
  none: '#e2e8f0'
};

@Component({
  selector: 'app-tunisia-map',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tunisia-map.component.html',
  styleUrls: ['./tunisia-map.component.scss']
})
export class TunisiaMapComponent implements AfterViewInit, OnDestroy {
  @ViewChild('mapContainer') mapContainer!: ElementRef<HTMLElement>;
  @Input() complaintData: GovernorateComplaintData | null = null;
  /** GeoJSON : contour réel de la Tunisie (fichier local) */
  @Input() geoJsonUrl = '/assets/geojson/tunisia-outline.geojson';

  private http = inject(HttpClient);
  private map: Map | null = null;
  private geoJsonLayer: GeoJSONLayer | null = null;

  loading = signal(true);
  error = signal<string | null>(null);

  data = computed(() => {
    const input = this.complaintData ?? DEFAULT_COMPLAINT_DATA;
    const entries = Object.entries(input).map(([name, count]) => ({ name: this.normalizeName(name), count }));
    const max = Math.max(1, ...entries.map(e => e.count));
    return { entries, max };
  });

  ngAfterViewInit(): void {
    this.loadGeoJson();
  }

  ngOnDestroy(): void {
    this.destroyMap();
  }

  private normalizeName(name: string): string {
    return name.trim().replace(/\s+/g, ' ');
  }

  private getCountForGovernorate(propName: string): number {
    const d = this.data();
    const normalized = this.normalizeName(propName);
    const found = d.entries.find(
      e => e.name.toLowerCase() === normalized.toLowerCase()
    );
    return found?.count ?? 0;
  }

  private getColorForCount(count: number): string {
    const d = this.data();
    if (d.max === 0 || count === 0) return COLORS.none;
    const ratio = count / d.max;
    if (ratio >= 0.5) return COLORS.high;
    if (ratio >= 0.25) return COLORS.moderate;
    return COLORS.low;
  }

  private loadGeoJson(): void {
    this.loading.set(true);
    this.error.set(null);
    this.http.get<GeoJSON.FeatureCollection>(this.geoJsonUrl).subscribe({
      next: (geojson) => {
        const tunisiaOnly = this.extractTunisiaFeature(geojson);
        this.loading.set(false);
        setTimeout(() => this.initMap(tunisiaOnly), 0);
      },
      error: (err) => {
        this.error.set(err?.message || 'Impossible de charger la carte.');
        this.loading.set(false);
      }
    });
  }

  /** Garde uniquement le contour de la Tunisie (feature id TN) pour avoir la vraie forme du pays */
  private extractTunisiaFeature(collection: GeoJSON.FeatureCollection): GeoJSON.FeatureCollection {
    const features = collection.features || [];
    const tn = features.find(
      (f: GeoJSON.Feature) => (f.properties as Record<string, string>)?.id === 'TN'
    );
    if (tn) {
      return {
        type: 'FeatureCollection',
        features: [{
          ...tn,
          properties: { ...tn.properties, name: 'Tunisie' }
        }]
      };
    }
    return collection;
  }

  private initMap(geojson: GeoJSON.FeatureCollection): void {
    const el = this.mapContainer?.nativeElement;
    if (!el) return;

    this.destroyMap();

    const map = L.map(el, {
      center: [34.0, 9.5],
      zoom: 6,
      zoomControl: false,
      attributionControl: false
    });

    // Uniquement la Tunisie (GeoJSON), pas de fond monde
    L.control.zoom({ position: 'topright' }).addTo(map);

    const geoJsonLayer = L.geoJSON(geojson, {
      style: (feature) => {
        const name = feature?.properties?.['name'] ?? '';
        const count = this.getCountForGovernorate(name);
        const color = this.getColorForCount(count);
        return {
          fillColor: color,
          color: '#374151',
          weight: 1.5,
          fillOpacity: 0.75
        };
      },
      onEachFeature: (feature, layer: Layer) => {
        const name = feature?.properties?.['name'] ?? 'Région';
        const count = this.getCountForGovernorate(name);
        const content = `${name}<br/><strong>${count}</strong> réclamation${count !== 1 ? 's' : ''}`;
        layer.bindTooltip(content, {
          permanent: false,
          direction: 'top',
          className: 'tunisia-map-tooltip',
          offset: [0, -8]
        });
      }
    }).addTo(map);

    map.fitBounds(geoJsonLayer.getBounds(), { padding: [20, 20], maxZoom: 7 });

    this.map = map;
    this.geoJsonLayer = geoJsonLayer;

    // Forcer le recalcul de la taille (utile si le conteneur venait d’apparaître)
    setTimeout(() => map.invalidateSize(), 100);
  }

  private destroyMap(): void {
    if (this.geoJsonLayer) {
      this.geoJsonLayer.remove();
      this.geoJsonLayer = null;
    }
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }

  readonly legendItems = [
    { color: COLORS.high, label: 'Réclamations élevées' },
    { color: COLORS.moderate, label: 'Modérées' },
    { color: COLORS.low, label: 'Faibles' }
  ];
}
