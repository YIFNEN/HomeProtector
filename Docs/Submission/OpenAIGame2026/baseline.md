# OpenAI Game Builders 2026 baseline

## Submission constraints

- Official source: https://openaigame2026.com/ (checked 2026-08-18 KST).
- Online qualifier submission closes on 2026-08-26.
- A publicly accessible game that runs directly in a browser is required.
- The core loop must be playable throughout judging without approval or installation.
- A thumbnail is required; 16:9 PNG or JPG up to 10 MB is recommended.
- A gameplay video of up to three minutes and a Codex collaboration explanation are optional scoring opportunities.
- Existing projects are allowed, but challenge-period additions must be identified.

## Pre-existing project baseline

- Baseline commit: `0e594273b5cda13ce8226dc187e2b6ea05c62e81`.
- Baseline date: 2026-08-18 KST.
- Engine: Unity 2022.3.60f1.
- Playable scene: `Assets/Scenes/isometric scene.unity`.
- Existing core: preparation, placement, wave combat, protected resources, legacy microphone activation, and keyboard-capable loading input.
- Preserved content identities include CommonSoldier, Monkey, Bear, Cockroach, Rice, Dryer, and CoolDryer.

## Challenge-period scope

Track additions here as they land on `codex/openaigame2026`:

| Date | Commit | New work | Codex contribution | Human decision |
|---|---|---|---|---|
| 2026-08-18 | `f6a46df9` | Contest worktree, WebGL contract, and reproducible baseline | Repository audit, isolation plan, and verification harness | WebGL-first schedule and use of the existing game concept |
| 2026-08-25 | `29bc436d` | Reproducible WebGL build, browser-safe keyboard activation fallback, and regression tests | WebGL compatibility diagnosis, implementation, TDD, deployment, and verification | Preserve native microphone play while making browser completion independent of microphone permission |

## Release gate

- Loading screen reaches gameplay in a supported desktop browser.
- One complete preparation-to-result loop works from the hosted HTTPS URL.
- Keyboard fallback can activate the player with microphone permission denied.
- WebGL console has no uncaught exceptions or missing-content errors.
- Controls are visible before play starts.
- Public link works in a fresh browser session without authentication.
