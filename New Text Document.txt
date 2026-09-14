@echo off
title GitHub Push Tool
color 0A

echo ==========================================
echo           GitHub Push Tool
echo ==========================================
echo.

git --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Git is not installed.
    pause
    exit
)

echo Current Folder:
echo %cd%
echo.

set /p REPO=GitHub Repository URL :
echo.

git init

git add .

set /p MSG=Commit Message :

git commit -m "%MSG%"

git remote remove origin >nul 2>&1

git remote add origin %REPO%

git branch -M main

echo.
echo ===== Uploading to GitHub =====
git push -u origin main

echo.
echo ===============================
echo Finished
echo ===============================
pause