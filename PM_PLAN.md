# PM_PLAN — Yard Master Suite v2

Official **backlog**. Cross off here when a story ships; refresh [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md) + [AGENT_HANDOFF.md](AGENT_HANDOFF.md) → *Current state* in the same change.

**Background:** [docs/YMS_v2_Onboarding_Guide.md](docs/YMS_v2_Onboarding_Guide.md)  
**Rebuild sequence:** [docs/YMS_v2_Architecture_Plan.md](docs/YMS_v2_Architecture_Plan.md)  
**Pub/Sub:** [docs/Unity_PubSub_Best_Practices.md](docs/Unity_PubSub_Best_Practices.md)

v1 (`DerailValleyMod`) is a reference library. Do not mark v1 epics done here.

**Leverage:** Before implementing a story, read [docs/LEVERAGE_REGISTER.md](docs/LEVERAGE_REGISTER.md). Do not invent a wheel that row already names.

**v1 coverage:** [docs/V1_FEATURE_COVERAGE.md](docs/V1_FEATURE_COVERAGE.md) — every v1 story mapped; Gemini 2026-08-21 “V2 PM Plan” **rejected** (Epic 6 collision + invented RCL/spawner/job-gen/click-map).

---

## How to read this

| Mark | Meaning |
|------|---------|
| `[x]` | Done (Tier 1 + applicable Tier 2) |
| `[~]` | In progress / partial |
| `[ ]` | Backlog |

**Version:** `info.json` is `2.{Epic}.{Story}` for the last **[x]** story (**9.1** → **2.9.1.12** on `main`). See [docs/Versioning_and_Release_Strategy.md](docs/Versioning_and_Release_Strategy.md).

---

## Headless foundation (CI bedrock)

**Detail:** [docs/HTP.md](docs/HTP.md). **Rule:** [.cursor/rules/htp.mdc](.cursor/rules/htp.mdc). Product checkboxes stay here.

The north star is **not** proven by cab smoke first. Each panacea checkpoint is **replayable in `dotnet test`** against `YardMasterSuite.Core` before we treat in-world chrome as the lock. Player.log `T2` lines **feed** harvest; they are not a second test runner. See [TEST_TDD.md](.cursor/skills/TEST_TDD.md) → *Evidence loop* and [smoke-gates-tier1-ci.mdc](.cursor/rules/smoke-gates-tier1-ci.mdc).

This is **not** a new product epic and **not** a parallel roadmap. Topology / Physics / State Machine expansions ship **inside** the story that needs them (**8.7**, **9.1**, **13.x**). Do not start the next expansion’s product code until that story’s simulator gate is green in CI.

### Repo strategy — stay in this repository

Keep the simulator in **this** repo (`YardMasterSuite.Core` + `YardMasterSuite.Tests`). **Do not** split it into a second repository.

- `dotnet test YardMasterSuite.sln` already gates merge-ready and CI on `main`. A split repo would break that evidence loop.
- The walk is Core-shaped: harvested graph + pose/tick inputs → `PathPlan` / `RouteClearanceEval` / `SwitchListPlanner` / (later) PID + step runner. Unity stays the gatherer and the chrome.
- Harvest files are **fixtures** once committed; the player gathers **once** per yard/scenario. After that, pin/PID/GO work happens headless until CI says the contract holds.

### Seed today (**8.7** — static poses)

| Piece | Role |
|-------|------|
| `RouteCorridorDrive` | Dijkstra → pin pick → Switch List bind → CLEARED **walk** on a list of `RouteCorridorPose` |
| `RouteHarvestCodec` / `RouteHarvestDump` | Once-per-graph + once-per-Set-dest text (`graph.txt` / `corridor.txt`) |
| `SwTurntableCorridorTests` | Named SW→TT smoke scenarios (hand-built topology until a live dump is folded in) |

**Static poses are enough for CP0.** A tick loop is **not** required to lock pin / CLEARED / Align. Do not re-walk the sawtooth in the cab until this walk is green on a harvested (or frozen-equivalent) fixture.

Known harvest gaps (fix in the **8.7** dump/codec, not as new stories): junction keys must match `PathPlan` (no silent instance-id drift); consist length is trainset sum (`ConsistLengthMeters`), not loco-only; rail polylines are **not** in harvest v1 — pose xz + along-track meters are the CP0 contract.

### Three expansions (scale, do not rewrite the backlog)

| Expansion | When | What CI owns | Still Tier 2 |
|-----------|------|--------------|--------------|
| **1. Topology** | **8.7** / CP0 | Harvest ingest → fixture; pin golden; At-switch → CLEARED polarity; Align/Next gate; Switch List Past-switch then Reverse-into | World pin chrome, hitch, Three-Gate throw in the yard |
| **2. Physics** | **9.1** / CP1 | Tick-based 1-D loop: throttle/brake commands → speed over `dt`; hold target; **never dump air**; cap = min(request, Posted Limit); **7.5** remains a separate reactive net | Cab feel, actual lever writes, Unity rigidbody |
| **3. State machine** | **13.1+** / CP2–CP10 | GO / Human / Done on Switch List; Align only after CLEARED; couple → step++; FILO; creep-to-couple; validate; stall drop; payout **event** | Desk chrome, job-office UI, payout screen |

**Physics loop (9.1):** replace the pose array with a clock. Each tick: PID (or governor) emits desired throttle/brake → integrator updates speed and along-track position → sample CLEARED / Posted Limit / Derail Risk as **Core inputs**. Start 1-D along the Dijkstra path; do not wait on harvested rail polylines to begin PID tests.

