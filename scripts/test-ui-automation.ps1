# UI Automation audit for TextBridge.
# Drives the published Windows app with real keyboard input and verifies the
# accessibility properties, focus order, dialogs, and recognition flow.
# Usage: scripts\test-ui-automation.ps1 [-ExePath path] [-ImagePath path]
param(
    [string]$ExePath,
    [string]$ImagePath
)
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
if (-not $ExePath) { $ExePath = Join-Path $Root 'publish\TextBridge.exe' }
if (-not $ImagePath) { $ImagePath = Join-Path $Root 'artifacts\accessibility-tests\english.png' }
if (-not (Test-Path $ExePath)) { throw "App not found at $ExePath. Run scripts\build-windows.ps1 first." }

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class Fg {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
  public static bool Force(IntPtr hWnd) {
    if (hWnd == IntPtr.Zero) return false;
    IntPtr fg = GetForegroundWindow();
    if (fg == hWnd) return true;
    uint pid; uint fgThread = GetWindowThreadProcessId(fg, out pid);
    uint cur = GetCurrentThreadId();
    bool attached = false;
    if (fgThread != cur) attached = AttachThreadInput(cur, fgThread, true);
    ShowWindow(hWnd, 9);
    SetForegroundWindow(hWnd);
    if (attached) AttachThreadInput(cur, fgThread, false);
    return GetForegroundWindow() == hWnd;
  }
}
'@
$wsh = New-Object -ComObject WScript.Shell
$script:results = New-Object System.Collections.Generic.List[string]
$script:fail = 0

function Report([string]$status, [string]$msg) {
    $line = "$status`: $msg"
    if ($status -eq 'FAIL') { $script:fail++ }
    $script:results.Add($line)
    Write-Output $line
}
function Pass([string]$m) { Report 'PASS' $m }
function Fail([string]$m) { Report 'FAIL' $m }

$root = [System.Windows.Automation.AutomationElement]::RootElement
$script:mainWindow = $null

