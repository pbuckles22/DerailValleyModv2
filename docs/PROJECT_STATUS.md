# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-09-15 (**13.2.5** `[~]` **`2.13.2.5.22.12`**; engineer 10-row + pins 1+4 and 6 cab PASS; pin-moment harvest + Core GitHub Action on **`chore/tier1-test-hardening`**; cab leftover-8 / `#1` still open)

## Summary

**DerailValleyModv2** — Yard Master Suite v2. **Epic 3 Display Shell (infra) closed** at **3.3.1**. **Epic 4 infra closed** at **4.3**. **Epic 6 Diagnostic HUD closed** at **6.21**. **Epic 7 Governors closed** at **7.5**. **8.7** / **9.1.x** / **13.1** / **13.2.1–2** / **13.2.4** / **13.6.1** / **13.4** on **`main`**. **Next:** remaining **13.2.5** (W5 ShowPin cab / remaining numbered pins). UMM **`2.13.2.5.22.12`**. Do **not** close Epic 13. Do **not** delete **`feature/13.2.5-multi-pickup-desk`** or **`bug/13.2.5-pin-board`**. Full v1 map: [V1_FEATURE_COVERAGE.md](V1_FEATURE_COVERAGE.md). Canonical HTP: [HTP.md](HTP.md). Walk: [13.2.5-WALK.md](13.2.5-WALK.md).

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — **13.2.4** `[x]` kiss **`2.13.2.4.14`** @ `aec31bb`. |
| **`feature/13.2.5-multi-pickup-desk`** | WIP — **13.2.5** **`2.13.2.5.22.12`** engineer 10-row + pins 1+4 and 6 + dark desk + Hide strip + Cruise off + AR off + desk Ctrl+Right + W1–W3 (not merged). |
| **`chore/tier1-test-hardening`** | Park — Core `dotnet test` GitHub Action + frog-matrix oracle / skippable dumps + pin-moment harvest (Observe `PinIdForStep`; SU-34 / FH-82 / SL-52 dumps). Not `main`. Four Gemini pose walks Skipped. |
| **`bug/13.2.5-pin-board`** | Spike park — leftover 6/8 overlay; do not merge wholesale. See [13.2.5-WALK.md](13.2.5-WALK.md). |
| **`feature/13.2.4.5-yard-taper`** | Keep — kiss land archaeology (do not delete). |
| **`feature/13.2.4-creep-to-couple`** | Keep — 13.2.4 land archaeology (do not delete). |
| **`feature/13.4-yard-chain-1-5`** | Keep — 13.4 full land archaeology. |
| **`feature/13.4-autonomous-transit-thin`** | Keep — thin land archaeology. |
| **`feature/13.6.1-remote-take`** | Keep — 13.6.1 land. |
| **`feature/13.2.3-filo-pickup-queue`** | Park — WIP stashed. |
| **`feature/8.7-route-pin-cleared`** | Keep — do not delete. |

---

## Sequence

**Next:** Remaining **13.2.5** on **`feature/13.2.5-multi-pickup-desk`** — Session.Apply leftover-8 walks, then W5 ShowPin cab / remaining numbered pins. UMM **`2.13.2.5.22.12`**. Do not pop `stash@{0}` (5.23 kiss/Rear). Do not merge `main` until CMPH. Do not delete **`feature/13.2.5-multi-pickup-desk`** or **`bug/13.2.5-pin-board`**. Default `dotnet test` must set `YMS_FROG_MATRIX_FULL=0`.

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
