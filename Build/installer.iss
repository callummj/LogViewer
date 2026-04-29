; LogViewer Inno Setup Installer Script
; Requires Inno Setup 6.3 or later
;
; Build via build.ps1, which passes /DMyAppVersion=X.Y.Z on the command line.
; Can also be compiled manually:
;   iscc.exe /DMyAppVersion=1.0.0 installer.iss

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

#define MyAppName    "LogViewer"
#define MyAppPublisher "Callum Jones"
#define MyAppExeName "LogViewerApp.exe"

[Setup]
; AppId must never change between versions — Windows uses it to detect upgrades.
AppId={{6D3F2A1E-8B4C-4E7F-A9D5-2C0E6B3F8A1D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\LogViewer
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=..\Release
OutputBaseFilename=LogViewer-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; Installs into 64-bit Program Files; also works under x64 emulation on ARM
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\Publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; \
  Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
  Flags: nowait postinstall skipifsilent

[Code]

// Check for .NET 10 Desktop Runtime in the 64-bit registry hive.
function IsDotNet10DesktopInstalled: Boolean;
var
  KeyPath: String;
  SubKeyNames: TArrayOfString;
  I: Integer;
begin
  Result := False;
  KeyPath := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  if RegGetSubkeyNames(HKLM64, KeyPath, SubKeyNames) then
    for I := 0 to GetArrayLength(SubKeyNames) - 1 do
      if Copy(SubKeyNames[I], 1, 3) = '10.' then
      begin
        Result := True;
        Break;
      end;
end;

function InitializeSetup: Boolean;
begin
  Result := True;
  if not IsDotNet10DesktopInstalled then
  begin
    if MsgBox(
      '.NET 10 Desktop Runtime is required but was not found on this machine.' + #13#10#13#10 +
      'Download it from:' + #13#10 +
      'https://dotnet.microsoft.com/download/dotnet/10.0' + #13#10#13#10 +
      'Continue with installation anyway?',
      mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;