function Get-ProcWindows($procId) {
    $out = @()
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
    foreach ($e in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $c)) { $out += $e }
    if ($script:mainWindow) {
        $wc = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Window)
        foreach ($e in $script:mainWindow.FindAll([System.Windows.Automation.TreeScope]::Descendants, $wc)) {
            try { if ($e.Current.ProcessId -eq $procId) { $out += $e } } catch { }
        }
    }
    return $out
}
function Wait-Window($procId, [string]$like, [int]$timeoutMs = 10000) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.ElapsedMilliseconds -lt $timeoutMs) {
        foreach ($w in (Get-ProcWindows $procId)) {
            try { if ($w.Current.Name -like $like) { return $w } } catch { }
        }
        Start-Sleep -Milliseconds 250
    }
    return $null
}
function Wait-Gone($procId, [string]$like, [int]$timeoutMs = 10000) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.ElapsedMilliseconds -lt $timeoutMs) {
        $found = $false
        foreach ($w in (Get-ProcWindows $procId)) {
            try { if ($w.Current.Name -like $like) { $found = $true } } catch { }
        }
        if (-not $found) { return $true }
        Start-Sleep -Milliseconds 250
    }
    return $false
}
function Describe($e) {
    $ct = $e.Current.ControlType.ProgrammaticName -replace 'ControlType\.', ''
    "{0} Name='{1}' Id='{2}' Focusable={3} Enabled={4}" -f $ct, $e.Current.Name, $e.Current.AutomationId, $e.Current.IsKeyboardFocusable, $e.Current.IsEnabled
}
$interactiveTypes = @('Button', 'MenuItem', 'CheckBox', 'ComboBox', 'Edit', 'TabItem', 'ListItem', 'Hyperlink', 'Slider', 'RadioButton', 'SplitButton', 'TreeItem', 'List', 'Tree')
function Get-Interactive($scope) {
    $out = @()
    foreach ($e in $scope.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        $ct = $e.Current.ControlType.ProgrammaticName -replace 'ControlType\.', ''
        $cls = $e.Current.ClassName
        if ($interactiveTypes -contains $ct -or $cls -in @('Button', 'Edit', 'ComboBox', 'ComboBoxEx32', 'SysTabControl32')) { $out += $e }
    }
    return $out
}
function Find-ByName($scope, [string]$name) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $scope.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}
function Find-ById($scope, [string]$id) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    return $scope.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}
function Get-Buttons($scope) {
    $out = @()
    foreach ($e in $scope.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        $ct = $e.Current.ControlType.ProgrammaticName -eq 'ControlType.Button'
        if ($ct -or $e.Current.ClassName -eq 'Button') { $out += $e }
    }
    return $out
}
function Get-TextBlocks($scope) {
    $out = @()
    foreach ($e in $scope.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        if ($e.Current.ControlType.ProgrammaticName -eq 'ControlType.Text' -or $e.Current.ClassName -eq 'Static') {
            if ($e.Current.Name) { $out += $e.Current.Name }
        }
    }
    return $out
}
function Invoke-Element($e) {
    $p = $null
    if ($e.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$p)) { $p.Invoke(); return $true }
    try { $e.SetFocus() } catch { }
    Start-Sleep -Milliseconds 300
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    return $true
}
function Get-FocusedName() {
    $f = [System.Windows.Automation.AutomationElement]::FocusedElement
    if ($f) { return $f.Current.Name }
    return ''
}
function Get-ToggleState($e) {
    $p = $null
    if ($e.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$p)) { return "$($p.Current.ToggleState)" }
    return 'no-toggle-pattern'
}
function Set-ToggleState($e, [string]$wanted) {
    $p = $null
    if (-not $e.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$p)) { return $false }
    for ($i = 0; $i -lt 4; $i++) {
        if ("$($p.Current.ToggleState)" -eq $wanted) { return $true }
        $p.Toggle(); Start-Sleep -Milliseconds 250
    }
    return ("$($p.Current.ToggleState)" -eq $wanted)
}
function Key([string]$keys) { [System.Windows.Forms.SendKeys]::SendWait($keys) }
function Activate($procId) {
    for ($i = 0; $i -lt 6; $i++) {
        $f = [System.Windows.Automation.AutomationElement]::FocusedElement
        try { if ($f -and $f.Current.ProcessId -eq $procId) { return } } catch { }
        try {
            $p = Get-Process -Id $procId -ErrorAction Stop
            if ($p.MainWindowHandle -ne [IntPtr]::Zero) { [Fg]::Force($p.MainWindowHandle) | Out-Null }
        } catch { }
        Start-Sleep -Milliseconds 400
        $f = [System.Windows.Automation.AutomationElement]::FocusedElement
        try { if ($f -and $f.Current.ProcessId -eq $procId) { return } } catch { }
        foreach ($w in (Get-ProcWindows $procId)) {
            try { $w.SetFocus(); break } catch { }
        }
        Start-Sleep -Milliseconds 400
    }
}
function Get-Focused() {
    $f = [System.Windows.Automation.AutomationElement]::FocusedElement
    if (-not $f) { return 'none' }
    "{0}:{1}:{2}" -f ($f.Current.ControlType.ProgrammaticName -replace 'ControlType\.', ''), $f.Current.Name, $f.Current.AutomationId
}
function Check-Unnamed($scope, [string]$what) {
    $unnamed = @()
    foreach ($e in (Get-Interactive $scope)) { if ([string]::IsNullOrWhiteSpace($e.Current.Name)) { $unnamed += (Describe $e) } }
    if ($unnamed.Count -eq 0) { Pass "$what`: every interactive control exposes an accessible name" }
    else { Fail "$what unnamed controls: $($unnamed -join ' | ')" }
}
function Press-Button($procId, $box, [string]$name) {
    $btn = Get-Buttons $box | Where-Object { $_.Current.Name -eq $name } | Select-Object -First 1
    if (-not $btn) { return $false }
    try { [Fg]::SetForegroundWindow([IntPtr]$box.Current.NativeWindowHandle) | Out-Null } catch { }
    Start-Sleep -Milliseconds 500
    for ($i = 0; $i -lt 8; $i++) {
        if ((Get-FocusedName) -eq $name) { Key '{ENTER}'; Start-Sleep -Milliseconds 600; return $true }
        Key '{TAB}'; Start-Sleep -Milliseconds 300
    }
    return $false
}
function Close-MessageBox($procId, [string]$title, [string]$buttonName) {
    $box = Wait-Window $procId $title 8000
    if (-not $box) { return $false }
    if (-not (Press-Button $procId $box $buttonName)) { return $false }
    return (Wait-Gone $procId $title 8000)
}

# ---------------------------------------------------------------- settings backup
$settingsPath = Join-Path $env:APPDATA 'TextBridge\settings.json'
$settingsBackup = $null
if (Test-Path $settingsPath) { $settingsBackup = Get-Content $settingsPath -Raw }
$corruptPdf = Join-Path $env:TEMP 'clearread-corrupt-test.pdf'
[IO.File]::WriteAllText($corruptPdf, 'This is not a real PDF file.')

