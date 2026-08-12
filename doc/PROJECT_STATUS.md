# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-08-12

---

## Summary

**DerailValleyModv2** — Yard Master Suite v2 clean-room rewrite. Docs and UMM entry stub are on `main`. Phase 1 Heartbeat (Event Bus + GC Probe) is next. v1 DerailValleyMod is reference-only.

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — docs + agentic overlay |

---

## Completed

- YMS background in `docs/` (onboarding, architecture, Pub/Sub, research manifesto)
- Public repo [pbuckles22/DerailValleyModv2](https://github.com/pbuckles22/DerailValleyModv2)
- AgenticTemplate upstream merge
- v1 rules delta: `no-cursor-commit-attribution`, `one-story-one-ship`, `deploy-before-smoke`, `smoke-gates-tier1-ci` (`hud-in-world-only` deferred)

---

## Next up

1. Scaffold solution + `YmsEventBus` + `GcCadenceProbe` (PM_PLAN Epic 1)
2. Wire `dotnet test` / Release merge-ready
3. Do not port v1 gameplay until Phase 1 is green

---

## Reading order for contributors

See [CONTRIBUTING.md](../CONTRIBUTING.md) and [docs/YMS_v2_Onboarding_Guide.md](../docs/YMS_v2_Onboarding_Guide.md).