**State machine (13.x):** the same corridor + ticks, plus a step index and GO/Human/Done. **13.1** is the runner; **13.4 thin** is one Transit leg end-to-end; yard robotics (**13.2**) layers couple/proximity on that runner.

### Done means (every CP)

1. **Named Core test** for that checkpoint’s scenario (not only an anonymous helper).
2. **Cab smoke** confirms world chrome / hitch — it does **not** substitute for (1).
3. Do not stack the next CP’s product code while (1) is red.

---

## North star and panacea path

**North star (2026-08-27):** One job, mostly hands-off — **take job → prep (stack cars) → validate → autonomous run → auto drop → turn-in / pay**. Maps multi-step (**Next** / **GO** / **Human**) + **PID/MPC** drive the train. You only pick the job and flip steps that need a person.

**HTP north star:** that loop is **true in `dotnet test`** before cab chrome. Grow the harness **on the current lock** (next **9.1** ticks) — inline, this repo, one-off dump, stop-and-ask. Detail: [docs/HTP.md](docs/HTP.md). Rule: [.cursor/rules/htp.mdc](.cursor/rules/htp.mdc).

| Phase | Goal | Epics | Simulator (must be green) |
|-------|------|-------|---------------------------|
| **A — Maps gate** | Sawtooth + CLEARED + Align trustworthy | **8.7** `[x]` (`2.8.7.31`) | **Topology** — harvested/frozen corridor walk; CLEARED polarity; Align gate |
| **B — Drive brain** | Hold speed / follow route legs | **9.1** PID (minimal spec); **9.2** MPC only if PID insufficient | **Physics** — tick loop holds target; never dumps air; Posted cap |
| **C — Single-job autonomous** | Prep stack, validate, auto transit, auto drop, step runner | **13.1–13.6** (new) | **State machine** on A+B — GO/Human/Done through one job |
| **D — Multi-job + profit** | FILO tour, N jobs, route/job optimizer | **10.x** (after **C** PASS; **14** if desk rewrite landed) | Reuse **C** runner on N jobs (no new physics engine) |
| **E — Maps desk** | Close chrome, amenity filter, live HUD, uGUI | **14.x** after **13**, before **10** | IMGUI hitch still Tier 2 |

**Critical path (do not stack out of order):** **8.7** `[x]` → **9.1** → **13.x** → **10.x**. Finish **8.7** (including Topology CI) before new **13** code. **9.1** unblocked after **8.7** PASS — spec = follow Maps/Switch List legs at safe speed (reuse **7.5** / Posted Limit as ceiling until look-ahead exists). **Epic 14** Maps desk sits **after 13, before 10** — not a 9.1 blocker.

**Defer (revisit only if autonomous loop blocks):** **8.8–8.9** (tester tools), **11** Catalog, **12** Roadside. Desk Close / amenity filter / live route HUD / uGUI → **Epic 14**. **8.10** couple auto-advance → **13.2** prep (not a standalone gate). Question **9.2** / full MPC until **9.1** + one end-to-end job PASS.

**Order (legacy):** Epic **6** HUD closed. **7** governors closed. **8** dispatcher in progress — **do not** “finish all 8.x” before **9** / **13**; only **8.7** is on the critical path. Pin / ModSettings stay Later except **6.15** when asked. See [docs/V1_FEATURE_COVERAGE.md](docs/V1_FEATURE_COVERAGE.md).

**Renumber (2026-08-25):** leftover stories moved past **6** so UMM versions never go backwards after **2.6.21.6**. Do not reuse Epic **5**.

| Was (pre-remap) | Now | First `info.json` |
|-----------------|-----|-------------------|
| 5.1–5.5 Governors | **7.1–7.5** | `2.7.1` … |
| 7.1–7.10 Dispatcher | **8.1–8.10** | `2.8.1` … |
| 4.4 PID / 4.5 MPC | **9.1 / 9.2** (was briefly **10**) | `2.9.1` … |
| Multi-job Maps (new 2026-08-27) | **10.1+** | `2.10.1` … |
| 8.1 Catalog | **11.1** (was briefly **9**) | `2.11.1` |
| Roadside Assist (new 2026-08-27) | **12.1+** | `2.12.1` … |
| Autonomous job loop (new 2026-08-27) | **13.1+** | `2.13.1` … |
| Maps desk upgrade (new 2026-08-29) | **14.1+** | `2.14.1` … |
| 4.1–4.3 Heavy-engine infra | stay **4.x** (already `2.4.x`) | — |

**Priority lock (2026-08-27):** **Panacea path** — **8.7** → **9.1** → **13** single-job autonomous → **10** multi-job / optimizer. **Epic 14** Maps desk (uGUI) after **13**, before **10**. **Catalog 11** and **Roadside 12** deferred until autonomous loop smokes. Multi-job **10** is **not** before **13** PASS.

**Autonomy happy path (checkpoints — CI walk **and** cab smoke each before stacking the next):**