try {
# ---------------------------------------------------------------- launch
foreach ($p in (Get-Process -Name TextBridge -ErrorAction SilentlyContinue)) { $p.Kill(); $p.WaitForExit() }
$proc = Start-Process -FilePath $ExePath -WorkingDirectory (Split-Path $ExePath) -PassThru
$procId = $proc.Id
Start-Sleep -Seconds 5

$main = Wait-Window $procId 'TextBridge*' 25000
if ($main) { Pass "Published app launches with top-level window '$($main.Current.Name)'" }
else { Fail 'Main window was not found'; exit 1 }
$script:mainWindow = $main

Start-Sleep -Seconds 4
$main = Wait-Window $procId 'TextBridge' 10000
$script:mainWindow = $main
if ($main -and $main.Current.Name -eq 'TextBridge') { Pass 'Welcome title sequence settles back to TextBridge' }
else { Fail "Window title did not settle (was '$($main.Current.Name))'" }

# ---------------------------------------------------------------- accessible names
Check-Unnamed $main 'Main window'

# ---------------------------------------------------------------- control roles and names
$editor = Find-ById $main 'DocumentText'
if ($editor -and $editor.Current.ControlType.ProgrammaticName -eq 'ControlType.Edit' -and $editor.Current.IsKeyboardFocusable -and $editor.Current.Name -eq 'Recognized document text') {
    Pass "Document area is a standard focusable Edit control named '$($editor.Current.Name)'"
} else {
    Fail "Document area role/name check failed: $(if ($editor) { Describe $editor } else { 'not found' })"
}
$status = Find-ById $main 'StatusText'
if ($status -and $status.Current.Name -eq 'Ready') { Pass 'Status region has its own name and does not rely on nearby text' }
else { Fail "Status region name was '$($status.Current.Name)'" }

# ---------------------------------------------------------------- F6 navigation
Activate $procId
$main.SetFocus(); Start-Sleep -Milliseconds 500
Key '{F6}'; Start-Sleep -Milliseconds 600
$f1 = Get-Focused
if ($f1 -like '*DocumentText*') { Pass "F6 focuses the recognized-document editor ($f1)" } else { Fail "F6 focus was '$f1'" }

Key '+{F6}'; Start-Sleep -Milliseconds 600
$f2 = Get-Focused
if ($f2 -like '*StatusText*') { Pass "Shift+F6 focuses the status message ($f2)" } else { Fail "Shift+F6 focus was '$f2'" }

# ---------------------------------------------------------------- tab order in the main window
$main.SetFocus(); Start-Sleep -Milliseconds 400
Key '{F6}'; Start-Sleep -Milliseconds 500
$tabSeq = @()
for ($i = 0; $i -lt 6; $i++) {
    Key '{TAB}'; Start-Sleep -Milliseconds 350
    $tabSeq += Get-Focused
}
$left = @($tabSeq | Where-Object { $_ -notlike '*DocumentText*' })
if ($left.Count -eq 0) { Fail "Tab never leaves the document editor in the main window (focus sequence: $(($tabSeq | Select-Object -Unique) -join ' -> '))." }
else {
    Pass "Tab moves focus beyond the document editor (first step: $($left[0]))"
    $visited = @($tabSeq | ForEach-Object {
        if ($_ -like '*DocumentText*') { 'editor' }
        elseif ($_ -like '*StatusText*') { 'status' }
        elseif ($_ -like 'MenuItem:*') { 'menu' }
        else { $_ }
    } | Select-Object -Unique)
    if ($visited -contains 'menu' -and $visited -contains 'status' -and $visited -contains 'editor') { Pass "Tab cycle covers menu, status and editor ($($visited -join ' -> '))" }
    else { Fail "Tab cycle does not cover every main-window stop (visited: $($visited -join ' -> '))" }
}

$status = Find-ById $main 'StatusText'
if ($status -and $status.Current.Name) { Pass "Status region exposes a live accessible name: '$($status.Current.Name)'" }
else { Fail 'Status region has no accessible name' }

# ---------------------------------------------------------------- keyboard menu walk
foreach ($menuName in @('File', 'Edit', 'View', 'OCR', 'Document', 'Export', 'Tools', 'Accessibility', 'Help')) {
    Activate $procId
    $top = Find-ByName $main $menuName
    if (-not $top) { Fail "Top-level menu '$menuName' not found"; continue }
    $exp = $null
    if ($top.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$exp)) { $exp.Expand() }
    else { Fail "Menu '$menuName' cannot be expanded from the keyboard tree"; continue }
    Start-Sleep -Milliseconds 400
    $children = $top.FindAll([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::MenuItem)))
    $unnamed = @()
    foreach ($child in $children) { if ([string]::IsNullOrWhiteSpace($child.Current.Name)) { $unnamed += (Describe $child) } }
    if ($children.Count -eq 0) { Fail "Menu '$menuName' exposes no items" }
    elseif ($unnamed.Count -gt 0) { Fail "Menu '$menuName' has unnamed items: $($unnamed -join ' | ')" }
    else { Pass "Menu '$menuName': all $($children.Count) items expose names and MenuItem roles" }
    Key '{ESC}'; Start-Sleep -Milliseconds 300
}
Activate $procId
$main.SetFocus(); Start-Sleep -Milliseconds 300
Key '{F6}'; Start-Sleep -Milliseconds 400
if ((Get-Focused) -like '*DocumentText*') { Pass 'Focus returns to the document after the menu walk' }
else { Fail "Focus after menu walk was '$(Get-Focused)'" }

