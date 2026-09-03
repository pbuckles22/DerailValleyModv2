# Feature branch archaeology

When smoke says **"it used to work"** but nobody can find the commit, the fix is usually **traceability**, not more memory.

## Problem

- **`main`** moves; old behavior lived on a **feature branch**. Keep those branches so archaeology still works.
- **Player.log** from one session is not a regression suite.
- **UMM version** (`2.N.M`) maps to a **story**, not always to a single git sha.
- Agents and humans grep **`main`** and miss the branch that actually held the working slice.

## Practice (this repo)

| Step | Do |
|------|-----|
| **Branch** | One PM story → `feature/<story-id>-short-topic` ([one-story-one-ship](.cursor/rules/one-story-one-ship.mdc)). |
| **Ship commit** | Subject names story + `info.json` version (e.g. `Ship 9.1.3 Win 5.1: travel roster refresh (2.9.1.39)`). |
| **Truth docs** | Same commit: `PM_PLAN` `[x]`, `TEST_PLAN` smoke line, `PROJECT_STATUS`, `AGENT_HANDOFF` *Current state*, `PERFORMANCE_LOG` H-row when Tier 2 ran. |
| **Push** | Always `git push -u origin <branch>` before smoke wait or handoff. |
| **Merge** | **`main`** only after smoke PASS + **CMPH** ([no-auto-merge-main](.cursor/rules/no-auto-merge-main.mdc)). |
| **Branches after merge** | **CMPH** (no **D**): **keep** local + remote. **CMPHD**: delete after `main` is pushed. Record land sha in `PROJECT_STATUS`. |
| **Find it later** | `git log main --oneline --grep='9.1.3'` · `git branch -a \| grep 9.1.3` · handoff `docs/handoff/*.md` · `PROJECT_STATUS` land sha · `TEST_PLAN` "Log / screens" date line. |

## "Merge but keep the branch"

**CMPH** (no **D**) is keep:

1. Merge feature branch → **`main`**.
2. **Keep** local + remote. Do not delete.
3. Record in **PROJECT_STATUS** | **Active branch** | "keep — land sha `abc1234`".

**CMPHD** (the **D** means delete): same land, then delete local + remote.

## What does not help

- Merging without committing truth docs (version bump alone is not enough).
- Deleting the branch before `PROJECT_STATUS` records the land sha.
- Cab-debugging a lock that has no Core Tier 1 name ([smoke-gates-tier1-ci](.cursor/rules/smoke-gates-tier1-ci.mdc)).

## Related

- [.cursor/skills/github-feature-workflow/SKILL.md](../../.cursor/skills/github-feature-workflow/SKILL.md)
- [AGENT_HANDOFF.md](../../AGENT_HANDOFF.md) → *Git workflow*
- [docs/Versioning_and_Release_Strategy.md](../Versioning_and_Release_Strategy.md)
