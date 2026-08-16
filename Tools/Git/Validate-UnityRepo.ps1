[CmdletBinding()]
param(
    [string]$ProjectRoot,
    [string]$ManifestPath = "Docs/Development/asset-import-manifest.json"
)

$ErrorActionPreference = "Stop"
$failures = [System.Collections.Generic.List[string]]::new()
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
}

$root = (Resolve-Path -LiteralPath $ProjectRoot).Path
$rootForGit = $root.Replace('\', '/')
$legacyLargeFiles = @(
    'Assets/TextMesh Pro/Fonts/Maplestory Bold SDF.asset',
    'Assets/TextMesh Pro/Fonts/Maplestory Light SDF.asset',
    'Assets/TextMesh Pro/Fonts/NotoSansKR-Light SDF.asset'
)

function Add-Failure([string]$message) {
    $script:failures.Add($message)
}

$versionFile = Join-Path $root "ProjectSettings/ProjectVersion.txt"
if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
    Add-Failure "Missing ProjectSettings/ProjectVersion.txt"
} else {
    $versionText = Get-Content -LiteralPath $versionFile -Raw -Encoding UTF8
    if ($versionText -notmatch '(?m)^m_EditorVersion:\s*2022\.3\.60f1\s*$') {
        Add-Failure "Unity version must be 2022.3.60f1"
    }
}

$repoFiles = @(& git -c "safe.directory=$rootForGit" -c core.quotePath=false -C $root ls-files --cached --others --exclude-standard)
if ($LASTEXITCODE -ne 0) {
    throw "git ls-files failed with exit code $LASTEXITCODE"
}

foreach ($relativePath in $repoFiles) {
    $path = $relativePath.Replace('\', '/')

    if ($path -match '(^|/)(Library|Temp|Obj|Build|Builds|Releases)(/|$)') {
        Add-Failure "Generated Unity output is not allowed: $path"
    }

    if ($path -match '(^|/)(Frames|QualityRefresh)(/|$)' -or
        $path -match '(?i)(fullres|native)') {
        Add-Failure "Raw or intermediate art is not allowed: $path"
    }

    $extension = [System.IO.Path]::GetExtension($path)
    if ($extension -ieq '.exe' -or
        ($extension -ieq '.dll' -and $path -notmatch '(?i)^Assets/Plugins/')) {
        Add-Failure "Generated executable or assembly is not allowed: $path"
    }

    $absolutePath = Join-Path $root $relativePath
    if (Test-Path -LiteralPath $absolutePath -PathType Leaf) {
        $length = (Get-Item -LiteralPath $absolutePath).Length
        if ($length -gt 20MB -and $legacyLargeFiles -notcontains $path) {
            Add-Failure "File exceeds 20 MiB: $path ($length bytes)"
        }
    }
}

$assetsRoot = Join-Path $root "Assets"
$assetFiles = @()
if (Test-Path -LiteralPath $assetsRoot -PathType Container) {
    $assetFiles = @(Get-ChildItem -LiteralPath $assetsRoot -Recurse -File -Force)
    foreach ($file in $assetFiles) {
        if ($file.Name.EndsWith('.meta', [StringComparison]::OrdinalIgnoreCase)) {
            $target = $file.FullName.Substring(0, $file.FullName.Length - 5)
            if (-not (Test-Path -LiteralPath $target)) {
                Add-Failure "Orphan meta file: $($file.FullName.Substring($root.Length + 1))"
            }
            continue
        }

        if (-not (Test-Path -LiteralPath ($file.FullName + '.meta') -PathType Leaf)) {
            Add-Failure "Missing meta file: $($file.FullName.Substring($root.Length + 1))"
        }
    }
}

$manifestAbsolutePath = Join-Path $root $ManifestPath
$manifestCount = 0
if (-not (Test-Path -LiteralPath $manifestAbsolutePath -PathType Leaf)) {
    Add-Failure "Missing asset import manifest: $ManifestPath"
} else {
    try {
        $manifest = Get-Content -LiteralPath $manifestAbsolutePath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($manifest.schemaVersion -ne 1) {
            Add-Failure "Manifest schemaVersion must be 1"
        }
        if ($null -eq $manifest.entries) {
            Add-Failure "Manifest must contain an entries array"
        } else {
            $manifestCount = @($manifest.entries).Count
        }
    } catch {
        Add-Failure "Manifest JSON is invalid: $($_.Exception.Message)"
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL unity-repo-hygiene failures=$($failures.Count)"
    $failures | Sort-Object -Unique | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host "PASS unity-repo-hygiene files=$($repoFiles.Count) assets=$($assetFiles.Count) manifest=$manifestCount"
