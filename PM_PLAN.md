# PM_PLAN — Yard Master Suite v2

Official **backlog**. Cross off here when a story ships; refresh [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md) + [AGENT_HANDOFF.md](AGENT_HANDOFF.md) → *Current state* in the same change.

**Background:** [docs/YMS_v2_Onboarding_Guide.md](docs/YMS_v2_Onboarding_Guide.md)  
**Rebuild sequence:** [docs/YMS_v2_Architecture_Plan.md](docs/YMS_v2_Architecture_Plan.md)  
**Pub/Sub:** [docs/Unity_PubSub_Best_Practices.md](docs/Unity_PubSub_Best_Practices.md)

v1 (`DerailValleyMod`) is a reference library. Do not mark v1 epics done here.

**Leverage:** Before implementing a story, read [docs/LEVERAGE_REGISTER.md](docs/LEVERAGE_REGISTER.md). Do not invent a wheel that row already names.

---

## How to read this

| Mark | Meaning |
|------|---------|
| `[x]` | Done (Tier 1 + applicable Tier 2) |
| `[~]` | In progress / partial |
| `[ ]` | Backlog |

**Version:** `info.json` is `2.{Epic}.{Story}` for the last **[x]** story (story **6.9** → **2.6.9**). See [docs/Versioning_and_Release_Strategy.md](docs/Versioning_and_Release_Strategy.md).

**Order:** Epics run **0 → 1 → 2 → 3 → 4 → 5 → 6**; within each epic, the next unchecked story. **Execution note:** Epic **6** (v1 HUD parity) may proceed in parallel after **3.3.1** closes Epic 3 — see matrix. Pin / top-band / ModSettings are **Later**, not the next story.

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

- [ ] **Epic 4 — Phase 4 Heavy Engines** — Time-sliced brains (Job/coroutine).

  - [x] **4.1 Type B mailbox** — `ConcurrentQueue<T>` drain to Type A on the main thread. (`info.json` **2.4.1**, Tier 2 PASS 2026-08-17)
    > As a heavy engine, I can push a struct off the worker and the HUD receives it without touching Unity APIs from that thread.
  - [x] **4.2 Track graph builder** — Yield across frames; publish via **4.1**. (`info.json` **2.4.2**, Tier 2 PASS 2026-08-17)
  - [x] **4.3 Geometry scanner (A116)** — Shipped 2026-08-17; **retired for Limit** in **6.9** (posted boards only). Scanner + Core curve ladder removed.
  - [ ] **4.4 PID speed governor** — **Blocked on user spec**. Start after Epic **6.9–6.10** posted Limit is honest (or user waives).
  - [ ] **4.5 Predictive braking (MPC)** — Only if still wanted after PID + HUD green; Type B mailbox.

- [ ] **Epic 5 — Phase 5 Tools & Governors** — Gameplay features on the solid foundation.

  - [ ] **5.1 Thermal governor**
  - [ ] **5.2 Dispatch desk & switch list**
  - [ ] **5.3 Auto-coupler / remote tools**

- [ ] **Epic 6 — Diagnostic HUD (v1 parity)** — Player-visible match to v1 **1.17 + Epic 4 HUD QOL** (minus explicit v2 cuts). Matrix: [docs/HUD_v1_Parity_Matrix.md](docs/HUD_v1_Parity_Matrix.md).

  - [x] **6.1 Always-on bar** — Heading + Clock (`DateTimeWrapper` world time). Marked / Path → **6.11**; Station → **6.12**. (`info.json` **2.6.1**, Tier 2 PASS 2026-08-18).
  - [x] **6.2 Look-at bar** — Pipe / Handbrake / Couplers / Car / Track / Cargo / Loco type; identity-only `T2 look-at bar`. Job chip → **6.13**. (`info.json` **2.6.2**, Tier 2 PASS 2026-08-17).
  - [x] **6.3 Usable target** — Spherecast + look-at wins + usable consist walk + consist publish on look-at (`info.json` **2.6.3**, Tier 2 PASS 2026-08-17).
  - [x] **6.4 AR stack sync** — Edge STN/LOCO sit **below** the HUD stack (`HudStackLayout.LastBottomGuiY`). OnObject stays on the world object. (`info.json` **2.6.4**, Tier 2 PASS 2026-08-17). Glide + pause-hide → Later.
  - [x] **6.5 Mass + Grade** — Cab Mass + Grade; change-only gadget gate (`info.json` **2.6.5**, Tier 2 PASS 2026-08-18).
  - [x] **6.6 Load + Motors + Fluids** — Cab Fuel / Oil / Load / Motors from sim (`info.json` **2.6.6**, Tier 2 PASS 2026-08-19).
  - [x] **6.7 MU sync** — Cab yellow `MU idle` / red `MU desync`; quiet when synced (`info.json` **2.6.7**, Tier 2 PASS 2026-08-19).
  - [x] **6.8 Full lever + Speed + Limit** — Live levers + Speed + Limit chip; omit dashes; no Next (`info.json` **2.6.8**, Tier 2 PASS 2026-08-20). Geometry Limit later retired in **6.9**.
  - [x] **6.9 Posted board index** — Posted sticky Limit; geometry scanner ripped. (`info.json` **2.6.9**, Tier 2 PASS 2026-08-20).
  - [x] **6.10 Next + distance** — Next chip on Limit; meters when close (`NextLimitReveal`). Dual numbers stay through-only. (`info.json` **2.6.10**, Tier 2 PASS 2026-08-20).
  - [ ] **6.11 Marked**
  - [ ] **6.12 Station chip**
  - [ ] **6.13 Active job bar** — slot + listener stub; look-at Job chip (API TBD).
  - ~~**6.14 Track + Cargo**~~ — **Cut.** Folded into **6.2**. Look-at Job chip is **6.13**.
  - [ ] **6.15 Pin AR slot**
  - [ ] **6.16 Loco radar**
  - [ ] **6.17 PNG icons** (48px + dark plate)
  - [ ] **6.18 Rear/Front proximity**
  - [ ] **6.19 Consist stress** — worst car % of derail threshold; change-only publish. `StressDisplay.PercentOfThreshold` exists. First file when started: `YardMasterSuite.Tests/ConsistStressTelemetryTests.cs`.

## Later (not a Display Shell gate)

- **UMM ModSettings** — when the first player toggle exists.
- **Top-band AR slide** — v1 4.9 (sticky row under HUD for now).
- **AR sticky ↔ object glide** — v1 `ArMarkerTransition` (~1 s ease). v2 hops. Not a 6.4 blocker.
- **Pause overlay hide** — Esc pause keeps HUD/AR (player still in world). Launcher hide is `HudWorldSession`. Hide-on-pause only if product asks.
