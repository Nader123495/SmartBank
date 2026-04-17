# 🤖 SmartBank — Workflows n8n
### Automatisation Intelligente de la Gestion des Réclamations — STB Bank

---

## 📋 Vue d'ensemble

Ces workflows **n8n** automatisent les processus critiques de la plateforme SmartBank conformément au cahier des charges. Ils s'intègrent avec l'API **.NET Core**, la base de données **SQL Server**, et le frontend **Angular**.

```
SmartBank Architecture n8n
─────────────────────────────────────────────────────────────
  Angular Frontend
       │
       ▼
  .NET Core Web API  ◄──────────── n8n (Webhooks / Planificateur)
       │                                  │
       ▼                                  │
  SQL Server DB ◄────────────────────────┘
       │
  Power BI Dashboard
```

---

## 📁 Fichiers des Workflows

| Fichier | Workflow | Déclencheur |
|---------|----------|-------------|
| `workflow1_nouvelle_reclamation.json` | Traitement Nouvelle Réclamation | Webhook POST |
| `workflow2_sla_escalade.json` | Surveillance SLA & Escalade | Planifié (toutes les heures) |
| `workflow3_statut_notifications.json` | Changement de Statut & Notifications | Webhook POST |

---

## 🔄 Workflow 1 — Traitement Nouvelle Réclamation

**Déclencheur :** `POST /webhook/complaint/new`

### Flux du processus

```
[Webhook]
    │
    ▼
[Valider & Enrichir Données]
    │  - Contrôle des champs obligatoires
    │  - Calcul priorité selon type (Carte=24h, Digital=12h...)
    │  - Génération référence STB-XXXXXXXXXX
    ▼
[Insérer Réclamation SQL Server]
    │  INSERT INTO Complaints (...)
    ▼
[Récupérer Charge des Agents]
    │  SELECT agents par charge + spécialité
    ▼
[Sélectionner Meilleur Agent] ← Algorithme Intelligent
    │  - Tri par spécialité correspondante
    │  - Tri par charge de travail
    ▼
[Mettre à Jour BDD + SLA]
    │  UPDATE Complaints → statut "Assignée"
    │  INSERT Assignments
    │  INSERT ComplaintStatusHistory
    │  INSERT SLA (timer démarré)
    │  INSERT AuditLogs
    ├──────────────────────────────────┐
    ▼                                 ▼
[Email HTML à l'Agent]      [Push Notification API]
    │                                 │
    └──────────────┬──────────────────┘
                   ▼
          [Réponse JSON 201]
```

### Corps de la requête

```json
{
  "clientId": 123,
  "type": "Carte",
  "description": "Carte bloquée sans raison",
  "canal": "e-banking",
  "agenceId": 5,
  "priorite": "Haute"
}
```

### Réponse

```json
{
  "success": true,
  "data": {
    "complaintId": 456,
    "refReclamation": "STB-1712867234521",
    "statut": "Assignée",
    "agentAssigne": "Mohamed Ben Ali",
    "slaDueDate": "2026-04-12T10:00:00Z",
    "slaHeures": 24
  }
}
```

### Mapping SLA par Type de Réclamation

| Type | SLA | Priorité Auto |
|------|-----|---------------|
| Virement | 8h | Critique |
| Digital Banking | 12h | Critique |
| Carte | 24h | Haute |
| Compte | 24h | Haute |
| Crédit | 48h | Moyenne |
| Autre | 72h | Basse |

---

## ⏱️ Workflow 2 — Surveillance SLA & Escalade Automatique

**Déclencheur :** Planificateur Cron — `0 * * * *` *(toutes les heures)*

### Flux du processus

```
[⏰ Planificateur - Chaque Heure]
    │
    ▼
[Réclamations SLA Critiques] ← SQL Server
    │  Toutes réclamations ouvertes avec SLA < +2h
    ▼
[Réclamations Trouvées ?]
    ├── NON → [Aucune Alerte SLA → Log OK]
    │
    └── OUI ▼
[Analyser Niveau d'Escalade]
    │
    │  AVERTISSEMENT  → < 1h restante  → Alerter l'Agent
    │  ORANGE         → Retard < 8h   → Escalade Responsable Agence
    │  ROUGE          → Retard 8-24h  → Escalade Responsable Département
    │  CRITIQUE       → Retard > 24h  → Escalade Direction Générale
    ▼
[Récupérer Responsables]
    │  SELECT Users par rôle + agence
    ▼
[Mettre à Jour Escalade BDD]
    │  UPDATE SLA.NiveauEscalade
    │  UPDATE Complaints.Statut = 'Escaladée' (si ROUGE/CRITIQUE)
    │  INSERT AuditLogs
    ├──────────────────────────┐
    ▼                         ▼
[Email Escalade HTML]  [Push Notification]
    │                         │
    └────────────┬────────────┘
                 ▼
    [Rapport Vérification SLA → Log]
```

### Niveaux d'Escalade

| Niveau | Condition | Action | Destinataire |
|--------|-----------|--------|--------------|
| ⚡ AVERTISSEMENT | < 1h restante | Rappel agent | Agent assigné |
| 🟠 ORANGE | Retard < 8h | Escalade | Responsable Agence |
| 🔴 ROUGE | Retard 8-24h | Escalade urgente | Responsable Département |
| 🚨 CRITIQUE | Retard > 24h | Escalade critique | Direction Générale |

---

## 🔔 Workflow 3 — Changement de Statut & Notifications

**Déclencheur :** `POST /webhook/complaint/status-update`

### Flux du processus

