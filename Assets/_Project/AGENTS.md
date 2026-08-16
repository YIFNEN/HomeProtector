# Maintained Unity workspace

`Assets/_Project` is the maintained layer. `Scripts/Legacy` remains compatible with the existing game scene while new core contracts live in `Scripts/Runtime`.

## Runtime design

- Put engine-independent phase and content contracts in the `HomeProtector.Core` assembly.
- Put adapters that reference legacy global-namespace components under `Scripts/Legacy/RuntimeIntegration` until legacy dependencies are migrated.
- `GameFlowBridge` is the only component allowed to coordinate `GameSession`, `TimeSystem`, `WaveSystem`, and `WaveResultSystem`.
- Result publication is idempotent per combat. Defeat aborts spawners, timers, delayed waves, and live enemies.
- Preparation restores configured resources. Victory advances the day; defeat retries the same day; final victory exposes completion.

## Content design

- `ContentCatalog` is the runtime inventory for all enemies, towers, valuables, VFX, player animation sets, and environment themes.
- `PlaceableResourceDefinition` separates placement from targeting, health totals, instant-defeat behavior, and preparation restoration.
- Decorative placeables remain draggable but are excluded from enemy targeting and total health.
- Prefer prefab variants and data definitions over duplicate scene-only configuration.

## Tests and Editor tooling

- Write EditMode tests first for pure state and catalog contracts; write PlayMode tests only for Unity lifecycle/scene behavior.
- Editor tooling owns texture importer settings, slicing, clips, controllers, prefab generation, catalog updates, scene migration, and builds.
- Do not assert Unity serialization by parsing YAML in runtime tests.
- Do not expand successful test logs. Record one-line totals and the result artifact path.

## Naming and compatibility

- Use English identifiers and UTF-8 files. Existing Korean inspector labels/comments may remain.
- Retain `DraggableResource` during migration and let it reference the new definition.
- Retain `MicrophoneSystem` only as a temporary facade; all new references target the separated voice components.
