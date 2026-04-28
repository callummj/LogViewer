; ---------------------------------------------------------------------------
;  LogViewer Inno Setup Script
;  Compile with:  ISCC.exe /DMyAppVersion=1.0.0 LogViewer.iss
; ---------------------------------------------------------------------------

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#define MyAppName        "LogViewer"
#define MyAppPublisher   "LogViewerApp"
#define MyAppExeName     "LogViewerApp.exe"
#define MyAppId          "{A3F2C1D9-8B4E-4F7A-9C2D-1E5F6A7B8C9D}"
#define DotNetMajor      "10"
#define DotNetDownloadUrl "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe"

; Published output must exist before compiling — build-installer.ps1 creates it
#define PublishDir "..\Build\Output\publish"

[Setup]
AppId={{#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com
VersionInfoVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline dialog
OutputDir=..\Build\Output
OutputBaseFilename=LogViewer-{#MyAppVersion}-Setup
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardResizable=yes
DisableProgramGroupPage=yes
UninstallDisplayName={#MyAppName} {#MyAppVersion}
UninstallDisplayIcon={app}\{#MyAppExeName}
ChangesAssociations=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: desktopicon;  Description: "Create a &desktop shortcut";  GroupDescription: "Additional shortcuts:"
Name: startmenuicon; Description: "Create a &Start Menu shortcut"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}";         Filename: "{app}\{#MyAppExeName}"; Tasks: startmenuicon
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"; Tasks: startmenuicon
Name: "{autodesktop}\{#MyAppName}";   Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; \
  Description: "Launch {#MyAppName}"; \
  Flags: nowait postinstall skipifsilent

; ---------------------------------------------------------------------------
[Code]

var
  DotNetPage: TDownloadWizardPage;

// ---------------------------------------------------------------------------
// Check whether .NET 10 Desktop Runtime (any 10.x patch) is present.
// Checks both Program Files paths (x64 and x86 host).
// ---------------------------------------------------------------------------
function IsDotNetDesktopRuntimeInstalled(): Boolean;
var
  FindRec: TFindRec;
  BasePath: String;
begin
  Result := False;

  // 64-bit .NET install location
  BasePath := ExpandConstant('{pf}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if DirExists(BasePath) then
  begin
    if FindFirst(BasePath + '\{#DotNetMajor}.*', FindRec) then
    begin
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Result := True;
          Break;
        end;
      until not FindNext(FindRec);
      FindClose(FindRec);
    end;
  end;

  if Result then Exit;

  // 32-bit .NET install on 64-bit Windows
  BasePath := ExpandConstant('{pf32}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if DirExists(BasePath) then
  begin
    if FindFirst(BasePath + '\{#DotNetMajor}.*', FindRec) then
    begin
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Result := True;
          Break;
        end;
      until not FindNext(FindRec);
      FindClose(FindRec);
    end;
  end;
end;

// ---------------------------------------------------------------------------
// Download progress callback
// ---------------------------------------------------------------------------
procedure OnDownloadProgress(const Url, Filename: String; const Progress, ProgressMax: Int64);
begin
  if ProgressMax <> 0 then
    DotNetPage.SetProgress(Progress, ProgressMax)
  else
    DotNetPage.SetProgress(0, 1);
end;

// ---------------------------------------------------------------------------
// Wizard setup — create download page
// ---------------------------------------------------------------------------
procedure InitializeWizard();
begin
  DotNetPage := CreateDownloadPage(
    'Downloading .NET {#DotNetMajor} Desktop Runtime',
    'The .NET {#DotNetMajor} Desktop Runtime is required. Downloading now...',
    @OnDownloadProgress);
end;

// ---------------------------------------------------------------------------
// Before copying files, download + install .NET if needed
// ---------------------------------------------------------------------------
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  RuntimeInstaller: String;
  ResultCode: Integer;
begin
  Result := '';

  if IsDotNetDesktopRuntimeInstalled() then
  begin
    Log('.NET {#DotNetMajor} Desktop Runtime already installed — skipping.');
    Exit;
  end;

  Log('.NET {#DotNetMajor} Desktop Runtime not found — downloading...');

  DotNetPage.Clear;
  DotNetPage.Add(
    '{#DotNetDownloadUrl}',
    'windowsdesktop-runtime-{#DotNetMajor}-win-x64.exe',
    '');
  DotNetPage.Show;

  try
    try
      DotNetPage.Download;
    except
      if DotNetPage.AbortedByUser then
        Result := 'Download cancelled. .NET {#DotNetMajor} Desktop Runtime is required to run {#MyAppName}.'
      else
        Result := 'Download failed: ' + GetExceptionMessage + #13#10 +
                  'Install manually from https://dotnet.microsoft.com/download/dotnet/{#DotNetMajor}';
      Exit;
    end;
  finally
    DotNetPage.Hide;
  end;

  // Run the installer silently
  RuntimeInstaller := ExpandConstant('{tmp}\windowsdesktop-runtime-{#DotNetMajor}-win-x64.exe');
  Log('Installing .NET runtime from: ' + RuntimeInstaller);

  if not Exec(RuntimeInstaller, '/install /quiet /norestart', '', SW_SHOW,
              ewWaitUntilTerminated, ResultCode) then
  begin
    Result := 'Could not launch the .NET runtime installer. Please install .NET {#DotNetMajor} Desktop Runtime manually.';
    Exit;
  end;

  if ResultCode <> 0 then
  begin
    // Exit code 3010 = success, reboot required
    if ResultCode = 3010 then
      NeedsRestart := True
    else
      Result := '.NET {#DotNetMajor} runtime installer exited with code ' + IntToStr(ResultCode) +
                '. Please install .NET {#DotNetMajor} Desktop Runtime manually.';
  end;
end;

// ---------------------------------------------------------------------------
// Show .NET requirement on the first page if not installed
// ---------------------------------------------------------------------------
function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNetDesktopRuntimeInstalled() then
    Log('.NET {#DotNetMajor} Desktop Runtime will be downloaded during installation.')
  else
    Log('.NET {#DotNetMajor} Desktop Runtime is already installed.');
end;
