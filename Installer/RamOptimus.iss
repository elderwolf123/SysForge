; RamOptimus Installer Script for Inno Setup
; Version 2.0.0

#define MyAppName "RAM OPTIMIZER NOVA"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "Professional RAM Optimization"
#define MyAppExeName "RamOptimizerUI.exe"
#define MyAppAssocName MyAppName + " File"
#define MyAppAssocExt ".nov"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
AppId={{8F9D2E3A-1B4C-4D5E-8F9A-2B3C4D5E6F7A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
OutputDir=C:\Users\Jarrod\Desktop
OutputBaseFilename=RamOptimus_Setup_v2.0.0
SetupIconFile=
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode
Name: "startup"; Description: "Run RamOptimus on Windows startup"; GroupDescription: "Startup Options:"; Flags: unchecked

[Files]
Source: "C:\Users\Jarrod\Desktop\VS Code Projects\Ram optimiser\Releases\RAM_OPTIMIZER_NOVA_Portable_v2.0.0\App\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "C:\Users\Jarrod\Desktop\VS Code Projects\Ram optimiser\Releases\RAM_OPTIMIZER_NOVA_Portable_v2.0.0\Data\*"; DestDir: "{app}\Data"; Flags: ignoreversion recursesubdirs

[Registry]
; File association for .roc files
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".roc"; ValueData: ""

; Application registry entries
Root: HKLM; Subkey: "Software\{#MyAppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#MyAppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"

; Startup entry (only if task selected)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Tasks: startup; Flags: uninsdeletevalue

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsAdminInstallMode then
  begin
    MsgBox('RamOptimus requires administrator privileges to install and run properly.' + #13#10 + #13#10 + 
           'Please run this installer as Administrator.', mbError, MB_OK);
    Result := False;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Additional post-install tasks can be added here
  end;
end;
