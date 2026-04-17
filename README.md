# 🏦 SmartBank - Intelligent Complaint Management Platform

[![Technology](https://img.shields.io/badge/Tech-Angular%2017%20%2B%20.NET%208-blue.svg)](https://angular.io/)
[![License](https://img.shields.io/badge/License-Private-red.svg)](#)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)](https://www.docker.com/)

SmartBank is a professional, full-stack enterprise solution designed to streamline banking complaint management. It integrates modern web technologies, AI-driven classification, and automated workflows to ensure high-quality customer service and SLA compliance.

---

## 🚀 Key Features

- **🛡️ Secure Authentication**: Multi-role system (Client, Agent, Admin) with JWT-based security.
- **📝 Complaint Lifecycle**: Full tracking from creation to resolution with real-time updates via SignalR.
- **🤖 AI Integration**: Automated classification and sentiment analysis of complaints using Ollama.
- **⚡ Workflow Automation**: Orchestrated backend processes using n8n.
- **📊 Interactive Dashboard**: Real-time analytics, status tracking, and SLA monitoring.
- **📍 Geolocation**: Map integration using Leaflet for branch/incident location management.
- **✉️ Automated Notifications**: Email alerts via Mailhog and in-app notifications.

---

## 🛠️ Architecture & Tech Stack

### Backend (.NET 8)
- **Pattern**: Clean Architecture (Domain, Application, Infrastructure, API).
- **ORM**: Entity Framework Core with SQL Server.
- **Validation**: FluentValidation for robust data integrity.
- **Real-time**: SignalR for live updates.
- **Auth**: JWT Bearer Authentication.

### Frontend (Angular 17)
- **Framework**: Angular 17 with Standalone Components.
- **Styling**: Modern SCSS with advanced CSS Grid/Flexbox.
- **Icons**: Lucide Angular.
- **Maps**: Leaflet.js.

### Infrastructure & Automation
- **Orchestration**: Docker & Docker Compose.
- **Workflows**: n8n integration.
- **Testing Mail**: Mailhog.

---

## 📂 Project Structure

```text
SmartBank/
├── backend/            # .NET 8 Web API (Clean Architecture)
├── frontend/           # Angular 17 SPA
├── n8n-workflows/      # Automation workflows & Python scripts
├── database/           # SQL initialization scripts
├── conception/         # 20+ UML & Design Diagrams (PNG)
├── docker/             # Docker configuration files
└── docker-compose.yml  # Main orchestration file
```

---

## 🔧 Installation & Setup

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js (v18+)](https://nodejs.org/)

### 1. Environment Configuration
Copy the `.env.example` file to `.env` and update the secrets:
```bash
cp .env.example .env
```

### 2. Quick Start with Docker
The easiest way to run the entire stack:
```powershell
./start-docker.cmd
```

### 3. Manual Development Setup

**Backend:**
```bash
cd backend/SmartBank.API
dotnet run
```

**Frontend:**
```bash
cd frontend
npm install
npm start
```

---

## 🖼️ Conception & Design
The project includes a comprehensive `/conception` directory containing:
- **UML Diagrams**: Class diagrams (Entities, DTOs, Audit, IA).
- **Sequence Diagrams**: Authentication, Complaint Assignment, SLA Verification.
- **Use Case Diagrams**: General system administration and client portal.

---

## 🛠️ Maintenance & Scripts
- `push-to-github.cmd`: Helper script to push changes to your private GitHub repo.
- `start-docker.cmd`: Automated orchestration of services.

---

## 📄 License
This project is private and confidential. All rights reserved.