```
[Webhook - Changement Statut]
    │
    ▼
[Valider Transition Statut]
    │  Matrice des transitions autorisées :
    │  Nouvelle   → Assignée | Rejetée
    │  Assignée   → En cours | Rejetée
    │  En cours   → Validation | Assignée | Rejetée
    │  Validation → Clôturée | En cours | Rejetée
    │  Escaladée  → En cours | Validation | Clôturée
    ▼
[Récupérer Détails Réclamation] ← SQL Server
    │  Client, Agent, Agence, SLA...
    ▼
[Mettre à Jour Statut BDD]
    │  UPDATE Complaints
    │  INSERT ComplaintStatusHistory
    │  UPDATE SLA (si Clôturée/Rejetée)
    │  INSERT AuditLogs
    ▼
[Switch — Type de Notification]
    ├── Clôturée   → [Email Client ✅ Résolution]
    ├── Rejetée    → [Email Client ❌ Rejet + Justification]
    ├── En cours   → [Email Agent 🔄 Démarrage]
    └── Validation → [Email Responsable 📋 Action Requise]
                 │
                 ▼
    [SignalR → Mise à Jour Temps Réel Angular]
                 │
                 ▼
         [Réponse JSON 200]
```

### Corps de la requête

```json
{
  "complaintId": 456,
  "ancienStatut": "En cours",
  "nouveauStatut": "Validation",
  "userId": 12,
  "commentaire": "Réclamation traitée, en attente validation responsable",
  "justification": ""
}
```

> ⚠️ Si `nouveauStatut = "Rejetée"`, le champ `justification` est **obligatoire**.

---

## ⚙️ Installation & Configuration

### Prérequis

- **n8n** version ≥ 1.30.0
- **SQL Server** accessible par n8n
- Serveur **SMTP** (pour l'envoi des emails)
- **API .NET Core** SmartBank démarrée

### Étapes d'installation

#### 1. Installer n8n (via npm)

```bash
npm install -g n8n
n8n start
```

Ou via **Docker** :

```bash
docker run -it --rm \
  --name n8n \
  -p 5678:5678 \
  -v ~/.n8n:/home/node/.n8n \
  n8nio/n8n
```

#### 2. Accéder à n8n

Ouvrir le navigateur : `http://localhost:5678`

#### 3. Importer les Workflows

1. Aller dans **Workflows** → **Import from file**
2. Importer les 3 fichiers JSON dans l'ordre :
   - `workflow1_nouvelle_reclamation.json`
   - `workflow2_sla_escalade.json`
   - `workflow3_statut_notifications.json`

#### 4. Configurer les Credentials

Dans **Settings** → **Credentials**, créer :

**a) SmartBank SQL Server (MSSQL)**
```
Name:     SmartBank SQL Server
Host:     localhost (ou IP du serveur)
Database: SmartBankDB
User:     sa
Password: [votre mot de passe]
Port:     1433
```

**b) SmartBank SMTP**
```
Name:     SmartBank SMTP
Host:     smtp.stbbank.com.tn (ou smtp.gmail.com)
Port:     587
User:     noreply@stbbank.com.tn
Password: [mot de passe email]
TLS:      true
```

#### 5. Variables d'environnement

Créer un fichier `.env` dans le répertoire n8n :

```env
SMARTBANK_API_TOKEN=votre_jwt_token_service_account
N8N_HOST=localhost
N8N_PORT=5678
```

#### 6. Activer les Workflows

Activer chaque workflow en cliquant sur le toggle **Active** dans l'interface n8n.

---

## 🔗 Intégration avec l'API .NET Core

Dans votre contrôleur Angular, appelez les webhooks n8n :

```typescript
// complaint.service.ts

// Créer une réclamation via n8n
createComplaint(data: ComplaintDto): Observable<any> {
  const n8nWebhook = 'http://localhost:5678/webhook/complaint/new';
  return this.http.post(n8nWebhook, data);
}

// Mettre à jour le statut via n8n
updateStatus(update: StatusUpdateDto): Observable<any> {
  const n8nWebhook = 'http://localhost:5678/webhook/complaint/status-update';
  return this.http.post(n8nWebhook, update);
}
```

Ou appeler depuis votre **API .NET Core** :

```csharp
// ComplaintsController.cs
[HttpPost]
public async Task<IActionResult> CreateComplaint([FromBody] CreateComplaintDto dto)
{
    // ... logique métier ...
    
    // Déclencher le workflow n8n
    await _n8nService.TriggerWorkflow("complaint/new", dto);
    
    return Ok();
}
```

---

## 📊 Tables SQL Server Utilisées

| Table | Workflow 1 | Workflow 2 | Workflow 3 |
|-------|:----------:|:----------:|:----------:|
| `Complaints` | INSERT + UPDATE | SELECT + UPDATE | SELECT + UPDATE |
| `Users` | SELECT | SELECT | SELECT |
| `Assignments` | INSERT | — | — |
| `SLA` | INSERT | SELECT + UPDATE | UPDATE |
| `ComplaintStatusHistory` | INSERT | — | INSERT |
| `AuditLogs` | INSERT | INSERT | INSERT |
| `Notifications` | — | — | — |
| `Agencies` | — | SELECT | SELECT |
| `Clients` | — | — | SELECT |

---

## 🎯 Valeur Ajoutée pour la Soutenance

Ces workflows n8n démontrent :

1. **🤖 Automatisation intelligente** — Attribution automatique basée sur charge + spécialité
2. **⏱️ SLA proactif** — Escalade multi-niveaux avant violation
3. **🔔 Notifications multicanal** — Email HTML + Push + SignalR temps réel
4. **🛡️ Sécurité** — Validation des transitions de statut + Audit Trail complet
5. **📈 Scalabilité** — Architecture event-driven extensible

---

*Projet PFE — ISIM Mahdia 2026 | SmartBank — STB Bank*
*Workflows n8n conçus comme valeur ajoutée au cahier des charges*