# ---------------------------------------------------------------- Preferences dialog
Activate $procId
Key '^,'; Start-Sleep -Milliseconds 1200
$dlg = Wait-Window $procId '*Settings*' 8000
if ($dlg) {
    Pass "Ctrl+, opens the preferences dialog '$($dlg.Current.Name)'"
    Check-Unnamed $dlg 'Preferences dialog'
    $tab = Find-ByName $dlg 'Settings categories'
    if ($tab) { Pass 'Preferences dialog exposes a named settings-category tab control' } else { Fail 'Settings category tab control not found' }
    $reached = @()
    $dlg.SetFocus(); Start-Sleep -Milliseconds 400
    for ($i = 0; $i -lt 30; $i++) {
        Key '{TAB}'; Start-Sleep -Milliseconds 200
        $f = Get-Focused; $reached += $f
        if ($f -like '*Save*') { break }
    }
    if (@($reached | Where-Object { $_ -like '*Save*' }).Count -gt 0) { Pass 'Preferences dialog tab order reaches the Save button' }
    else { Fail "Preferences dialog tab order never reached Save (last focus: $($reached[-1]))" }
    Key '{ESC}'; Start-Sleep -Milliseconds 1000
    if (Wait-Gone $procId '*Settings*' 8000) { Pass 'Escape closes the preferences dialog' } else { Fail 'Escape did not close the preferences dialog' }
    if ((Get-Focused) -like '*DocumentText*') { Pass 'Focus returns to the document after closing Preferences' }
    else { Fail "Focus after Preferences close was '$(Get-Focused)'" }
} else { Fail 'Ctrl+, did not open the preferences dialog' }

# ---------------------------------------------------------------- Find / Replace
foreach ($combo in @(@('^f', 'Find'), @('^h', 'Find and Replace'))) {
    Activate $procId
    Key $combo[0]; Start-Sleep -Milliseconds 1200
    $dlg = Wait-Window $procId $combo[1] 8000
    if ($dlg) {
        Pass "Shortcut opens the '$($dlg.Current.Name)' dialog"
        Check-Unnamed $dlg "$($dlg.Current.Name) dialog"
        $findField = Find-ById $dlg 'FindText'
        if ($findField -and $findField.Current.Name -eq 'Find text') { Pass "'$($dlg.Current.Name)' dialog exposes a named find-text field" }
        else { Fail "FindText field missing or unnamed: $(if ($findField) { $findField.Current.Name } else { 'not found' })" }
        if ($combo[1] -eq 'Find and Replace') {
            if (Find-ById $dlg 'ReplaceAllButton') { Pass 'Replace mode exposes the Replace All button' } else { Fail 'Replace All button missing in replace mode' }
        }
        Key '{ESC}'; Start-Sleep -Milliseconds 1000
        if (Wait-Gone $procId $combo[1] 8000) { Pass "Escape closes the '$($combo[1])' dialog" } else { Fail "Escape did not close the '$($combo[1])' dialog" }
        if ((Get-Focused) -like '*DocumentText*') { Pass "Focus returns to the document after closing '$($combo[1])'" }
        else { Fail "Focus after '$($combo[1])' close was '$(Get-Focused)'" }
    } else { Fail "Shortcut did not open the '$($combo[1])' dialog" }
}

# ---------------------------------------------------------------- API / privacy dialog
function Get-MenuItem($scope, [string]$topName, [string]$childName) {
    $top = Find-ByName $scope $topName
    if (-not $top) { return $null }
    $exp = $null
    if ($top.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$exp)) { $exp.Expand() }
    Start-Sleep -Milliseconds 500
    if ($childName) { return Find-ByName $top $childName }
    return $top
}

Activate $procId
$item = Get-MenuItem $main 'Tools' 'API and AI Settings...'
if ($item -and (Invoke-Element $item)) {
    $dlg = Wait-Window $procId 'API and Privacy Settings' 8000
    if ($dlg) {
        Pass "Tools > API and AI Settings opens '$($dlg.Current.Name)'"
        Check-Unnamed $dlg 'API/privacy dialog'
        $offline = Find-ById $dlg 'OfflineOnly'
        if ($offline) { Pass 'Offline only mode control is exposed to UI Automation' } else { Fail 'OfflineOnly control not found' }
        $state = Get-ToggleState $offline
        if ($state -eq 'On') { Pass 'Offline only mode is on by default' } else { Fail "Offline only mode default state was '$state'" }
        $allow = Find-ById $dlg 'AllowContent'
        $state2 = Get-ToggleState $allow
        if ($state2 -eq 'Off') { Pass 'Document-content transmission permission is off by default (opt-in)' }
        else { Fail "Document-content permission state was '$state2'" }
        $capabilityStates = @()
        foreach ($id in @('OcrCorrection', 'ImageDescription', 'TableReconstruction')) {
            $cb = Find-ById $dlg $id
            if (-not $cb) { Fail "API capability control '$id' not found"; continue }
            if ((Get-ToggleState $cb) -eq 'Off') { $capabilityStates += $id }
        }
        if ($capabilityStates.Count -eq 3) { Pass 'API capability checkboxes are off by default and exposed to UI Automation' }
        else { Fail "API capability defaults wrong: $($capabilityStates -join ', ')" }
        Key '{ESC}'; Start-Sleep -Milliseconds 1000
        if (Wait-Gone $procId 'API and Privacy Settings' 8000) { Pass 'Escape closes the API/privacy dialog without saving' } else { Fail 'Escape did not close the API/privacy dialog' }
        if ((Get-Focused) -like '*DocumentText*') { Pass 'Focus returns to the document after closing the API/privacy dialog' }
        else { Fail "Focus after API/privacy close was '$(Get-Focused)'" }
    } else { Fail 'API/privacy dialog did not appear' }
} else { Fail 'Tools > API and AI Settings menu item not invokable' }

