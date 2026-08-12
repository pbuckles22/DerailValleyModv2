# Versioning and release strategy

Version numbers map **1-to-1** to [PM_PLAN.md](../PM_PLAN.md). Do not guess SemVer (minor vs patch). The version **is** the plan coordinate.

Player-visible version lives in root [`info.json`](../info.json) (`Version`, no `v` prefix). UMM Mod Manager shows that string.

Canonical agent rule: [`.cursor/rules/pm-versioning.mdc`](../.cursor/rules/pm-versioning.mdc). Merge-ready / rollback: [RELEASE.md](../RELEASE.md).

---

## Format

`MAJOR.EPIC.STORY` with optional sub-patch: `MAJOR.EPIC.STORY.FIX`

| Segment | Meaning | When it changes |
|---------|---------|-----------------|
| **MAJOR** | Clean-room architecture | Locked at **2**. Go to 3 only for another from-scratch rewrite. |
| **EPIC** | Epic number in PM_PLAN | Working in Epic 1 → `2.1.x`. Epic 3 → `2.3.x`. |
| **STORY** | Story number within that epic | Completing story **1.4** → `2.1.4`. Completing **3.2** → `2.3.2`. |
| **FIX** | Bugfix after that story, before the next | First fix after 3.2 → `2.3.2.1`, then `2.3.2.2`. |

Examples (this repo’s IDs, not v1):

- Story **1.1** shipped → `2.1.1`
- Story **1.2** ships → `2.1.2`
- Bugfix on 1.2 before 1.3 starts → `2.1.2.1`

Display as `v2.1.2` in prose; store `2.1.2` in `info.json`.

---

## When to bump

| Event | `info.json` | PM_PLAN |
|-------|-------------|---------|
| Numbered story ships (Tier 1 + applicable Tier 2) | Set to `2.{epic}.{story}` | Mark `[x]` in the **same** change |
| Bugfix after a story, next story not started | Append `.1`, `.2`, … | Do **not** check off the next story |
| Docs/rules with **no** story id | **No** bump | No fake story checkbox |
| Epic 0 historical (0.1–0.3) | Never retroactively versioned | Already `[x]` |

**Private / testing builds:** every completed **story** (and each sub-patch). That is the UMM version you deploy for smoke.

**Public / NexusMods (or GitHub Release):** every completed **epic**. Bundle the stories, write a player-facing changelog, publish only when the user asks. Epic close does **not** auto-upload.

---

## How agents calculate

1. Read **PM_PLAN.md**. Find the story this ship is completing (`N.M` in the heading, e.g. `1.2`).
2. Version = `2.N.M` (or `2.N.M.k` for a sub-patch).
3. If the user said “done” / “it works” and the story id is **ambiguous** — **ask**: “Should I set `info.json` to `2.N.M` and mark **N.M** `[x]`?”
4. If this session **is** the story ship (implemented, merge-ready green) — set `info.json`, check the box, refresh `docs/PROJECT_STATUS.md` + AGENT_HANDOFF *Current state* in that same ship. State the version in the summary. Do not bump a second time for the same story.

Do not invent story numbers. Do not use v1 IDs (e.g. “1.12 Personal Heading”) unless they exist in **this** PM_PLAN.

---

## +BUILD (local compile counter)

Format in the **DLL** (not `info.json`): `2.1.1+104` via `AssemblyInformationalVersion`.

- File: `build_number.txt` at repo root — **gitignored**. Seed: `build_number.txt.example` (`0`).
- Increments when `YardMasterSuite` actually compiles (Release or Debug). Skips `DesignTimeBuild` (IntelliSense).
- `dotnet test` does not build the UMM project, so it does not increment.
- Per-machine: a fresh clone starts at 1. Not a public identity. UMM still shows `info.json` (`2.{Epic}.{Story}`).
- Do **not** write `+BUILD` into tracked `info.json`.

---

## Rollback

Prefer `git revert` of the ship commit ([RELEASE.md](../RELEASE.md)). `info.json` reverts with it. Re-run merge-ready (`dotnet test` + Release build). Do not force-push `main` unless the user explicitly asks.
