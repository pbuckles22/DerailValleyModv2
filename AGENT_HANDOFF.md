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
| Rules (`one-story-one-ship`, `no-cursor-commit-attribution`, `testing.mdc`, handoff-checklist, epic-close, gemini-handoff) | `TEST_TDD.md` (commands + evidence names) |
| Handoff templates                          | `DESIGN_SYSTEM.md` (UI framework)     |
| Operating model skills (green-and-clean, etc.) | `always.mdc` (5–10 project invariant lines only) |
|                                            | `AGENT_HANDOFF.md` (run/test section) |
|                                            | Optional overlay rules (install/smoke paths) |

All project docs live in **`docs/`** (YMS background, `PROJECT_STATUS`, requirements, gemini drop, optional handoff). Upstream AgenticTemplate still uses `doc/` — keep this repo’s `docs/` paths when merging upstream.

---

## Source of truth

- **YMS background:** [docs/YMS_v2_Onboarding_Guide.md](docs/YMS_v2_Onboarding_Guide.md), [docs/YMS_v2_Architecture_Plan.md](docs/YMS_v2_Architecture_Plan.md), [docs/Unity_PubSub_Best_Practices.md](docs/Unity_PubSub_Best_Practices.md), [docs/Research_and_Leverage_Manifesto.md](docs/Research_and_Leverage_Manifesto.md), [docs/LEVERAGE_REGISTER.md](docs/LEVERAGE_REGISTER.md), [docs/PERFORMANCE_LOG.md](docs/PERFORMANCE_LOG.md)
- **Scope / sprints:** [PM_PLAN.md](PM_PLAN.md)
- **Versioning:** [docs/Versioning_and_Release_Strategy.md](docs/Versioning_and_Release_Strategy.md) — `info.json` = `2.{Epic}.{Story}`
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

- **Level 1:** [CONTRIBUTING.md](CONTRIBUTING.md), [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md), `.cursor/rules/always.mdc`, this file, `docs/YMS_v2_*`
- **Level 2:** [PM_PLAN.md](PM_PLAN.md), [TEST_PLAN.md](TEST_PLAN.md)
- **Level 3:** current task plan + acceptance criteria
- **Level 4 (optional, local only):** `docs/handoff/NNNN-HANDOFF-*.md` (prefer) or `.cursor/handoff/NNNN-handoff-*.md` — gitignored; never sole source of truth

Token hygiene: prefer Level 1 + Level 2 + current files over transcript dumps.

## Risk discipline

Keep the top risks explicit and current:

- [RISKS.md](RISKS.md) — top 5 only (impact/likelihood/trigger/mitigation/rollback)

## Release / merge discipline

Keep "ship" criteria explicit and boring:

- [RELEASE.md](RELEASE.md) — merge-ready, rollback, and pointer to PM-driven versioning

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
3. [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md)
4. [PM_PLAN.md](PM_PLAN.md)

When shipping: update **PM_PLAN**, **docs/PROJECT_STATUS.md**, `info.json` (`2.{Epic}.{Story}`), and **Current state** below in the same PR.

## Current state

| | |
|--|--|
| **Project** | *Yard Master Suite v2* (UMM / Harmony / net48) — clean-room rewrite |
| **MVP** | Epic **3** display shell **closed** at **3.3.1**; Epic **6** v1 HUD parity in flight ([HUD_v1_Parity_Matrix.md](docs/HUD_v1_Parity_Matrix.md)). **4.4** PID blocked on spec. |
| **Version** | **2.6.13** (`info.json`) |
| **Active branch** | **`origin/main`** @ `a90ef6b` — **6.13** Job + on-consist (`2.6.13`) landed. Do not re-merge. Next is **6.15** when asked. |

**Shipped on `main`**