# ---------------------------------------------------------------- API capability toggles persist
Activate $procId
$item = Get-MenuItem $main 'Tools' 'API and AI Settings...'
if ($item -and (Invoke-Element $item)) {
    $dlg = Wait-Window $procId 'API and Privacy Settings' 8000
    if ($dlg) {
        foreach ($id in @('OcrCorrection', 'ImageDescription', 'TableReconstruction')) {
            $cb = Find-ById $dlg $id
            if ($cb -and (Set-ToggleState $cb 'On')) { Pass "Turned on '$id' in the API dialog" } else { Fail "Could not turn on '$id'" }
        }
        $save = Get-Buttons $dlg | Where-Object { $_.Current.Name -eq 'Save' } | Select-Object -First 1
        if ($save -and (Invoke-Element $save) -and (Wait-Gone $procId 'API and Privacy Settings' 8000)) { Pass 'API dialog Save button persists and closes the dialog' }
        else { Fail 'API dialog Save button did not persist and close' }
        if ((Get-Focused) -like '*DocumentText*') { Pass 'Focus returns to the document after saving the API dialog' }
        else { Fail "Focus after API dialog save was '$(Get-Focused)'" }
        $saved = $null
        if (Test-Path $settingsPath) { $saved = Get-Content $settingsPath -Raw | ConvertFrom-Json }
        if ($saved -and $saved.AllowApiOcrCorrection -eq $true -and $saved.AllowImageDescription -eq $true -and $saved.AllowTableReconstruction -eq $true) {
            Pass 'API capability choices are persisted to settings.json'
        } else { Fail 'API capability choices were not persisted to settings.json' }
        # reset to defaults through the dialog so the app exits with clean in-memory state
        Activate $procId
        $item = Get-MenuItem $main 'Tools' 'API and AI Settings...'
        if ($item -and (Invoke-Element $item)) {
            $dlg = Wait-Window $procId 'API and Privacy Settings' 8000
            if ($dlg) {
                foreach ($id in @('OcrCorrection', 'ImageDescription', 'TableReconstruction')) {
                    $cb = Find-ById $dlg $id
                    if ($cb) { Set-ToggleState $cb 'Off' | Out-Null }
                }
                $save = Get-Buttons $dlg | Where-Object { $_.Current.Name -eq 'Save' } | Select-Object -First 1
                if ($save) { Invoke-Element $save | Out-Null; Wait-Gone $procId 'API and Privacy Settings' 8000 | Out-Null }
            }
        }
    } else { Fail 'API/privacy dialog did not reopen for the persistence check' }
} else { Fail 'Tools > API and AI Settings menu item not invokable for the persistence check' }

# ---------------------------------------------------------------- About and diagnostics
Activate $procId
$item = Get-MenuItem $main 'Help' 'About TextBridge'
if ($item -and (Invoke-Element $item)) {
    $dlg = Wait-Window $procId 'About TextBridge' 8000
    if ($dlg) {
        Pass 'Help > About opens the About message box'
        $text = (Get-TextBlocks $dlg) -join ' '
        [xml]$project = Get-Content (Join-Path $Root 'src/AccessibleOCRStudio/AccessibleOCRStudio.csproj')
        $expectedVersion = [string]$project.Project.PropertyGroup.Version
        if ($text -match [regex]::Escape($expectedVersion)) { Pass "About box reports version $expectedVersion" } else { Fail "About box text did not contain ${expectedVersion}: '$text'" }
        if ($text -match 'TextBridge') { Pass 'About box identifies the TextBridge product' } else { Fail "About box text missing product name: '$text'" }
        if (Close-MessageBox $procId 'About TextBridge' 'OK') { Pass 'About box can be dismissed' } else { Fail 'About box could not be dismissed' }
    } else { Fail 'About message box did not appear' }
} else { Fail 'Help > About menu item not invokable' }

Activate $procId
$item = Get-MenuItem $main 'Help' 'Runtime Diagnostics'
if ($item -and (Invoke-Element $item)) {
    $dlg = Wait-Window $procId 'Runtime Diagnostics' 8000
    if ($dlg) {
        $text = (Get-TextBlocks $dlg) -join ' '
        if ($text -match 'tesseract|poppler|OCR|ready|dependency|dependencies') { Pass 'Runtime diagnostics reports bundled dependency status' }
        else { Fail "Runtime diagnostics text unexpected: '$text'" }
        if (Close-MessageBox $procId 'Runtime Diagnostics' 'OK') { Pass 'Runtime diagnostics dialog closes' } else { Fail 'Runtime diagnostics dialog could not be closed' }
    } else { Fail 'Runtime diagnostics dialog did not appear' }
} else { Fail 'Help > Runtime Diagnostics menu item not invokable' }

