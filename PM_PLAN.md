# PM_PLAN — Yard Master Suite v2

Official **backlog**. Cross off here when a story ships; refresh [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md) + [AGENT_HANDOFF.md](AGENT_HANDOFF.md) → *Current state* in the same change.

**Background:** [docs/YMS_v2_Onboarding_Guide.md](docs/YMS_v2_Onboarding_Guide.md)  
**Rebuild sequence:** [docs/YMS_v2_Architecture_Plan.md](docs/YMS_v2_Architecture_Plan.md)  
**Pub/Sub:** [docs/Unity_PubSub_Best_Practices.md](docs/Unity_PubSub_Best_Practices.md)

v1 (`DerailValleyMod`) is a reference library. Do not mark v1 epics done here.

---

## How to read this

| Mark | Meaning |
|------|---------|
| `[x]` | Done (Tier 1 + applicable Tier 2) |
| `[~]` | In progress / partial |
| `[ ]` | Backlog |

---

## Backlog

- [x] **Epic 0 — Repo bootstrap** — Folder layout, public GitHub repo, AgenticTemplate + v1 rules delta. **Closed 2026-08-12.**

  - [x] **0.1 Docs layout** — Move YMS background into `docs/`; archive Predictive Braking templates.
  - [x] **0.2 Public repo** — `pbuckles22/DerailValleyModv2` on `main`.
  - [x] **0.3 Agentic overlay** — Upstream AgenticTemplate; v1 delta rules; stack-specific handoff/docs.

- [ ] **Epic 1 — Phase 1 Heartbeat** — Core infrastructure before any train telemetry.

  - [x] **1.1 Solution scaffold** — `YardMasterSuite.sln`, csproj, `info.json`, `Directory.Build.targets.example`.
    > As a maintainer, I want a net48 UMM project that builds so Phase 1 code has a home.
  - [ ] **1.2 YmsEventBus** — Central Type A `Action` bus with `ClearAllSubscriptions()`.
    > As a subscriber, I receive primitive/struct payloads with zero alloc and can unsubscribe on disable.
  - [ ] **1.3 GcCadenceProbe** — Silent frametime monitor that logs GC/stutter warnings.
    > As a developer, I am warned when a feature introduces a hitch.
  - [ ] **1.4 GuiContentCache / StringBuilder pool** — No concatenated strings in render loops.
    > As a player, the HUD does not hitch from string allocs.

- [ ] **Epic 2 — Phase 2 Senses** — Event-driven telemetry (no polling).

  - [ ] **2.1 Loco state listener** — Board/unboard → cached current loco + bus event.
  - [ ] **2.2 Control telemetry** — Throttle/brake/reverser only when levers move.
  - [ ] **2.3 Trainset topology** — Consist length/weight on coupler events only.

- [ ] **Epic 3 — Phase 3 Display Shell** — Zero-alloc HUD/AR before heavy math.

  - [ ] **3.1 HUD manager** — Top bar + always-on compass.
  - [ ] **3.2 AR overlay engine** — Pooled world-space markers.

- [ ] **Epic 4 — Phase 4 Heavy Engines** — Time-sliced brains (Job/coroutine).

  - [ ] **4.1 Track graph builder** — Yield across frames.
  - [ ] **4.2 Geometry scanner (A116)** — Cache until segment change.
  - [ ] **4.3 Predictive braking (MPC)** — Port from v1 reference, Type B mailbox.

- [ ] **Epic 5 — Phase 5 Tools & Governors** — Gameplay features on the solid foundation.

  - [ ] **5.1 Thermal governor**
  - [ ] **5.2 Dispatch desk & switch list**
  - [ ] **5.3 Auto-coupler / remote tools**

Keep this file in sync with AGENT_HANDOFF "Current state" and `docs/` when you add them.
