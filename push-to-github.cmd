@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo ==========================================
echo    SmartBank - Push to GitHub (Private)
echo ==========================================
echo.

git --version >nul 2>&1
if errorlevel 1 (
  echo [ERROR] Git is not installed or not in PATH.
  echo Download Git: https://git-scm.com/download/win
  pause
  exit /b 1
)

if not exist .git (
  echo Initializing local repository...
  git init
  git branch -M main
)

echo Adding files to git...
git add .

echo Creating commit...
git commit -m "Full project documentation and setup - SmartBank"

echo.
echo IMPORTANT: Make sure you have created a PRIVATE repository named 'SmartBank' 
echo on your GitHub account (https://github.com/new).
echo.

set /p GITHUB_USER="Enter your GitHub username: "
if "%GITHUB_USER%"=="" (
  echo Cancelled.
  pause
  exit /b 0
)

set REMOTE_URL=https://github.com/%GITHUB_USER%/SmartBank.git

git remote remove origin 2>nul
git remote add origin %REMOTE_URL%

echo.
echo Preparing to push to %REMOTE_URL%...
git push -u origin main

if errorlevel 1 (
  echo.
  echo [ERROR] Push failed. 
  echo Possible reasons:
  echo 1. The repository 'SmartBank' does not exist on your account.
  echo 2. You don't have permission (try logging in via Git Credential Manager).
  echo 3. The remote URL is incorrect.
  echo.
  echo Re-run this script after checking your GitHub repository.
) else (
  echo.
  echo [SUCCESS] Project pushed successfully to GitHub!
)

echo.
pause
