# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-08-12

---

## Summary

**DerailValleyModv2** — Yard Master Suite v2 clean-room rewrite. **Epic 0 closed.** Next: **PM 1.1** solution scaffold (net48 UMM + Core types `Main.cs` already calls) so `dotnet test` / Release build exist. v1 DerailValleyMod is reference-only.

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — Epic 0 (docs + agentic overlay) |

---

## Epics

- [x] **Epic 0** — Repo bootstrap (closed 2026-08-12)
- [ ] **Epic 1** — Phase 1 Heartbeat — **next: 1.1 only**
- [ ] **Epic 2–5** — Senses / Display / Engines / Tools (blocked on Epic 1)

### Next

1. **1.1** `feature/1-1-solution-scaffold` — sln, csproj, `info.json` 0.1.0, `Directory.Build.targets.example`, `YmsEventBus` + `GcCadenceProbe` so `Main.cs` builds. One story. No v1 port.
2. Then 1.2/1.3 thicken bus/probe + Tier 1 tests; 1.4 string cache later.

---

## Reading order for contributors

See [CONTRIBUTING.md](../CONTRIBUTING.md), [docs/YMS_v2_Onboarding_Guide.md](../docs/YMS_v2_Onboarding_Guide.md), latest local handoff `.cursor/handoff/0001-handoff-2026-08-12_1300.md`.
