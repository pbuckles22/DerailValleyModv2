# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-10-02 (**13.2.5** `[~]` at **`2.16.61`** on `spike/b4l-route-pause`, not merged)

## Summary

**DerailValleyModv2** — Yard Master Suite v2. **13.2.5** is parked at UMM **`2.16.61`** on **`spike/b4l-route-pause`** (not on `main`). **Win 1** frozen phone: same-track advance and Align keep `RoutePlanSession` commands (cab: `list-next frozen SW-C4S`, Align `n=5` with Stop/Throw). Product cab **FAIL**: consist on SW-B3I while Prep → SW-C4S — map did not rewrite; next is a driver that follows those directions only. **`main`** stays **Epic 16** `[x]` at **`2.16.36.9`** except **16.4**. Do **not** merge this park. Do **not** re-smoke pin 6, pin 8, Epic 16, or Win 1 foundation. Do **not** pop `stash@{0}`. Keep **`feature/13.2.5-pin8-job-couple`**. Full v1 map: [V1_FEATURE_COVERAGE.md](V1_FEATURE_COVERAGE.md). Canonical HTP: [HTP.md](HTP.md). Walk: [13.2.5-WALK.md](13.2.5-WALK.md).

---

## Active branch

| Branch | Role |
|--------|------|
| **`spike/b4l-route-pause`** | **13.2.5** `[~]` at **`2.16.61`** Win 1. Not merged. |
| **`feature/13.2.5-pin8-job-couple`** | Prior park **`2.16.48`**. Keep. |
| **`main`** | **Epic 16** `[x]` at **`2.16.36.9`** except **16.4**. |
| **`fix/16-run-a-car-before-pin`** | Kept. Epic 16 land **`2.16.36.9`**. |
| **`fix/16-stall-aim-holds`** | Kept. Pin pass and Now row at **`2.16.36.6`**. |
| **`fix/16-final-leg-no-prep-stop`** | Kept. Same stop as `main` (`2.16.36.3`). |
| **`feature/13.2.5-multi-pickup-desk`** | WIP — **13.2.5** **`2.13.2.5.22.58`**; pin 8=1+4 cab FAIL (not merged). |
| **`chore/tier1-test-hardening`** | Same tip as product (`5eca866`) — Core `dotnet test` GitHub Action + frog-matrix oracle / skippable dumps + pin-moment harvest. Keep; do not re-merge. |
| **`bug/13.2.5-pin-board`** | Spike park — leftover 6/8 overlay; do not merge wholesale. See [13.2.5-WALK.md](13.2.5-WALK.md). |
| **`feature/13.2.4.5-yard-taper`** | Keep — kiss land archaeology (do not delete). |
| **`feature/13.2.4-creep-to-couple`** | Keep — 13.2.4 land archaeology (do not delete). |
| **`feature/13.4-yard-chain-1-5`** | Keep — 13.4 full land archaeology. |
| **`feature/13.4-autonomous-transit-thin`** | Keep — thin land archaeology. |
| **`feature/13.6.1-remote-take`** | Keep — 13.6.1 land. |
| **`feature/13.2.3-filo-pickup-queue`** | Park — WIP stashed. |
| **`feature/8.7-route-pin-cleared`** | Keep — do not delete. |
| **`feature/16-spatial-routing`** | Keep — **16.2** land archaeology (do not delete). |

---

## Sequence

**Next:** lift loco drive above the phone so the driver follows the frozen command list only (Win 1 foundation stays). Then Win 2 reader log / Win 3 hold. Do not merge. Do not re-smoke pin 6, pin 8, Epic 16, or Win 1 foundation. Do not pop `stash@{0}`. Do not delete the feature branches.

### Autonomy tracker (re-baseline)

| Story | Est (days) | Started | Done | Actual | Notes |
|-------|------------|---------|------|--------|-------|
| 13.4 thin foundation | — | 2026-09-03 | 2026-09-03 | ~1 | `2.13.4.7` per-leg GO |
| 13.4 full (steps 1–5) | 2–4 | 2026-09-03 | 2026-09-04 | ~2 | `2.13.4.18`; designed crash PASS |
| 13.2.4 | 2–3 | 2026-09-04 | 2026-09-08 | ~4 | `2.13.2.4.3` couple + `2.13.2.4.14` kiss |
| 15.1 haul Transit | 2–3 | | | | was part of thin 13.4 haul |
| 15.2 delivery drop | 3–4 | | | | was 13.5 |
| 15.3 turn-in | 1.5–2.5 | | | | was 13.6; 13.6.1 stays on 13 |

*Fill **Actual** on ship; adjust **Est** when reality diverges.*
