# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-09-30 (**13.2.5** `[~]` at **`2.16.46`** on `feature/13.2.5-pin8-job-couple`, not merged)

## Summary

**DerailValleyModv2** — Yard Master Suite v2. **13.2.5** is parked at UMM **`2.16.46`** on **`feature/13.2.5-pin8-job-couple`** (not on `main`). Pin 6 CLEARED on the stem. Square 8 is still `990200` while the haul throws `1002788`, and that leg requested 3 km/h. **`main`** stays **Epic 16** `[x]` at **`2.16.36.9`** except **16.4**. Do **not** merge this park. Do **not** re-smoke pin 6 or Epic 16. Do **not** pop `stash@{0}`. Do **not** delete **`feature/13.2.5-pin8-job-couple`**, **`feature/16-spatial-routing`**, or **`feature/13.2.5-multi-pickup-desk`**. Full v1 map: [V1_FEATURE_COVERAGE.md](V1_FEATURE_COVERAGE.md). Canonical HTP: [HTP.md](HTP.md). Walk: [13.2.5-WALK.md](13.2.5-WALK.md).

---

## Active branch

| Branch | Role |
|--------|------|
| **`feature/13.2.5-pin8-job-couple`** | **13.2.5** `[~]` at **`2.16.46`**. Not merged. |
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

**Next:** square 8 is the haul throw `1002788`. The C4S→B4L leg stays at 25 until the normal brake. Do not merge. Do not re-smoke pin 6 CLEARED or Epic 16. Do not pop `stash@{0}`. Do not delete the feature branches.

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
