@echo off
chcp 65001 >nul
cd /d "%~dp0"

if not exist ".env" (
  echo [Erreur] Fichier .env introuvable. Copiez .env.example vers .env.
  pause
  exit /b 1
)

echo Démarrage SmartBank (SQL Server + API + site web)...
echo.
docker compose up -d --build
if errorlevel 1 (
  echo.
  echo Échec. Vérifiez que Docker Desktop est démarré.
  pause
  exit /b 1
)

echo.
echo Conteneurs démarrés.
echo.
echo Première fois uniquement — initialiser la base dans PowerShell :
echo   .\docker\init-db.ps1
echo.
echo Ensuite ouvrez : http://localhost:8080
echo API : http://localhost:5000
echo.
pause
