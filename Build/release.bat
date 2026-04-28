@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul
title LogViewer — Release Builder

:: ─────────────────────────────────────────────────────────────────────────────
::  Paths
:: ─────────────────────────────────────────────────────────────────────────────
set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%build.ps1"
set "VERSION_PROPS=%SCRIPT_DIR%version.props"
set "TMPNOTES=%TEMP%\logviewer_notes_%RANDOM%.txt"

:: ─────────────────────────────────────────────────────────────────────────────
::  Header
:: ─────────────────────────────────────────────────────────────────────────────
cls
echo.
echo  ============================================================
echo    LogViewer  -  Release Builder
echo  ============================================================
echo.

:: Read current version from version.props using PowerShell
for /f "delims=" %%v in ('powershell -NoProfile -Command ^
    "[xml]$x = Get-Content '%VERSION_PROPS%'; ^
     '{0}.{1}.{2}' -f $x.Project.PropertyGroup.VersionMajor, ^
                       $x.Project.PropertyGroup.VersionMinor, ^
                       $x.Project.PropertyGroup.VersionPatch" 2^>nul') do (
    set "CURRENT_VERSION=%%v"
)
if not defined CURRENT_VERSION set "CURRENT_VERSION=unknown"

echo    Current version : %CURRENT_VERSION%
echo.

:: ─────────────────────────────────────────────────────────────────────────────
::  Step 1 — Version number
:: ─────────────────────────────────────────────────────────────────────────────
echo  [ Step 1/4 ]  Version Number
echo  ------------------------------------------------------------
echo    Format: MAJOR.MINOR.PATCH   (e.g.  1.2.0  or  2.0.0)
echo.

:ask_version
set "VERSION="
set /p "VERSION=    New version: "

if not defined VERSION (
    echo    Please enter a version number.
    goto ask_version
)

:: Validate MAJOR.MINOR.PATCH with PowerShell (batch regex is unreliable)
powershell -NoProfile -Command ^
    "if ('%VERSION%' -notmatch '^\d+\.\d+\.\d+$') { exit 1 }" >nul 2>&1
if errorlevel 1 (
    echo.
    echo    Invalid format.  Expected MAJOR.MINOR.PATCH  e.g. 1.2.0
    echo.
    goto ask_version
)

echo.
echo    Version set to: %VERSION%

:: ─────────────────────────────────────────────────────────────────────────────
::  Step 2 — Release notes
:: ─────────────────────────────────────────────────────────────────────────────
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
    echo    No notes entered — Notepad will open for editing.
)

:: ─────────────────────────────────────────────────────────────────────────────
::  Step 3 — Build options
:: ─────────────────────────────────────────────────────────────────────────────
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

set "OPT_NOPUSH="
set /p "ANS_PUSH=    Push commit and tag to remote? [Y/n]: "
if /i "!ANS_PUSH!"=="n" (
    set "OPT_NOPUSH=-NoPush"
    echo    Push skipped — you can push manually afterwards.
) else (
    echo    Commit and tag will be pushed.
)

:: ─────────────────────────────────────────────────────────────────────────────
::  Step 4 — Confirm
:: ─────────────────────────────────────────────────────────────────────────────
echo.
echo  [ Step 4/4 ]  Confirm
echo  ============================================================
echo.
echo    Version  :  %VERSION%  (was %CURRENT_VERSION%)

if "!HAS_NOTES!"=="1" (
    echo    Notes    :
    for /f "delims=" %%l in ("%TMPNOTES%") do echo      %%l
) else (
    echo    Notes    :  [Notepad will open]
)

if defined OPT_NOBUILD (echo    Build    :  SKIPPED) else (echo    Build    :  Release)
if defined OPT_NOPUSH  (echo    Push     :  SKIPPED) else (echo    Push     :  Yes)

echo.
echo  ============================================================
echo.

set "CONFIRM="
set /p "CONFIRM=    Proceed with release? [Y/n]: "
if /i "!CONFIRM!"=="n" goto cancelled
if /i "!CONFIRM!"=="no" goto cancelled

:: ─────────────────────────────────────────────────────────────────────────────
::  Launch PowerShell script
:: ─────────────────────────────────────────────────────────────────────────────
echo.
echo  Starting build script...
echo  ============================================================
echo.

if "!HAS_NOTES!"=="1" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" ^
        -Version "%VERSION%" ^
        -NotesFile "%TMPNOTES%" ^
        !OPT_NOBUILD! !OPT_NOPUSH!
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" ^
        -Version "%VERSION%" ^
        !OPT_NOBUILD! !OPT_NOPUSH!
)

set "EXIT_CODE=%ERRORLEVEL%"

:: Cleanup temp notes file
if exist "%TMPNOTES%" del "%TMPNOTES%"

:: ─────────────────────────────────────────────────────────────────────────────
::  Result
:: ─────────────────────────────────────────────────────────────────────────────
echo.
echo  ============================================================
if "!EXIT_CODE!"=="0" (
    echo    SUCCESS — LogViewer v%VERSION% released.
) else (
    echo    FAILED — Exit code: !EXIT_CODE!
    echo    Check the output above for details.
)
echo  ============================================================
echo.
goto done

:cancelled
echo.
echo  ============================================================
echo    Cancelled — no changes made.
echo  ============================================================
echo.
if exist "%TMPNOTES%" del "%TMPNOTES%"

:done
pause
endlocal
