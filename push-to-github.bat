@echo off
setlocal EnableDelayedExpansion
title GitHub Auto Push
color 0A

echo =====================================================
echo            GitHub Auto Push
echo =====================================================
echo.

:: ตรวจสอบ Git
where git >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Git is not installed.
    echo Download: https://git-scm.com/downloads
    pause
    exit
)

:: ถ้ายังไม่มี .git ให้สร้าง
if not exist ".git" (
    echo Creating Git Repository...
    git init
)

echo.
set REPO=https://github.com/varinsaeleung/KM-AI-Workflow-OCR.git


echo.
set /p MSG=Commit Message :

if "%MSG%"=="" (
    set MSG=Update %date% %time%
)

echo.
echo ==========================
echo Adding files...
echo ==========================
git add .

echo.
echo ==========================
echo Commit...
echo ==========================
git commit -m "%MSG%"

:: ตรวจสอบว่ามี origin หรือยัง
git remote | findstr origin >nul

if %errorlevel%==0 (
    git remote set-url origin %REPO%
) else (
    git remote add origin %REPO%
)

git branch -M main

echo.
echo ==========================
echo Uploading...
echo ==========================
git push -u origin main

echo.
if %errorlevel%==0 (
    echo =====================================
    echo         Upload Success
    echo =====================================
) else (
    echo =====================================
    echo         Upload Failed
    echo =====================================
)

pause