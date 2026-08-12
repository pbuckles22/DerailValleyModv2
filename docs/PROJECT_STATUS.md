# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-08-12

---

## Summary

**DerailValleyModv2** — Yard Master Suite v2 clean-room rewrite. **Epic 0 closed.** **1.1–1.3 shipped** — net48 UMM solution, Type A `YmsEventBus`, and `package.ps1` (`info.json` **2.1.3**). Versioning is `2.{Epic}.{Story}` from PM_PLAN. Next: **1.4 `GcCadenceProbe`**. v1 DerailValleyMod is reference-only.

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — Epic 0 + 1.1–1.3 (scaffold, Type A bus, packaging) |

---

## Epics

- [x] **Epic 0** — Repo bootstrap (closed 2026-08-12)
- [ ] **Epic 1** — Phase 1 Heartbeat — **next: 1.4** (1.1–1.3 done)
- [ ] **Epic 2–5** — Senses / Display / Engines / Tools (blocked on Epic 1)

### Next

1. **1.4** `GcCadenceProbe` (first in-world smoke). Deploy with `package.ps1 -NoArchive` before asking for smoke.
2. Then **1.5** string cache.

---

## Reading order for contributors

See [CONTRIBUTING.md](../CONTRIBUTING.md), [YMS_v2_Onboarding_Guide.md](YMS_v2_Onboarding_Guide.md).