| CP | Story | What “done” looks like | Simulator target (Core) | Smoke size |
|----|-------|------------------------|-------------------------|------------|
| CP0 | **8.7** | Sawtooth: past pin → CLEARED → Align threw | **Topology.** Harvested/frozen corridor: pin golden ≠ first flip; pose walk Idle/AtSwitch → CLEARED only after leading edge + length; `RouteClearanceGate.Align` Ok only on CLEARED | ~6 steps |
| CP1 | **9.1** | PID holds ~25 km/h on straight; never dumps air | **Physics.** Tick loop: throttle/brake → speed; hold target; cap = min(request, Posted); never dump; **7.5** may still idle independently | ~5 steps |
| CP2 | **13.1** | **GO** on one Transit leg; **Human** pauses until **Done** | **State machine.** GO ticks PID on Transit; Human holds (no Next); Done resumes | ~5 steps |
| CP3 | **13.4** *(thin)* | **GO** drives **one** Switch List transit leg (Align + CLEARED); prep still manual | CP0 walk + CP1 ticks on **one** Transit; Align after CLEARED; fail-closed no path / Derail Risk | ~6 steps |
| CP4 | **13.2.1** | Couple one car on Prep → list auto-advances (was **8.10**) | Couple-success event → step index++ (no physics required) | ~4 steps |
| CP5 | **13.2.3–13.2.4** | FILO queue + creep-to-couple **one** car on spur | Queue head + creep ticks: speed ≤8 km/h + Rear/Front green → `AutoCoupleAssist` Couple | ~5 steps |
| CP6 | **13.2.5–13.2.6** | **Two** cars stacked; Prep complete → **Validate** ready | Couple → short pull-forward → second couple; consist ⊆ job task cars → Prep complete | ~6 steps |
| CP7 | **13.3** | Validate arms Transit **GO** | Consist vs job → Transit GO armed (fail-closed mismatch) | ~4 steps |
| CP8 | **13.5** | Auto stop + drop at delivery stall | Length-aware stall occupancy → stop; uncouple/handbrake; advance to turn-in | ~6 steps |
| CP9 | **13.6** | Turn-in + payout T2 | Turn-in complete **event** (payload); payout UI stays Tier 2 | ~4 steps |
| CP10 | **E2E** | Full job: take → prep → validate → transit → drop → pay | Scripted fixture chain of CP0–CP9 (no Unity) | one scripted run |

**Within Epic 13:** ship **13.1** → **13.4 thin** before full **13.2** stack so driving autonomy is proven before yard robotics. **13.2** is **six sub-stories** (below), not one 40-step smoke. Each 13.x ship includes its **Simulator gate** (named Core test) before cab smoke.

**Estimates & re-baseline (2026-08-27):** Rough LOE lives in chat/planning only until a story starts; then log **Est / Started / Done / Actual** in [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md) → *Autonomy tracker* so we can rebaseline (“1 week → 3 days” vs “1 week → 3 weeks”). Update **Est** when scope splits (e.g. **13.2.x**).

---

## Backlog

- [x] **Epic 0 — Repo bootstrap** — Folder layout, public GitHub repo, AgenticTemplate + v1 rules delta. **Closed 2026-08-12.**

  - [x] **0.1 Docs layout** — Move YMS background into `docs/`; archive Predictive Braking templates.
  - [x] **0.2 Public repo** — `pbuckles22/DerailValleyModv2` on `main`.
  - [x] **0.3 Agentic overlay** — Upstream AgenticTemplate; v1 delta rules; stack-specific handoff/docs.

