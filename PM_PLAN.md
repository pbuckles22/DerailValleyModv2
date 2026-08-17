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

**Version:** `info.json` is `2.{Epic}.{Story}` for the last **[x]** story (story **4.3** → **2.4.3**). See [docs/Versioning_and_Release_Strategy.md](docs/Versioning_and_Release_Strategy.md).

**Order:** Always the next unchecked numbered story in this file. Do not pause to pick. Pin / top-band / ModSettings are **Later**, not the next story.

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

- [ ] **Epic 3 — Phase 3 Display Shell** — Zero-alloc HUD/AR before heavy math.

  - [x] **3.1 HUD manager** — Top bar + always-on compass. (`info.json` **2.3.1**, Tier 2 PASS 2026-08-13)
  - [x] **3.2 AR overlay engine** — Fixed 3-slot buffer (hide off-screen); office STN + own-loco LOCO; mid-edge fan; hitch-summary. (`info.json` **2.3.2**, Tier 2 PASS 2026-08-17). Pin and top-band slide are later.

- [ ] **Epic 4 — Phase 4 Heavy Engines** — Time-sliced brains (Job/coroutine).

  - [x] **4.1 Type B mailbox** — `ConcurrentQueue<T>` drain to Type A on the main thread. (`info.json` **2.4.1**, Tier 2 PASS 2026-08-17)
    > As a heavy engine, I can push a struct off the worker and the HUD receives it without touching Unity APIs from that thread.
  - [x] **4.2 Track graph builder** — Yield across frames; publish via **4.1**. (`info.json` **2.4.2**, Tier 2 PASS 2026-08-17)
  - [x] **4.3 Geometry scanner (A116)** — Cache until segment change. (`info.json` **2.4.3**, Tier 2 PASS 2026-08-17)
    > Current `RailTrack` bezier → SignPlacer ladder + sustained-zone finder. Type A `GeometryScanResult`. No HUD Limit chip.
    > **Logic for 4.4:** `TrackPathSpan`, zone start/end meters, same `Evaluate` on a longer arc list.
    > **Not this story:** posted boards, MPC, thrown-switch path-ahead walk, pin, top-band.
  - [ ] **4.4 Predictive braking (MPC)** — Port from v1 reference; Type B mailbox.

- [ ] **Epic 5 — Phase 5 Tools & Governors** — Gameplay features on the solid foundation.

  - [ ] **5.1 Thermal governor**
  - [ ] **5.2 Dispatch desk & switch list**
  - [ ] **5.3 Auto-coupler / remote tools**

## Later (not a Display Shell gate)

- **UMM ModSettings** — `UnityModManager.ModSettings` when the first player toggle exists (after **3.2**, or folded into that story). Do **not** number as **3.3** / do not start while **3.1** is in flight.

Keep this file in sync with AGENT_HANDOFF "Current state" and `docs/` when you add them.
