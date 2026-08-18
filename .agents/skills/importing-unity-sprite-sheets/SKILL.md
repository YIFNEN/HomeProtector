---
name: importing-unity-sprite-sheets
description: Use when copying manifest-approved Home Protector art into Unity, configuring TextureImporter and sprite slicing, or generating AnimationClips and controllers from importer contracts.
---

# Importing Unity Sprite Sheets

## Overview

Turn the audited manifest into Unity assets without duplicating Unity's job. The agent chooses and checks the import contract; a single Unity Editor C# command performs copy, import, slice, naming, clip, and controller mutations.

## Preconditions

1. Read the root and `Assets/_Project/AGENTS.md` files.
2. Run the catalog script with **Windows PowerShell 5.1**, not `pwsh 7`: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File <catalog-script> -Check`.
3. Require `PASS asset-manifest sheets=101/103 singles=22 total=123`.
4. Require a working Unity license and exclusive Unity-writer ownership.

If any precondition fails, stop before copying. Report `BLOCKED Unity license unavailable; no import mutation performed` when licensing is the cause.

## Workflow

1. Select only records whose `selection.status` is `included`; never infer approval from a filename.
2. Preflight source path, SHA-256, bytes, dimensions, destination uniqueness, and importer texture type, mode, cell/grid, PPU, pivot, direction rows, clip meaning, FPS, loop, filter, and compression.
3. Pass records to the Editor importer under `Assets/_Project/Scripts/Editor/AssetPipeline`.
4. Preserve an existing destination `.meta` and GUID. Make reruns idempotent: update assets rather than adding duplicate sprites, clips, or controllers.
5. Apply manifest cell size, PPU, pivot, direction rows, FPS, loop, filter, and compression exactly. Treat `MixedBySlice` as a required per-slice decision, never a global pivot.
6. Save and refresh through Unity, then run one Unity validation and one repository-hygiene check. Do not reimplement semantic Unity checks in PowerShell or prose.

## Never do this

- Import all 103 sheet candidates; Player Walk B05/B06 are historical.
- Omit the 22 canonical single sprites.
- Copy `Frames`, `fullres`, `native`, `QualityRefresh`, comparisons, or raw revisions.
- Split a sheet into per-frame PNG files.
- Hand-author `.meta`, scene/prefab YAML, `.anim`, or `.controller` files.
- Collapse Bear B20 and BearHeavy B31, replace CommonSoldier or Monkey, or confuse the PorchYard mailbox with Refrigerator.
- Generate default Unity metadata while Unity is unavailable.

## Reporting

Success is concise: `PASS sprite-import sheets=101 singles=22 total=123`. On failure report `FAIL asset=<manifest-id> stage=<copy|import|slice|clip> reason=<summary> log=<path>` and inspect the full Unity log only then.
