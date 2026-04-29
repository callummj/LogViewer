@echo off
setlocal EnableDelayedExpansion
title LogViewer Release Builder

:: ---------------------------------------------------------------------------
::  Paths
:: ---------------------------------------------------------------------------
set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%build.ps1"
set "VERSION_PROPS=%SCRIPT_DIR%version.props"
set "TMPNOTES=%TEMP%\lv_notes_%RANDOM%.txt"
set "TMPGETVER=%TEMP%\lv_getver_%RANDOM%.ps1"

:: ---------------------------------------------------------------------------
::  Header
:: ---------------------------------------------------------------------------
cls
echo.
echo  ============================================================
echo    LogViewer  -  Release Builder
echo  ============================================================
echo.

:: Write a tiny PS1 to read the version - avoids $-escaping issues in cmd
echo [xml]$x = Get-Content '%VERSION_PROPS%'; $x.Project.PropertyGroup.VersionMajor + '.' + $x.Project.PropertyGroup.VersionMinor + '.' + $x.Project.PropertyGroup.VersionPatch > "%TMPGETVER%"
set "CURRENT_VERSION=unknown"
for /f "usebackq delims=" %%v in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%TMPGETVER%" 2^>nul`) do set "CURRENT_VERSION=%%v"
del "%TMPGETVER%" >nul 2>&1

echo    Current version : %CURRENT_VERSION%
echo.

:: ---------------------------------------------------------------------------
::  Step 1 - Version number
:: ---------------------------------------------------------------------------
echo  [ Step 1/4 ]  Version Number
echo  ------------------------------------------------------------
echo    Format: MAJOR.MINOR.PATCH  (e.g. 1.2.0 or 2.0.0)
echo.

:ask_version
set "VERSION="
set /p "VERSION=    New version: "

if not defined VERSION (
    echo    Please enter a version number.
    goto ask_version
)

:: Validate format via PS - no $ needed, just -match on a literal string
powershell -NoProfile -Command "if ('%VERSION%' -notmatch '^\d+\.\d+\.\d+$') { exit 1 }" >nul 2>&1
if errorlevel 1 (
    echo.
    echo    Invalid format. Expected MAJOR.MINOR.PATCH  e.g. 1.2.0
    echo.
    goto ask_version
)

echo.
echo    Version set to: %VERSION%

:: ---------------------------------------------------------------------------
::  Step 2 - Release notes
:: ---------------------------------------------------------------------------
echo.
echo  [ Step 2/4 ]  Release Notes
echo  ------------------------------------------------------------
echo    Type your release notes, one line at a time.
echo    Press ENTER on a blank line when finished.
echo    Leave everything blank to open Notepad instead.
echo.

if exist "%TMPNOTES%" del "%TMPNOTES%"
set "HAS_NOTES=0"

:notes_loop
set "LINE="
set /p "LINE=    > "
if "!LINE!"=="" goto notes_done
echo !LINE!>> "%TMPNOTES%"
set "HAS_NOTES=1"
goto notes_loop

:notes_done

if "!HAS_NOTES!"=="0" (
    echo.
    echo    No notes entered - Notepad will open for editing.
)

:: ---------------------------------------------------------------------------
::  Step 3 - Build options
:: ---------------------------------------------------------------------------
echo.
echo  [ Step 3/4 ]  Build Options
echo  ------------------------------------------------------------
echo.

set "OPT_NOBUILD="
set /p "ANS_BUILD=    Run Release build? [Y/n]: "
if /i "!ANS_BUILD!"=="n" (
    set "OPT_NOBUILD=-NoBuild"
    echo    Build skipped.
) else (
    echo    Release build will run.
)

echo.

set "OPT_NOINSTALLER="
set /p "ANS_INSTALLER=    Build installer? [Y/n]: "
if /i "!ANS_INSTALLER!"=="n" (
    set "OPT_NOINSTALLER=-NoInstaller"
    echo    Installer skipped.
) else (
    echo    Installer will be built ^(requires Inno Setup 6^).
)

echo.

set "OPT_NOPUSH="
set /p "ANS_PUSH=    Push commit and tag to remote? [Y/n]: "
if /i "!ANS_PUSH!"=="n" (
    set "OPT_NOPUSH=-NoPush"
    echo    Push skipped - you can push manually afterwards.
) else (
    echo    Commit and tag will be pushed.
)

:: ---------------------------------------------------------------------------
::  Step 4 - Confirm
:: ---------------------------------------------------------------------------
echo.
echo  [ Step 4/4 ]  Confirm
echo  ============================================================
echo.
echo    Version  :  %VERSION%  (was %CURRENT_VERSION%)

if "!HAS_NOTES!"=="1" (
    echo    Notes    :
    for /f "usebackq delims=" %%l in ("%TMPNOTES%") do echo      %%l
) else (
    echo    Notes    :  [Notepad will open]
)

if defined OPT_NOBUILD      (echo    Build     :  SKIPPED) else (echo    Build     :  Release ^(publish win-x64^))
if defined OPT_NOINSTALLER  (echo    Installer :  SKIPPED) else (echo    Installer :  Yes ^(Inno Setup^))
if defined OPT_NOPUSH       (echo    Push      :  SKIPPED) else (echo    Push      :  Yes)

echo.
echo  ============================================================
echo.

set "CONFIRM="
set /p "CONFIRM=    Proceed with release? [Y/n]: "
if /i "!CONFIRM!"=="n" goto cancelled
if /i "!CONFIRM!"=="no" goto cancelled

:: ---------------------------------------------------------------------------
::  Call PowerShell build script
:: ---------------------------------------------------------------------------
echo.
echo  Starting build script...
echo  ============================================================
echo.

if "!HAS_NOTES!"=="1" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" ^
        -Version "%VERSION%" ^
        -NotesFile "%TMPNOTES%" ^
        !OPT_NOBUILD! !OPT_NOINSTALLER! !OPT_NOPUSH!
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" ^
        -Version "%VERSION%" ^
        !OPT_NOBUILD! !OPT_NOINSTALLER! !OPT_NOPUSH!
)

set "EXIT_CODE=%ERRORLEVEL%"
if exist "%TMPNOTES%" del "%TMPNOTES%"

:: ---------------------------------------------------------------------------
::  Result
:: ---------------------------------------------------------------------------
echo.
echo  ============================================================
if "!EXIT_CODE!"=="0" (
    echo    SUCCESS  -  LogViewer v%VERSION% released.
) else (
    echo    FAILED  -  Exit code: !EXIT_CODE!
    echo    Check the output above for details.
)
echo  ============================================================
echo.
goto done

:cancelled
echo.
echo  ============================================================
echo    Cancelled - no changes made.
echo  ============================================================
echo.
if exist "%TMPNOTES%" del "%TMPNOTES%"

:done
del "%TMPGETVER%" >nul 2>&1
pause
endlocal
