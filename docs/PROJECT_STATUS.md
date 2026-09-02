# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-09-02 (**13.1** WIP **`2.13.1.14`** on **`feature/13.1-step-runner`**; Now queue **13.1.15** → **6.21.7** → **13.6.1** → **9.1.4**; **9.1.3** on **`main`** @ **`2.9.1.39`**)

## Summary

**DerailValleyModv2** — Yard Master Suite v2. **Epic 3 Display Shell (infra) closed** at **3.3.1**. **Epic 4 infra closed** at **4.3**. **Epic 6 Diagnostic HUD closed** at **6.21** (`2.6.21.6`). **Epic 7 Governors closed** at **7.5** (`2.7.5.7`). **8.7** on **`main`** (`2.8.7.31`). **9.1** + **9.1.2** + **9.1.3** on **`main`** (`2.9.1.39`). **13.1** in flight: inbound TT pin **990152** PASS **`2.13.1.10`**; leave 6-row + stale-pin drop **`2.13.1.14`**. **Next:** 13.1 cab smoke, then Now queue **13.1.15** harvest logging. **9.2** after **13.4**. Full v1 map: [V1_FEATURE_COVERAGE.md](V1_FEATURE_COVERAGE.md). Canonical HTP: [HTP.md](HTP.md).

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — **9.1.3** (`2.9.1.39`). Land sha recorded in handoff. |
| **`feature/9.1.3-win0-graph-dump`** | **9.1.3** archaeology — **keep** ([Feature_Branch_Archaeology.md](git/Feature_Branch_Archaeology.md)). |
| **`feature/13.1-step-runner`** | **13.1** GO / Human / Done (active). |
| **`feature/8.7-route-pin-cleared`** | Keep — do not delete. |

---

## Sequence

**Next:** **13.1** cab smoke on **`feature/13.1-step-runner`** (`2.13.1.14`): 6-row leave list from face-into-Exit Load; Prep reload without CLEARED. Then **13.1.15** harvest logging. Do not re-smoke inbound **990152**. **9.2** if needed after **13.4** → **Epic 14** Maps desk → **Epic 10**. **Deferred:** 8.8–8.9, 11 Catalog, 12 Roadside. Reverse-cruise gold remains cab **`feature=0`** with desk closed.

### Autonomy tracker (re-baseline)

| Story | Est (days) | Started | Done | Actual | Notes |
|-------|------------|---------|------|--------|-------|
| 8.7 remainder | 0.5–1 | 2026-08-27 | 2026-08-29 | ~2 | `2.8.7.31` CP0 + chords |
| 9.1 PID hold | 2–3 | 2026-08-29 | 2026-08-30 | ~1.5 | `2.9.1.14` CP1 + takeoff/coast |
| 9.1.3 walker + span + refresh | 2–4 | 2026-09-01 | 2026-09-01 | ~1 | `2.9.1.39` Wins 0–5.1 |
| 13.1 | 1.5–2 | 2026-09-01 | | | `2.13.1.14` leave 6-row; cab smoke then CMPH |
| 13.4 thin | 2–3 | | | | before full 13.2 |
| 13.2.1 | 0.5–1 | | | | |
| 13.2.2 | 1 | | | | |
| 13.2.3 | 1–1.5 | | | | |
| 13.2.4 | 2–3 | | | | |
| 13.2.5 | 2–3 | | | | |
| 13.2.6 | 1 | | | | |
| 13.3 | 1 | | | | |
| 13.5 | 3–4 | | | | |
| 13.6 | 1.5–2.5 | | | | |

*Fill **Actual** on ship; adjust **Est** for remaining rows when reality diverges.*
