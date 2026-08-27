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

**Version:** `info.json` is `2.{Epic}.{Story}` for the last **[x]** story (story **8.6** → **2.8.6.4**). Next **8.7** / **8.11** / **8.12** / **12.1** when asked. Catalog **11** stays last among *store* features. See [docs/Versioning_and_Release_Strategy.md](docs/Versioning_and_Release_Strategy.md).

**Order:** Epic **6** HUD closed. Attack leftover work in epic-number order: **7** governors → **8** dispatcher → **9** speed/brakes → **10** multi-job Maps → **11** catalog (**last** — playable without it), unless the user jumps. Pin / ModSettings stay Later except **6.15** when asked. See [docs/V1_FEATURE_COVERAGE.md](docs/V1_FEATURE_COVERAGE.md).

**Renumber (2026-08-25):** leftover stories moved past **6** so UMM versions never go backwards after **2.6.21.6**. Do not reuse Epic **5**.

| Was (pre-remap) | Now | First `info.json` |
|-----------------|-----|-------------------|
| 5.1–5.5 Governors | **7.1–7.5** | `2.7.1` … |
| 7.1–7.10 Dispatcher | **8.1–8.10** | `2.8.1` … |
| 4.4 PID / 4.5 MPC | **9.1 / 9.2** (was briefly **10**) | `2.9.1` … |
| Multi-job Maps (new 2026-08-27) | **10.1+** | `2.10.1` … |
| 8.1 Catalog | **11.1** (was briefly **9**) | `2.11.1` |
| Roadside Assist (new 2026-08-27) | **12.1+** | `2.12.1` … |
| 4.1–4.3 Heavy-engine infra | stay **4.x** (already `2.4.x`) | — |

**Priority lock (2026-08-27):** Speed/brakes before Catalog. Catalog stays last among **store** epics (**11**). **Roadside Assist** is **Epic 12** (recovery). Multi-job Maps is **not** immediate **8.x** work — start only after Epic **8** (and preferably **9**) when asked.

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

- [ ] **Epic 8 — Google Maps / Dispatcher (v1 3.5–3.7)** — City→track Set dest, Path/ETA/Facing, Align Route, then Switch List legs. Type B Dijkstra (Gemini’s real perf help); Three-Gate throws on the main thread. **Not** a 2D click-map. After Epic **7** (or user jump).

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
  - [ ] **8.7 Route pin + CLEARED** — v1 junction-first pin: At switch / CLEARED, latched until clear; re-enter danger cancels. **Length-aware** consist (v1 debt: do not hard-code DE2 18 m). Poll-cached eval (Maps hitch lesson).
    > As a driver on a Maps route, I want the same green CLEARED as Switch List.
  - [ ] **8.8 License spawn (iced)** — v1 **3.1b**. Do not start until **8.5**. If spawn ever lands, trickle over frames (Gemini) — that does **not** replace **8.1–8.2** Type B routing.
  - [ ] **8.9 Place ghost / Snap polish** — v1 **3.1** follow-on: re-rail-style place ghost + facing cue; Snap office spawn not under the mesh.
    > As a yard master placing cars, I want to see where they will land before Confirm.
  - [ ] **8.10 Switch List couple auto-advance** — v1 **3.6** parking: auto-advance after couple; arrival-track split. Not part of **8.3** first ship.
    > As a dispatcher, I want the checklist to move on when I couple the pickup, not only when I press Next.
  - [ ] **8.11 Desk Close chrome** — Rename **Hide** → **Close**; put it where a window chrome expects (title-bar right). Same Ctrl+Insert toggle. Do not change Clear semantics (**8.5** Clear already wipes dest + Switch List).
    > As a dispatcher, I want an obvious Close on the desk so I am not hunting Hide in the footer.
  - [ ] **8.12 Track amenity filter + nearest hint** — City Track dropdown: omit **Turntable** / maintenance / service tokens the sticky yard does not have; when useful, show nearest amenity yard (“closest service / TT”) instead of a dead pick. Pure catalog filter + optional distance cue — not a mini-map.
    > As an engineer picking a city, I want only tracks that exist there, and a nudge to the nearest service yard when I need one.

- [ ] **Epic 9 — Speed / brake brains** — leftover from Epic **4**. After Epic **8**. Blocked on user spec until **9.1**. Ships as **2.9.x**.

  - [ ] **9.1 PID speed governor** — **Blocked on user spec**. Hold a speed / look-ahead before Derail spikes. **7.5** is the reactive ≥65 % net only.
  - [ ] **9.2 Predictive braking (MPC)** — Only if still wanted after PID; Type B mailbox (**4.1**). Never dump air (same as **7.5**).

- [ ] **Epic 10 — Multi-job Maps** — After Epic **8** (and preferably **9**). **Not** immediate **8.x** work. Optimize a handful of taken/held jobs as one tour — pickup order, FILO-style queue, shared Align/Switch List. Needs multi-job license. Ships as **2.10.x**.

  - [ ] **10.1 Multi-job tour board** — Select N taken/held jobs (multi-job license); show a combined pickup/delivery board on the desk.
    > As an engineer with the multi-job license, I want to pick up X jobs at once and see them as one board so I am not juggling N separate Switch Lists.
  - [ ] **10.2 Pickup order optimizer** — Order pickups (FILO / nearest / yard-cluster heuristics); fail-closed when tracks cannot be read. Reuses **8.2** path costs where useful.
    > As an engineer with several jobs in hand, I want an optimized pickup order (like a FILO queue) so I am not zigzagging the yard by guesswork.
  - [ ] **10.3 Tour Align + Next** — Drive the tour with **8.2** Align + Switch List Next semantics; one active leg at a time.
    > As a dispatcher on a multi-job run, I want Align/Next to follow the optimized tour without re-picking city/track per job.

- [ ] **Epic 11 — Digital Catalog (v1 Epic 5)** — **Last among store/order features.** Game is playable without it. Ships as **2.11.x**. Roadside Assist is **Epic 12** (recovery), not Catalog.

  - [ ] **11.1 Digital Catalog** — Order keys / flags / tools to the player. Not custom job generation.
    > As an operator, I want stores to come to me so I do not deadhead for a flag.

- [ ] **Epic 12 — Roadside Assist** — Emergency fuel/oil at the stranded loco (not station deadhead). After Epic **8** when asked; does **not** replace **11.1** Catalog. Overlaps Later Auto-Service — this epic is the paid call-out. Ships as **2.12.x**.

  - [ ] **12.1 Emergency fuel/oil call-out** — Cab/desk control: top off **fuel** and/or **oil** on the usable loco anywhere on the map. **Pricing (locked):** (1) flat **dispatch fee** (~$3,000, tunable) plus (2) **2.5×** Career Manager unit rate for liters delivered, plus (3) route through **insurance copay** / Career Manager fee path so the player’s copay applies (user: “co-pay + 250% of fuel/oil”). Fail closed if wallet/fee API missing. Sandbox / free-money mode TBD. UI trigger + T2 fee line in same ship.
    > As an engineer dying on a grade with a full train, I want roadside fuel/oil delivered to my loco so I can finish the job — and pay a painful but non-bankrupting call-out (copay + 2.5× liquids).

## Later (not a Display Shell gate)

v1 parking lot + follow-ons that are **not** the next numbered story. Promote into an epic only when the user asks.

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
