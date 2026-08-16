---
name: cataloging-home-protector-assets
description: Use when inventorying D:/GameAsset for Home Protector, selecting canonical Unity-ready sprite revisions, or updating the asset import manifest before any art is copied.
---

# Cataloging Home Protector Assets

## Overview

Make `Docs/Development/asset-import-manifest.json` the auditable boundary between generated art and Unity. Catalog first; never copy or slice assets while using this skill.

## Workflow

1. Read the root `AGENTS.md`, the existing manifest, and source revision notes.
2. Run `scripts/New-HomeProtectorAssetManifest.ps1` to inventory relative paths, byte sizes, dimensions, SHA-256 hashes, and importer contracts. Use `-Check` after generation.
3. Group by semantic content, animation, level, and batch. A different hash is not proof of different gameplay content.
4. Select one intentional revision and record why. Mark rejected candidates in the manifest instead of silently forgetting them.
5. Check destination collisions under `Assets/_Project/Art/Runtime/<source-relative-path>`.
6. Report counts for discovered sheets, imported sheets, excluded revisions, single sprites, and total bytes.

## Canonical policy

| Case | Decision |
|---|---|
| `*_Sheet_BNN.png` | Candidate sheet |
| `Frames`, `fullres`, `native`, `QualityRefresh*` | Exclude |
| Per-frame or comparison crops | Exclude |
| Player Walk B05/B06 | Historical; exclude |
| Player Walk B08 | Canonical |
| Bear B20 | Canonical normal Bear role |
| BearHeavy B31 | Canonical elite BearHeavy role |
| CommonSoldier and Monkey | Keep existing project art; no external replacement |
| PorchYardDecor mailbox | Valid environment prop, not the Refrigerator valuable |

The known source has 103 sheet candidates: import 101 and retain the two historical Walk sheets as excluded manifest records. Also catalog required final single sprites separately; do not inflate the sheet count.

## Manifest contract

Each record needs a stable ID, content kind and role, source-relative and destination paths, batch, hash, bytes, selection status/reason, and importer contract. Importer data must state texture mode, cell size/grid, PPU, pivot, direction rows, clip meaning, and loop behavior. Use `null` only when Unity review is explicitly required; never guess PPU.

## Stop conditions

- Two included records target one destination.
- One semantic role has multiple unexplained revisions.
- A source file changed hash without a batch/reason update.
- A new item has no runtime role or provenance.

Resolve these in the manifest before invoking the import skill.