- [x] **Epic 0** Repo bootstrap — closed 2026-08-12 (docs layout, public repo, agentic overlay)
- [x] **Epic 1** Phase 1 Heartbeat — closed 2026-08-12 (scaffold, Type A bus, `package.ps1`, hitch probe, string cache)
- [x] **1.1** Solution scaffold — `YardMasterSuite.sln`, csproj, `info.json` **2.1.1**, `Directory.Build.targets.example`, stub `YmsEventBus` + `GcCadenceProbe` so `Main.cs` builds
- [x] **1.2** `YmsEventBus` — Type A `Action` bus, primitive/readonly-struct payloads, `ClearAllSubscriptions()`, unsubscribe tests (`info.json` **2.1.2**)
- [x] **1.3** `package.ps1` — deploy Release DLL + `info.json` into `Mods\YardMasterSuite\`; Release PostBuild zips `dist/`
- [x] **1.4** `GcCadenceProbe` — hitch gate + throttled `T2 hitch-spike` (Tier 2 PASS 2026-08-12)
- [x] **1.5** `GuiContentCache` / `StringBuilderPool` — commit label text only when it changes (`info.json` **2.1.5**)
- [x] **2.1** Loco state listener — `PlayerManager.CarChanged` → cached boarded loco + `YmsEventBus.OnPlayerBoardedTrain` (`info.json` **2.2.1**, Tier 2 PASS 2026-08-12)
- [x] **2.2** Control telemetry — named thr/indy/train/eng/rev on lever move (`info.json` **2.2.2**, Tier 2 PASS 2026-08-12)
- [x] **2.3** Trainset topology — consist cars/tonnes on couple/uncouple; yard pin-pulls on foot (`info.json` **2.2.3**, Tier 2 PASS 2026-08-12)
- [x] **Epic 2** Phase 2 Senses — closed 2026-08-12
- [x] **docs** — `doc/` merged into `docs/` (single tree)
- [x] **Versioning** — `2.{Epic}.{Story}` from PM_PLAN ([docs/Versioning_and_Release_Strategy.md](docs/Versioning_and_Release_Strategy.md)); local `+BUILD` in gitignored `build_number.txt`

- [x] **3.1** HUD manager — top bar + always-on compass; look-direction 16-point; hitch probe 100 ms + world-session gate (`info.json` **2.3.1**, Tier 2 PASS 2026-08-13)
- [x] **3.2** AR overlay — office STN + own-loco LOCO; mid-edge fan; hitch-summary; no HUD clamp (`info.json` **2.3.2**, Tier 2 PASS 2026-08-17)
- [x] **4.1** Type B mailbox — `YmsMailbox<T>` + main-thread drain → Type A; worker probe `T2 mailbox: n=1` (`info.json` **2.4.1**, Tier 2 PASS 2026-08-17)
- [x] **4.2** Track graph builder — time-sliced `RailTrack` walk (64/tick) + worker A\* via Type B (`info.json` **2.4.2**, Tier 2 PASS 2026-08-17)
- [x] **4.3** Geometry scanner — bezier once per segment + cache-until-change + Type A (`info.json` **2.4.3**, Tier 2 PASS 2026-08-17)
- [x] **3.3.1** HUD v1 chrome parity — product labels, `UsableTrainGate`, four-bar stack, AR sticky Y (`info.json` **2.3.5.1**, Tier 2 PASS 2026-08-17)
- [x] **Epic 3** Display Shell (infra) — **closed 2026-08-17** (ends at **3.3.1**; **3.4**/**3.5** → Epic **6**)
- [x] **6.1** Always-on Clock — Heading + world-time Clock chip (`info.json` **2.6.1**, Tier 2 PASS 2026-08-18)
- [x] **6.3** Consist on look-at usable train — on-foot Cars/Mass (`info.json` **2.6.3**, Tier 2 PASS 2026-08-17)
- [x] **6.2** Look-at polish — car id / cargo / loco type; identity-only look-at log (`info.json` **2.6.2**, Tier 2 PASS 2026-08-17)
- [x] **6.5** Mass + Grade — cab Mass + Grade; change-only gadget gate (`info.json` **2.6.5**, Tier 2 PASS 2026-08-18)
- [x] **6.4** AR stack sync — Edge STN/LOCO under HUD stack; OnObject on world object (`info.json` **2.6.4**, Tier 2 PASS 2026-08-17)
- [x] **6.6** Load + Motors + Fluids — cab Fuel / Oil / Load / Motors (`info.json` **2.6.6**, Tier 2 PASS 2026-08-19)
- [x] **6.7** MU sync — cab MU idle / desync (`info.json` **2.6.7**, Tier 2 PASS 2026-08-19)
- [x] **6.8** Full lever + Speed + Limit — cab Speed / Limit; live levers (`info.json` **2.6.8**, Tier 2 PASS 2026-08-20)
- [x] **6.9** Posted board index — posted sticky Limit; geometry scanner ripped (`info.json` **2.6.9**, Tier 2 PASS 2026-08-20)
- [x] **6.10** Next + distance — Next chip + meters when close; path-ahead on thrown route; dual numbers through-only (`info.json` **2.6.10**, Tier 2 PASS 2026-08-20)
- [x] **6.11** Marked + Path — Home return chip; End dest Path check; look-away keeps origin (`info.json` **2.6.11**, Tier 2 PASS 2026-08-20)
- [x] **6.12** Station chip — in-zone `Station CP … m` / `here`; omit outside job zone (`info.json` **2.6.12**, Tier 2 PASS 2026-08-20)
- [x] **6.13** Job bar + look-at Job chip — taken GO/HOLD/RED + Bonus; on-consist cab keys stacked (`info.json` **2.6.13**, Tier 2 PASS 2026-08-21)
- [~] **Epic 6** Diagnostic HUD — **6.1–6.13** `[x]`; **6.14** cut; **6.19** consist stress backlog

### In flight

- Epic **6** remainder (**6.15** Pin AR). Dual junction **numbers** still through-only. Look-around hitch is TECH_DEBT. PNG icons → **6.17**. Auto-coupler still **5.3**. Glide + pause-hide are Later.

### Sequence (do not pause to pick)

Next in [PM_PLAN.md](PM_PLAN.md): **6.15** Pin AR, then remaining Epic **6** waves (**6.19** consist stress after **6.18**), then **4.4** PID when spec lands. Epic 5 auto-coupler after on-consist slice.

### Next

1. Continue Epic **6** from updated `main`: **6.15** Pin AR. Consist stress → **6.19**. PNG icons → **6.17**. Auto-coupler still **5.3**.
2. Dual junction **numbers** stay through-only until a later follow-up (`selectedBranch` already walks the thrown track).
3. **4.4** PID when user spec lands.

**Merge-ready:** `npx --yes markdownlint-cli2` · `dotnet test YardMasterSuite.sln` · `dotnet build YardMasterSuite.sln -c Release`. Deploy to Mods via `package.ps1 -NoArchive` before asking for Tier 2 smoke.

## Run and test

**Game (this machine):** `C:\Program Files (x86)\Steam\steamapps\common\Derail Valley`  
**Mods drop:** `...\Mods\YardMasterSuite\`  
**Player.log:** `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log`

```bash
# First clone: copy Directory.Build.targets.example → Directory.Build.targets
npx --yes markdownlint-cli2
dotnet test YardMasterSuite.sln
dotnet build YardMasterSuite.sln -c Release

