#define MyAppName "Clear Read"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Iman Rammal"
#define MyAppExeName "ClearRead.exe"
[Setup]
#ifdef TestInstall
AppId=ClearRead-Isolated-QA
#else
AppId={{9A9C570A-BA44-49B6-95E4-73B724DD0A60}
#endif
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Clear Read
DefaultGroupName=Clear Read
#ifdef TestInstall
OutputBaseFilename=ClearRead-Setup-{#MyAppVersion}-QA
#else
OutputBaseFilename=ClearRead-Setup-{#MyAppVersion}
#endif
Compression=lzma2/fast
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline
OutputDir=..\artifacts
LicenseFile=LICENSE.txt
[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Clear Read"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Clear Read"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Clear Read"; Flags: nowait postinstall skipifsilent
