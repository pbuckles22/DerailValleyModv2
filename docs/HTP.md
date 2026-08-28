# Headless Test Platform (HTP)

**Canonical vision + architecture** for panacea CI. Hard rule: [.cursor/rules/htp.mdc](../.cursor/rules/htp.mdc). Product order and story checkboxes stay in [PM_PLAN.md](../PM_PLAN.md).

## North star (HTP)

The **product** north star is one job, mostly hands-off — take → prep → validate → autonomous run → auto drop → turn-in / pay.

The **HTP** north star is how we know that loop is **true**:

1. Each panacea checkpoint is **replayable in `dotnet test`** against `YardMasterSuite.Core` **before** we treat cab chrome as the lock.
2. We grow that replay **inline on the product story that needs the next layer** — not a platform epic, not a second repo, not a leap across closed work.
3. The player **gathers once** (yard dump). After that, agents do not ask them to re-prove the lock until CI says it holds. When Core is green, **stop and ask** for that lock only.

Today that means **8.7 pin / CLEARED** (Topology, static poses). **9.1** is the tick loop. **13.x** is GO/Human/Done on that loop. Closed Epic **7** (`2.7.2` thermal) is not HTP work; do not confuse it with Maps golden **`2.8.7.2`**.

## Tandem (Cursor + Gemini)

One team, one lock. **Gemini** reviews / contradiction-checks / names CP0 gaps. **Cursor** lands Core, fixtures, deploy. **Human** gathers one dump and does pin-sized cab smoke when asked.

Locked **2026-08-28:** Gemini in sync — HTP is an **inline CI gate**, not a parallel epic. Next product slice is still **8.7**: fold the first live SW→TT dump, then pin-only smoke. Do not start **9.1**.

If Gemini and Cursor disagree, **stop and ask the human**. Do not silently fork the plan.

---

## What HTP is

A **Core replay loop** in this repo:

harvested (or frozen) yard graph + pose or tick inputs → `YardMasterSuite.Core` decisions → named xUnit tests.

Unity **gathers** (one dump per yard/scenario) and **paints** (HUD / AR / Three-Gate writes). CI does **not** run Derail Valley.

| HTP is | HTP is not |
|--------|------------|
| The Maps / PID / autonomy **Tier 1** shape | A new PM epic (`2.14.x`) |
| Topology → Physics → State Machine **inside** **8.7** / **9.1** / **13.x** | A leap from closed **7.x** (`2.7.2` thermal) to **8.7** |
| Stay in `YardMasterSuite.Core` + `.Tests` | A split simulator repository |
| Pin-only until CP0 is green | “Test everything” in one cab session |

**Version mix-up:** Epic **7** `2.7.2` is closed (thermal). Open lock is Maps **8.7** / golden **`2.8.7.2`** (pin placed; CLEARED axis inverted). HTP starts there.

---

## Who owns what (no new skill)

| Role | Skill | HTP job |
|------|-------|---------|
| Sequence slices, seams, “don’t start 9.1 yet” | **tech-lead** | Read this file + PM_PLAN gates; vertical slice = product lock **and** the expansion that story needs |
| Red/green Core walks, black-box | **tester** + [TEST_TDD.md](../.cursor/skills/TEST_TDD.md) | Named tests; harvest dump → fixture; no Unity in CI |
| Objective ACs for a CP | **eval-engineer** | 2–5 verifiable items; golden = the named Core test |
| Scope / no Epic 14 | **pm-governance** | HTP is a gate column, not a backlog epic |
| Stay on this pin | **green-and-clean** | Out of scope: PID ticks, GO runner, Catalog |
| Cab find → Core | **incident-triager** then tester harvest | `T2` feeds the walk; log is not the suite |

**Do not** add an `htp-architect` skill. Architecture lives here; TL **orchestrates**; tester **implements tests**.

---

## Roadmap (follow panacea, not a platform epic)

| Expansion | Product story | CI owns | Cab still owns |
|-----------|---------------|---------|----------------|
| **1. Topology** (now) | **8.7** / CP0 | Harvest ingest; pin golden; pose walk At-switch → CLEARED; Align gate | Pin chrome, hitch, Three-Gate throw |
| **2. Physics** | **9.1** / CP1 | Tick loop: throttle/brake → speed; hold target; never dump air; Posted cap | Lever feel, rigidbody |
| **3. State machine** | **13.1+** / CP2–CP10 | GO / Human / Done; couple→step++; FILO; creep; validate; stall drop; payout **event** | Desk / job-office UI |

