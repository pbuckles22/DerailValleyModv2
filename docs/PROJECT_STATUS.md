# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-08-27

## Summary

**DerailValleyModv2** — Yard Master Suite v2. **Epic 3 Display Shell (infra) closed** at **3.3.1**. **Epic 4 infra closed** at **4.3**. **Epic 6 Diagnostic HUD closed** at **6.21** (`2.6.21.6`). **Epic 7 Governors closed** at **7.5** (`2.7.5.7`). **8.5** Multi-step Maps on **`main`** (`2.8.5.1`). Next numbered story is **8.6** when asked (or **8.11**/**8.12** desk UX). Full v1 map: [V1_FEATURE_COVERAGE.md](V1_FEATURE_COVERAGE.md).

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — **8.5** Multi-step Maps (`2.8.5.1`). |

---

## Sequence

**Next:** **8.6** Move cars here when asked (or **8.11** Close / **8.12** amenity filter). Finish remaining **8.x** before **9** speed/brakes. 8.x cab hitch gate remains **`feature=0`**. Deferred: live always-on route HUD, **8.10** couple auto-advance, parallel Next metres. **9.1** PID blocked on user spec. **11** catalog last.

---

## Epics

- [x] **Epic 3** — Display Shell infra (**3.3.1** closes epic; **3.4** delivered with **6.8**; **3.5** → **6.9–6.10**)
- [x] **Epic 4** — Heavy-engine infra (**4.1–4.3**; PID/MPC → **9**)
- [x] **Epic 6** — Diagnostic HUD v1 parity (**closed 2026-08-24** at **6.21**; **6.14** cut)
- [x] **Epic 7** — Governors (**closed 2026-08-26** at **7.5**; `2.7.5.7`)
- [ ] **Epic 8** — Yard / Dispatcher (desk, Align, Switch List, Maps, teleport)
- [ ] **Epic 9** — Speed / brake brains (PID / MPC; user spec)
- [ ] **Epic 10** — Multi-job Maps (tour optimizer; not immediate 8.x)
- [ ] **Epic 11** — Digital Catalog (**last** — playable without it)

Epic **5** is unused (governors remapped to **7** on 2026-08-25).

---

## Reading order

See [CONTRIBUTING.md](../CONTRIBUTING.md), [YMS_v2_Onboarding_Guide.md](YMS_v2_Onboarding_Guide.md), [LEVERAGE_REGISTER.md](LEVERAGE_REGISTER.md), [HUD_v1_Parity_Matrix.md](HUD_v1_Parity_Matrix.md).