# ---------------------------------------------------------------- offline-only update gate
Activate $procId
$item = Get-MenuItem $main 'Help' 'Check for Updates'
if ($item -and (Invoke-Element $item)) {
    $dlg = Wait-Window $procId 'TextBridge Updates' 8000
    if ($dlg) {
        $text = (Get-TextBlocks $dlg) -join ' '
        if ($text -match 'offline-only mode is on') { Pass 'Check for Updates respects offline-only mode and explains why it is disabled' }
        else { Fail "Check for Updates message did not explain offline-only mode: '$text'" }
        if (Close-MessageBox $procId 'TextBridge Updates' 'OK') { Pass 'Update-disabled message can be dismissed' } else { Fail 'Update-disabled message could not be dismissed' }
    } else { Fail 'Check for Updates did not show the offline-only message' }
} else { Fail 'Help > Check for Updates menu item not invokable' }

# ---------------------------------------------------------------- error announcement
Activate $procId
Key '^o'; Start-Sleep -Milliseconds 1500
$open = Wait-Window $procId 'Open' 8000
if ($open) {
    Activate $procId
    Key '%n'; Start-Sleep -Milliseconds 500
    Key '^a{DEL}'
    [System.Windows.Forms.SendKeys]::SendWait($corruptPdf); Start-Sleep -Milliseconds 400
    Key '{ENTER}'; Start-Sleep -Milliseconds 2500
    Wait-Gone $procId 'Open' 8000 | Out-Null
    Pass 'Opened a damaged PDF through the keyboard'
} else { Fail 'Ctrl+O did not open a file dialog for the damaged file' }
$status = Find-ById $main 'StatusText'
if ($status -and $status.Current.Name -like 'Opened clearread-corrupt-test.pdf*') { Pass "Damaged file load reported in the status region: '$($status.Current.Name)'" }
else { Fail "Status after opening the damaged file was '$($status.Current.Name)'" }

Activate $procId
Key '^r'; Start-Sleep -Milliseconds 800
$errorBox = Wait-Window $procId 'OCR Error' 30000
if ($errorBox) {
    $errText = (Get-TextBlocks $errorBox) -join ' '
    if ($errText -match '\S') { Pass "Recognition error is shown in a modal error dialog: '$errText'" }
    else { Fail 'OCR error dialog had no readable message' }
    if (Close-MessageBox $procId 'OCR Error' 'OK') { Pass 'OCR error dialog can be dismissed' } else { Fail 'OCR error dialog could not be dismissed' }
} else { Fail 'Recognition of the damaged file did not raise an error dialog' }
$status = Find-ById $main 'StatusText'
if ($status -and $status.Current.Name -eq 'Recognition failed.') { Pass 'Failure status persists in the status region after the error dialog closes' }
else { Fail "Status after failed recognition was '$($status.Current.Name)'" }
if ((Get-Focused) -like '*DocumentText*') { Pass 'Focus is on the document after the error dialog closes' }
else { Fail "Focus after error dismissal was '$(Get-Focused)'" }
Key '+{F6}'; Start-Sleep -Milliseconds 500
$f = Get-Focused
if ($f -like '*StatusText*' -and (Find-ById $main 'StatusText').Current.Name -eq 'Recognition failed.') {
    Pass 'Failure message is reachable again with Shift+F6 instead of being lost'
} else { Fail "Shift+F6 reached '$f'" }
Key '{F6}'; Start-Sleep -Milliseconds 400

# ---------------------------------------------------------------- recognition through the real UI
Activate $procId
Key '^o'; Start-Sleep -Milliseconds 1500
$open = Wait-Window $procId 'Open' 8000
if ($open) {
    Activate $procId
    Key '%n'; Start-Sleep -Milliseconds 500
    Key '^a{DEL}'
    [System.Windows.Forms.SendKeys]::SendWait($ImagePath); Start-Sleep -Milliseconds 400
    Key '{ENTER}'; Start-Sleep -Milliseconds 2500
    Pass 'Open dialog accepted a keyboard-typed file path'
    Wait-Gone $procId 'Open' 8000 | Out-Null
} else { Fail 'Ctrl+O did not open a file dialog' }

$main = Wait-Window $procId 'TextBridge' 5000
$script:mainWindow = $main
$status = Find-ById $main 'StatusText'
if ($status -and $status.Current.Name -like 'Opened*') { Pass "Status reports the opened file: '$($status.Current.Name)'" }
else { Fail "Status after open was '$($status.Current.Name)'" }
if ((Get-Focused) -like '*DocumentText*') { Pass 'Focus moves to the document text area after opening a file' }
else { Fail "Focus after open was '$(Get-Focused)'" }

Activate $procId
Key '^r'; Start-Sleep -Milliseconds 800
$done = $false
$sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.ElapsedMilliseconds -lt 180000) {
    $status = Find-ById $main 'StatusText'
    if ($status) {
        $n = $status.Current.Name
        if ($n -eq 'Recognition complete.') { $done = $true; break }
        if ($n -eq 'Recognition failed.') { break }
    }
    Start-Sleep -Milliseconds 700
}
if ($done) { Pass 'Ctrl+R recognition completes through the real UI and reports completion in the status region' }
else {
    $status = Find-ById $main 'StatusText'
    Fail "Recognition did not complete (status: '$($status.Current.Name)')"
}
if ((Get-Focused) -like '*DocumentText*') { Pass 'Focus stays on the document after recognition (completion is announced without stealing focus)' }
else { Fail "Focus after recognition was '$(Get-Focused)'" }

