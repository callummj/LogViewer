<#
.SYNOPSIS
    Builds LogViewer, stamps the version, and creates an annotated git tag.

.PARAMETER Version
    Version string in MAJOR.MINOR.PATCH format. Required.

.PARAMETER Notes
    Release notes as a string. If omitted the script opens Notepad for you to write them.

.PARAMETER NotesFile
    Path to a plain-text file containing the release notes.

.PARAMETER NoBuild
    Skip dotnet build.

.PARAMETER NoPush
    Do not push the commit or tag to the remote. Tag is created locally only.

.EXAMPLE
    .\build.ps1 -Version 1.2.0
    .\build.ps1 -Version 1.2.0 -Notes "Fixed crash on startup"
    .\build.ps1 -Version 1.2.0 -NotesFile .\notes.txt -NoPush
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version,

    [string] $Notes     = "",
    [string] $NotesFile = "",
    [switch] $NoBuild,
    [switch] $NoPush
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Write-Step { param([string]$Msg) Write-Host "`n==> $Msg" -ForegroundColor Cyan }
function Write-Ok   { param([string]$Msg) Write-Host "    [OK] $Msg" -ForegroundColor Green }
function Write-Warn { param([string]$Msg) Write-Host "    [!!] $Msg" -ForegroundColor Yellow }
function Write-Fail { param([string]$Msg) Write-Host "    [FAIL] $Msg" -ForegroundColor Red }

