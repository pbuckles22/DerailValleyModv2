# Receiver brief — required handoff shape

**Every CMPH close** pastes this brief **in chat** and writes the **same body** to the note. A one-line “landed on main” is not a handoff.

Do **not** run code-reviewer / dead-code / tech-debt on mid-epic CMPH. **Gates** (PASS/WARN) only on **SWAT** / epic close.

## Filename (mandatory — last line of chat and note)

| Location | Pattern |
|----------|---------|
| Prefer | `docs/handoff/NNNN-HANDOFF-YYYY-MM-DD_HHmm.md` |
| Copy | `.cursor/handoff/NNNN-handoff-YYYY-MM-DD_HHmm.md` |

- `NNNN` = next unused monotonic serial (`0001`, `0002`, …). Never reuse. Never edit an old note in place.
- `YYYY-MM-DD_HHmm` = local 24h time.
- End the brief with: `**Filename:** \`docs/handoff/NNNN-HANDOFF-YYYY-MM-DD_HHmm.md\``

---

## Worked example (6.17 closed) — copy this density

Receiver brief (6.17 closed)

**Objective:** Next agent starts 6.18 only when asked. Do not re-prove this land.

**Git**

| | |
|--|--|
| **Story** | 6.17 `[x]` (patch 2.6.17.2) |
| **Version** | `2.6.17.2` |
| **On** | `origin/main @ 4190919` |
| **Do not** | re-merge 6.17, re-smoke PNGs or MU 9% tap, or `git log` to confirm |
| **Next** | 6.18 Rear/Front proximity when the user asks |

**Decisions:** v1 48px PNGs + dark plate; radar = loco art + amber, max 3 others ≤600 m, skip own consist; cab keys redirect to front only from wagons; zcoupler hose on push-couple is not YMS.

**In scope next:** 6.18 when asked. **Out:** stacking HUD stories, dual junction numbers, 10.1 PID until spec.

**Acceptance (already met):** Tier 1 590 green; smoke PASS on icons + MU one-tap; UMM `2.6.17.2`.

**Next steps:** 1) Wait for “go” / 6.18. 2) Then `feature/6.18-rear-front-proximity` from this `main`. 3) Dual junction numbers stay through-only.

**Performance**

| Window | This session | Prior | Verdict |
|--------|--------------|-------|---------|
| Spawn | `feature=29` `load=3` `max=98` | 6.16.14 `feature=16` `load=2` | same spawn class |
| Cab drive | `feature=0` `max=43–98` | H107 `feature=0` | **not worse** |
| On-foot look | H67/H72 class | H67/H72 | open, **not worse** |

**Filename:** `docs/handoff/0005-HANDOFF-2026-08-24_0900.md`

If there was no in-world session: keep the Performance heading and write `no hitch-summary this turn`.

---

## Blank (fill every heading)

Receiver brief (N.M open | closed | WIP)

**Objective:** (one sentence: what the next agent does / does not re-prove)

**Git**

| | |
|--|--|
| **Story** | N.M `[x]` / `[ ]` |
| **Version** | `2.N.M` |
| **On** | `origin/main @ <sha>` **or** `origin/feature/… @ <sha> (not merged)` |
| **Do not** | (wasted step: re-merge / re-smoke / `git log`) |
| **Next** | (one story id, or pause until asked) |

**Decisions:** (semicolon-separated; decision + why)

**In scope next:** … **Out:** …

**Acceptance (already met | not yet):** Tier 1 …; smoke …; UMM `…`

**Next steps:** 1) … 2) … 3) …

**Performance**

| Window | This session | Prior | Verdict |
|--------|--------------|-------|---------|
| Spawn | | | |
| Cab drive | | | |
| On-foot look | | | |

**Filename:** `docs/handoff/NNNN-HANDOFF-YYYY-MM-DD_HHmm.md`

## Gates (file only)

| Gate | Result |
|------|--------|
| Code review | PASS / WARN / FAIL + one line |
| Tech debt | none new / Do first: … |
| Tests | `dotnet test` N passed |
| Readiness | N/A or one line |
| Security | N/A or PASS/WARN/FAIL |
