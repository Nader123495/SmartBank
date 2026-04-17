@echo off
echo Demarrage de l'API SmartBank sur http://localhost:5000 ...
echo.
cd /d "%~dp0SmartBank.API"
dotnet run
pause
