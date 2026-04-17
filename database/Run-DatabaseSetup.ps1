# SmartBank - Script de création et peuplement de la base de données
# Prérequis : SQL Server (LocalDB ou SQLEXPRESS) avec sqlcmd disponible
# Exécution : .\Run-DatabaseSetup.ps1   ou   powershell -ExecutionPolicy Bypass -File .\Run-DatabaseSetup.ps1

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$DbFolder = Join-Path $ProjectRoot "database"
$Script1 = Join-Path $DbFolder "01_CreateDatabase.sql"
$Script2 = Join-Path $DbFolder "02_SeedData.sql"
$Script3 = Join-Path $DbFolder "03_AddEmailVerification.sql"

# Instance SQL Server (modifier si besoin : (localdb)\MSSQLLocalDB ou .\SQLEXPRESS)
$ServerInstance = ".\SQLEXPRESS"

if (-not (Test-Path $Script1)) { Write-Error "Fichier introuvable: $Script1"; exit 1 }
if (-not (Test-Path $Script2)) { Write-Error "Fichier introuvable: $Script2"; exit 1 }
if (-not (Test-Path $Script3)) { Write-Error "Fichier introuvable: $Script3"; exit 1 }

Write-Host "=== SmartBank - Configuration base de donnees ===" -ForegroundColor Cyan
Write-Host "Serveur: $ServerInstance" -ForegroundColor Gray
Write-Host ""

# 1) Créer la base et les tables
Write-Host "1/3 Execution de 01_CreateDatabase.sql ..." -ForegroundColor Yellow
try {
    # -C : faire confiance au certificat du serveur (ODBC Driver 18 active le chiffrement par defaut)
    sqlcmd -S $ServerInstance -C -i $Script1
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd a retourne un code erreur" }
    Write-Host "   OK." -ForegroundColor Green
} catch {
    Write-Host "   ERREUR: $_" -ForegroundColor Red
    Write-Host "   Si sqlcmd n'est pas reconnu, executez les scripts manuellement dans SSMS." -ForegroundColor Gray
    exit 1
}

# 2) Données de test
Write-Host "2/3 Execution de 02_SeedData.sql ..." -ForegroundColor Yellow
try {
    sqlcmd -S $ServerInstance -C -d SmartBankDB -i $Script2
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd a retourne un code erreur" }
    Write-Host "   OK." -ForegroundColor Green
} catch {
    Write-Host "   ERREUR: $_" -ForegroundColor Red
    exit 1
}

# 3) Vérification email (colonnes)
Write-Host "3/3 Execution de 03_AddEmailVerification.sql ..." -ForegroundColor Yellow
try {
    sqlcmd -S $ServerInstance -C -d SmartBankDB -i $Script3
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd a retourne un code erreur" }
    Write-Host "   OK." -ForegroundColor Green
} catch {
    Write-Host "   ERREUR: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Base SmartBankDB prete. Comptes de test : admin@stb.tn, responsable@stb.tn, agent1@stb.tn (mot de passe: Admin@2025)" -ForegroundColor Green