function Invoke-Cmd {
    param([string]$Exe, [string[]]$CmdArgs)
    & $Exe @CmdArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Fail "'$Exe $($CmdArgs -join ' ')' exited with code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

$RepoRoot      = Resolve-Path "$PSScriptRoot\.."
$VersionProps  = Join-Path $PSScriptRoot "version.props"
$CsprojPath    = Join-Path $RepoRoot "LogViewerApp\LogViewerApp.csproj"
$ChangelogPath = Join-Path $RepoRoot "CHANGELOG.md"

# ---------------------------------------------------------------------------
# Parse version
# ---------------------------------------------------------------------------

$parts   = $Version.Split('.')
$Major   = $parts[0]
$Minor   = $parts[1]
$Patch   = $parts[2]
$TagName = "v$Version"

Write-Step "Preparing release $TagName"

$existingTag = git -C $RepoRoot tag --list $TagName 2>$null
if ($existingTag) {
    Write-Fail "Git tag '$TagName' already exists. Delete it first with: git tag -d $TagName"
    exit 1
}

# ---------------------------------------------------------------------------
# Collect release notes
# ---------------------------------------------------------------------------

Write-Step "Collecting release notes"

if ($NotesFile -ne "" -and (Test-Path $NotesFile)) {
    $Notes = Get-Content $NotesFile -Raw
    Write-Ok "Read notes from $NotesFile"
}

if ($Notes.Trim() -eq "") {
    $tempFile = [System.IO.Path]::GetTempFileName()
    $template = "# Release Notes -- LogViewer $TagName`r`n# Lines starting with '#' are ignored. Save and close Notepad when done.`r`n`r`n"
    Set-Content -Path $tempFile -Value $template -Encoding UTF8
    Write-Host "    Opening Notepad for release notes -- save and close when done..." -ForegroundColor Yellow
    Start-Process -FilePath "notepad.exe" -ArgumentList $tempFile -Wait
    $rawLines = Get-Content $tempFile -Encoding UTF8
    Remove-Item $tempFile -Force
    $Notes = ($rawLines | Where-Object { $_ -notmatch '^\s*#' }) -join "`n"
    $Notes = $Notes.Trim()
}

if ($Notes.Trim() -eq "") {
    Write-Warn "No release notes provided -- using default message."
    $Notes = "Release $TagName"
}

Write-Ok "Release notes ready ($($Notes.Length) chars)"

# ---------------------------------------------------------------------------
# Update version.props
# ---------------------------------------------------------------------------

Write-Step "Updating Build\version.props to $Version"

[xml]$props = Get-Content $VersionProps
$pg = $props.Project.PropertyGroup
$pg.VersionMajor = $Major
$pg.VersionMinor = $Minor
$pg.VersionPatch = $Patch

$xmlSettings                    = New-Object System.Xml.XmlWriterSettings
$xmlSettings.Indent             = $true
$xmlSettings.IndentChars        = "  "
$xmlSettings.Encoding           = [System.Text.Encoding]::UTF8
$xmlSettings.OmitXmlDeclaration = $false

$writer = [System.Xml.XmlWriter]::Create($VersionProps, $xmlSettings)
$props.Save($writer)
$writer.Close()

Write-Ok "version.props updated"

# ---------------------------------------------------------------------------
# Update CHANGELOG.md
# ---------------------------------------------------------------------------

Write-Step "Updating CHANGELOG.md"

$date     = Get-Date -Format "yyyy-MM-dd"
$newEntry = "## [$Version] -- $date`n`n$Notes`n"

if (Test-Path $ChangelogPath) {
    $existing = Get-Content $ChangelogPath -Raw
    if ($existing -match '^# ') {
        $firstNewline = $existing.IndexOf("`n")
        $header       = $existing.Substring(0, $firstNewline + 1)
        $rest         = $existing.Substring($firstNewline + 1).TrimStart()
        $updated      = "$header`n$newEntry`n$rest"
    } else {
        $updated = "$newEntry`n$existing"
    }
    Set-Content -Path $ChangelogPath -Value $updated -Encoding UTF8 -NoNewline
} else {
    $header = "# Changelog`n`nAll notable changes to LogViewer are documented here.`n"
    Set-Content -Path $ChangelogPath -Value "$header`n$newEntry" -Encoding UTF8
}

Write-Ok "CHANGELOG.md updated"

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------

if (-not $NoBuild) {
    Write-Step "Building Release configuration"
    Invoke-Cmd dotnet @("build", $CsprojPath, "-c", "Release", "--nologo", "-v", "minimal")
    Write-Ok "Build succeeded"
} else {
    Write-Warn "Build skipped (-NoBuild)"
}

# ---------------------------------------------------------------------------
# Git: commit + tag
# ---------------------------------------------------------------------------

Write-Step "Creating git commit and tag"

Invoke-Cmd git @("-C", $RepoRoot, "add", $VersionProps, $ChangelogPath)

$commitMsg = "chore: release $TagName"
Invoke-Cmd git @("-C", $RepoRoot, "commit", "-m", $commitMsg)
Write-Ok "Committed: $commitMsg"

$tagMsg = "LogViewer $TagName`n`n$Notes"
Invoke-Cmd git @("-C", $RepoRoot, "tag", "-a", $TagName, "-m", $tagMsg)
Write-Ok "Created annotated tag $TagName"

# ---------------------------------------------------------------------------
# Push
# ---------------------------------------------------------------------------

if (-not $NoPush) {
    $remote = git -C $RepoRoot remote 2>$null | Select-Object -First 1
    if ($remote) {
        Write-Step "Pushing to '$remote'"
        Invoke-Cmd git @("-C", $RepoRoot, "push", $remote)
        Invoke-Cmd git @("-C", $RepoRoot, "push", $remote, $TagName)
        Write-Ok "Pushed $TagName to $remote"
    } else {
        Write-Warn "No git remote configured -- skipping push."
    }
} else {
    Write-Warn "Push skipped (-NoPush). Run when ready:"
    Write-Host "      git push origin" -ForegroundColor DarkGray
    Write-Host "      git push origin $TagName" -ForegroundColor DarkGray
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "  Released: LogViewer $TagName" -ForegroundColor Green
Write-Host "  Tag:      $TagName"
Write-Host "  Notes:"
$Notes -split "`n" | ForEach-Object { Write-Host "    $_" }
Write-Host ""
