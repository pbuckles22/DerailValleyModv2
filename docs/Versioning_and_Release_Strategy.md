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
| **EPIC** | Epic number in PM_PLAN | Working in Epic 1 → `2.1.x`. Epic 3 → `2.3.x`. After HUD (**6**), leftover work is **7+** (speed **9**, multi-job Maps **10**, catalog **11**, roadside **12**, yard/Prep autonomy **13**, Maps desk **14**, haul/delivery **15**) so UMM never goes backwards from `2.6.21`. |
| **STORY** | Story number within that epic | Completing story **1.4** → `2.1.4`. Completing **3.2** → `2.3.2`. |
| **FIX** | Bugfix after that story, before the next | First fix after 3.2 → `2.3.2.1`, then `2.3.2.2`. |

Examples (this repo’s IDs, not v1):

- Story **1.1** shipped → `2.1.1`
- Story **1.2** ships → `2.1.2`
- Story **1.3** (`package.ps1`) ships → `2.1.3`
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