$editor = Find-ById $main 'DocumentText'
function Get-EditorText($e) {
    if (-not $e) { return '' }
    $tp = $null
    if ($e.TryGetCurrentPattern([System.Windows.Automation.TextPattern]::Pattern, [ref]$tp)) { return $tp.DocumentRange.GetText(-1) }
    $vp = $null
    if ($e.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) { return $vp.Value }
    return ''
}
$text = Get-EditorText $editor
if ($text -match 'quick brown fox') { Pass "Editor exposes the recognized English text to screen readers ($($text.Length) characters)" }
else { Fail "Editor text via TextPattern was '$text'" }

# recognition review
Activate $procId
$item = Get-MenuItem $main 'Document' 'Review Recognition'
if ($item -and (Invoke-Element $item)) {
    $review = Wait-Window $procId 'Recognition Review' 8000
    if ($review) {
        Pass 'Document > Review Recognition opens the review window'
        Check-Unnamed $review 'Recognition review'
        Key '{ESC}'; Start-Sleep -Milliseconds 800
        if (Wait-Gone $procId 'Recognition Review' 8000) { Pass 'Recognition review closes with Escape' } else { Fail 'Recognition review did not close' }
        if ((Get-Focused) -like '*DocumentText*') { Pass 'Focus returns to the document after closing the review window' }
        else { Fail "Focus after review close was '$(Get-Focused)'" }
    } elseif (Close-MessageBox $procId 'Recognition Review' 'OK') {
        Pass 'Recognition Review reported no suspicious text for this clean image'
    } else { Fail 'Recognition review did not open' }
} else { Fail 'Document > Review Recognition menu item not invokable' }

# ---------------------------------------------------------------- unsaved-text prompt
Activate $procId
Key '^o'; Start-Sleep -Milliseconds 1500
$open = Wait-Window $procId 'Open' 8000
if (-not $open) { Fail 'Second Ctrl+O did not open a file dialog' }
else {
    Activate $procId
    Key '%n'; Start-Sleep -Milliseconds 500
    Key '^a{DEL}'
    [System.Windows.Forms.SendKeys]::SendWait($ImagePath); Start-Sleep -Milliseconds 400
    Key '{ENTER}'; Start-Sleep -Milliseconds 2500
    Wait-Gone $procId 'Open' 8000 | Out-Null
}
$prompt = Wait-Window $procId '*Unsaved text*' 8000
if ($prompt) {
    Pass "Opening another document raises the unsaved-text prompt '$($prompt.Current.Name)'"
    $buttons = Get-Buttons $prompt | ForEach-Object { $_.Current.Name }
    $missing = @('Yes', 'No', 'Cancel') | Where-Object { $buttons -notcontains $_ }
    if (-not $missing) { Pass "Unsaved prompt exposes Yes, No and Cancel (labels: $($buttons -join ', '))" }
    else { Fail "Unsaved prompt missing buttons: $($missing -join ', '); found: $($buttons -join ', ')" }
    $cancel = Get-Buttons $prompt | Where-Object { $_.Current.Name -eq 'Cancel' } | Select-Object -First 1
    if ($cancel -and (Press-Button $procId $prompt 'Cancel')) { Pass 'Unsaved prompt reacts to the Cancel button' }
    elseif ($cancel) { Fail 'Unsaved prompt Cancel button did not respond'; Key '{ESC}' }
    else { Key '{ESC}' }
    Start-Sleep -Milliseconds 1200
    if (Wait-Gone $procId '*Unsaved text*' 8000) { Pass 'Cancel dismisses the unsaved-text prompt' } else { Fail 'Unsaved prompt did not close' }
    $editor = Find-ById $main 'DocumentText'
    $text = Get-EditorText $editor
    if ($text -match 'quick brown fox') { Pass 'Cancelling the prompt keeps the recognized text in the editor' }
    else { Fail "Text was lost after cancelling the prompt (editor holds $($text.Length) characters)" }
} else { Fail 'Unsaved-text prompt did not appear after choosing a second document' }