# Deploy into the game (mandatory before asking for Tier 2 smoke):
powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -Configuration Release -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

Keep in sync with [TEST_PLAN.md](TEST_PLAN.md).

**Tier 2 performance:** After every in-world smoke, read `GcCadenceProbe` output in Player.log (`T2 hitch-spike`, `T2 hitch-summary`). Print spawn / cab / look vs the last session **in the chat summary** ([chat-performance-summary.mdc](.cursor/rules/chat-performance-summary.mdc)). Archive H-rows in [docs/PERFORMANCE_LOG.md](docs/PERFORMANCE_LOG.md). Product chips without that block is an incomplete PASS.

## Conventions

- Prefer pure functions for business logic in `YardMasterSuite.Core` (no Unity/game refs).
- **Zero-allocation:** no new objects, lists, or uncached string generation in `Update()` loops. Event payloads are primitives or readonly structs.
- **Pub/Sub:** Type A (`System.Action`) on the main thread; Type B mailbox (`ConcurrentQueue<T>`) for heavy math. Unsubscribe in `OnDisable`/`OnDestroy`. See [docs/Unity_PubSub_Best_Practices.md](docs/Unity_PubSub_Best_Practices.md).
- **Research first:** scout open-source Unity/DV patterns before inventing. See [docs/Research_and_Leverage_Manifesto.md](docs/Research_and_Leverage_Manifesto.md) and the per-story log [docs/LEVERAGE_REGISTER.md](docs/LEVERAGE_REGISTER.md).
- **v1:** [DerailValleyMod](https://github.com/pbuckles22/DerailValleyMod) is a reference for game API hooks and math only.
- **Docs:** Use the **techwriter** skill when editing README, AGENT_HANDOFF, or internal docs.
- **Tests:** Black-box; run your project test command after logic or test changes; keep the suite green (see .cursor/skills/tester/SKILL.md). Prefer writing a failing test before new production code (TDD) where applicable.

---

## Git workflow (how work lands on `main`)

**One story, one ship (hard):** one PM_PLAN story (or one agreed ship) per branch/commit cycle. Do **not** start the next story while the current one is uncommitted — including while waiting on merge-to-`main` approval or Tier 2 smoke. If stacking seems necessary, ask first. Rule: [.cursor/rules/one-story-one-ship.mdc](.cursor/rules/one-story-one-ship.mdc).

1. **Integration branch:** **`main`**. All shipped product state (PM_PLAN, roadmap checkboxes) should reflect what is merged here.
2. **Short-lived branches:** One story per branch (`feature/<story-id>-topic`). Agents follow [.cursor/skills/github-feature-workflow/SKILL.md](.cursor/skills/github-feature-workflow/SKILL.md) and [.cursor/rules/one-story-one-ship.mdc](.cursor/rules/one-story-one-ship.mdc).
3. **Before push / merge-ready:** Run the **full gate** in **Run and test** above. Same checks should run in CI if you use GitHub Actions. Then **commit** and **`git push -u origin <feature-branch>`**.
4. **After push — stop:** Do **not** merge to `main` or `git push origin main` until the user approves in chat ([.cursor/rules/no-auto-merge-main.mdc](.cursor/rules/no-auto-merge-main.mdc)). Waiting on that approval is a pause — do not start the next story.
5. **After the user approves merge:** `git checkout main && git pull && git merge <branch> && [merge-ready] && git push origin main`. Then delete the local feature branch; delete the remote feature branch if your flow created one. When Actions exist, run `gh run watch --repo pbuckles22/DerailValleyModv2` (or `gh run list` + `gh run view --log-failed`) after `main` updates.
6. **Pull requests:** **Optional.** Do not open a PR unless the user asks. If a PR is opened, use the same test plan text you ran locally.

---

## Handoff protocol

When ending a session:

1. Run the handoff checklist ([handoff-checklist.mdc](.cursor/rules/handoff-checklist.mdc)).
2. Update **PM_PLAN.md** when shipped scope changed.
3. Update **[docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md)** and **Current state** above (required for contributor-visible changes).
4. Local session note (gitignored): prefer **`docs/handoff/NNNN-HANDOFF-*.md`** (visible in the docs tree). Optional copy: `.cursor/handoff/NNNN-handoff-*.md` ([template](.cursor/handoff/_template.md)). Include **Git truth** (`origin/main @ sha` or not-merged). The next agent **does not** re-prove that land ([context-bootstrapper](.cursor/skills/context-bootstrapper/SKILL.md)). Promote decisions to tracked docs. If **Next** is Tier 2 smoke, include the player-facing ask ([deploy-before-smoke.mdc](.cursor/rules/deploy-before-smoke.mdc) → *How to ask*).

## Epic close (automatic)

When an epic's in-scope work is done, **do not wait for the user to ask**. Run [.cursor/rules/epic-close.mdc](.cursor/rules/epic-close.mdc) / pm-governance *Epic close* **in full** — no docs-only or self-graded partials. Order: handoff-checklist skills (code-reviewer, tech-debt-evaluator) → merge-ready this pass → Tier 2 evidence → status docs → close note → commit/push **after** smoke PASS for deployable ships → **GitHub Release after merge to `main`**. Nexus waits until the mod is playable. See [.cursor/skills/pm-governance/SKILL.md](.cursor/skills/pm-governance/SKILL.md), [.cursor/rules/deploy-before-smoke.mdc](.cursor/rules/deploy-before-smoke.mdc), and [docs/Versioning_and_Release_Strategy.md](docs/Versioning_and_Release_Strategy.md).

Anything the team must see on GitHub belongs in **PROJECT_STATUS**, **PM_PLAN**, **README**, or the **PR** — not only gitignored handoff files.
