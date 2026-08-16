# Home Protector Content Integration Status

Last updated: 2026-08-16

## Status keys

- **Cataloged:** canonical source and runtime role are recorded.
- **Imported:** Unity created or updated texture metadata, slices, clips, and controllers.
- **Wired:** prefab, definition, catalog, dependencies, and scene/campaign exposure are connected.
- **Playtested:** the expected role appeared and worked in a milestone hands-on run.
- **Blocked:** the next mutation requires an active Unity license.

## Asset baseline

- Manifest: 103 sheet candidates, 101 included sheets, 2 excluded historical Player Walk sheets, and 22 included single sprites.
- Existing project identities retained: CommonSoldier and Monkey.
- Intentional role split: Bear B20 and BearHeavy B31.
- Import source: `D:/GameAsset/GameAssets/HomeProtector/UnityReadySprites`.
- Runtime destination: `Assets/_Project/Art/Runtime`.
- Current import blocker: expired Unity entitlement; no art has been copied by the harness.

## Integration matrix

| Area | Cataloged | Imported | Wired | Playtested | Current note |
|---|---:|---:|---:|---:|---|
| Player (Idle, Walk, Attack, Damage, Sleep, Buff/Repair, Spawn VFX) | Yes | Blocked | Partial legacy | No | Preserve existing voice-tuned player behavior while replacing visual set. |
| Protected resources (Refrigerator, Rice, Bed) | Yes | Blocked | Partial legacy | No | Refrigerator GUID `013bf229ebe2b6247b621e48f6137a06` is canonical; PostBox migration pending. |
| Other valuables/placeables (6 roles) | Yes | Blocked | No | No | Decorative definitions must disable enemy target and total-health contribution. |
| Enemies (14 canonical roles) | Yes | Blocked | Partial legacy | No | Existing CommonSoldier, Monkey, Cockroach, and Bear identities are reused; roster waves pending. |
| Towers (Dryer, BookShelf, CoolDryer, Microwave, Vacuum) | Yes | Blocked | 3 partial, 2 pending | No | Prefer TowerDefinition level data over level-prefab duplication. |
| Projectiles and shared VFX (8 families) | Yes | Blocked | Partial legacy | No | Wire with the owning tower/player content unit. |
| Environment tiles, props, overlays, and themes | Yes | Blocked | No | No | PorchYard mailbox remains a separate environment prop. |
| Runtime game flow | N/A | N/A | Prototype only | No | Final isometric scene still needs GameSession bridge migration. |
| Voice plus keyboard fallback | N/A | N/A | Legacy combined | No | Split source/profile/controller/router while preserving existing log-scale tuning. |

## Next mutation gate

1. Activate a Unity Personal license in Unity Hub.
2. Run EditMode tests to obtain a real RED result before runtime production changes.
3. Import only manifest-included records through Unity Editor automation.
4. Wire content in atomic role units, then migrate the final scene last.
