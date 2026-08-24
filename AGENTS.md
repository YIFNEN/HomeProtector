# Home Protector OpenAI Game Builders contract

This worktree is the WebGL-first OpenAI Game Builders submission lane for the Unity 2022.3.60f1 project. Keep changes incremental and preserve the verified `isometric scene` gameplay while adapting it for browser play.

## Required boundaries

- `HomeProtector.Core.GameSession` is the only source of Preparation, Combat, and Result state.
- Keep `WaveSystem`, `EnemySpawner`, `TowerSpawner`, `TargetManager`, and `ResourceManager` as the combat engine; adapt them through bridges instead of rewriting them.
- Keep CommonSoldier and Monkey. Treat the old PostBox prefab as the refrigerator placeholder and preserve its draggable, target, and resource behavior while migrating it.
- Keep microphone activation and a keyboard fallback on the same activation path.
- Add WebGL and browser compatibility only on `codex/openaigame2026`; keep platform-neutral fixes easy to cherry-pick.
- Do not add NAN submission automation or an in-game AI director.

## Contest delivery

- The required deliverable is a public browser link that starts without installation, approval, or login.
- Browser completion must never depend on microphone permission. Keyboard fallback uses the same player-activation path.
- Request microphone permission only after an explicit user gesture and only on HTTPS. Denial, timeout, or missing devices must fall back cleanly.
- Use this contest worktree as the only Unity writer while WebGL work is active. Treat the original Windows worktree as read-only for contest changes.
- Write local builds only to ignored `Builds/OpenAIGame2026-WebGL`. Never commit generated WebGL output to the source branch.
- If deployment needs a generated-output branch or hosting project, keep it separate from source history.
- Record the pre-challenge baseline and every challenge-period feature in `Docs/Submission/OpenAIGame2026`.
- Internal submission freeze is 2026-08-26 18:00 KST; prioritize a completable hosted build over additional content.

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
- WebGL readiness requires a Unity WebGL build plus an HTTPS browser smoke test of Loading to final result, including keyboard fallback.
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
