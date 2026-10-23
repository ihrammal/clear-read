#define MyAppName "TextBridge"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Iman Rammal"
#define MyAppExeName "TextBridge.exe"
#ifdef TestInstall
#define MyAppShortcutName "TextBridge QA"
#else
#define MyAppShortcutName "TextBridge"
#endif
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
DefaultGroupName={#MyAppShortcutName}
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
DisableWelcomePage=yes
DisableProgramGroupPage=yes
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
[Files]
Source: "..\publish\*"; DestDir: "{app}"; Excludes: "paddle,paddle\*,ClearRead.*,ClearRead-LICENSE.txt"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\publish\paddle\*"; DestDir: "{app}\paddle"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: paddle
Source: "LICENSE.txt"; DestName: "TextBridge-License.txt"; Flags: dontcopy
[Types]
Name: "full"; Description: "PaddleOCR and Windows OCR"
Name: "compact"; Description: "Windows OCR only"
Name: "custom"; Description: "Custom installation"; Flags: iscustom
[Components]
Name: "paddle"; Description: "PaddleOCR offline runtime and language models"; Types: full
[Icons]
Name: "{group}\{#MyAppShortcutName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenu
Name: "{autodesktop}\{#MyAppShortcutName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
[InstallDelete]
Type: filesandordirs; Name: "{app}\paddle"; Check: RemoveInstalledPaddle
Type: files; Name: "{autodesktop}\{#MyAppShortcutName}.lnk"; Check: not WizardIsTaskSelected('desktopicon')
Type: files; Name: "{autoprograms}\{#MyAppShortcutName}\{#MyAppShortcutName}.lnk"; Check: not WizardIsTaskSelected('startmenu')
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
Name: "desktopicon"; Description: "{cm:DesktopShortcut}"; GroupDescription: "{cm:ShortcutGroup}"
Name: "startmenu"; Description: "{cm:StartShortcut}"; GroupDescription: "{cm:ShortcutGroup}"
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch TextBridge"; Flags: nowait postinstall skipifsilent unchecked
[UninstallDelete]
Type: files; Name: "{app}\ocr-engine.txt"

[CustomMessages]
english.DesktopShortcut=Create a desktop shortcut
arabic.DesktopShortcut=إنشاء اختصار على سطح المكتب
english.StartShortcut=Create a Start menu shortcut
arabic.StartShortcut=إنشاء اختصار في قائمة ابدأ
english.ShortcutGroup=Shortcuts:
arabic.ShortcutGroup=الاختصارات:
english.SetupTypeTitle=Choose setup type
arabic.SetupTypeTitle=اختر نوع التثبيت
english.SetupTypeDescription=Express uses recommended settings. Custom lets you choose.
arabic.SetupTypeDescription=يستخدم التثبيت السريع الإعدادات الموصى بها، ويتيح التثبيت المخصص اختيارها.
english.SetupTypeHelp=Express installs PaddleOCR and desktop/Start menu shortcuts. Click Install to accept the license and begin. Choose Custom to review the license, OCR engine, shortcuts and folder.
arabic.SetupTypeHelp=يثبّت الخيار السريع PaddleOCR واختصارات سطح المكتب وقائمة ابدأ. اضغط تثبيت للموافقة على الترخيص والبدء، أو اختر مخصص لمراجعة الترخيص والمحرك والاختصارات والمجلد.
english.ExpressOption=Express (recommended)
arabic.ExpressOption=سريع (موصى به)
english.CustomOption=Custom
arabic.CustomOption=مخصص
english.ViewLicense=View license
arabic.ViewLicense=عرض الترخيص
english.EngineTitle=Choose text recognition
arabic.EngineTitle=اختر محرك استخراج النصوص
english.EngineDescription=Choose the recognition engine to use after installation.
arabic.EngineDescription=اختر محرك استخراج النصوص الذي تريد استخدامه بعد التثبيت.
english.EngineHelp=Both options work offline. Windows OCR needs installed Windows language packs. Choosing Windows skips the bundled Paddle files and uses less disk space.
arabic.EngineHelp=يعمل الخياران دون إنترنت. يحتاج محرك Windows إلى حزم اللغات المثبّتة في النظام. يتجاوز هذا الخيار ملفات Paddle المضمّنة لتوفير مساحة القرص.
english.PaddleOption=PaddleOCR: install bundled language models
arabic.PaddleOption=PaddleOCR: تثبيت نماذج اللغات المضمّنة
english.WindowsOption=Windows OCR only: use Windows language packs
arabic.WindowsOption=محرك Windows فقط: استخدام حزم لغات النظام

[Code]
var
  ModePage, EnginePage: TInputOptionWizardPage;
  LicenseButton: TNewButton;

procedure ViewLicense(Sender: TObject);
var ResultCode: Integer;
begin
  ExtractTemporaryFile('TextBridge-License.txt');
  ShellExec('open', ExpandConstant('{tmp}\TextBridge-License.txt'), '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
end;

procedure ModeChanged(Sender: TObject);
begin
  if ModePage.Values[0] then WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall)
  else WizardForm.NextButton.Caption := SetupMessage(msgButtonNext);
end;

procedure InitializeWizard;
begin
  ModePage := CreateInputOptionPage(wpWelcome, CustomMessage('SetupTypeTitle'),
    CustomMessage('SetupTypeDescription'),
    CustomMessage('SetupTypeHelp'), True, False);
  ModePage.Add(CustomMessage('ExpressOption'));
  ModePage.Add(CustomMessage('CustomOption'));
  ModePage.CheckListBox.Height := ScaleY(60);
  ModePage.CheckListBox.OnClickCheck := @ModeChanged;
  ModePage.Values[0] := True;
  LicenseButton := TNewButton.Create(WizardForm);
  LicenseButton.Parent := ModePage.Surface;
  LicenseButton.Caption := CustomMessage('ViewLicense');
  LicenseButton.Left := 0;
  LicenseButton.Top := ModePage.CheckListBox.Top + ModePage.CheckListBox.Height + ScaleY(12);
  LicenseButton.Width := ScaleX(110);
  LicenseButton.OnClick := @ViewLicense;
  EnginePage := CreateInputOptionPage(ModePage.ID, CustomMessage('EngineTitle'),
    CustomMessage('EngineDescription'), CustomMessage('EngineHelp'), True, False);
  EnginePage.Add(CustomMessage('PaddleOption'));
  EnginePage.Add(CustomMessage('WindowsOption'));
  EnginePage.Values[0] := True;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := PageID = wpSelectComponents;
  if ModePage.Values[0] then
    Result := Result or (PageID = EnginePage.ID) or (PageID = wpLicense) or
      (PageID = wpSelectDir) or (PageID = wpSelectProgramGroup) or
      (PageID = wpSelectTasks) or (PageID = wpReady);
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = ModePage.ID) and ModePage.Values[0] then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall)
  else if CurPageID <> wpReady then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonNext);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if WizardSilent then exit;
  if (CurPageID = ModePage.ID) and ModePage.Values[0] then
  begin
    WizardSelectComponents('paddle');
    WizardSelectTasks('desktopicon,startmenu');
  end;
  if CurPageID = EnginePage.ID then
  begin
    if EnginePage.Values[0] then WizardSelectComponents('paddle')
    else WizardSelectComponents('');
  end;
end;

function RemoveInstalledPaddle: Boolean;
begin
  Result := (not WizardIsComponentSelected('paddle')) and
    FileExists(ExpandConstant('{app}\TextBridge.exe')) and
    FileExists(ExpandConstant('{app}\paddle\worker.py'));
end;

procedure CurStepChanged(CurStep: TSetupStep);
var Engine: String;
begin
  if CurStep = ssPostInstall then
  begin
    if WizardIsComponentSelected('paddle') then Engine := 'paddle'
    else Engine := 'windows';
    SaveStringToFile(ExpandConstant('{app}\ocr-engine.txt'), Engine, False);
  end;
end;

