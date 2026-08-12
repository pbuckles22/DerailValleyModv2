# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-08-12

---

## Summary

**DerailValleyModv2** — Yard Master Suite v2 clean-room rewrite. **Epic 0 closed.** **1.1 shipped** — net48 UMM solution builds (`info.json` **2.1.1**). Versioning is `2.{Epic}.{Story}` from PM_PLAN. Next: **1.2 YmsEventBus**. v1 DerailValleyMod is reference-only.

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — Epic 0 + 1.1 scaffold |

---

## Epics

- [x] **Epic 0** — Repo bootstrap (closed 2026-08-12)
- [ ] **Epic 1** — Phase 1 Heartbeat — **next: 1.2** (1.1 scaffold done)
- [ ] **Epic 2–5** — Senses / Display / Engines / Tools (blocked on Epic 1)

### Next

1. **1.2** `YmsEventBus` — Type A `Action` bus + `ClearAllSubscriptions()` with unsubscribe tests. One story.
2. Then **1.3** `package.ps1` (first deploy path). Do **not** ask for Tier 2 smoke until that story ships.
3. Then **1.4** `GcCadenceProbe` (first in-world smoke); **1.5** string cache later.

---

## Reading order for contributors

See [CONTRIBUTING.md](../CONTRIBUTING.md), [YMS_v2_Onboarding_Guide.md](YMS_v2_Onboarding_Guide.md).
