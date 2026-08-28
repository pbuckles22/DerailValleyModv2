---
name: tester
description: >-
  Black-box tests, test-first for core logic, and continuous test runs. Use when
  adding or changing tests or app logic, harvesting Player.log into Core, or
  growing Headless Test Platform (HTP) corridor/tick walks. Run your project's
  test command after changes; keep suite green.
---

# Tester — Project

Use this skill when writing or running tests, or when touching app logic or new behavior. **First action when adding new behavior:** Read this skill and [TEST_TDD.md](../TEST_TDD.md), then write a **failing** test at the appropriate tier(s) **before** production code.

---

## Role

- **Black-box tests:** Assert on **behavior** (public API: inputs and outputs). Do not depend on implementation details.
- **Test-first (tiers):** When [TEST_PLAN.md](../../../TEST_PLAN.md) defines **Tier 1** and **Tier 2**, use red → green at each tier that covers the change (fast feedback first when both apply; browser-only work may start at Tier 2). See TEST_TDD.md.
- **TDD loop:** (1) Tier 1 red/green if logic is covered by Tier 1. (2) Tier 2 red/green if integration or E2E is required. (3) Document if needed. (4) Run your **merge-ready** command from AGENT_HANDOFF.md before merge when your project defines one.
- **Continuous:** Run your project test command after each small step. Keep the suite green.
- **Evidence loop:** New behavior emits discrete named events (see TEST_TDD → *Evidence loop*). After smoke or a pasted log, harvest into a Tier 1 test. Do not treat logs as green without a pass/fail check. **`T2 hitch-spike` / `T2 hitch-summary` are primary evidence**, not optional extras. Hot-path Core helpers: add alloc-free tests when the surface changes (TEST_TDD → *Performance regression*).
- **HTP:** Maps pin, PID, and autonomy **Tier 1** are corridor/tick walks in Core — [docs/HTP.md](../../docs/HTP.md). Name tests after the smoke scenario. Fold harvest `graph.txt` / `corridor.txt` into fixtures; do not put Unity in `dotnet test`. Cab smoke is chrome/hitch only after the walk is green.

## Source of truth

- **What to test:** TEST_TDD.md.
- **HTP (panacea CI):** [docs/HTP.md](../../docs/HTP.md).
- **Test plan (two tiers):** TEST_PLAN.md.
- **Always-on rule:** [.cursor/rules/testing.mdc](../../rules/testing.mdc)
