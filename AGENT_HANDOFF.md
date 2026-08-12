# Agent handoff — DerailValleyModv2

## Purpose

**DerailValleyModv2** — Yard Master Suite v2 clean-room rewrite (UMM / Harmony / net48) with Cursor rules, skills, handoff protocol, and testing discipline.

**Sync:** This repo tracks **AgenticTemplate** as an **upstream remote** for shared skills/rules.
To pull shared updates: `git fetch upstream && git merge upstream/main` (resolve stack-specific conflicts manually).

---

## Syncing updates from AgenticTemplate

When [AgenticTemplate](https://github.com/pbuckles22/AgenticTemplate) gets new skills or enhancements:

```bash
cd DerailValleyModv2
git fetch upstream
git merge upstream/main
# Resolve conflicts — keep stack-specific overrides in:
#   - DEV_GUIDE.md, TEST_TDD.md, DESIGN_SYSTEM.md
#   - always.mdc (project context)
#   - AGENT_HANDOFF.md (run/test commands)
git push origin main
```

### What stays shared vs stack-specific

| Shared (sync from upstream)                | Stack-specific (keep yours)           |
| ------------------------------------------ | ------------------------------------- |
| Most skills (techwriter, tester, code-reviewer, code-quality-gate, tech-lead, etc.) | `DEV_GUIDE.md` (architecture, tooling) |
| Rules (handoff-checklist, testing.mdc)     | `TEST_TDD.md` (test commands)         |
| Handoff templates                          | `DESIGN_SYSTEM.md` (UI framework)     |
| Operating model skills (green-and-clean, etc.) | `always.mdc` (project context)    |
|                                            | `AGENT_HANDOFF.md` (run/test section) |

YMS background stays in `docs/`. Agentic governance stays in `doc/`. Do not collapse those trees.

---

## Source of truth

- **YMS background:** [docs/YMS_v2_Onboarding_Guide.md](docs/YMS_v2_Onboarding_Guide.md), [docs/YMS_v2_Architecture_Plan.md](docs/YMS_v2_Architecture_Plan.md), [docs/Unity_PubSub_Best_Practices.md](docs/Unity_PubSub_Best_Practices.md), [docs/Research_and_Leverage_Manifesto.md](docs/Research_and_Leverage_Manifesto.md)
- **Scope / sprints:** [PM_PLAN.md](PM_PLAN.md)
- **Skills:** [.cursor/skills/](.cursor/skills/) — DEV_GUIDE.md, TEST_TDD.md, DESIGN_SYSTEM.md, techwriter, tester, code-reviewer, **code-quality-gate**, **tech-lead**, tech-debt-evaluator, eval-engineer, risk-manager, release-manager, security-reviewer, incident-triager, green-and-clean, context-bootstrapper, session-summarizer, pm-governance, ui-ux, game-readiness, visual-match, **github-feature-workflow**

## Green and clean operating model (how we work)

This project assumes a strict operating model aimed at **green and clean** delivery:

- **Green**: each change is verifiable against explicit acceptance criteria and validated at the appropriate tier.
- **Clean**: context is curated; durable state lives in tracked docs; handoffs are compressed and decision-first.

Skills that enforce this:

- [.cursor/skills/green-and-clean/SKILL.md](.cursor/skills/green-and-clean/SKILL.md)
- [.cursor/skills/context-bootstrapper/SKILL.md](.cursor/skills/context-bootstrapper/SKILL.md)
- [.cursor/skills/session-summarizer/SKILL.md](.cursor/skills/session-summarizer/SKILL.md)
- [.cursor/skills/eval-engineer/SKILL.md](.cursor/skills/eval-engineer/SKILL.md)

## Context hierarchy (what belongs where)

Contributors and agents use **tracked docs** for product truth. See [CONTRIBUTING.md](CONTRIBUTING.md).

- **Level 1:** [CONTRIBUTING.md](CONTRIBUTING.md), [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md), `.cursor/rules/always.mdc`, this file, `docs/YMS_v2_*`
- **Level 2:** [PM_PLAN.md](PM_PLAN.md), [TEST_PLAN.md](TEST_PLAN.md)
- **Level 3:** current task plan + acceptance criteria
- **Level 4 (optional, local only):** `.cursor/handoff/NNNN-handoff-*.md` — gitignored; never sole source of truth

Token hygiene: prefer Level 1 + Level 2 + current files over transcript dumps.

## Risk discipline

Keep the top risks explicit and current:

- [RISKS.md](RISKS.md) — top 5 only (impact/likelihood/trigger/mitigation/rollback)

## Release / merge discipline

Keep "ship" criteria explicit and boring:

- [RELEASE.md](RELEASE.md) — merge-ready expectations and rollback posture

## Technical debt discipline

Track debt continuously and evaluate ROI:

- [.cursor/skills/tech-debt-evaluator/SKILL.md](.cursor/skills/tech-debt-evaluator/SKILL.md) — produces "Do first" items during handoff
- [TECH_DEBT.md](TECH_DEBT.md) — durable ranked backlog (promote persistent "Do first" items here)

## Incident / debugging discipline

When something breaks, use evidence-driven triage and keep it bounded:

- [.cursor/skills/incident-triager/SKILL.md](.cursor/skills/incident-triager/SKILL.md)
- [INCIDENTS.md](INCIDENTS.md) — what to capture (minimum) for handoff and prevention

## Pod (agents always working)

- **Techwriter:** Use when editing README, AGENT_HANDOFF, or internal docs.
- **Tester:** Black-box tests; run your **documented** test command after changes; keep the suite green. See [TEST_PLAN.md](TEST_PLAN.md).
- **Handoff (mandatory):** When the user wants a handoff, run code review (code-reviewer), tech debt (tech-debt-evaluator), and your **tests or coverage** as documented below; record in the handoff note. See [.cursor/rules/handoff-checklist.mdc](.cursor/rules/handoff-checklist.mdc).

## Contributor onboarding (norm)

1. [CONTRIBUTING.md](CONTRIBUTING.md)
2. [docs/YMS_v2_Onboarding_Guide.md](docs/YMS_v2_Onboarding_Guide.md)
3. [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md)
4. [PM_PLAN.md](PM_PLAN.md)

When shipping: update **PM_PLAN**, **doc/PROJECT_STATUS.md**, and **Current state** below in the same PR.

## Current state

| | |
|--|--|
| **Project** | *Yard Master Suite v2* (UMM / Harmony / net48) — clean-room rewrite |
| **MVP** | Phase 1 Heartbeat — Event Bus + GC Probe (not implemented) |
| **Version** | unversioned (no `info.json` yet) |
| **Active branch** | **`main`** |

**Shipped on `main`**

- [x] Docs layout: `docs/YMS_v2_*` + archived templates
- [x] UMM entry stub: `YardMasterSuite/Main.cs` (does not compile — Core types missing)
- [x] AgenticTemplate merge + v1 rules delta (no `hud-in-world-only` yet)

**In flight**

- None. Next work is Phase 1 code pillars.

**Next**

1. Scaffold `YardMasterSuite.sln`, `info.json`, and `YardMasterSuite.Core` (`YmsEventBus`, `GcCadenceProbe`).
2. Wire merge-ready `dotnet test` / Release build once the solution exists.
3. Do **not** port v1 gameplay until Phase 1 heartbeat is green.

**Merge-ready (until a solution exists):** docs and rules only — no `dotnet test` gate yet. After scaffold: `dotnet test YardMasterSuite.sln` · `dotnet build YardMasterSuite.sln -c Release` · **deploy to Mods** via `package.ps1 -NoArchive` (required before Tier 2 smoke — see [.cursor/rules/deploy-before-smoke.mdc](.cursor/rules/deploy-before-smoke.mdc)).

## Run and test

**Game (this machine):** `C:\Program Files (x86)\Steam\steamapps\common\Derail Valley`  
**Mods drop:** `...\Mods\YardMasterSuite\` (not packaged yet)  
**Player.log:** `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log`

```bash
# After scaffold (not yet):
# copy Directory.Build.targets.example → Directory.Build.targets
# dotnet test YardMasterSuite.sln
# dotnet build YardMasterSuite.sln -c Release

# Deploy into the game (mandatory before asking for Tier 2 smoke, once package.ps1 exists):
# powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -Configuration Release -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

Keep in sync with [TEST_PLAN.md](TEST_PLAN.md).

## Conventions

- Prefer pure functions for business logic in `YardMasterSuite.Core` (no Unity/game refs).
- **Zero-allocation:** no new objects, lists, or uncached string generation in `Update()` loops. Event payloads are primitives or readonly structs.
- **Pub/Sub:** Type A (`System.Action`) on the main thread; Type B mailbox (`ConcurrentQueue<T>`) for heavy math. Unsubscribe in `OnDisable`/`OnDestroy`. See [docs/Unity_PubSub_Best_Practices.md](docs/Unity_PubSub_Best_Practices.md).
- **Research first:** scout open-source Unity/DV patterns before inventing. See [docs/Research_and_Leverage_Manifesto.md](docs/Research_and_Leverage_Manifesto.md).
- **v1:** [DerailValleyMod](https://github.com/pbuckles22/DerailValleyMod) is a reference for game API hooks and math only.
- **Docs:** Use the **techwriter** skill when editing README, AGENT_HANDOFF, or internal docs.
- **Tests:** Black-box; run your project test command after logic or test changes; keep the suite green (see .cursor/skills/tester/SKILL.md). Prefer writing a failing test before new production code (TDD) where applicable.

---

## Git workflow (how work lands on `main`)

**One story, one ship (hard):** one PM_PLAN story (or one agreed ship) per branch/commit cycle. Do **not** start the next story while the current one is uncommitted — including while waiting on Tier 2 smoke. If stacking seems necessary, ask first. Rule: [.cursor/rules/one-story-one-ship.mdc](.cursor/rules/one-story-one-ship.mdc).

1. **Integration branch:** **`main`**. All shipped product state (PM_PLAN, roadmap checkboxes) should reflect what is merged here.
2. **Short-lived branches:** One story per branch (`feature/<story-id>-topic`); merge into `main` before starting the next. Agents follow [.cursor/skills/github-feature-workflow/SKILL.md](.cursor/skills/github-feature-workflow/SKILL.md) and [.cursor/rules/one-story-one-ship.mdc](.cursor/rules/one-story-one-ship.mdc).
3. **Before push / merge-ready:** Run the **full gate** in **Run and test** above. Same checks should run in CI if you use GitHub Actions.
4. **After push — verify CI:** When Actions exist, run `gh run watch --repo pbuckles22/DerailValleyModv2` (or `gh run list` + `gh run view --log-failed`) before declaring work done on `main`.
5. **Pull requests:** **Optional** for DerailValleyModv2. Direct push to `main` after green local gate is valid; if a PR is opened, use the same test plan text you ran locally.
6. **After merge:** Delete the local feature branch; delete the remote feature branch if your flow created one.

---

## Handoff protocol

When ending a session:

1. Run the handoff checklist ([handoff-checklist.mdc](.cursor/rules/handoff-checklist.mdc)).
2. Update **PM_PLAN.md** when shipped scope changed.
3. Update **[doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md)** and **Current state** above (required for contributor-visible changes).
4. Optional local note: `.cursor/handoff/NNNN-handoff-*.md` ([template](.cursor/handoff/_template.md)) — gitignored; promote decisions to tracked docs.

## Epic close (automatic)

When an epic's in-scope work is done, **do not wait for the user to ask**. Run [.cursor/rules/epic-close.mdc](.cursor/rules/epic-close.mdc) / pm-governance *Epic close* **in full** — no docs-only or self-graded partials. Order: handoff-checklist skills (code-reviewer, tech-debt-evaluator) → merge-ready this pass → Tier 2 evidence → status docs → close note → commit/push **after** smoke PASS for deployable ships. See [.cursor/skills/pm-governance/SKILL.md](.cursor/skills/pm-governance/SKILL.md) and [.cursor/rules/deploy-before-smoke.mdc](.cursor/rules/deploy-before-smoke.mdc).

Anything the team must see on GitHub belongs in **PROJECT_STATUS**, **PM_PLAN**, **README**, or the **PR** — not only gitignored handoff files.
