# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-08-28 (8.7 pin/CLEARED cab PASS; dest Set word open)

## Summary

**DerailValleyModv2** — Yard Master Suite v2. **Epic 3 Display Shell (infra) closed** at **3.3.1**. **Epic 4 infra closed** at **4.3**. **Epic 6 Diagnostic HUD closed** at **6.21** (`2.6.21.6`). **Epic 7 Governors closed** at **7.5** (`2.7.5.7`). **8.6** on **`main`** (`2.8.6.4` @ `e9346e5`). **8.7** on **`spike/8.7-virtual-nose`** (`2.8.7.22`, not merged): pin + reverse CLEARED + Align cab PASS; dest Set word still wrong at Set dest. Full v1 map: [V1_FEATURE_COVERAGE.md](V1_FEATURE_COVERAGE.md). Canonical HTP: [HTP.md](HTP.md).

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — **8.6** (`2.8.6.4` @ `e9346e5`). |
| **`spike/8.7-virtual-nose`** | **8.7** + HTP — **`2.8.7.22`** (not merged). |
| **`feature/8.7-route-pin-cleared`** | Keep — do not delete. |

---

## Sequence

**Next:** **8.7** dest Set word at bind (post-pin corridor) → CMPH when asked → **9.1** PID → **Epic 13** → **Epic 10**. **Deferred:** 8.8–8.9, 8.11–8.12, 11 Catalog, 12 Roadside. 8.x cab hitch gate remains **`feature=0`**. Do **not** start 9.1 while 8.7 is open.

### Autonomy tracker (re-baseline)

| Story | Est (days) | Started | Done | Actual | Notes |
|-------|------------|---------|------|--------|-------|
| 8.7 remainder | 0.5–1 | 2026-08-27 | | | WIP `2.8.7.2` |
| 9.1 PID | 2–3 | | | | cruise w/o MPC |
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

- [x] **Epic 3** — Display Shell infra (**closed 2026-08-17**)
- [x] **Epic 4** — Heavy-engine infra (**closed**; PID/MPC → **9**)
- [x] **Epic 6** — Diagnostic HUD (**closed 2026-08-24** at **6.21**)
- [x] **Epic 7** — Governors (**closed 2026-08-26** at **7.5**)
- [ ] **Epic 8** — Maps / Dispatcher (**8.7** on critical path; rest deferred)
- [ ] **Epic 9** — PID/MPC (**9.1** on critical path after **8.7**)
- [ ] **Epic 13** — Autonomous single-job loop (**NEW** — panacea Phase C)
- [ ] **Epic 10** — Multi-job + optimizer (Phase D, after **13**)
- [ ] **Epic 11** — Catalog (**deferred**)
- [ ] **Epic 12** — Roadside (**deferred**)

---

## Notes

See [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) for run/test commands and merge-ready gate.
