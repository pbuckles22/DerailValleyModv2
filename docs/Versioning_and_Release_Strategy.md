# Versioning and release strategy

Version numbers map **1-to-1** to [PM_PLAN.md](../PM_PLAN.md). Do not guess SemVer (minor vs patch). The version **is** the plan coordinate.

Player-visible version lives in root [`info.json`](../info.json) (`Version`, no `v` prefix). UMM Mod Manager shows that string.

Canonical agent rule: [`.cursor/rules/pm-versioning.mdc`](../.cursor/rules/pm-versioning.mdc). Merge-ready / rollback: [RELEASE.md](../RELEASE.md).

---

## Format

`MAJOR.EPIC.COUNTER` with optional sub-patch: `MAJOR.EPIC.COUNTER.FIX`

**The number must never go backwards.** UMM Mod Manager shows this string, and it is how the player confirms the new DLL loaded. A lower number after a deploy reads as "the build did not load."

| Segment | Meaning | When it changes |
|---------|---------|-----------------|
| **MAJOR** | Clean-room architecture | Locked at **2**. Go to 3 only for another from-scratch rewrite. |
| **EPIC** | Epic number in PM_PLAN | Working in Epic 1 → `2.1.x`. Epic 3 → `2.3.x`. After HUD (**6**), leftover work is **7+** (speed **9**, multi-job Maps **10**, catalog **11**, roadside **12**, yard/Prep autonomy **13**, Maps desk **14**, haul/delivery **15**) so UMM never goes backwards from `2.6.21`. |
| **COUNTER** | Strictly increasing ship counter **within that epic** — *not* the story number | Start the epic at `2.N.1`. Every later ship raises it, whatever story it closes. Story progress is tracked in PM_PLAN only. |
| **FIX** | Bugfix after that ship, before the next | First fix after `2.3.2` → `2.3.2.1`, then `2.3.2.2`. |

Early epics ran one ship per story, so the counter and the story id matched (`1.4` → `2.1.4`). That is a coincidence of pace, not the rule. When an epic needs many ships to close one story, the counter keeps climbing and the story id stays in PM_PLAN.

Worked example (Epic 16, the case that set this rule):

- Story **16.1** shipped → `2.16.1`
- Story **16.2** opened → `2.16.2`, then `2.16.16`, `2.16.23` … `2.16.33` across repeated cab ships
- Bugfixes on that last ship → `2.16.34.7`, `2.16.34.8`, `2.16.34.10` (**16.2** closed here)
- Story **16.3** ships → **`2.16.35`**. Not `2.16.3`, which would roll UMM backwards.

Display as `v2.1.2` in prose; store `2.1.2` in `info.json`.

---

## When to bump

| Event | `info.json` | PM_PLAN |
|-------|-------------|---------|
| Numbered story ships (Tier 1 + applicable Tier 2) | Raise the epic counter above the shipped number | Mark `[x]` in the **same** change |
| Mid-story ship on an open story | Raise the epic counter | Story stays `[ ]` / `[~]` |
| Bugfix after a ship, next ship not started | Append `.1`, `.2`, … | Do **not** check off the next story |
| Docs/rules with **no** story id | **No** bump | No fake story checkbox |
| Epic 0 historical (0.1–0.3) | Never retroactively versioned | Already `[x]` |

**Private / testing builds:** every completed **story** (and each sub-patch). That is the UMM version you deploy for smoke.

**GitHub Release:** every completed **epic**, on epic close, once that close is on `main`. Do not wait for the user to ask.

**Nexus Mods:** wait until the mod is **playable** (first player-facing feature). Then build the Nexus page with the user (summary, images, file). Do not auto-upload to Nexus.

---

## GitHub Release (epic close)

After the epic-close commit is **merged to `main`**:

1. `dotnet build YardMasterSuite.sln -c Release` (produces `dist/YardMasterSuite_v{Version}.zip`).
2. Tag and publish from that `main` commit:

```bash
gh release create "v{Version}" --repo pbuckles22/DerailValleyModv2 --target main --title "v{Version} — {Epic name}" --notes-file notes.md "dist/YardMasterSuite_v{Version}.zip"
```

`{Version}` is `info.json` (e.g. `2.1.5`). Notes are player-facing: what they can do, what they cannot. Foundation-only epics must say the mod is not playable yet.

Epic 0 was never versioned — no retroactive GitHub Release.

---

## Nexus Mods (deferred)

Do **not** create a Nexus page until the first playable / player-facing feature. Then help the user (do not auto-upload):

- Nexus account + Derail Valley mod page
- Summary and description (draft from `PM_PLAN` + the GitHub Release notes)
- Cover image and in-game screenshots
- File: the same `dist/YardMasterSuite_v*.zip` as the GitHub Release
- Requirements: Unity Mod Manager, current Derail Valley version

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
