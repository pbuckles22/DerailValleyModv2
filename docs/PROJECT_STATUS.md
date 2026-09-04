# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-09-04 (**13.4** WIP `2.13.4.18` on `feature/13.4-yard-chain-1-5`; thin `2.13.4.7` on `main`)

## Summary

**DerailValleyModv2** — Yard Master Suite v2. **Epic 3 Display Shell (infra) closed** at **3.3.1**. **Epic 4 infra closed** at **4.3**. **Epic 6 Diagnostic HUD closed** at **6.21**. **Epic 7 Governors closed** at **7.5**. **8.7** / **9.1.x** / **13.1** / **13.2.1–2** / **13.6.1** / thin **13.4** `2.13.4.7` on **`main`**. **Next:** leave TT → Prep designed crash → **CMPH** (park mid-epic with **UCPH**). Do **not** close Epic 13. Full v1 map: [V1_FEATURE_COVERAGE.md](V1_FEATURE_COVERAGE.md). Canonical HTP: [HTP.md](HTP.md).

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — thin GO foundation `2.13.4.7`; **13.4** full still open. |
| **`feature/13.4-yard-chain-1-5`** | **Active** — yard crawl TT stop PASS (`2.13.4.18`); Prep exit open. |
| **`feature/13.4-autonomous-transit-thin`** | Keep — thin land archaeology. |
| **`feature/13.6.1-remote-take`** | Keep — 13.6.1 land. |
| **`feature/13.2.3-filo-pickup-queue`** | Park — WIP stashed. |
| **`feature/8.7-route-pin-cleared`** | Keep — do not delete. |

---

## Sequence

**Next:** **13.4** full (Prep crash → CMPH) → **13.2.4** → **13.2.5/6** + **13.3** → **15.1–15.3** → **14** → **10**. Clear-line pin deferred to **8.7** revisit. Yard crawl + rem≤d_stop = precision testbed before **9.2**.

### Autonomy tracker (re-baseline)

| Story | Est (days) | Started | Done | Actual | Notes |
|-------|------------|---------|------|--------|-------|
| 13.4 thin foundation | — | 2026-09-03 | 2026-09-03 | ~1 | `2.13.4.7` per-leg GO; **not** story done |
| 13.4 full (steps 1–5) | 2–4 | | | | reopen; multi-leg through Prep |
| 13.2.4 | 2–3 | | | | after 13.4 full |
| 15.1 haul Transit | 2–3 | | | | was part of thin 13.4 haul |
| 15.2 delivery drop | 3–4 | | | | was 13.5 |
| 15.3 turn-in | 1.5–2.5 | | | | was 13.6; 13.6.1 stays on 13 |

*Fill **Actual** on ship; adjust **Est** when reality diverges.*