# ---------------------------------------------------------------- multilingual recognition
$fixturesDir = Split-Path $ImagePath
function Open-DocForRecognition([string]$path, [string]$promptButton) {
    Activate $procId
    Key '^o'; Start-Sleep -Milliseconds 1500
    $open = Wait-Window $procId 'Open' 8000
    if (-not $open) { return $false }
    Activate $procId
    Key '%n'; Start-Sleep -Milliseconds 500
    Key '^a{DEL}'
    [System.Windows.Forms.SendKeys]::SendWait($path); Start-Sleep -Milliseconds 400
    Key '{ENTER}'; Start-Sleep -Milliseconds 2500
    Wait-Gone $procId 'Open' 8000 | Out-Null
    if ($promptButton) {
        $prompt = Wait-Window $procId '*Unsaved text*' 8000
        if ($prompt) { Press-Button $procId $prompt $promptButton | Out-Null; Wait-Gone $procId '*Unsaved text*' 8000 | Out-Null }
    }
    $script:mainWindow = Wait-Window $procId 'TextBridge' 5000
    return $true
}
function Select-Language([string]$label) {
    $lang = Get-MenuItem $main 'OCR' 'Language'
    if (-not $lang) { return $false }
    $exp = $null
    if ($lang.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$exp)) { $exp.Expand() }
    Start-Sleep -Milliseconds 400
    $item = Find-ByName $lang $label
    if (-not $item) { return $false }
    return (Invoke-Element $item)
}
function Recognize-ToCompletion() {
    Activate $procId
    Key '^r'
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($watch.ElapsedMilliseconds -lt 180000) {
        $st = Find-ById $main 'StatusText'
        if ($st) { $n = $st.Current.Name; if ($n -in @('Recognition complete.', 'Recognition failed.')) { return $n } }
        Start-Sleep -Milliseconds 700
    }
    return 'timed out'
}
foreach ($case in @(
    @{ File = 'arabic.png'; Language = 'Arabic'; Pattern = '[\u0600-\u06FF]'; What = 'Arabic' },
    @{ File = 'french.png'; Language = 'French'; Pattern = '[\u00E0-\u00FF\u0152\u0153]'; What = 'French' })) {
    if (-not (Open-DocForRecognition (Join-Path $fixturesDir $case.File) 'No')) { Fail "Ctrl+O did not open $($case.File)"; continue }
    $status = Find-ById $main 'StatusText'
    if ($status -and $status.Current.Name -like "Opened $($case.File)*") { Pass "Opened $($case.File) through the keyboard, discarding unsaved text via the prompt" }
    else { Fail "Status after opening $($case.File) was '$($status.Current.Name)'" }
    if (Select-Language $case.Language) {
        Start-Sleep -Milliseconds 500
        $status = Find-ById $main 'StatusText'
        if ($status -and $status.Current.Name -like 'OCR language profile set to*') { Pass "OCR menu sets the $($case.Language) language profile: '$($status.Current.Name)'" }
        else { Fail "Language menu status was '$($status.Current.Name)'" }
    } else { Fail "Could not select the $($case.Language) language profile from the OCR menu" }
    $result = Recognize-ToCompletion
    if ($result -eq 'Recognition complete.') { Pass "$($case.What) recognition completes through the real UI" }
    else { Fail "$($case.What) recognition ended with '$result'" }
    $editor = Find-ById $main 'DocumentText'
    $text = Get-EditorText $editor
    if ($text -match $case.Pattern) { Pass "$($case.What) text reaches the document area and is readable via TextPattern ($($text.Length) characters)" }
    else { Fail "$($case.What) output did not contain $($case.What) characters: '$($text.Substring(0, [Math]::Min(120, $text.Length)))'" }
    if ((Get-Focused) -like '*DocumentText*') { Pass "Focus is on the document after $($case.What) recognition" }
    else { Fail "Focus after $($case.What) recognition was '$(Get-Focused)'" }
}

# close attempt with unsaved text
$null = $proc.CloseMainWindow()
Start-Sleep -Milliseconds 2000
$prompt = Wait-Window $procId '*Unsaved text*' 8000
if ($prompt) {
    Pass 'Closing with unsaved text raises the same save/discard/cancel prompt'
    $cancel = Get-Buttons $prompt | Where-Object { $_.Current.Name -eq 'Cancel' } | Select-Object -First 1
    if ($cancel) { Press-Button $procId $prompt 'Cancel' | Out-Null }
    Start-Sleep -Milliseconds 1500
    $proc.Refresh()
    if (-not $proc.HasExited) { Pass 'Cancelling the close prompt keeps the application running' }
    else { Fail 'Application exited even though the prompt was cancelled' }
} else { Fail 'Closing with unsaved text did not raise the prompt' }

# discard and exit
$null = $proc.CloseMainWindow()
Start-Sleep -Milliseconds 2000
$prompt = Wait-Window $procId '*Unsaved text*' 8000
if ($prompt) {
    $no = Get-Buttons $prompt | Where-Object { $_.Current.Name -eq 'No' } | Select-Object -First 1
    if ($no -and (Press-Button $procId $prompt 'No')) { Pass 'Discard path can be chosen from the prompt' } else { Fail 'No button not found or not reachable' }
} else { Fail 'Second close prompt missing' }

$exited = $proc.WaitForExit(25000)
if ($exited) {
    if ($proc.ExitCode -eq 0) { Pass 'Discard path exits the application normally with exit code 0' }
    else { Fail "Application exited with code $($proc.ExitCode)" }
} else { Fail 'Application did not exit within 25 seconds'; $proc.Kill() }
}
finally {
    if ($null -ne $settingsBackup) { Set-Content $settingsPath -Value $settingsBackup -Encoding UTF8 }
    elseif (Test-Path $settingsPath) { Remove-Item $settingsPath -Force }
    foreach ($p in (Get-Process -Name TextBridge -ErrorAction SilentlyContinue)) { $p.Kill() }
}

Write-Output ''
if ($script:fail -eq 0) { Write-Output "UIA audit: all $($script:results.Count) checks passed." }
else { Write-Output "UIA audit: $script:fail of $($script:results.Count) checks FAILED." }
exit $script:fail
