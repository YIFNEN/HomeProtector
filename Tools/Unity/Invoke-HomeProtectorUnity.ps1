[CmdletBinding()]
param(
    [ValidateSet('EditMode', 'PlayMode', 'Validate')]
    [string]$Mode = 'EditMode',
    [string]$ProjectRoot,
    [string]$UnityPath = $env:UNITY_EDITOR_PATH
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
}

$root = (Resolve-Path -LiteralPath $ProjectRoot).Path

if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $candidates = @(
        'E:\tools\2022.3.60f1\Editor\Unity.exe',
        'C:\Program Files\Unity\Hub\Editor\2022.3.60f1\Editor\Unity.exe'
    )
    $UnityPath = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($UnityPath) -or -not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw 'Unity 2022.3.60f1 was not found. Set UNITY_EDITOR_PATH or pass -UnityPath.'
}

$resultDirectory = Join-Path $root 'Logs/HomeProtectorAutomation'
New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null
$modeSlug = $Mode.ToLowerInvariant()
$logPath = Join-Path $resultDirectory "$modeSlug.log"
$resultPath = Join-Path $resultDirectory "$modeSlug-results.xml"

if ($Mode -in @('EditMode', 'PlayMode') -and (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
    Remove-Item -LiteralPath $resultPath -Force
}

$arguments = @(
    '-batchmode',
    '-nographics',
    '-projectPath', $root,
    '-logFile', $logPath
)

if ($Mode -eq 'Validate') {
    $arguments += '-quit'
}

switch ($Mode) {
    'EditMode' {
        $arguments += @('-runTests', '-testPlatform', 'EditMode', '-testResults', $resultPath)
    }
    'PlayMode' {
        $arguments += @('-runTests', '-testPlatform', 'PlayMode', '-testResults', $resultPath)
    }
    'Validate' {
        $arguments += @('-executeMethod', 'HomeProtector.Editor.AssetPipeline.HomeProtectorAutomation.ValidateProject')
    }
}

$processArguments = @($arguments | ForEach-Object {
    $value = [string]$_
    if ($value -match '[\s"]') {
        return '"' + $value.Replace('"', '\"') + '"'
    }
    return $value
})
$unityProcess = Start-Process -FilePath $UnityPath `
    -ArgumentList $processArguments `
    -PassThru `
    -WindowStyle Hidden
$unityProcess.WaitForExit()
$exitCode = $unityProcess.ExitCode
$logText = if (Test-Path -LiteralPath $logPath) {
    Get-Content -LiteralPath $logPath -Raw -Encoding UTF8
} else {
    ''
}

if ($logText -match 'No valid Unity Editor license found|LICENSE SYSTEM.*No valid license') {
    Write-Host "BLOCKED unity-$modeSlug reason=missing-license log=$logPath"
    exit 3
}

if ($Mode -in @('EditMode', 'PlayMode')) {
    if (Test-Path -LiteralPath $resultPath -PathType Leaf) {
        [xml]$results = Get-Content -LiteralPath $resultPath -Raw -Encoding UTF8
        $run = $results.'test-run'
        if ($null -eq $run) {
            Write-Host "FAIL unity-$modeSlug reason=invalid-results result=$resultPath log=$logPath"
            exit 2
        }

        $total = [int]$run.total
        $passed = [int]$run.passed
        $failed = [int]$run.failed
        if ($total -le 0) {
            Write-Host "FAIL unity-$modeSlug reason=no-tests result=$resultPath log=$logPath"
            exit 2
        }


        if ($failed -gt 0) {
            Write-Host "FAIL unity-$modeSlug total=$total passed=$passed failed=$failed result=$resultPath log=$logPath"
            exit 1
        }

        if ($exitCode -ne 0) {
            Write-Host "FAIL unity-$modeSlug exit=$exitCode total=$total passed=$passed failed=$failed result=$resultPath log=$logPath"
            exit $exitCode
        }

        Write-Host "PASS unity-$modeSlug total=$total passed=$passed failed=$failed result=$resultPath"
        exit 0
    }

    if ($exitCode -ne 0) {
        Write-Host "FAIL unity-$modeSlug exit=$exitCode log=$logPath"
        exit $exitCode
    }

    Write-Host "FAIL unity-$modeSlug reason=missing-results log=$logPath"
    exit 2
}

if ($exitCode -ne 0) {
    Write-Host "FAIL unity-$modeSlug exit=$exitCode log=$logPath"
    exit $exitCode
}

Write-Host "PASS unity-$modeSlug log=$logPath"