**Do not** start the next expansion’s product code while this story’s **Simulator gate** in PM_PLAN is red.

CP table (done = named Core test **and** pin-sized cab smoke): [PM_PLAN.md](../PM_PLAN.md) → *Autonomy happy path*.

---

## Seed today (Topology v0)

| Piece | Role |
|-------|------|
| `RouteCorridorDrive` | Plan → pin → Switch List bind → CLEARED **walk** on `RouteCorridorPose[]` |
| `RouteHarvestCodec` / `RouteHarvestDump` | Once per graph-ready + once per Set dest → `graph.txt` / `corridor.txt` |
| `SwTurntableCorridorTests` | Named SW→TT scenarios; **hand-built** edges until a live dump is folded in |

**Static poses are enough for CP0.** A physics tick loop is **9.1**, not a prerequisite for the pin.

Known dump gaps (fix in **8.7** codec, not new stories): junction ids must match `PathPlan`; consist length is trainset sum (`ConsistLengthMeters`); no rail polylines in harvest v1 (xz + along-track meters).

---

## Getting started (this ship only)

1. **Core polarity** — already encoded: windshield pin stays At-switch; reverse CLEARED only after leading edge + length. That is HTP v0; no dump required to *start*.
2. **One-off seed dump** — player loads SW, waits `T2 harvest: graph`, Maps **Set dest Turntable**, quits. Files: `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\YardMasterSuite\harvest\graph.txt` and `corridor.txt`. Fold into `YardMasterSuite.Tests/Fixtures/Htp/` (create when the first dump lands). Re-run the walk on **live** edges.
3. **Stop.** Deploy. Ask for **pin-only** cab smoke (past pin → CLEARED → Align). Do not ask for PID, GO, or a full job.

Until (2) is committed, CI replays a **sketch** of SW for routing goldens; polarity tests can still be true.

---

## Dump protocol

- **When:** graph frozen (once) and on **Set dest** (once). Never per-frame / per-tick.
- **Who:** player gathers; agent folds; CI replays.
- **After fold:** do not ask the player to re-walk CLEARED until the named Core walk is green.
- **New yard / dest:** another one-off dump; do not grow a live-always harvest.

---

## Core seams (grow, don’t fork)

| Concern | Today | Next |
|---------|-------|------|
| Graph | `PathEdge` + selected branches | Same; fixtures from codec |
| Pin | `SwitchListRouteLeg.PickPinJunctionId` | Unchanged contract |
| CLEARED | `RouteClearanceEval` + `RouteClearanceTravel` | Same; ticks feed the sample |
| Align | `RouteClearanceGate` | Same |
| Speed (9.1) | — | 1-D integrator + PID; reuse `LimitThrottleCap` never-dump |
| Steps (13.1) | `SwitchListPlanner` | Index + GO/Human/Done on that list |
| Couple (13.2) | `AutoCoupleAssist` | Event in → step++ |

Do **not** invent a second graph, a second CLEARED, or a Unity physics clone in Tests.

---

## Definition of done (every HTP slice)

1. Named Core test for the **smoke scenario** (not only a helper).
2. Cab smoke only for chrome / hitch that Core cannot see.
3. When Core is green on this slice: **stop and ask** (deploy-before-smoke). Do not stack the next expansion in the same breath.

---

## Anti-patterns

- New epic or second repo “for cleanliness.”
- Cab-debug the pin while the Core walk is red.
- Tick loop before CP0 Topology is green.
- Per-tick `T2` spam (evidence loop still change-only).
- Treating a pasted Player.log as the regression suite.
- Re-opening Epic **7** (`2.7.2`) as HTP work.

---

## Pointers

- Hard rule: [.cursor/rules/htp.mdc](../.cursor/rules/htp.mdc)
- Gates on stories: [PM_PLAN.md](../PM_PLAN.md) → *Headless foundation*
- TDD / harvest: [TEST_TDD.md](../.cursor/skills/TEST_TDD.md), [TEST_PLAN.md](../TEST_PLAN.md)
- Smoke → Core: [.cursor/rules/smoke-gates-tier1-ci.mdc](../.cursor/rules/smoke-gates-tier1-ci.mdc)
