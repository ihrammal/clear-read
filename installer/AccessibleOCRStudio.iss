#define MyAppName "TextBridge"
#define MyAppVersion "1.0.2"
#define MyAppPublisher "Iman Rammal"
#define MyAppExeName "TextBridge.exe"
[Setup]
#ifdef TestInstall
AppId=TextBridge-Isolated-QA
#else
AppId={{9A9C570A-BA44-49B6-95E4-73B724DD0A60}
#endif
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\TextBridge
DefaultGroupName=TextBridge
#ifdef TestInstall
OutputBaseFilename=TextBridge-Setup-{#MyAppVersion}-QA
#else
OutputBaseFilename=TextBridge-Setup-{#MyAppVersion}
#endif
Compression=lzma2/fast
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline
OutputDir=..\artifacts
LicenseFile=LICENSE.txt
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
[Files]
Source: "..\publish\*"; DestDir: "{app}"; Excludes: "ClearRead.*,ClearRead-LICENSE.txt"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\TextBridge"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\TextBridge"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
[InstallDelete]
Type: files; Name: "{app}\ClearRead.exe"
Type: files; Name: "{app}\ClearRead.dll"
Type: files; Name: "{app}\ClearRead.deps.json"
Type: files; Name: "{app}\ClearRead.runtimeconfig.json"
Type: files; Name: "{app}\ClearRead.pdb"
Type: files; Name: "{app}\licenses\ClearRead-LICENSE.txt"
#ifndef TestInstall
Type: files; Name: "{autoprograms}\Clear Read\Clear Read.lnk"
Type: files; Name: "{autodesktop}\Clear Read.lnk"
#endif
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch TextBridge"; Flags: nowait postinstall skipifsilent