- [x] **Epic 1 — Phase 1 Heartbeat** — Core infrastructure before any train telemetry. **Closed 2026-08-12.**

  - [x] **1.1 Solution scaffold** — `YardMasterSuite.sln`, csproj, `info.json`, `Directory.Build.targets.example`.
    > As a maintainer, I want a net48 UMM project that builds so Phase 1 code has a home.
  - [x] **1.2 YmsEventBus** — Central Type A `Action` bus with `ClearAllSubscriptions()`.
    > As a subscriber, I receive primitive/struct payloads with zero alloc and can unsubscribe on disable.
  - [x] **1.3 package.ps1** — Deploy Release DLL + `info.json` into `Mods\YardMasterSuite\`; optional zip.
    > As a maintainer, I can install a local build so later stories can smoke in-world.
  - [x] **1.4 GcCadenceProbe** — Silent frametime monitor that logs GC/stutter warnings.
    > As a developer, I am warned when a feature introduces a hitch.
  - [x] **1.5 GuiContentCache / StringBuilder pool** — No concatenated strings in render loops.
    > As a player, the HUD does not hitch from string allocs.

- [x] **Epic 2 — Phase 2 Senses** — Event-driven telemetry (no polling). **Closed 2026-08-12.**

  - [x] **2.1 Loco state listener** — Board/unboard → cached current loco + bus event. (`info.json` **2.2.1**, Tier 2 PASS 2026-08-12)
  - [x] **2.2 Control telemetry** — Throttle, indy, train brake, engine/dynamic brake, reverser only when levers move. (`info.json` **2.2.2**, Tier 2 PASS 2026-08-12)
  - [x] **2.3 Trainset topology** — Consist length/weight on coupler events only (keeps listening on foot). (`info.json` **2.2.3**, Tier 2 PASS 2026-08-12)

- [x] **Epic 3 — Phase 3 Display Shell (infra)** — Zero-alloc HUD/AR shell before heavy math / v1 parity. **Closed 2026-08-17** at **3.3.1**. Full v1 diagnostic look/feel → **Epic 6**.

  - [x] **3.1 HUD manager** — Top bar + always-on compass. (`info.json` **2.3.1**, Tier 2 PASS 2026-08-13)
  - [x] **3.2 AR overlay engine** — Fixed 3-slot buffer; office STN + own-loco LOCO; mid-edge fan; hitch-summary. (`info.json` **2.3.2**, Tier 2 PASS 2026-08-17)
  - [x] **3.3 Centered HUD stack** — v1 `MonitorHudStackLayout`; centered bars. (`info.json` **2.3.3**)
  - [x] **3.3.1 HUD v1 chrome parity (stop-state patch)** — v1 bar chrome; product labels; **4.3** `UsableTrainGate`; four-bar stack slots; AR sticky Y publish. (`info.json` **2.3.5.1**, Tier 2 PASS 2026-08-17)
  - [x] **3.4 Speed telemetry + chip** — Event path + product labels in **6.8**. (`info.json` **2.6.8**, Tier 2 PASS 2026-08-20)
  - [x] **3.5 Limit display** — Posted authority in **6.9**; Next in **6.10**. Geometry Limit **retired**.

- [x] **Epic 4 — Phase 4 Heavy Engines (infra)** — Time-sliced brains (Job/coroutine). **Closed 2026-08-25** at **4.3**. PID/MPC → **Epic 9**.

  - [x] **4.1 Type B mailbox** — `ConcurrentQueue<T>` drain to Type A on the main thread. (`info.json` **2.4.1**, Tier 2 PASS 2026-08-17)
    > As a heavy engine, I can push a struct off the worker and the HUD receives it without touching Unity APIs from that thread.
  - [x] **4.2 Track graph builder** — Yield across frames; publish via **4.1**. (`info.json` **2.4.2**, Tier 2 PASS 2026-08-17)
  - [x] **4.3 Geometry scanner (A116)** — Shipped 2026-08-17; **retired for Limit** in **6.9** (posted boards only). Scanner + Core curve ladder removed.

- [x] **Epic 6 — Diagnostic HUD (v1 parity)** — Player-visible match to v1 **1.17 + Epic 4 HUD QOL** (minus explicit v2 cuts). Matrix: [docs/HUD_v1_Parity_Matrix.md](docs/HUD_v1_Parity_Matrix.md). **Closed 2026-08-24** at **6.21** (`info.json` **2.6.21.6**).

  - [x] **6.1 Always-on bar** — Heading + Clock (`DateTimeWrapper` world time). Marked / Path → **6.11**; Station → **6.12**. (`info.json` **2.6.1**, Tier 2 PASS 2026-08-18).
  - [x] **6.2 Look-at bar** — Pipe / Handbrake / Couplers / Car / Track / Cargo / Loco type; identity-only `T2 look-at bar`. Job chip → **6.13**. (`info.json` **2.6.2**, Tier 2 PASS 2026-08-17).
  - [x] **6.3 Usable target** — Spherecast + look-at wins + usable consist walk + consist publish on look-at (`info.json` **2.6.3**, Tier 2 PASS 2026-08-17).
  - [x] **6.4 AR stack sync** — Edge STN/LOCO sit **below** the HUD stack (`HudStackLayout.LastBottomGuiY`). OnObject stays on the world object. (`info.json` **2.6.4**, Tier 2 PASS 2026-08-17). Glide + pause-hide → Later.
  - [x] **6.5 Mass + Grade** — Cab Mass + Grade; change-only gadget gate (`info.json` **2.6.5**, Tier 2 PASS 2026-08-18).
  - [x] **6.6 Load + Motors + Fluids** — Cab Fuel / Oil / Load / Motors from sim (`info.json` **2.6.6**, Tier 2 PASS 2026-08-19).
  - [x] **6.7 MU sync** — Cab yellow `MU idle` / red `MU desync`; quiet when synced (`info.json` **2.6.7**, Tier 2 PASS 2026-08-19).
  - [x] **6.8 Full lever + Speed + Limit** — Live levers + Speed + Limit chip; omit dashes; no Next (`info.json` **2.6.8**, Tier 2 PASS 2026-08-20). Geometry Limit later retired in **6.9**.
  - [x] **6.9 Posted board index** — Posted sticky Limit; geometry scanner ripped. (`info.json` **2.6.9**, Tier 2 PASS 2026-08-20).
  - [x] **6.10 Next + distance** — Next chip on Limit; meters when close (`NextLimitReveal`). Dual numbers stay through-only. (`info.json` **2.6.10**, Tier 2 PASS 2026-08-20). **FILO funnel restore** (`2.8.1.16`, 2026-08-26): EventBus HUD; takes while rolling; Next after direction lock. Parallel Next metres + leftover cab hitch (`feature` 15–17) **out of 6.10**.
  - [x] **6.11 Marked** — Home / Shift+Home return chip; End / Shift+End Path check (sticky origin on look-away). (`info.json` **2.6.11**, Tier 2 PASS 2026-08-20).
  - [x] **6.12 Station chip** — In-zone `Station SM NE 84m` / `here` from office transform. Omit outside job-generation zone. Fluids `Next: Farm [km]` stays cut. AR pin/icons/radar → **6.15–6.17**; job-car ■ → **6.21**. (`info.json` **2.6.12**, Tier 2 PASS 2026-08-20).
  - [x] **6.13 Active job bar** — taken `Job · GO/HOLD/RED · Bonus`; look-at Job chip (`GetJobOfCar`). Preview / license / Cancelled → **6.20**; job-car AR → **6.21**. On-consist cab keys stacked by request. (`info.json` **2.6.13**, Tier 2 PASS 2026-08-21).
  - ~~**6.14 Track + Cargo**~~ — **Cut.** Folded into **6.2**. Look-at Job chip is **6.13**.
  - [x] **6.15 Pin AR slot** — Home mark world PIN (amber quad); hide within 8 m; Shift+Home clears. PNG stays **6.17**. (`info.json` **2.6.15**, Tier 2 PASS 2026-08-21).
  - [x] **6.16 Loco radar** — v1 **4.10** parity: nearest **other locos** (licence filter **parked**) as amber AR, **≤600 m**, up to 3, own consist excluded. Caption is **loco name / meters** (two lines). Cyan LOCO uses `LastLoco` **else usable consist loco**. Hide AR on loading / pause / save overlays. F8 is licence **debug** (not F11 — game stats overlay). Cab hitch: overlay `FindObjectOfType` capped at 2 lookups/world (`2.6.16.13`, cab `feature=0`). On-foot LastLoco excludes the whole trainset (`2.6.16.14`). (`info.json` **2.6.16.14**, Tier 2 PASS 2026-08-23).
    > As a yard master, I want to see where other locos are so I can walk to one and MU without searching the whole yard.
    >
    > **Deferred on top of v1 parity** (user, 2026-08-23):
    > 1. **Licence filter** — parked: `LocoRadarLicenseGate.FilterEnabled = false`. DE2-only save saw **0 of 9** nearby locos when filtering. Needs dim-unlicensed or UMM toggle before re-arm.
    > 2. **UMM “Show nearest locos”** — waits on ModSettings.
    > 3. **PNG icons** — shipped **6.17**.
  - [x] **6.17 PNG icons** (48px + dark plate) — v1 **4.9**: loco / house / pin under `Mods/.../Icons/`; radar reuses loco art with amber tint (v1 4.10). Tint secondary. `2.6.17.2` also stops on-consist lever redirect when standing on any loco (MU double-notch). (`info.json` **2.6.17.2**, Tier 2 PASS 2026-08-23).
  - [x] **6.18 Rear/Front proximity** — v1 **4.11–4.12**: Reverse → `Rear N.Nm`; Forward → `Front …`; Neutral omit. Green ≤0.5 m + couple-scan; yellow through 30 m; open tip `Front —` / `Rear —`. No “Couple ready”. (`info.json` **2.6.18**, Tier 2 PASS 2026-08-24).
    > As a driver reversing to pick up a train, I want distance before impact and a clear cue when I am close enough to brake and couple.
  - [x] **6.19 Derail Risk** — cab chip after Motors while boarded: consist-max `derailBuildUp` % of game threshold (worst car, wagons included). No coupler. Always on in cab (green &lt;15 %; yellow 15–94 %; red ≥95 %). Omit on foot. Fail-closed `— Derail Risk`. **Not** Limit occupancy. (`info.json` **2.6.19.5**, Tier 2 PASS 2026-08-24).
    > As an engineer, I want Derail Risk on the cab bar for the worst car in my train so I can slow down before a tip-over anywhere in the consist.
  - [x] **6.20 Job preview / Cancelled / license warn** — v1 **4.8** remainder: inventory `Preview Nm` to Regular destroy (−30 m HUD buffer); Abandoned/Expired → red Cancelled ~8 s; `No license: TL2` (etc.). Wipe station is job-id origin (`SW-SU-72` → SW), not dest. Taken job still **6.13**. Job-car AR → **6.21**. (`info.json` **2.6.20.1**, Tier 2 PASS 2026-08-24).
  - [x] **6.21 Job-car AR** — v1 **4.8** @ **0.6.16**: purple ■ on taken-job **task cars**, **one pin per pickup spur**. Distinct from STN / LOCO / PIN / radar. Quad this story (PNG Later). Hide on taken GO. Pin hops at the next car center (accepted). Cab Incremental rising-edge (chatter hotfix). (`info.json` **2.6.21.6**, Tier 2 PASS 2026-08-24).
    > As a yard master with a job in hand, I want the cars I still need marked in the world so I am not reading numbers off the look-at bar.

- [x] **Epic 7 — Governors (v1 Epic 2)** — Soft writes via Three-Gate. **Closed 2026-08-26** at **7.5** (`2.7.5.7`).

  - [x] **7.1 Three-Gate helper** — v1 **2.1**: Integrity → State Registry → Safety → Soft Write; fail closed. On-consist reverser/TM fuse are the first writers. Loading-screen world gate + Ctrl/Numpad hotkey policy (no Rewired). (`info.json` **2.7.1.6**, Tier 2 PASS 2026-08-25).
    > As a maintainer, I want one write path so every governor aborts the same safe way.
  - [x] **7.2 Thermal governor** — v1 **2.2**: soft-roll throttle when Motors Hot (Warning 75% / Critical 55%) via Three-Gate. (`info.json` **2.7.2**, Tier 2 PASS 2026-08-25).
    > As an engineer, I want the mod to soft-cap throttle when motors overheat so I avoid TM Offline.
  - [x] **7.3 Auto-brake governor** — v1 **2.3**: engine on→off soft-rolls train + independent toward full and throttle toward idle; never auto-release on start. (`info.json` **2.7.3**, Tier 2 PASS 2026-08-26)
    > As an engineer, I want air applied when I shut down so an unpowered loco is not free to roll.
  - [x] **7.4 Auto-coupler** — fail-closed on-consist couple assist (not zCouplers physics, not RCL). Green ≤0.5 m + ≤8 km/h Three-Gate TryCouple; finish hose/cocks if already knuckled. (`info.json` **2.7.4.1**, Tier 2 PASS 2026-08-26)
    > As a shunter, I want a fail-closed couple assist without a full RCL remote (v1 never shipped RCL).
  - [x] **7.5 Limit auto-throttle** — v1 parking **2.4** evolved: Derail Risk ≥65 % Three-Gate idle + raise air (never dump). Posted / Next stay HUD-only. Speed-hold / look-ahead → **9.1**. (`info.json` **2.7.5.7**, Tier 2 PASS 2026-08-26)
    > As an engineer, I want a consist-safety net when I am not watching Derail Risk, without a posted-speed cop.

- [ ] **Epic 8 — Google Maps / Dispatcher** — **8.7 `[x]`.** Rest deferred to Later. Type B Dijkstra + Three-Gate throws. **Not** a 2D click-map. **Simulator:** Topology expansion lives in **8.7** (see Headless foundation).

  - [x] **8.1 Google Maps desk** — v1 Dispatch Desk Route tab: city / track / **Set dest** / Recheck. **Ctrl+Insert**. Click publishes Type A (`YmsEventBus.OnMapsDestCommand`). **No pathfind / Align / switch throws on the click.** Maps dest does **not** arm 6.11 Path check (`2.8.1.1`). Align + Path/ETA/Facing HUD + Three-Gate throws = **8.2**. (`info.json` **2.8.1.1**, Tier 2 PASS 2026-08-26).
    > As a licensed dispatcher, I want Google Maps–style Set dest (city → track) without hitching the cab.
  - [x] **8.2 Google Maps route + Align** — v1 **3.5** “Google Maps Align Route”: Type B pathfind (`RoutePlanReady` mailbox); desk Path / ETA / Facing (bucket T2); **Align Route** throws via ThreeGate. Dispatcher-gated. Through-lane bias. Live always-on route HUD + `#Y` turntable→cross-city → **8.4–8.5** / TECH_DEBT. (`info.json` **2.8.2**, Tier 2 PASS 2026-08-26).
    > As a licensed dispatcher, I want the path drawn like Maps, then Align so I am not hiking every lever.
  - [x] **8.3 Digital Switch List** — v1 **3.6**: taken job → Prep / Transit / Delivery; each step uses **8.2** Align + Next. Manual Next only (couple auto-advance / arrival-track split → **8.10**). Per job footer shows job id (not Route catalog counts). (`info.json` **2.8.3.1**, Tier 2 PASS 2026-08-27).
    > As a dispatcher, I want the job Switch List so I do not re-pick city/track three times.
  - [x] **8.4 Town turntable dest** — v1 Town TT: Set dest **Turntable** in sticky yard (same Maps engine as **8.1**). Single dest + one Align; multi-leg Align/Next → **8.5**. FoT cached (v1 0.6.49 stutter). (`info.json` **2.8.4**, Tier 2 PASS 2026-08-27).
    > As an engineer in town, I want Set dest to the yard turntable.
  - [x] **8.5 Multi-step Maps** — v1 **3.7**: TurnAround inject, reverse-into leg, current-leg AR on the Switch List (Route tab stays single dest). Clear wipes dest + list (`2.8.5.1`). (`info.json` **2.8.5.1**, Tier 2 PASS 2026-08-27).
    > As an engineer facing the wrong way, I want Switch List to send me to the turntable then reverse into the spur.
  - [x] **8.6 Loco turn + re-rail place** — Loco-only. **Turn** = look-at solo loco → `MoveToTrack` same footprint 180° (not TeleportTrainset spin; not on-rails `Rerail` no-op). **Bring** = type dropdown → on-rails source → Lock aim → `TeleportTrainset`. Coupled refuse; derailed sources refuse. Bring Flip removed. (`info.json` **2.8.6.4**, Tier 2 PASS 2026-08-27).
    > As an engineer or tester, I want to reverse a loco where it sits (nose where the toes were) so I do not need a turntable and do not slide into neighbors.
    > As an engineer or tester, I want a dropdown of loco types, then place one from anywhere on the map onto the rail I am looking at (e.g. DH4 into SW when the yard has none).
  - [x] **8.7 Route pin + CLEARED + switch-back coach** — **ON CRITICAL PATH (closed).** Length-aware frog; pin latch (Dijkstra first-stop); reverse travel at Set dest; Align/Next Ok only on CLEARED; pin hides on **Next**. Closed-desk **Ctrl+PageUp** / **Ctrl+PageDown**. HTP CP0 live SW dump + sketch walks green. Ritual: Set dest stopped → **close desk** → roll; chords at CLEARED (do not reopen desk). (`info.json` **2.8.7.31**, Tier 2 PASS 2026-08-29).
    > As a driver on a sawtooth, I want CLEARED only after I am past the throw switch, then Align throws, then Facing tells me which way to go.
    >
    > **Simulator gate (Topology — CP0):** Named Core walk on a harvested or frozen corridor (SW→TT first): pin = conflict switch not first flip; pose sequence stays At-switch while the pin is in the windshield; CLEARED only after leading edge + consist length past the frog; Align/Next Ok only on CLEARED. Fold live `graph.txt` / `corridor.txt` into fixtures when a dump exists — do not keep cab-debugging the pin while this walk is red. Cab smoke confirms world chrome only.
  - [ ] **8.8 License spawn** — **DEFER** → Later.
  - [ ] **8.9 Place ghost / Snap polish** — **DEFER** → Later.
  - [ ] **8.10 Switch List couple auto-advance** — **DEFER** standalone; absorb into **13.2**.
  - [ ] **8.11 Desk Close chrome** — **DEFER** → **14.1**.
  - [ ] **8.12 Track amenity filter** — **DEFER** → **14.2**.

