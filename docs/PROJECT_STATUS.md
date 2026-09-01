# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-09-01 (**9.1.3** Win 5.1 smoke PASS `2.9.1.39`; commit pending CMPH)

## Summary

**DerailValleyModv2** — Yard Master Suite v2. **Epic 3 Display Shell (infra) closed** at **3.3.1**. **Epic 4 infra closed** at **4.3**. **Epic 6 Diagnostic HUD closed** at **6.21** (`2.6.21.6`). **Epic 7 Governors closed** at **7.5** (`2.7.5.7`). **8.7** on **`main`** (`2.8.7.31`). **9.1** on **`main`** (`2.9.1.14`): DE2 PID hold + takeoff slew + ±2 coast. **9.1.2** Path Limit **`[x]`** on feature branch. **9.1.3** Core graph walker **`[x]`** on **`feature/9.1.3-win0-graph-dump`** (`2.9.1.23`–`.39`): walker + span + travel roster refresh; tunnel **30** PASS; **CMPH** not granted. **13.1** after CMPH. **9.2** after **13.4**. Full v1 map: [V1_FEATURE_COVERAGE.md](V1_FEATURE_COVERAGE.md). Canonical HTP: [HTP.md](HTP.md).

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — **9.1** (`2.9.1.14`). |
| **`feature/9.1.3-win0-graph-dump`** | **9.1.3** complete on branch — **`2.9.1.39`** Win 5.1 PASS; **keep after CMPH** ([Feature_Branch_Archaeology.md](git/Feature_Branch_Archaeology.md)). |
| **`feature/8.7-route-pin-cleared`** | Keep — do not delete. |

---

## Sequence

**Next:** **CMPH** **`9.1.3`** when granted → **Epic 13** (**13.1**). **9.2** if needed after **13.4** → **Epic 14** Maps desk → **Epic 10**. **Deferred:** 8.8–8.9, 11 Catalog, 12 Roadside. Reverse-cruise gold remains cab **`feature=0`** with desk closed.

### Autonomy tracker (re-baseline)

| Story | Est (days) | Started | Done | Actual | Notes |
|-------|------------|---------|------|--------|-------|
| 8.7 remainder | 0.5–1 | 2026-08-27 | 2026-08-29 | ~2 | `2.8.7.31` CP0 + chords |
| 9.1 PID hold | 2–3 | 2026-08-29 | 2026-08-30 | ~1.5 | `2.9.1.14` CP1 + takeoff/coast |
| 9.1.3 walker + span + refresh | 2–4 | 2026-09-01 | 2026-09-01 | ~1 | `2.9.1.39` Wins 0–5.1 |
| 13.1 | 1.5–2 | | | | after 9.1.3 CMPH |
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
