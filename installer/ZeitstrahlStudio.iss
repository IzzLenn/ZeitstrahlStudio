; Inno Setup-Skript für Zeitstrahl Studio
; Erzeugt einen 64-Bit-Windows-Installer mit .zeitprojekt-Dateizuordnung.

#define MyAppName "Zeitstrahl Studio"
#define MyAppPublisher "Zeitstrahl Studio"
#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif
#define MyAppExeName "ZeitstrahlStudio.App.exe"
#define MyAppAssocName "Zeitstrahl Studio Projekt"
#define MyAppAssocExt ".zeitprojekt"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
AppId={{7A3B9C2D-5E4F-4A1B-9C8D-2E3F4A5B6C7D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={pf64}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir={#SourcePath}\..\artifacts\release
OutputBaseFilename=ZeitstrahlStudio-{#MyAppVersion}-win-x64-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
SetupIconFile=
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoTextVersion={#MyAppVersion}

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "fileassoc"; Description: "Dateien mit der Erweiterung .zeitprojekt mit Zeitstrahl Studio verknüpfen"; GroupDescription: "Dateizuordnungen"

[Files]
Source: "{#SourcePath}\..\artifacts\publish\win-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\..\artifacts\publish\win-x64\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "{#SourcePath}\..\artifacts\publish\win-x64\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\..\artifacts\publish\win-x64\*.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\..\artifacts\publish\win-x64\*.xml"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#SourcePath}\..\artifacts\publish\win-x64\*.config"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#SourcePath}\..\artifacts\publish\win-x64\*.dat"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#SourcePath}\..\artifacts\publish\win-x64\*.db"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#SourcePath}\..\artifacts\publish\win-x64\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs skipifsourcedoesntexist
Source: "{#SourcePath}\..\artifacts\publish\win-x64\samples\*"; DestDir: "{app}\samples"; Flags: ignoreversion recursesubdirs
Source: "{#SourcePath}\..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\..\PRIVACY.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\..\THIRD_PARTY_LICENSES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\..\CHANGELOG.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion; Check: LicenseExists

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; .zeitprojekt-Dateizuordnung
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocKey}"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: fileassoc

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function LicenseExists: Boolean;
begin
  Result := FileExists(ExpandConstant('{src}\..\LICENSE.txt'));
end;

function InitializeSetup: Boolean;
begin
  Result := true;
end;
