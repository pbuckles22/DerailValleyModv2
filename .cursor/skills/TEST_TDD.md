# TEST_TDD — DerailValleyModv2

## How to test

- **Black-box:** Assert on behavior (public API: inputs and outputs). Do not depend on implementation details. See [tester/SKILL.md](tester/SKILL.md).
- **Continuous:** Run your project's test command after adding or changing logic or tests; keep the suite green.
- **Tiers ([TEST_PLAN.md](../../TEST_PLAN.md)):** When defined, **Tier 1** is fast feedback; **Tier 2** is integration or E2E. Validate at every tier that applies to the change.

---

## TDD when TEST_PLAN defines Tier 1 and Tier 2

**Default:** Do not merge production changes until the right tier(s) have **failing test → passing test** for the behavior you are adding or changing.

### Tier 1

Use for logic covered by your fast test command (unit, headless, mocked APIs — whatever TEST_PLAN.md says).

1. **Red** — Add or extend a test that describes the new behavior and fails with the current code.
2. **Green** — Implement until the Tier 1 command passes.

Tier 1 command: `dotnet test YardMasterSuite.sln`. Docs/rules-only changes still skip red/green.

### Tier 2

Use when behavior must hold in a real runtime (Derail Valley + UMM).

1. **Red** — Add or extend an integration or E2E checklist / `T2` log that fails until the feature exists.
2. **Green** — Implement until the Tier 2 command / smoke checklist passes. The ask to the human must follow [deploy-before-smoke.mdc](../../.cursor/rules/deploy-before-smoke.mdc) → *How to ask* (on-screen PASS first, `T2` lines second, hitch-summary vs prior in the verdict).

**When both apply:** Usually Tier 1 first, then Tier 2. Pure integration-only changes may start at Tier 2; add Tier 1 later if you extract testable logic.

### Exceptions

- Docs-only, config-only, or comment-only changes.
- Trivial one-line fixes with no behavior change (still run your merge-ready command if the project uses one).
- Pure refactors preserving behavior: keep tests green.

Never leave failing tests on the default branch.

---

## Evidence loop (Player.log → Core tests → CI)

1. **Emit** — Each new behavior ships discrete lines:
   - Lifecycle: `[YMS v2] …` (load / activate / deactivate).
   - Decisions/gates: `T2 <topic> …` with fields you would assert (counts, ids, enums — not formatted HUD strings).
   - No per-frame / per-physics-tick logs. If a value chatters, log on change or on a debug hotkey only.
2. **Verify** — Tier 2 checklist in TEST_PLAN.md is those exact lines after a real Mods deploy. Missing expected line = fail.
3. **Harvest** — After smoke (PASS or find): extract the decision into `YardMasterSuite.Core` (pure inputs → outputs). Add a Tier 1 test **named after the smoke scenario**. Keep the `T2` line as the Tier 2 item.
4. **CI** — `dotnet test` **is** the regression suite (local merge-ready and CI). Player.log / `T2` lines are the **feed**, not a second test runner. Harvesting a smoke gate into a named Core test is automatic regression; no extra “regression” tier. Do not leave “we’ll catch it next smoke” as the only plan.

Document intended `T2` names in TEST_PLAN when you spec a story; do not invent a logger until Monitor / packaging exists.

---

## Merge-ready

```bash
dotnet test YardMasterSuite.sln
dotnet build YardMasterSuite.sln -c Release
```

In-world UI / telemetry also needs Tier 2 smoke ([TEST_PLAN.md](../../TEST_PLAN.md)). Documented in **AGENT_HANDOFF.md**.