- [ ] **Epic 9 — Speed / brake brains** — **Critical path: 9.1** after **8.7** PASS. Ships as **2.9.x**. **Simulator:** Physics expansion (tick loop) lives in **9.1**; do not invent a second physics engine for **13**.

  - [x] **9.1 PID speed governor** — **Hold PASS (`2.9.1.12`, 2026-08-30).** Three-Gate throttle to target km/h on active Maps/Switch List leg. **Target cap = min(request, Posted Limit)**. **No derail term in PID v1** — **7.5** separate; **7.2** thermal ceiling. Never dump air. DE2 HUD notches + `MUOverride` write path. **Open follow-ups (patches, not 9.2):** gradual takeoff (log: thr 9→100 by ~10 km/h / wheel slip); softer thr↔indy at hold; `motors=Dead` after CLEARED (likely slip/fuse). If **13.4** smokes lots of 7.5 trips, add **9.1.1** derail-aware target trim (optional, not MPC).
    > As an engineer, I want the loco to hold a safe speed on a Maps leg so I am not babysitting throttle between switches.
    >
    > **Simulator gate (Physics — CP1):** Tick-based 1-D loop in Core — **green**. Cab: bleed → hold ~25 (`apply thr=0 indy=27`) → CLEARED. Do not start **13.1** until takeoff ramp is playable enough (or user waives).
  - [ ] **9.2 Predictive braking (MPC)** — **DEFER** until **9.1** + **13.4** smoke.

