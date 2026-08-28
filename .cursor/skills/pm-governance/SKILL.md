---
name: pm-governance
description: >-
  Project management and governance. Use when planning sprints, making scope
  decisions, enforcing quality gates, identifying risks, or closing an epic.
  Epic close runs automatically when an epic is done — do not wait for the user to ask.
---

# PM governance — Project

Use this skill when doing sprint planning, scope tradeoffs, quality gates, risk mitigation, or **epic close**. Keep in sync with docs/requirements/ if present.

---

## Tactical oversight

- **Risk mitigation:** Identify blocking dependencies or risks early.
- **Scope management:** Focus on MVP first; when adding scope, note whether it's MVP or later.
- **Quality gates:** Define what "done" means (e.g. tests green, coverage, no known blockers). Panacea stories: HTP named Core test is a gate — [docs/HTP.md](../../docs/HTP.md). Do **not** add a Headless epic; expand inside **8.7** / **9.1** / **13.x**.

## Communication

- **Developer sync:** Flag performance or architecture risks (e.g. UI thread load).
- **UX/requirements:** Point to docs/requirements/ or DESIGN_SYSTEM when UX is in scope.

## When to apply

- User asks for sprint planning, scope review, or "what's MVP."
- Deciding whether a feature belongs in current vs next sprint.
- Before marking a build or feature "done."
- **Whenever an epic’s in-scope acceptance is met** — run [Epic close](#epic-close-automatic) without waiting to be asked.

## Output

- **Scope:** Clear MVP vs later; which sprint a change belongs to.
- **Risks:** Listed with mitigation.
- **Quality:** Gates stated; link to PM_PLAN.md and docs/requirements/.
- **Epic close:** Status docs updated + short close summary for the user.

---

## Epic close (automatic)

**Policy:** When the last in-scope story of an epic is done **and** SWAT permission is granted (they typed **SWAT**, or that story’s **CMPH**), run this procedure in the same session. See [wrap-on-command.mdc](../../rules/wrap-on-command.mdc). Mid-epic CMPH is not SWAT.

### Done means

- In-scope features in `PM_PLAN.md` / product plan / epics docs are checked or explicitly **cut**.
- Merge-ready gate from `AGENT_HANDOFF.md` was green for the closing work.
- Applicable Tier 2 checklist items for that epic are checked (or N/A / cut).

### Procedure (do all)

1. **SWAT gates** — Run [.cursor/rules/handoff-checklist.mdc](../../rules/handoff-checklist.mdc) → *SWAT / epic close only*. Do **not** run this swarm on a mid-epic CMPH. Record PASS/WARN/FAIL in the close note.
2. **PM_PLAN** — Mark the phase/epic **Status: complete** (date). Move leftover non-epic items to the next phase or backlog.
3. **Product / epics doc** — One-line epic status (`**Status:** complete — YYYY-MM-DD`) when you maintain that file.
4. **docs/PROJECT_STATUS.md** + **AGENT_HANDOFF.md** → *Current state* — epic closed; **Next** = next phase only (do not invent work).
5. **TEST_PLAN / TECH_DEBT / RISKS** — Align with closed scope; promote persistent debt into `TECH_DEBT.md`.
6. **README** — One-line product state if the public blurb is stale.
7. **Local handoff / close note** — required; include review, debt, tests, close results (same sections as handoff).
8. **Commit + push the feature branch** per github-feature-workflow / `AGENT_HANDOFF.md`. Merge to `main` only after the user approves.
9. **After merge to `main`:** cut a **GitHub Release** (`v{info.json}`, zip + player notes). Do not wait to be asked. Do **not** upload to Nexus until the mod is playable.
10. **Summarize for the user** — dual-audience close results. Do **not** start the next epic’s implementation unless the user already asked.

### Do not auto-do on close

- Start next-epic code unless directed.
- Retire deferred prototypes / dual trees unless the user or plan **Next** says so.
- Re-open cut features.
