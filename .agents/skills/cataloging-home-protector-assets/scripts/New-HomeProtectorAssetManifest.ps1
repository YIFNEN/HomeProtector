[CmdletBinding()]
param(
    [string]$SourceRoot = 'D:\GameAsset\GameAssets\HomeProtector\UnityReadySprites',
    [string]$OutputPath,
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $projectRoot 'Docs/Development/asset-import-manifest.json'
}

$source = (Resolve-Path -LiteralPath $SourceRoot).Path.TrimEnd('\')
$destinationRoot = 'Assets/_Project/Art/Runtime'
$directionRows = @('DownLeft', 'DownRight', 'UpLeft', 'UpRight')

function Get-RelativeSourcePath([System.IO.FileInfo]$file) {
    return $file.FullName.Substring($source.Length).TrimStart('\').Replace('\', '/')
}

function Get-ImageSize([string]$path) {
    $image = [System.Drawing.Image]::FromFile($path)
    try {
        return [ordered]@{ width = $image.Width; height = $image.Height }
    } finally {
        $image.Dispose()
    }
}

function Get-ContentKind([string]$relativePath) {
    return ($relativePath -split '/')[0]
}

function Get-Pivot([string]$kind, [string]$relativePath) {
    if ($kind -in @('VFX', 'Currency', 'UI')) {
        return 'Center'
    }
    if ($kind -eq 'Enemies' -and $relativePath -match '/(Wasp|AntSwarm|MothPest)/') {
        return 'Center'
    }
    if ($kind -in @('Tiles', 'Overlays', 'Props')) {
        if ($relativePath -match '/(GroundTiles|GroundOverlays)/') {
            return 'Center'
        }
        if ($relativePath -match 'Environment_Foundation|OldCabin_Tiles') {
            return 'MixedBySlice'
        }
    }
    return 'BottomCenter'
}

function Get-ClipMeaning([string]$relativePath, [bool]$isSheet) {
    if (-not $isSheet) {
        return 'Static'
    }
    foreach ($name in @('Idle', 'Walk', 'Move', 'Attack', 'BuffTower', 'Damage', 'Sleep')) {
        if ($relativePath -match "_$name(_|\.|/)" -or $relativePath -match "/$name/") {
            return $name
        }
    }
    if ($relativePath -match '/VFX/([^/]+)/') {
        return $matches[1]
    }
    if ($relativePath -match '/Valuables/' -and $relativePath -match '/Damage/') {
        return 'DamageStates'
    }
    return 'Atlas'
}

function Get-FrameRate([string]$clipMeaning) {
    switch ($clipMeaning) {
        'Idle' { return 4 }
        'Sleep' { return 4 }
        'Walk' { return 8 }
        'Move' { return 8 }
        'BuffTower' { return 8 }
        'Damage' { return 8 }
        'DamageStates' { return 8 }
        'Attack' { return 10 }
        default {
            if ($clipMeaning -in @('Atlas', 'Static')) { return 0 }
            return 10
        }
    }
}

function Get-CellSize([string]$kind, [string]$relativePath, [bool]$isSheet, [hashtable]$imageSize) {
    if (-not $isSheet) {
        return [ordered]@{ width = $imageSize.width; height = $imageSize.height }
    }
    if ($kind -eq 'Currency') {
        return [ordered]@{ width = 64; height = 64 }
    }
    if ($kind -in @('Player', 'Enemies')) {
        return [ordered]@{ width = 96; height = 96 }
    }
    if ($kind -eq 'VFX') {
        $size = if ($relativePath -match 'LevelUpGlow') { 128 } else { 96 }
        return [ordered]@{ width = $size; height = $size }
    }
    return [ordered]@{ width = 128; height = 128 }
}

function Get-PixelsPerUnit([string]$kind) {
    if ($kind -eq 'Player') {
        return 32
    }
    if ($kind -in @('Tiles', 'Props', 'Overlays')) {
        return 128
    }
    return 100
}

function Get-SelectionReason([string]$relativePath, [string]$status, [string]$assetType) {
    if ($status -eq 'excluded') {
        return 'Historical Player Walk revision; B08 is canonical.'
    }
    if ($relativePath -match '^Enemies/Bear/B20/') {
        return 'Canonical normal Bear role; intentionally distinct from BearHeavy B31.'
    }
    if ($relativePath -match '^Enemies/BearHeavy/B31/') {
        return 'Canonical elite BearHeavy role; intentionally distinct from Bear B20.'
    }
    if ($assetType -eq 'singleSprite' -and $relativePath -match '^Towers/') {
        return 'Canonical static level sprite for Dryer, BookShelf, or CoolDryer.'
    }
    if ($assetType -eq 'singleSprite' -and $relativePath -match '^Valuables/') {
        return 'Canonical undamaged valuable sprite paired with its damage sheet.'
    }
    if ($assetType -eq 'singleSprite' -and $relativePath -match '^UI/') {
        return 'Canonical runtime UI sprite.'
    }
    return 'Promoted UnityReady sheet for a distinct runtime role and revision.'
}

function New-ManifestEntry([System.IO.FileInfo]$file, [string]$assetType, [string]$status) {
    $relativePath = Get-RelativeSourcePath $file
    $kind = Get-ContentKind $relativePath
    $imageSize = Get-ImageSize $file.FullName
    $isSheet = $assetType -eq 'spriteSheet'
    $cellSize = Get-CellSize $kind $relativePath $isSheet $imageSize

    if (($imageSize.width % $cellSize.width) -ne 0 -or ($imageSize.height % $cellSize.height) -ne 0) {
        throw "Image dimensions do not match cell contract: $relativePath"
    }

    $columns = [int]($imageSize.width / $cellSize.width)
    $rows = [int]($imageSize.height / $cellSize.height)
    $clipMeaning = Get-ClipMeaning $relativePath $isSheet
    $batch = if ($relativePath -match '(?<batch>B\d{2})') { $matches.batch } else { $null }
    $id = (($relativePath.ToLowerInvariant() -replace '\.png$', '') -replace '[^a-z0-9]+', '-').Trim('-')
    $directions = if ($isSheet -and $rows -eq 4 -and $kind -in @('Player', 'Enemies', 'Towers')) {
        $directionRows
    } else {
        @()
    }
    $loop = $clipMeaning -in @('Idle', 'Walk', 'Move', 'BuffTower', 'Sleep')
    if ($kind -eq 'Towers' -and $clipMeaning -eq 'Attack') {
        $loop = $true
    }

    return [ordered]@{
        id = $id
        assetType = $assetType
        contentKind = $kind
        runtimeRole = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        sourceRelativePath = $relativePath
        destinationPath = "$destinationRoot/$relativePath"
        batch = $batch
        sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        bytes = $file.Length
        image = $imageSize
        selection = [ordered]@{
            status = $status
            reason = Get-SelectionReason $relativePath $status $assetType
        }
        importer = [ordered]@{
            textureType = 'Sprite'
            spriteMode = if ($isSheet) { 'Multiple' } else { 'Single' }
            cell = $cellSize
            columns = $columns
            rows = $rows
            pixelsPerUnit = Get-PixelsPerUnit $kind
            pivot = Get-Pivot $kind $relativePath
            directionRows = @($directions)
            clipMeaning = $clipMeaning
            frameRate = Get-FrameRate $clipMeaning
            loop = [bool]$loop
            filterMode = 'Point'
            compression = 'Uncompressed'
        }
    }
}

$pngFiles = @(Get-ChildItem -LiteralPath $source -Recurse -File -Filter '*.png')
$sheetCandidates = @($pngFiles | Where-Object { $_.Name -match '_Sheet_B\d{2}\.png$' })

$singleSprites = @($pngFiles | Where-Object {
    $relative = Get-RelativeSourcePath $_
    $relative -match '^Valuables/[^/]+/Normal/B\d{2}/[^/]+\.png$' -or
    $relative -match '^Towers/T_HP_Tower_(BookShelf|CoolDryer|Dryer)_Lv0[123]\.png$' -or
    $relative -match '^UI/[^/]+\.png$'
})

$entries = [System.Collections.Generic.List[object]]::new()
foreach ($file in ($sheetCandidates | Sort-Object FullName)) {
    $relative = Get-RelativeSourcePath $file
    $status = if ($relative -match '^Player/Walk/B0(5|6)/') { 'excluded' } else { 'included' }
    $entries.Add((New-ManifestEntry $file 'spriteSheet' $status))
}
foreach ($file in ($singleSprites | Sort-Object FullName)) {
    $entries.Add((New-ManifestEntry $file 'singleSprite' 'included'))
}

$included = @($entries | Where-Object { $_.selection.status -eq 'included' })
$sheetIncluded = @($included | Where-Object { $_.assetType -eq 'spriteSheet' })
$sheetExcluded = @($entries | Where-Object {
    $_.assetType -eq 'spriteSheet' -and $_.selection.status -eq 'excluded'
})
$singleIncluded = @($included | Where-Object { $_.assetType -eq 'singleSprite' })

if ($sheetCandidates.Count -ne 103 -or $sheetIncluded.Count -ne 101 -or
    $sheetExcluded.Count -ne 2 -or $singleIncluded.Count -ne 22) {
    throw "Unexpected canonical counts: candidates=$($sheetCandidates.Count) includedSheets=$($sheetIncluded.Count) excludedSheets=$($sheetExcluded.Count) singles=$($singleIncluded.Count)"
}

$duplicateDestinations = @($included | Group-Object { $_.destinationPath } | Where-Object Count -gt 1)
if ($duplicateDestinations.Count -gt 0) {
    throw "Destination collision: $($duplicateDestinations[0].Name)"
}

$duplicateHashes = @($included | Group-Object { $_.sha256 } | Where-Object Count -gt 1)
if ($duplicateHashes.Count -gt 0) {
    throw "Duplicate included content hash: $($duplicateHashes[0].Name)"
}

$manifest = [ordered]@{
    schemaVersion = 1
    sourceRoot = $source.Replace('\', '/')
    destinationRoot = $destinationRoot
    policy = [ordered]@{
        candidates = 'Promoted *_Sheet_BNN.png plus explicit canonical single-sprite allowlist.'
        excludedPatterns = @('Frames', 'fullres', 'native', 'QualityRefresh', 'historical comparison revisions')
        retainedExistingProjectContent = @('CommonSoldier', 'Monkey')
        intentionalRoleSplit = @('Bear B20', 'BearHeavy B31')
    }
    summary = [ordered]@{
        sheetCandidates = $sheetCandidates.Count
        sheetsIncluded = $sheetIncluded.Count
        sheetsExcluded = $sheetExcluded.Count
        singleSpritesIncluded = $singleIncluded.Count
        totalIncluded = $included.Count
        includedBytes = ($included | ForEach-Object { $_.bytes } | Measure-Object -Sum).Sum
    }
    entries = @($entries)
}

$json = $manifest | ConvertTo-Json -Depth 12
if ($Check) {
    if (-not (Test-Path -LiteralPath $OutputPath -PathType Leaf)) {
        throw "Manifest does not exist: $OutputPath"
    }
    $existing = Get-Content -LiteralPath $OutputPath -Raw -Encoding UTF8
    if ($existing.Trim() -ne $json.Trim()) {
        throw 'Manifest is stale. Regenerate it with this script.'
    }
} else {
    $directory = Split-Path -Parent $OutputPath
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    [System.IO.File]::WriteAllText($OutputPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
}

Write-Host "PASS asset-manifest sheets=$($sheetIncluded.Count)/$($sheetCandidates.Count) singles=$($singleIncluded.Count) total=$($included.Count)"
