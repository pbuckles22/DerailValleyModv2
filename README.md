# Yard Master Suite v2

Clean-room rewrite of the Derail Valley **Yard Master Suite** mod. v2 abandons the v1 bolt-on `Update()` loop in favor of event-driven, zero-allocation Unity modding.

The net48 UMM solution builds (`info.json` **2.3.1**). Phase 1 Heartbeat and Phase 2 Senses are complete. Phase 3 Display Shell: in-world top bar + compass (story 3.1).

```powershell
powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -Configuration Release -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

## Docs

- [Project status](docs/PROJECT_STATUS.md)
- [Versioning and release](docs/Versioning_and_Release_Strategy.md)
- [Onboarding & architecture guide](docs/YMS_v2_Onboarding_Guide.md)
- [Rebuild sequence (Phase 1–5)](docs/YMS_v2_Architecture_Plan.md)
- [Unity Pub/Sub best practices](docs/Unity_PubSub_Best_Practices.md)
- [Research and leverage manifesto](docs/Research_and_Leverage_Manifesto.md)
- [Leverage register](docs/LEVERAGE_REGISTER.md) — per-story reuse vs invent

Archived template dumps (not project docs) live in [`docs/_templates/`](docs/_templates/).

## Start here (agentic layer)

**Start with [CONTRIBUTING.md](CONTRIBUTING.md)** — reading order, tracked vs gitignored docs, PR expectations.

**Current work:** [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md)

Document **test** and **coverage** commands in [AGENT_HANDOFF.md](AGENT_HANDOFF.md) and [TEST_PLAN.md](TEST_PLAN.md).

## What not to put in the repo

- **No secrets** — API keys, tokens, credentials. Use environment variables or a local config that is gitignored.
- **Game assemblies** — never commit Derail Valley `Managed/` or `*.dll` except `YardMasterSuite*.dll`.
- **Session handoff notes** — `docs/handoff/*-HANDOFF-*.md` and `.cursor/handoff/*-handoff-*.md` are gitignored. Commit `_template.md` and READMEs only.
- **Gemini drop** — `docs/gemini/` working files are gitignored; keep `README.md`. Full dump is gitignored `Gemini_Snapshot.txt`.

## Source of truth

[CONTRIBUTING.md](CONTRIBUTING.md), [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md), [AGENT_HANDOFF.md](AGENT_HANDOFF.md), [PM_PLAN.md](PM_PLAN.md), [TEST_PLAN.md](TEST_PLAN.md), `docs/YMS_v2_*`, and `.cursor/skills/`.
