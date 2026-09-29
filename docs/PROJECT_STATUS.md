# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-09-29 (**16.3** `[x]` **`2.16.35`**; final-leg stop cab **PASS** **`2.16.36.3`** on `main`; park **`2.16.36.6`** on `fix/16-stall-aim-holds`; **Epic 16** stays `[~]`)

## Summary

**DerailValleyModv2** — Yard Master Suite v2. **Epic 3 Display Shell (infra) closed** at **3.3.1**. **Epic 4 infra closed** at **4.3**. **Epic 6 Diagnostic HUD closed** at **6.21**. **Epic 7 Governors closed** at **7.5**. **8.7** / **9.1.x** / **13.1** / **13.2.1–2** / **13.2.4** / **13.6.1** / **13.4** / **16.2** / **16.3** and the final-leg stop on **`main`** (UMM **`2.16.36.3`**). **16.3** is the ladder re-entry block. The final Drive stopped in cab at **`2.16.36.3`** (C4S with a car, and empty B4L). Park **`fix/16-stall-aim-holds`** at **`2.16.36.6`** (not on `main`): Route GO keeps stall aim on a far corridor, a passed frog pin hides while CLEARED stays until the tail is through, and the Now row moves from **1/2** to **2/2**. **Epic 16** stays open until asked. **16.4** stays deferred. Run A and Run B stay deferred. The C4S drop is the job-id cut and is waived until the Switch List rewire. This land also contains the unfinished **13.2.5** commits, because **16** was cut from that branch. **`feature/13.2.5-multi-pickup-desk`** still points at **`0b92485`**. Gemini TDD Mini Win Steps **1–7** are Tier 1 only (`HtpSawtoothTddStepsTests`) — not a cab drive. Desk IMGUI text overlap is known polish debt. Do **not** close Epic 16. Do **not** delete **`feature/16-spatial-routing`** or **`feature/13.2.5-multi-pickup-desk`**. Full v1 map: [V1_FEATURE_COVERAGE.md](V1_FEATURE_COVERAGE.md). Canonical HTP: [HTP.md](HTP.md). Walk: [13.2.5-WALK.md](13.2.5-WALK.md).

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Final-leg stop cab **PASS** **`2.16.36.3`**. **16.3** `[x]` at **`2.16.35`**. Also contains unfinished **13.2.5** history. **Epic 16** stays `[~]`. |
| **`fix/16-stall-aim-holds`** | Park **`2.16.36.6`**. Stall aim, spent pin hides, Now row advances. Not merged. |
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

**Next:** leave Epic 16 open until asked. `main` stays at `2.16.36.3`. The pin-pass park is `fix/16-stall-aim-holds` at `2.16.36.6` (not merged). Do not start **13.2.5**. Do not re-smoke the pin pass, the Now row, B4L, this C4S stop, the B1S hold, or the 16.3 path. Run A and Run B stay deferred. The C4S job-id cut stays parked for the Switch List rewire. Do not pop `stash@{0}`. Do not delete the feature branches.

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
