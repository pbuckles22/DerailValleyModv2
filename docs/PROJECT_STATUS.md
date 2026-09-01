# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-08-31 (**9.1.2** Win 5 checked in `2.9.1.19` on feature branch)

## Summary

**DerailValleyModv2** — Yard Master Suite v2. **Epic 3 Display Shell (infra) closed** at **3.3.1**. **Epic 4 infra closed** at **4.3**. **Epic 6 Diagnostic HUD closed** at **6.21** (`2.6.21.6`). **Epic 7 Governors closed** at **7.5** (`2.7.5.7`). **8.7** on **`main`** (`2.8.7.31`). **9.1** on **`main`** (`2.9.1.14`): DE2 PID hold + takeoff slew + ±2 coast; HTP CP1 green. **9.1.2** Path Limit rebuild in flight on **`feature/9.1.2-win1-corridor-12m`**: Wins **0–5** (`2.9.1.15`–`.19`), Win **6** next; blocks **13.1**. **9.2** after **13.4**. Full v1 map: [V1_FEATURE_COVERAGE.md](V1_FEATURE_COVERAGE.md). Canonical HTP: [HTP.md](HTP.md).

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — **9.1** (`2.9.1.14`). |
| **`feature/9.1.2-win1-corridor-12m`** | **9.1.2** Limit ladder — Wins 0–5 @ `2.9.1.19`; Win 6 next. |
| **`feature/8.7-route-pin-cleared`** | Keep — do not delete. |

---

## Sequence

**Next:** **9.1.2 Win 6** (Evaluate = Maps authority) → Win 7 pin smoke → then **Epic 13** (**13.1**). **9.2** if needed after **13.4** → **Epic 14** Maps desk → **Epic 10**. **Deferred:** 8.8–8.9, 11 Catalog, 12 Roadside. Reverse-cruise gold remains cab **`feature=0`** with desk closed.

### Autonomy tracker (re-baseline)

| Story | Est (days) | Started | Done | Actual | Notes |
|-------|------------|---------|------|--------|-------|
| 8.7 remainder | 0.5–1 | 2026-08-27 | 2026-08-29 | ~2 | `2.8.7.31` CP0 + chords |
| 9.1 PID hold | 2–3 | 2026-08-29 | 2026-08-30 | ~1.5 | `2.9.1.14` CP1 + takeoff/coast |
| 13.1 | 1.5–2 | | | | |
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

---

## Epics

See [PM_PLAN.md](../PM_PLAN.md) and [AGENT_HANDOFF.md](../AGENT_HANDOFF.md).