- [ ] **Epic 13 — Autonomous job loop (single job)** — **Phase C.** After **8.7** + **9.1**. **GO** / **Human** / **Done** on Switch List. Ships as **2.13.x**. **Simulator:** State machine on top of Topology + Physics; each story below has a named Core gate before cab smoke.

  - [ ] **13.1 Step runner (GO / Human / Done)** — **GO** = PID + Maps; **Human** = pause until **Done**; **Next** only on manual legs.
    > As a dispatcher, I want GO on transit and to mark human-only steps done myself.
    >
    > **Simulator gate (CP2):** Same corridor + PID ticks. GO runs the Physics loop on a Transit step; Human holds (no auto Next); Done resumes. Fail-closed if no path / not CLEARED for Align.
  - [ ] **13.2 Yard prep — stack job cars** — **Split into sub-stories** (each = own ship + smoke). Parent absorbs deferred **8.10**. Full stack = **13.2.1** … **13.2.6** PASS.

    - [ ] **13.2.1 Couple auto-advance** — On **7.4** success during **Prep** step, auto **Next** (Tier 1: couple event → step index++). *Was **8.10**.*
      > As a dispatcher, I want the checklist to move when I couple, not only when I press Next.
      >
      > **Simulator gate (CP4):** Couple-success input → step index++. No tick loop required.
    - [ ] **13.2.2 Prep track arrival** — Loco on prep leg dest track → T2 `prep: at track` + desk cue; optional auto-advance to “at spur” (fail-closed if ambiguous).
      > As a shunter, I want to know I am on the right pickup track before I reverse to the cars.
      >
      > **Simulator gate:** Along-track position on dest track id → at-track; ambiguous track → no advance.
    - [ ] **13.2.3 FILO pickup queue** — Core order of task cars; desk “Next pickup: …”; **6.21** pin follows active queue head (Tier 1 named smoke scenario).
      > As a yard master, I want to know which car is next in FILO order.
      >
      > **Simulator gate (CP5 part):** Queue head identity from job cars; pin/target follows head after couple-advance.
    - [ ] **13.2.4 Creep-to-couple** — **GO** on Prep: **9.1** slow creep toward queue-head car using **6.18** Rear/Front green; stop; **7.4** couple (≤8 km/h). One car only this ship.
      > As a shunter, I want the loco to inch up to the job car without me on the throttle.
      >
      > **Simulator gate (CP5 part):** Creep ticks toward a stubbed car pose; speed ≤ `AutoCoupleAssist.MaxCoupleSpeedKmh`; green clearance → Couple action; refuse slam speed.
    - [ ] **13.2.5 Between-car shunt** — After couple, short pull-forward to clear knuckle; queue advances; repeat **13.2.4** for car 2 on **same spur** (two-car smoke max).
      > As a yard master, I want space to reach the next job car without uncoupling what I already have.
      >
      > **Simulator gate (CP6 part):** After couple, pull-forward distance; queue head = car 2; second creep+couple.
    - [ ] **13.2.6 Prep complete** — All task cars in consist → auto-advance Prep boundary; arms **13.3** Validate. Tier 1: consist ⊆ job task cars.
      > As a dispatcher, I want Prep to finish when every job car is coupled, not when I guess.
      >
      > **Simulator gate (CP6 part):** Consist ⊆ task cars → Prep complete; missing car → stay on Prep.
  - [ ] **13.3 Validate gate** — Confirm consist vs job; **Validate** arms Transit **GO**.
    > As an engineer, I want to sign off the train before the mod drives away.
    >
    > **Simulator gate (CP7):** Match → Transit GO armed; mismatch → fail-closed (no GO).
  - [ ] **13.4 Autonomous transit** — **9.1** drives Switch List legs (Align, CLEARED, Facing); fail-closed on Derail Risk / no path.
    > As an engineer, I want the train to follow the Switch List to delivery without me on the throttle.
    >
    > **Simulator gate (CP3 thin):** One Transit leg: CP0 CLEARED walk + CP1 ticks + CP2 GO; Align after CLEARED; Facing/reverse-into as Switch List; fail-closed Derail Risk / no path. Prep still manual this ship.
  - [ ] **13.5 Auto delivery drop** — Length-aware **fully in stall**; stop; uncouple/handbrake; advance to turn-in.
    > As an engineer, I want to know when the train is fully in the delivery track.
    >
    > **Simulator gate (CP8):** Consist envelope vs stall span → fully in; then stop + uncouple/handbrake events; step → turn-in.
  - [ ] **13.6 Turn-in + payout** — Auto or one-click complete; T2 payout line.
    > As an engineer, I want to get paid without walking every UI step if the drop was correct.
    >
    > **Simulator gate (CP9):** Turn-in complete event from a valid drop; payout UI / job-office chrome stays Tier 2. **CP10** is the scripted chain of CP0–CP9 on one fixture job.

