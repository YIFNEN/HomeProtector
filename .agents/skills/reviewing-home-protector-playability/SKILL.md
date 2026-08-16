---
name: reviewing-home-protector-playability
description: Use when hands-on reviewing a playable Home Protector milestone for game flow, readability, voice activation, feedback, UI, balance, or content exposure.
---

# Reviewing Home Protector Playability

## Overview

Review what a player sees, understands, and feels. Treat Unity verification and repository hygiene as prerequisites, not work to repeat.

## Inputs

- Milestone and commit/build ID, platform or scene, and playable artifact.
- Scoped changes, expected day/wave/theme coverage, and newly exposed content.
- Controls plus microphone and keyboard-fallback setup.
- Concise Unity verification and repository-hygiene summaries with known blockers.
- `Docs/Development/playtest-notes.md` for the current milestone.

## Review loop

1. Launch the Unity scene or Windows build as a player.
2. Exercise Loading ¡æ Preparation ¡æ Combat ¡æ Result ¡æ next day or same-day retry ¡æ final completion.
3. Check placement and tower controls plus Refrigerator, Rice, and Bed damage, destruction, and recovery.
4. Check combat readability: teams, targets, projectiles, damage, hit reaction, and death.
5. Try voice activation and keyboard fallback through the same gameplay path, including unavailable-microphone behavior.
6. Judge wave pacing, role variety, enemy introductions, tower choices, and difficulty spikes.
7. Judge attack anticipation, impact VFX/audio, hit feel, and result feedback.
8. Check phase, day, goal, protected health, controls, and result UI clarity.
9. Confirm promised player, enemy, tower, valuable, VFX, and environment/theme content appears in normal play.

## Boundaries

- Do not rerun compile, EditMode, PlayMode, reference, serialization, `.meta`, or hygiene checks already summarized by their owners.
- Do not paste successful full logs; request a failure log only when handing an actual blocker to debugging.
- Do not inspect source for root cause or fix issues while reviewing. Route investigation through `systematic-debugging`.
- Automated readiness is not evidence of playability; hands-on observation is required.

## Findings

Record a header with milestone, commit/build, platform/scene, date, input setup, scoped coverage, and concise verification references. Each finding needs severity, timestamp/phase, observation, player impact, expected behavior, at most four repro steps, optional screenshot/clip, and owning area. End with coverage gaps and one status: `PLAYABLE`, `PLAYABLE WITH FINDINGS`, or `BLOCKED`.

## Severity and stopping

- **P0:** crash, softlock, wrong result/state, both voice and fallback unusable, or core loop cannot complete. Stop blocked coverage and hand off immediately.
- **P1:** a major required path or repeated combat-comprehension failure; continue other reachable coverage.
- **P2:** meaningful pacing, readability, feedback, or UI degradation.
- **P3:** polish only.
- An unavailable microphone alone is a coverage gap when fallback works; both inputs failing is P0.
- If the playable artifact or hands-on access is unavailable, report `BLOCKED` with an untested coverage gap and no runtime severity; reserve P0 for an observed failure.

## Handoff

Separate observed defect, tuning concern, and untested coverage. Give the smallest reproducible player path and impact, group findings by owning subsystem, and do not claim root cause.

## Reporting

On a clean run, report the status and covered milestone in one line. On findings, link the compact playtest note; never dump raw Unity logs.
