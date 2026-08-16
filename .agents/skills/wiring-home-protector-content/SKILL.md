---
name: wiring-home-protector-content
description: Use when connecting imported Home Protector art to prefabs, variants, definitions, ContentCatalog entries, waves, or the final Unity scene.
---

# Wiring Home Protector Content

## Overview

Integrate one gameplay role at a time while preserving working references. A sprite sheet is animation input, not proof that a new prefab is required.

## Decision order

1. Identify the runtime role, dependencies, and existing references.
2. **Reuse** an existing prefab when its components and behavior match; preserve its GUID and add art, animation, or definition data.
3. Create a **variant** when behavior is shared but visuals, stats, or role differ.
4. Create a **new base** only when the existing contracts cannot express required behavior.
5. Represent tower levels in `TowerDefinition` data before considering level-specific prefab duplication.

## Atomic content unit

Commit the approved sheet, `.meta`, clips, controller, prefab or variant, definition, `ContentCatalog` entry, and related tests together. Include required projectile or VFX dependencies in the same unit. Catalog IDs and prefab references must be unique and non-null.

## Workflow

1. Require a successful manifest-driven import and exclusive Unity-writer ownership.
2. Wire prefab, definition, and catalog before touching a scene.
3. Reuse CommonSoldier, Monkey, Cockroach, Bear, existing towers, and other compatible identities rather than recreating them.
4. Keep `Assets/Prefabs/Refrigerator.prefab` GUID `013bf229ebe2b6247b621e48f6137a06` as the canonical protected resource. Preserve the placeholder PostBox's `DraggableResource`, `TargetObject`, and `ResourceObject` behavior while migrating its scene instances to Refrigerator.
5. Retire the placeholder PostBox asset only after Unity confirms all references were migrated.
6. Wire the final scene last through an Editor C# migration; never hand-edit Unity YAML.
7. Resolve the canonical roster from `ContentCatalog`: CommonSoldier, Monkey, Cockroach, Bear, BearHeavy, WildBoar, Snake, Spider, Mouse, Wasp, Fox, Squirrel, AntSwarm, and MothPest. Distribute all 14 across introduction, mixed, and pressure waves; never infer roles from sheet count or put every type in every wave.

## Project rules

- Keep CommonSoldier and Monkey. Keep Bear B20 and BearHeavy B31 as distinct normal and elite roles.
- Protected Refrigerator, Rice, and Bed contribute to target/health rules; decorative placeables do not.
- PorchYardDecor's mailbox is an environment prop, not Refrigerator. It may remain draggable with target and total-health flags disabled.
- Only one agent writes `.unity`, `.prefab`, `.asset`, `.controller`, `.anim`, or `.meta` files; review agents stay read-only.

## Verification

- Let Unity check catalog IDs, nulls, references, compile state, and representative spawn-to-death or placeable lifecycles.
- Confirm Refrigerator migration, decoration policy, campaign-wide roster exposure, wave role limits, and final-scene missing references.
- Run repository hygiene separately. Do not duplicate Unity semantic checks in shell scripts.

## Reporting

Report one-line success by role or batch. On failure name the role, wiring stage, and Unity log path; read the full log only on failure.
