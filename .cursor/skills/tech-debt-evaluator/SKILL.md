---
name: tech-debt-evaluator
description: Assess and prioritize technical debt (code, architecture, tests, docs, performance). Use when planning refactors, sprint planning, or evaluating codebase health.
---

# Tech debt evaluator — Project

Use this skill when evaluating technical debt, planning refactors, or assessing codebase health. Produces a structured, prioritized list.

---

## Role

- **Classify debt** by category: **code** (duplication, complexity, coupling), **architecture** (layering, boundaries), **tests** (gaps, flakiness), **documentation** (out-of-date specs), **performance** (hot-path GC, frame timing).
- **Severity:** **Low**, **Medium**, **High**, **Critical** (e.g. data loss, crash). For performance:
  - **Critical / High:** new GC alloc on `Update` / `LateUpdate` / `OnGUI` (lists, strings, boxing); a **new hitch class** vs the last comparable smoke (cab drive `feature>0` when prior was `0`; spawn is graph/load, not this).
  - **Do not re-escalate** the known on-foot look 110–170 ms class already in [TECH_DEBT.md](../../TECH_DEBT.md) (H67/H72) unless a hitch-isolation story is open.
  - **40–99 ms** is counted in `T2 hitch-summary` `below=` (not silent). A *new* cab/drive `below` jump vs prior is **High**; the 100 ms line is `T2 hitch-spike` only.
- **Prioritize:** Impact and effort; surface high-impact, lower-effort items first.
- **Reference:** Use DEV_GUIDE.md, TEST_TDD.md, DESIGN_SYSTEM.md, AGENT_HANDOFF.md, [docs/PERFORMANCE_LOG.md](../../docs/PERFORMANCE_LOG.md) as the bar. Debt = deviation or gap vs. those.

## When to use

- User asks for a tech-debt pass, health check, or refactor plan.
- Before a large feature or architectural change.
- Sprint or handoff planning.

## Output format

For each item: **Category** | **Severity** | **What** (one line + file/area) | **Why it matters** | **Suggested fix** | **Effort** (optional). Optionally group by "Do first" vs "Backlog".