- [ ] **Epic 14 — Maps desk upgrade** — **After 13, before 10.** IMGUI desk stays through single-job autonomy so GO/Human/Done exist before a rewrite. Ships as **2.14.x**. **Not HTP** (HTP stays inside **8.7** / **9.1** / **13.x**). UniverseLib only if hitch probe fails IMGUI and the player accepts a second mod.

  - [ ] **14.1 Desk Close chrome** — Hide → Close. *Was **8.11**.*
    > As a dispatcher, I want Close to mean the desk is gone so I do not reopen it while rolling.
  - [ ] **14.2 Track amenity filter** — Omit dead turntable / service picks. *Was **8.12**.*
    > As a dispatcher, I want the track list to skip amenities I cannot use.
  - [ ] **14.3 Live always-on route HUD** — Rem/ETA with the desk closed (TECH_DEBT from **8.2**).
    > As an engineer, I want Path/ETA without keeping the desk open.
  - [ ] **14.4 uGUI Maps desk** — Native uGUI or UniverseLib after IMGUI hitch **fails** `GcCadenceProbe`. One rewrite after **13** chrome exists.
    > As a dispatcher, I want a Maps desk that does not hitch the cab and can skip Layout.

- [ ] **Epic 10 — Multi-job Maps + optimizer** — **Phase D.** After **13** PASS (and **14** if the desk rewrite has started). Ships as **2.10.x**. **Simulator:** reuse Epic **13** state machine on N jobs; no new physics engine.

  - [ ] **10.1 Multi-job tour board** — N jobs; one board (multi-job license).
  - [ ] **10.2 Pickup order optimizer** — FILO / nearest / yard-cluster.
  - [ ] **10.3 Tour Align + Next** — Shared **13.1** GO/Human semantics.
  - [ ] **10.4 Job + route profit optimizer** — Pick jobs for max payout; feeds **10.2** + **13**.

