# Yard Master Suite v2

Clean-room rewrite of the Derail Valley **Yard Master Suite** mod. v2 abandons the v1 bolt-on `Update()` loop in favor of event-driven, zero-allocation Unity modding.

The net48 UMM solution builds (`info.json` **2.1.1** = PM story 1.1). Phase 1 pillars are stubs: `YmsEventBus.ClearAllSubscriptions()` and a `GcCadenceProbe` MonoBehaviour so `Main.cs` compiles. Real bus/probe behavior is Epic 1.2 / 1.3.

## Docs

- [Project status](docs/PROJECT_STATUS.md)
- [Versioning and release](docs/Versioning_and_Release_Strategy.md)
- [Onboarding & architecture guide](docs/YMS_v2_Onboarding_Guide.md)
- [Rebuild sequence (Phase 1–5)](docs/YMS_v2_Architecture_Plan.md)
- [Unity Pub/Sub best practices](docs/Unity_PubSub_Best_Practices.md)
- [Research and leverage manifesto](docs/Research_and_Leverage_Manifesto.md)

Archived template dumps (not project docs) live in [`docs/_templates/`](docs/_templates/).

## Start here (agentic layer)

**Start with [CONTRIBUTING.md](CONTRIBUTING.md)** — reading order, tracked vs gitignored docs, PR expectations.

**Current work:** [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md)

Document **test** and **coverage** commands in [AGENT_HANDOFF.md](AGENT_HANDOFF.md) and [TEST_PLAN.md](TEST_PLAN.md).

## What not to put in the repo

- **No secrets** — API keys, tokens, credentials. Use environment variables or a local config that is gitignored.
- **Game assemblies** — never commit Derail Valley `Managed/` or `*.dll` except `YardMasterSuite*.dll`.
- **Session handoff notes** — `.cursor/handoff/*-handoff-*.md` and `docs/handoff/*-HANDOFF-*.md` are gitignored. Commit `_template.md` and READMEs only.

## Source of truth

[CONTRIBUTING.md](CONTRIBUTING.md), [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md), [AGENT_HANDOFF.md](AGENT_HANDOFF.md), [PM_PLAN.md](PM_PLAN.md), [TEST_PLAN.md](TEST_PLAN.md), `docs/YMS_v2_*`, and `.cursor/skills/`.
