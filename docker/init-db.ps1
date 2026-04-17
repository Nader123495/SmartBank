# Initialise SmartBankDB dans le conteneur SQL Server (une fois, base vide).
# Prérequis : docker compose up -d ; fichier .env avec MSSQL_SA_PASSWORD
# Usage : .\docker\init-db.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

$sqlFile = Join-Path $root "database\01_CreateDatabase.sql"
if (-not (Test-Path $sqlFile)) {
    Write-Error "Fichier introuvable : $sqlFile"
}

$pwd = $env:MSSQL_SA_PASSWORD
if (-not $pwd) {
    Write-Error "Définissez MSSQL_SA_PASSWORD (fichier .env à la racine du projet ou variable d'environnement)."
}

docker cp $sqlFile smartbank-sql:/tmp/init.sql
docker exec smartbank-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $pwd -C -i /tmp/init.sql
if ($LASTEXITCODE -ne 0) {
    Write-Error "sqlcmd a échoué. Vérifiez le mot de passe SA et que le conteneur smartbank-sql tourne."
}
Write-Host "Base initialisée."