- [ ] **Epic 11 — Digital Catalog** — **DEFER** until **10** or player asks. **2.11.x**.

  - [ ] **11.1 Digital Catalog** — Order keys / flags / tools.

- [ ] **Epic 12 — Roadside Assist** — **DEFER** until autonomous loop stable. **2.12.x**.

  - [ ] **12.1 Emergency fuel/oil call-out** — Paid call-out at stranded loco.

## Later (not on panacea critical path)

v1 parking lot + **deferred 8.x tester tools**. Desk chrome lives on **Epic 14**. Promote only when autonomous loop (**13**) blocks or user asks.

- **8.8 License spawn** — iced; tester spawn trickle.
- **8.9 Place ghost / Snap polish** — Bring/Turn UX polish.
- **Pickup “clear to couple”** — Rear/Front green at job car (**13.2** may subsume).
- **Multi-step shunting jobs (complex)** — after **13** single-job PASS; may extend **13.1** before **10**.
- **Epic 11 Catalog / Epic 12 Roadside** — see epic headers (**DEFER**).

- **UMM ModSettings** — when the first player toggle exists (loco radar **6.16** “Show nearest locos”).
- **Job-car PNG** — **6.21** shipped a purple quad; dedicated art waits like **6.17**.
- **Job-car pin glued to lumber** — **6.21** accepted hop at the next car **center** (`2.6.21.6`). Tighten FOV only if asked.
- **On-consist wagon lever writes** — disabled **2.6.21.3** (Rewired chatter). Cab Incremental is Harmony rising-edge (no hold-repeat). Numpad Enter reverser + Numpad . TM fuse stay.
- **Motors Off chip** — engine stall vs TM Dead; parked during **6.21** throttle hotfix.
- **Loco radar licence filter** — **6.16** requirement 1, parked behind `LocoRadarLicenseGate.FilterEnabled`. Filtering alone empties the radar at low career stages; promote with a "dim the unlicensed" or UMM-toggle design, not as a plain hide.
- **Top-band AR slide** — v1 4.9 (sticky row under HUD for now).
- **AR sticky ↔ object glide** — v1 `ArMarkerTransition` (~1 s ease). v2 hops. Not a 6.4 blocker.
- **Pause overlay hide** — Esc pause keeps HUD/AR (player still in world). Launcher hide is `HudWorldSession`. Hide-on-pause only if product asks.
- **AR in-view-only** — no false edge-stick on occluded targets (v1 4.9 / 4.10 follow-on; house @ 120 m on a freight flank).
- **Coupler tension HUD** — v1 parked; cut from Derail Risk. Consist-max `derailBuildUp` shipped **6.19**.
- **PgUp/PgDn turntable** — local QOL (v1 Epic 4); not remote CTC (**3.3** cut).
- **Session reset hotkey** — e.g. Shift+F6: time ~07:00, weather, invalidate/refresh jobs (sandbox).
- **Player headlamp** — camera-mounted spot (concept `L`); hands-free vs flashlight.
- **Anti-Wheelslip**
- **Startup Assist** — needs **7.1** Three-Gate.
- **Auto-Service / Auto-Shop** — overlap check vs **11.1** Catalog; paid emergency liquids → **12.1** Roadside Assist.
- **Manual Transmission Override (DM3)** — reverser must leave Neutral to unlock throttle; DM3 has no MU (knowledge note, not a ship).
- **Mounting Suite / precision mounting**
- **Engine Temp Soft Governor** — only if distinct from **7.2** TM thermal.
- **Flight-sim HUD** — v1 parking (not the Monitor stack).
- **Harmony missing-target self-disable** — v1 **0.4** still `[~]`: log + disable on broken signatures, no session crash.
- **Thermal ▼GOV flash / F5–F9 inject** — tester; v1 Motors heat inject parked @ **0.6.21**. F11 all-licenses already has `SmokeLicenseGrantGate`.
- **1.17 #4 behind-seed** — ice if Limit stays empty when a board is &lt;600 m behind.
- **Do not restore:** Recommended/Brake chips, Pos HUD, in-HUD Version, Comms overlay, remote throw, yard schematic, `Next: Farm [km]`, delete-cars, RCL remote, custom JobGenerator, click-to-switch 2D CTC map, Stress-as-occupancy (Limit stays posted).
