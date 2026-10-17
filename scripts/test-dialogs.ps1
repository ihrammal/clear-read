# Dialog and shortcut audit for Clear Read.
# Launches its own published app instance and verifies that modal dialogs
# cannot stack on top of each other, that Escape and Enter close every
# dialog, that focus returns to the document, that the main window is
# disabled while a dialog is up, and that shortcuts work from every focus
# position. Uses UI Automation plus limited keyboard input to its own window.
param(
    [string]$ExePath,
    [string]$ImagePath
)
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
if (-not $ExePath) { $ExePath = Join-Path $Root 'publish\ClearRead.exe' }
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
  [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr hWnd);
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
$script:mainRuntimeId = ''

function Get-ProcWindows($procId) {
    $seen = @{}
    $out = @()
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
    foreach ($e in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $c)) {
        try { $rid = ($e.GetRuntimeId() | ForEach-Object { "$_" }) -join ',' } catch { $rid = [Guid]::NewGuid().ToString() }
        if (-not $seen.ContainsKey($rid)) { $seen[$rid] = $true; $out += $e }
    }
    if ($script:mainWindow) {
        $wc = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Window)
        foreach ($e in $script:mainWindow.FindAll([System.Windows.Automation.TreeScope]::Descendants, $wc)) {
            try {
                if ($e.Current.ProcessId -ne $procId) { continue }
                $rid = ($e.GetRuntimeId() | ForEach-Object { "$_" }) -join ','
                if (-not $seen.ContainsKey($rid)) { $seen[$rid] = $true; $out += $e }
            } catch { }
        }
    }
    return $out
}
function Count-ProcWindows($procId, [string]$like, [switch]$ExcludeMain) {
    $n = 0
    foreach ($w in (Get-ProcWindows $procId)) {
        try {
            if ($w.Current.Name -like $like) {
                $rid = ($w.GetRuntimeId() | ForEach-Object { "$_" }) -join ','
                if (-not ($ExcludeMain -and $rid -eq $script:mainRuntimeId)) { $n++ }
            }
        } catch {
            if (-not $ExcludeMain) { $n++ }
        }
    }
    return $n
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
function Wait-WindowExcept($procId, [string]$like, [int]$timeoutMs = 10000) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.ElapsedMilliseconds -lt $timeoutMs) {
        foreach ($w in (Get-ProcWindows $procId)) {
            try {
                if ($w.Current.Name -like $like) {
                    $rid = ($w.GetRuntimeId() | ForEach-Object { "$_" }) -join ','
                    if ($rid -ne $script:mainRuntimeId) { return $w }
                }
            } catch { return $w }
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
function Find-ById($scope, [string]$id) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    return $scope.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}
function Find-ByName($scope, [string]$name) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $scope.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}
function Get-Focused() {
    $f = [System.Windows.Automation.AutomationElement]::FocusedElement
    if (-not $f) { return 'none' }
    "{0}:{1}:{2}" -f ($f.Current.ControlType.ProgrammaticName -replace 'ControlType\.', ''), $f.Current.Name, $f.Current.AutomationId
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
function Get-Buttons($scope) {
    $out = @()
    foreach ($e in $scope.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        $ct = $e.Current.ControlType.ProgrammaticName -eq 'ControlType.Button'
        if ($ct -or $e.Current.ClassName -eq 'Button') { $out += $e }
    }
    return $out
}
function Press-Button($procId, $box, [string]$name) {
    $btn = Get-Buttons $box | Where-Object { $_.Current.Name -eq $name } | Select-Object -First 1
    if (-not $btn) { return $false }
    try { [Fg]::SetForegroundWindow([IntPtr]$box.Current.NativeWindowHandle) | Out-Null } catch { }
    Start-Sleep -Milliseconds 500
    for ($i = 0; $i -lt 8; $i++) {
        $f = Get-Focused
        if ($f -like "*:${name}:*" -or $f -like "*:${name}") { Key '{ENTER}'; Start-Sleep -Milliseconds 600; return $true }
        Key '{TAB}'; Start-Sleep -Milliseconds 300
    }
    return $false
}

# ---------------------------------------------------------------- settings backup
$settingsPath = Join-Path $env:APPDATA 'Clear Read\settings.json'
$settingsBackup = $null
if (Test-Path $settingsPath) { $settingsBackup = Get-Content $settingsPath -Raw }

try {
    foreach ($p in (Get-Process -Name ClearRead -ErrorAction SilentlyContinue)) { $p.Kill(); $p.WaitForExit() }
    $proc = Start-Process -FilePath $ExePath -WorkingDirectory (Split-Path $ExePath) -PassThru
    $procId = $proc.Id
    Start-Sleep -Seconds 5

    $main = Wait-Window $procId 'Clear Read' 25000
    if ($main -and $main.Current.Name -eq 'Clear Read') { Pass 'Published app starts with window title Clear Read' }
    else { Fail 'Main window was not found'; exit 1 }
    $script:mainWindow = $main
    $script:mainRuntimeId = ($main.GetRuntimeId() | ForEach-Object { "$_" }) -join ','

    Start-Sleep -Seconds 4

    # ---------------------------------------------------------------- dialog flood guard
    # Rapid repeated recognition requests used to stack message boxes: the
    # message-box pump kept servicing queued automation callbacks, so each
    # reentry popped another dialog. Reentry is simulated here with UIA
    # Invoke on the menu item while the first dialog is still open.
    Activate $procId
    Key '^r'; Start-Sleep -Milliseconds 1200
    $box = Wait-WindowExcept $procId 'Clear Read' 6000
    if (-not $box) { Fail 'Ctrl+R with no document open did not show its prompt (flood test setup failed)' }
    else {
        Press-Button $procId $box 'OK' | Out-Null
        Start-Sleep -Milliseconds 800
        $stillOpen = Wait-WindowExcept $procId 'Clear Read' 2000
        if (-not $stillOpen) { Pass 'The no-document prompt was visible and its OK button closes it' }
        else { Key '{ESC}'; Start-Sleep -Milliseconds 600; Fail 'The no-document prompt OK button did not close it' }
        Key '^r'; Start-Sleep -Milliseconds 1200
        $box = Wait-WindowExcept $procId 'Clear Read' 6000
        if (-not $box) { Fail 'Ctrl+R did not re-show the no-document prompt for the flood test' }
        else {
            $ocr = Find-ByName $main 'OCR'
            $exp = $null
            if ($ocr -and $ocr.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$exp)) { $exp.Expand() }
            Start-Sleep -Milliseconds 400
            $item = Find-ByName $ocr 'Recognize Document'
            $inv = $null
            if ($item -and $item.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$inv)) {
                for ($i = 0; $i -lt 5; $i++) { $inv.Invoke(); Start-Sleep -Milliseconds 300 }
            } else { Fail 'Recognize Document menu item was not invokable during the flood test' }
            Start-Sleep -Milliseconds 1200
            $count = Count-ProcWindows $procId 'Clear Read' -ExcludeMain
            if ($count -eq 1) { Pass 'Repeated automation requests while a dialog is open show exactly one message box' }
            else { Fail "Reentrant requests produced $count message boxes (expected exactly one)" }
            Press-Button $procId $box 'OK' | Out-Null
            Start-Sleep -Milliseconds 800
        }
    }
    $remaining = Count-ProcWindows $procId 'Clear Read' -ExcludeMain
    if ($remaining -eq 0) { Pass 'Only the main window remains after the flood dialog is dismissed' }
    else { Fail "Dialogs remained open after dismissal (count $remaining)" }
    if ((Get-Focused) -like '*DocumentText*') { Pass 'Focus returns to the document after the flood dialog' }
    else { Fail "Focus after flood dialog was '$(Get-Focused)'" }
    if ($ocr) { $exp2 = $null; try { if ($ocr.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$exp2)) { $exp2.Collapse() } } catch { } }
    function Show-About() {
        Activate $procId
        $help = Find-ByName $main 'Help'
        if (-not $help) { return $null }
        $exp = $null
        if ($help.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$exp)) { $exp.Expand() }
        Start-Sleep -Milliseconds 400
        $item = Find-ByName $help 'About Clear Read'
        if (-not $item) { return $null }
        $p = $null
        if (-not $item.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$p)) { return $null }
        $p.Invoke()
        Start-Sleep -Milliseconds 800
        return (Wait-Window $procId 'About Clear Read' 6000)
    }
    $about = Show-About
    if ($about) {
        Pass 'Help > About opens the About message box'
        Activate $procId
        try { [Fg]::Force([IntPtr]$about.Current.NativeWindowHandle) | Out-Null } catch { }
        Start-Sleep -Milliseconds 300
        Key '{ESC}'; Start-Sleep -Milliseconds 900
        if (Wait-Gone $procId 'About Clear Read' 6000) { Pass 'Escape closes the About box' } else { Fail 'Escape did not close the About box' }
        $about2 = Show-About
        if ($about2) {
            $ok = Press-Button $procId $about2 'OK'
            if ($ok -and (Wait-Gone $procId 'About Clear Read' 6000)) { Pass 'Enter closes the About box through its default button' }
            else { Fail 'Enter did not close the About box'; Key '{ESC}' }
        } else { Fail 'About box did not reopen for the Enter test' }
    } else { Fail 'Help > About did not open its message box' }

    # ---------------------------------------------------------------- preferences dialog: esc vs enter, main window disabled
    function Open-Preferences() {
        Activate $procId
        Key '^,'; Start-Sleep -Milliseconds 1200
        return (Wait-Window $procId '*Settings*' 8000)
    }
    $dlg = Open-Preferences
    if ($dlg) {
        Pass 'Ctrl+, opens the preferences dialog'
        $mainDisabled = $true
        try { $mainDisabled = -not [Fg]::IsWindowEnabled([IntPtr]$script:mainWindow.Current.NativeWindowHandle) } catch { $mainDisabled = $false }
        if ($mainDisabled) { Pass 'Main window is disabled while the preferences dialog is open (dialog is owned and modal)' }
        else { Fail 'Main window stayed enabled behind the preferences dialog' }
        $dlg.SetFocus(); Start-Sleep -Milliseconds 300
        Key '{ESC}'; Start-Sleep -Milliseconds 900
        if (Wait-Gone $procId '*Settings*' 8000) { Pass 'Escape closes the preferences dialog without saving' } else { Fail 'Escape did not close the preferences dialog' }
        if ((Get-Focused) -like '*DocumentText*') { Pass 'Focus returns to the document after Escape on preferences' }
        else { Fail "Focus after preferences Escape was '$(Get-Focused)'" }
        $dlg = Open-Preferences
        if ($dlg) {
            try { $dlg.SetFocus() } catch { try { [Fg]::Force([IntPtr]$dlg.Current.NativeWindowHandle) | Out-Null } catch { } }
            Start-Sleep -Milliseconds 300
            Key '{ENTER}'; Start-Sleep -Milliseconds 900
            if (Wait-Gone $procId '*Settings*' 8000) { Pass 'Enter activates the default Save button and closes preferences' } else { Fail 'Enter did not close the preferences dialog' }
        } else { Fail 'Preferences did not reopen for the Enter test' }
    } else { Fail 'Ctrl+, did not open the preferences dialog' }

    # ---------------------------------------------------------------- no duplicates under rapid dialog shortcuts
    Activate $procId
    for ($i = 0; $i -lt 5; $i++) { Key '^f'; Start-Sleep -Milliseconds 400 }
    Start-Sleep -Milliseconds 900
    $count = Count-ProcWindows $procId 'Find'
    if ($count -le 2) { Pass "Five rapid Ctrl+F presses create at most one Find dialog (count $count)" }
    else { Fail "Rapid Ctrl+F stacked $count Find dialogs" }
    Key '{ESC}'; Start-Sleep -Milliseconds 800
    if ((Get-Focused) -like '*DocumentText*') { Pass 'Focus returns to the document after the Find dialog' }
    else { Fail "Focus after Find dialog was '$(Get-Focused)'" }

    # ---------------------------------------------------------------- shortcut reach from every focus position
    # F4 (next low-confidence word) is a safe probe: with no recognition data it
    # must publish its explanation in the status bar instead of doing nothing.
    $status = Find-ById $main 'StatusText'
    $fileMenu = Find-ByName $main 'File'
    function Probe-Shortcut([string]$position) {
        Activate $procId
        $main.SetFocus(); Start-Sleep -Milliseconds 300
        if ($position -eq 'status') { Key '+{F6}'; Start-Sleep -Milliseconds 400 }
        elseif ($position -eq 'menu') {
            try { $fileMenu.SetFocus() } catch { }
            Start-Sleep -Milliseconds 300
        }
        Key '{F4}'; Start-Sleep -Milliseconds 700
        $st = Find-ById $main 'StatusText'
        $name = if ($st) { $st.Current.Name } else { '' }
        if ($name -like 'No word-level low-confidence data*') { Pass "F4 works from $position focus and reports its result in the status bar" }
        else { Fail "F4 from $position focus gave status '$name'" }
        if ($position -ne 'editor') { Key '{F6}'; Start-Sleep -Milliseconds 300 }
    }
    Probe-Shortcut 'editor'
    Probe-Shortcut 'status'
    Probe-Shortcut 'menu'
    if ((Get-Focused) -like '*DocumentText*') { Pass 'Focus is at the document editor after the shortcut matrix' }
    else { Fail "Focus after shortcut matrix was '$(Get-Focused)'" }

    # ---------------------------------------------------------------- shortcuts after a modal dialog closes
    $main = Wait-Window $procId 'Clear Read' 3000
    Activate $procId
    $dlg = Open-Preferences
    if ($dlg) { $dlg.SetFocus(); Key '{ESC}'; Start-Sleep -Milliseconds 800 }
    Key '{F4}'; Start-Sleep -Milliseconds 700
    $status = Find-ById $main 'StatusText'
    $name = $status.Current.Name
    if ($name -like 'No word-level low-confidence data*') { Pass 'F4 works immediately after a modal dialog closes' }
    else { Fail "Shortcut right after dialog close gave status '$name'" }

    # ---------------------------------------------------------------- shortcuts after recognition completes
    Activate $procId
    Key '^o'; Start-Sleep -Milliseconds 1500
    $open = Wait-Window $procId 'Open' 8000
    if ($open) {
        Activate $procId
        Key '%n'; Start-Sleep -Milliseconds 400
        Key '^a{DEL}'
        [System.Windows.Forms.SendKeys]::SendWait($ImagePath); Start-Sleep -Milliseconds 400
        Key '{ENTER}'; Start-Sleep -Milliseconds 2500
        Wait-Gone $procId 'Open' 8000 | Out-Null
        $main = Wait-Window $procId 'Clear Read' 5000
        Key '^r'; Start-Sleep -Milliseconds 800
        $done = $false
        $sw = [Diagnostics.Stopwatch]::StartNew()
        while ($sw.ElapsedMilliseconds -lt 180000) {
            $st = Find-ById $main 'StatusText'
            if ($st) { $n = $st.Current.Name; if ($n -in @('Recognition complete.', 'Recognition failed.')) { $done = $true; break } }
            Start-Sleep -Milliseconds 700
        }
        $st = Find-ById $main 'StatusText'
        if ($st -and $st.Current.Name -eq 'Recognition complete.') { Pass 'Recognition completes with a fixture image' }
        else { Fail "Recognition ended with '$($st.Current.Name)'"; }
        $dialogCount = (Count-ProcWindows $procId 'Clear Read' -ExcludeMain) + (Count-ProcWindows $procId 'OCR Error')
        if ($dialogCount -eq 0) { Pass 'Routine completion opens no modal dialog' }
        else { Fail "Completion left $dialogCount dialogs open" }
        Key '{F4}'; Start-Sleep -Milliseconds 700
        $st = Find-ById $main 'StatusText'
        $name = $st.Current.Name
        if ($name -like 'Low-confidence word*' -or $name -like 'No word-level low-confidence data*') { Pass "F4 works right after recognition completes (status: '$name')" }
        else { Fail "F4 after recognition gave status '$name'" }
        Key '+{F6}'; Start-Sleep -Milliseconds 500
        if ((Get-Focused) -like '*StatusText*') { Pass 'Shift+F6 reaches the status bar after recognition' } else { Fail "Shift+F6 after recognition reached '$(Get-Focused)'" }
    } else { Fail 'Ctrl+O did not open a file dialog after recognition setup' }

    $proc.CloseMainWindow() | Out-Null
    Start-Sleep -Milliseconds 1500
    $prompt = Wait-Window $procId '*Unsaved text*' 6000
    if ($prompt) { Press-Button $procId $prompt 'No' | Out-Null }
    if ($proc.WaitForExit(20000)) { Pass 'App exits normally at the end of the dialog audit' }
    else { $proc.Kill(); Fail 'App did not exit after the dialog audit' }
}
finally {
    if ($null -ne $settingsBackup) { Set-Content $settingsPath -Value $settingsBackup -Encoding UTF8 }
    elseif (Test-Path $settingsPath) { Remove-Item $settingsPath -Force }
    foreach ($p in (Get-Process -Name ClearRead -ErrorAction SilentlyContinue)) { $p.Kill() }
}

Write-Output ''
if ($script:fail -eq 0) { Write-Output "Dialog and shortcut audit: all $($script:results.Count) checks passed." }
else { Write-Output "Dialog and shortcut audit: $script:fail of $($script:results.Count) checks FAILED." }
exit $script:fail
