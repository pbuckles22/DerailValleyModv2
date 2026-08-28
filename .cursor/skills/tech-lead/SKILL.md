---
name: tech-lead
description: >-
  Technical leadership: turn plans into sequenced work, clarify definition of done,
  surface risks and dependencies, align tests and CI with scope, and coordinate
  cross-cutting changes (storage, messaging, APIs). Use when the user asks for a
  tech lead, implementation plan, story breakdown, risk assessment before a large
  change, merge ordering across epics, or Headless Test Platform (HTP) sequencing
  (Maps pin / PID / autonomy CI).
---

# Tech lead — Project

Use this skill for **orchestrating** work across files or epics—not for line-by-line review (use **code-reviewer**) or product prioritization alone (use **pm-governance**).

---

## Responsibilities

- **Sequencing:** Order tasks so foundations land first (types, contracts, storage shape) before UI polish; avoid changes that alter contracts without updating all callers.
- **Definition of done:** Behavior matches the plan story + tests (Tier 1 / Tier 2 per **TEST_TDD.md**); **merge-ready command** green when the change set warrants it; docs (**PM_PLAN**, product plan checkboxes) updated if scope or user-visible contract changed.
- **Risks:** Call out **data migration**, **permission or security** increases, **integration** with third-party systems, and **performance** (new hitch class vs last smoke; GC on hot paths). Link mitigations to [TECH_DEBT.md](../../TECH_DEBT.md). Immediate fix vs accept is a tech-lead call — do not invent a performance skill.
- **Consistency:** Same patterns as existing modules; avoid parallel frameworks or duplicate primitives.
- **HTP (panacea CI):** When the story is Maps pin / CLEARED, PID, or autonomy, read [docs/HTP.md](../../docs/HTP.md) **before** slicing. Topology → Physics → State Machine ship **inside** **8.7** / **9.1** / **13.x**. Do **not** invent Epic 14, a second repo, or a second graph/CLEARED. Do **not** start the next expansion while this story’s PM_PLAN simulator gate is red. Stop and ask for pin-sized cab smoke when Core is green ([deploy-before-smoke.mdc](../../.cursor/rules/deploy-before-smoke.mdc)).

## Workflow

1. Read the relevant **epic / story** in the product plan and **PM_PLAN** pointer. Panacea / HTP: also [docs/HTP.md](../../docs/HTP.md).
2. List **touchpoints** (files, modules, APIs, tests).
3. Decide **vertical slices** shippable without breaking `main`.
4. Assign **test tier** per **tester** / **TEST_TDD** skill.

## Handoffs

- **HTP architecture:** [docs/HTP.md](../../docs/HTP.md) — not a new skill. Tester writes the walks; eval-engineer writes ACs.
- **Layering / spaghetti:** code-quality-gate.
- **Copy and internal docs:** techwriter skill.
- **Correctness / security:** code-reviewer skill.
