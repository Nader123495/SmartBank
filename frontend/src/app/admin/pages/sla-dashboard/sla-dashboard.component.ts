import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SlaConfigurationTabComponent } from '../../components/sla-configuration-tab/sla-configuration-tab.component';

interface Tab {
  id: string;
  name: string;
  icon: string;
}

@Component({
  selector: 'app-sla-dashboard',
  standalone: true,
  imports: [CommonModule, SlaConfigurationTabComponent],
  template: `
    <div class="sla-dashboard">
      <!-- HEADER -->
      <header class="dashboard-header">
        <div class="header-content">
          <h1>
            <span class="icon">🏦</span>
            BANKING SLA DASHBOARD
          </h1>
          <p class="subtitle">Gestion et Suivi des SLA - Service Level Agreement</p>
        </div>
        <div class="header-actions">
          <input 
            type="text" 
            class="search-box"
            placeholder="🔍 Rechercher...">
          <button class="notification-btn">
            🔔
            <span class="badge">3</span>
          </button>
        </div>
      </header>

      <!-- TABS NAVIGATION -->
      <nav class="tabs-navigation">
        <button 
          *ngFor="let tab of tabs"
          class="tab-btn"
          [class.active]="activeTab === tab.id"
          (click)="activeTab = tab.id">
          <span class="icon">{{ tab.icon }}</span>
          {{ tab.name }}
        </button>
      </nav>

      <!-- CONTENT -->
      <main class="dashboard-content">
        <app-sla-configuration-tab *ngIf="activeTab === 'configuration'"></app-sla-configuration-tab>
        <div *ngIf="activeTab === 'overview'" class="overview-tab">
          <p>Onglet Overview - À implémenter</p>
        </div>
        <div *ngIf="activeTab === 'details'" class="details-tab">
          <p>Onglet Details - À implémenter</p>
        </div>
        <div *ngIf="activeTab === 'reports'" class="reports-tab">
          <p>Onglet Reports - À implémenter</p>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .sla-dashboard {
      background: #f5f7fa;
      min-height: 100vh;
    }

    .dashboard-header {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      padding: 30px 40px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);

      .header-content h1 {
        margin: 0;
        font-size: 28px;
        font-weight: 700;
        display: flex;
        align-items: center;
        gap: 10px;

        .icon {
          font-size: 32px;
        }
      }

      .subtitle {
        margin: 6px 0 0 0;
        font-size: 14px;
        opacity: 0.9;
      }

      .header-actions {
        display: flex;
        gap: 15px;
        align-items: center;

        .search-box {
          padding: 10px 16px;
          border: none;
          border-radius: 6px;
          width: 250px;
          font-size: 14px;
          background: rgba(255, 255, 255, 0.9);

          &:focus {
            outline: none;
            background: white;
          }
        }

        .notification-btn {
          position: relative;
          background: none;
          border: none;
          color: white;
          font-size: 24px;
          cursor: pointer;
          padding: 8px;

          .badge {
            position: absolute;
            top: 0;
            right: 0;
            background: #ff4757;
            color: white;
            width: 20px;
            height: 20px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 11px;
            font-weight: 700;
          }
        }
      }
    }

    .tabs-navigation {
      background: white;
      border-bottom: 2px solid #e9ecef;
      padding: 0 40px;
      display: flex;
      gap: 30px;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);

      .tab-btn {
        padding: 16px 0;
        background: none;
        border: none;
        font-size: 15px;
        font-weight: 600;
        color: #666;
        cursor: pointer;
        position: relative;
        transition: all 0.3s ease;
        display: flex;
        align-items: center;
        gap: 8px;

        .icon {
          font-size: 18px;
        }

        &:hover {
          color: #2c3e50;
        }

        &.active {
          color: #667eea;

          &::after {
            content: '';
            position: absolute;
            bottom: -2px;
            left: 0;
            right: 0;
            height: 3px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
          }
        }
      }
    }

    .dashboard-content {
      padding: 30px 40px;
      max-width: 1400px;
      margin: 0 auto;
    }

    @media (max-width: 768px) {
      .dashboard-header {
        padding: 20px;
        flex-direction: column;
        gap: 20px;

        .header-actions {
          width: 100%;
          flex-direction: column;

          .search-box {
            width: 100%;
          }
        }
      }

      .tabs-navigation {
        padding: 0 20px;
        gap: 15px;
        overflow-x: auto;
      }

      .dashboard-content {
        padding: 20px;
      }
    }
  `]
})
export class SLADashboardComponent implements OnInit {
  activeTab = 'configuration';
  
  tabs: Tab[] = [
    { id: 'overview', name: 'Overview', icon: '📊' },
    { id: 'details', name: 'Details', icon: '📋' },
    { id: 'reports', name: 'Reports', icon: '📈' },
    { id: 'configuration', name: 'Configuration', icon: '⚙️' }
  ];

  ngOnInit(): void {
    // À implémenter
  }
}
