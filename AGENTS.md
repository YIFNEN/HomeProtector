# Home Protector agent contract

This repository is a Unity 2022.3.60f1 project completed from the existing `isometric scene` gameplay. Keep changes incremental and preserve working legacy systems while moving ownership to `Assets/_Project`.

## Required boundaries

- `HomeProtector.Core.GameSession` is the only source of Preparation, Combat, and Result state.
- Keep `WaveSystem`, `EnemySpawner`, `TowerSpawner`, `TargetManager`, and `ResourceManager` as the combat engine; adapt them through bridges instead of rewriting them.
- Keep CommonSoldier and Monkey. Treat the old PostBox prefab as the refrigerator placeholder and preserve its draggable, target, and resource behavior while migrating it.
- Keep microphone activation and a keyboard fallback on the same activation path.
- Do not add WebGL, GitHub Pages, NAN submission automation, or an in-game AI director.

## Unity ownership

- Only one agent may write `.unity`, `.prefab`, `.asset`, `.controller`, `.anim`, or `.meta` files at a time.
- Never hand-edit Unity scene or prefab YAML. Use Unity Editor C# migration/build commands.
- Read-only asset inventory and playability review may run in parallel.
- Preserve GUIDs when moving assets. Commit a runtime asset together with its `.meta`, clips, controller, prefab, and definition.

## Art intake

- Import only canonical Unity-ready sprite sheets from `D:/GameAsset` that are listed in `Docs/Development/asset-import-manifest.json`.
- Exclude raw generations, `Frames`, `fullres`, `native`, comparison images, and intermediate QualityRefresh revisions.
- Slice approved sheets in Unity. Do not commit per-frame PNG copies.
- Store imported runtime art under `Assets/_Project/Art/Runtime` and provenance in the manifest.

## Verification

- Let Unity own compile, serialization, reference, EditMode, PlayMode, and build verification.
- Use `Tools/Unity/Invoke-HomeProtectorUnity.ps1` for concise summaries. Read full Unity logs only after a failure.
- Use `Tools/Git/Validate-UnityRepo.ps1` for repository hygiene; do not duplicate Unity semantic checks in shell scripts or skills.
- A missing Unity license is an explicit blocked check, never a passing result.

## Git

- Work on `codex/*` branches; never push or force-push `main`.
- Stage explicit feature paths. Do not use `git add -A` in this mixed Unity worktree.
- Do not commit `Library`, `Temp`, `Build`, `Builds`, `Releases`, executables, generated DLLs, or raw/full-resolution art.
- Use normal Git for approved sheets. Reconsider LFS only for future source assets such as PSD/WAV or files over 20 MiB.
- Keep code and its tests together. Keep scene wiring in a later, separate commit.

## Local skills

Use the smallest applicable skill under `.agents/skills`:

- `cataloging-home-protector-assets` for canonical asset selection and manifest updates.
- `importing-unity-sprite-sheets` for importer contracts and Editor-driven slicing.
- `wiring-home-protector-content` for prefab, definition, catalog, and scene integration.
- `reviewing-home-protector-playability` for milestone play reviews.

Use existing `systematic-debugging`, `verification-before-completion`, and GitHub `yeet` instead of creating overlapping project skills.
