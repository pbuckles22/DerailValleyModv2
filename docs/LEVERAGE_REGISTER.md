# Leverage register

**Purpose:** Before each PM story, pick an existing wheel. Invent only when zero-alloc / Pub/Sub / product rules make the existing wheel unusable. This file is the **decision log**. The workflow lives in [Research_and_Leverage_Manifesto.md](Research_and_Leverage_Manifesto.md).

**v1** ([pbuckles22/DerailValleyMod](https://github.com/pbuckles22/DerailValleyMod), local sibling `../DerailValleyMod`) is a **reference library**: game API hooks and math. Do not copy `Update()` loops, string-in-OnGUI, or bolt-on structure.

---

## Stance (do not re-litigate per story)

| Prefer (in order) | Meaning |
|-------------------|---------|
| 1. Native Unity / Derail Valley API | Events, IMGUI, coroutines, `WorldToScreenPoint`, `RailTrack`, coupler events |
| 2. Pattern, not a shipped DLL | MediatR-style registration, object pool, A*, cache-until-invalid |
| 3. v1 Core math / hook names | Port formulas and event names into Type A/B; rewrite the loop |
| 4. Community DV mod (read-only) | How they touch the same game API — not their product UX |
| 5. Third-party UMM dependency | Last resort. Needs hitch-probe failure **and** explicit user OK |

**Ship a library** (UniverseLib, UniTask, CommsRadioAPI, …) only after the native path fails `GcCadenceProbe` / `T2 hitch-spike`, and the user accepts a second installed mod.

**Do not** vendor academic ROS/MPC stacks, Mirror/Netcode canvas kits, or ZCouplers physics. Those are analogs or different products.

---

## How to read a row

| Column | Meaning |
|--------|---------|
| **Leverage** | Existing wheel (API, pattern, v1 file, community repo) |
| **Decision** | `reuse` = call as-is · `adapt` = same idea, YMS constraints · `invent` = we own this |
| **Invent only if** | The gate that would justify writing new machinery |

Status: `[x]` shipped · `[~]` in flight · `[ ]` backlog.

---

## Epic 1 — Heartbeat (shipped)

| Story | Leverage | Decision | Invent only if |
|-------|----------|----------|----------------|
| **1.1** Solution scaffold | [derail-valley-modding/template-umm](https://github.com/derail-valley-modding/template-umm); [Creating a mod](https://derailvalley.wiki.gg/wiki/Creating_a_mod) | **adapt** — net48 UMM layout, `info.json` | We needed a non-UMM loader (we do not) |
| **1.2** YmsEventBus | Native `Action`; MediatR-style pub/sub (pattern only). No DV Type A bus exists. | **invent** the bus; **reuse** the pattern | A zero-alloc DV/community bus appears (none known) |
| **1.3** package.ps1 | template-umm `package.ps1` + UMM drop layout | **adapt** | UMM packaging changes; then re-read the template |
| **1.4** GcCadenceProbe | Unity `Time.deltaTime` / hitch sampling; v1 `HitchCadenceProbe` | **adapt** | Profiler UI in-game (not required) |
| **1.5** GuiContentCache / StringBuilder pool | BCL `StringBuilder`; object-pool pattern | **adapt** | A zero-alloc UI string kit that does not add a DLL |

---

## Epic 2 — Senses (shipped)

| Story | Leverage | Decision | Invent only if |
|-------|----------|----------|----------------|
| **2.1** Loco state | Vanilla `PlayerManager.CarChanged` (and equivalent board/unboard). v1 telemetry as hook names only. | **reuse** game events | Game drops the event; then Harmony Prefix/Postfix on the same call sites |
| **2.2** Control telemetry | Vanilla lever/control change (Harmony Prefix/Postfix if the game has no event). Named thr/indy/train/eng/rev. | **adapt** | Polling beats events on hitch probe (should not) |
| **2.3** Trainset topology | Vanilla `Coupler.OnCoupled` / `OnUncoupled`; consist mass/length from game trainset | **reuse** | Game stops firing coupler events |

---

## Epic 3 — Display shell

| Story | Leverage | Decision | Invent only if |
|-------|----------|----------|----------------|
| **[x] 3.1** HUD manager | Native `MonoBehaviour.OnGUI` + `GuiContentCache`. Compass = look yaw (`PlayerManager.ActiveCamera`), Unity **+Z = north**, 16-point labels. **UniverseLib deferred** ([DESIGN_SYSTEM.md](../.cursor/skills/DESIGN_SYSTEM.md)): canvas/GameObject kit, allocates, extra UMM dep. Community HUD ([mspielberg/dv-hud](https://github.com/mspielberg/dv-hud)) is a **read analog** for *what* to show, not *how* (they poll; we subscribe). v1 `AlwaysOnHudLine` / `TrainHudLine` = product copy only. Hitch probe: 100 ms + world session only (3.1 smoke harvest). | **adapt** native IMGUI; **do not ship** UniverseLib | Hitch probe fails IMGUI **and** user OK on UniverseLib |
| **[x] 3.2** AR overlay | … | **adapt** … | … |
| **[x] 3.3.1** HUD v1 chrome | v1 `MonitorHudDriver.CreateBarStyle` / `DrawCenteredBar`; `TrainHudLine`, `CabLeverDisplay`, `UsableTrainGate` (**4.3**). Matrix: [HUD_v1_Parity_Matrix.md](HUD_v1_Parity_Matrix.md). | **adapt** v1 chrome + labels; v2 Type A bus | Full diagnostic before Epic **6** |
| **[~] 3.4–3.5** Speed/Limit | v1 formatters shipped; Epic **6** owns posted boards + visibility | **adapt** | Posted index needs **6.9** |

---

## Epic 6 — Diagnostic HUD (v1 parity)

| Story | Leverage | Decision | Invent only if |
|-------|----------|----------|----------------|
| **[~] 6.1–6.4** Four-bar shell | v1 `MonitorHudDriver`, `AlwaysOnHudLine`, `LocalCarHudLine`, `TelemetryReader` target-car (**read** hooks only). v2 `UsableTrainProbe`, `HudStackLayout`. | **adapt** Type A listeners | Polling beats events on hitch probe |
| **[x] 6.3** Consist on look-at | v1 `TelemetryReader` usable-consist walk. v2 `ConsistTopology.PrepareForLoco` + `ReadConsist` on the look-at usable loco (boarded consist still wins). | **adapt** | Polling consist every frame |
| **[x] 6.2** Look-at polish | v1 `TelemetryReader.FormatCarNumber` / `LoadedCargo` / `carLivery`; v1 `Tier2LocalCarDebug` log-on-change (identity only, not analog pipe). **6.14 cut** — cargo folded here; Job chip → **6.13**. | **adapt** | Cargo API missing (it is not — `TrainCar.LoadedCargo`) |
| **[~] 6.5–6.8** Loco gadgets | v1 `TrainHudLine`, `FluidDisplay`, `MotorDisplay`, `GradeDisplay`, `ConsistFreeMotion` | **adapt** Core + listeners | Game drops sim APIs we use |
| **[ ] 6.9–6.10** Posted Limit | v1 `WorldSpeedBoardIndex`, `PostedLimitFilo`, `SignDebug` | **adapt** v1 index policy | Game exposes cheaper board API |
| **[ ] 6.15–6.17** AR polish | v1 `ArWaypointOverlay`, `Icons/` PNGs | **adapt** 48px + plate | Procedural quads OK for dev; PNG for parity |

---

## Epic 4 — Heavy engines

| Story | Leverage | Decision | Invent only if |
|-------|----------|----------|----------------|
| **[x] 4.1** Type B mailbox | BCL `ConcurrentQueue<T>` of readonly structs; drain on main thread → Type A. Pattern already in [Unity_PubSub_Best_Practices.md](Unity_PubSub_Best_Practices.md). No UniTask / Jobs in this story. | **adapt** (thin Core queue + tests) | A zero-alloc DV mailbox exists (none known) |
| **[x] 4.2** Track graph builder | Graph: **A\*** (standard). Game: `RailTrack` graph. Community: [WallyCZ/DVRouteManager](https://github.com/WallyCZ/DVRouteManager) `PathFinder.cs` (A\* over `RailTrack`, turntables, yard penalty). v1 `PathGraphBuilder` + `PathGraphBuildPump` (already time-sliced). Yield: **native Update tick** every 64 units (not UniTask). Publish via **4.1**. | **adapt** A\* + v1 pump | Coroutine still hitch-spikes → then inspect UniTask **or** Job System (Jobs struggle if nodes are managed `RailTrack` objects) |
| **[x] 4.3** Geometry scanner (A116) | Unity `BezierArcApproximation.CalculateArcs` (game `BezierCurves.dll`, 0.5 m). v1 `SpeedLimitGeometry` / `SpeedLimitGeometryZones` / `TrackPathSpan`. Cache until **segment change**; bezier-once store per track id. Type A (cheap). Path-ahead walk stays **4.4**. | **adapt** native bezier + v1 cache policy | Game exposes a cheaper curvature API we are not using |
| **[ ] 4.4** Predictive braking (MPC) | **v1 is the source of truth** for our feed-forward stress math (`AutoBrakeGovernor` / related Core; architecture name “PredictiveBrakeController”). Academic MPC / ROS = analogy only — do not vendor. Type B mailbox (**4.1**). | **adapt** (port math, new bus) | v1 formula is wrong in current DV physics; then re-derive from game, not ROS |

**UniTask** ([Cysharp/UniTask](https://github.com/Cysharp/UniTask)): allocation-free async. Default **do not ship**. Native coroutine matches “yield across frames” without a DLL. Revisit only if **4.2** hitch-fails.

---

## Epic 5 — Tools & governors

| Story | Leverage | Decision | Invent only if |
|-------|----------|----------|----------------|
| **[ ] 5.1** Thermal governor | Game engine-temp events / loco thermal fields. v1 `ThermalGovernor` + `ThermalThrottleCap`. Clamp throttle on Type A temp change — no poll. | **adapt** v1 cap + game events | Game adds a vanilla thermal limiter we should subscribe to instead |
| **[ ] 5.2** Dispatch desk & switch list | **Product logic:** v1 `SwitchListPlanner`, `SwitchListSession`, `PathPlan`, `MapsClearUiGate`, yard minimap. **Graph/API analog:** DVRouteManager (path + map markers) — we are **not** an AI driver. **Map analog:** [mspielberg/dv-remote-dispatch](https://github.com/mspielberg/dv-remote-dispatch) is a **browser dispatcher**, different product; steal coordinate/track-id ideas only. **Widgets:** revisit UniverseLib here (panels, scroll pools) if IMGUI cannot do a desk. **Radio:** [fauxnik/dv-comms-radio-api](https://github.com/fauxnik/dv-comms-radio-api) only if the desk is a Comms Radio mode. | **adapt** v1 planner; **read** those three repos before drawing UI | IMGUI cannot do the desk **and** user OK on UniverseLib and/or CommsRadioAPI |
| **[ ] 5.3** Auto-coupler / remote tools | Vanilla coupler / remote APIs. v1 `CouplerLinkStatus` and related. [mspielberg/dv-zcouplers](https://github.com/mspielberg/dv-zcouplers) is **knuckle physics** — different product; **do not copy**. Small junction analog: [imagitama/derail-valley-switch-next-junction](https://github.com/imagitama/derail-valley-switch-next-junction). | **adapt** vanilla + v1 QOL | We want radio-based remote (then CommsRadioAPI) |
| **Later** UMM ModSettings | `UnityModManager.ModSettings` (template-umm / every DV UMM mod). v1 `ModSettings.cs`. | **adapt** when the first player toggle exists | Not a 3.3 Display Shell story. Fold into 3.2+ or a later numbered story |

---

## Third-party libraries (default: out)

| Library | Role | When it gets in |
|---------|------|-----------------|
| UniverseLib | Runtime uGUI / UnityExplorer-class menus | 5.2 desk widgets after IMGUI fails; never for 3.1 overlay |
| UniTask | Zero-alloc async/await | 4.2 after coroutine hitch-fails |
| Unity Job System + Burst | Parallel blittable math | 4.4 / graph **if** data is structs, not `RailTrack` objects |
| CommsRadioAPI | Extra radio modes | 5.2 / 5.3 if UX is radio, not a HUD desk |
| Mirror / Netcode UI pooling | Networked canvas | **Never** — wrong domain |

---

## GitHub to inspect (do not clone until asked)

Read-only reconnaissance. Clone or add as a sibling only after the user says so.

| Repo | Why | Stories |
|------|-----|---------|
| [pbuckles22/DerailValleyMod](https://github.com/pbuckles22/DerailValleyMod) | Already local — hooks + math | all |
| [derail-valley-modding/template-umm](https://github.com/derail-valley-modding/template-umm) | Already used for scaffold/packaging | 1.x done |
| [WallyCZ/DVRouteManager](https://github.com/WallyCZ/DVRouteManager) | `RailTrack` A\*, `BezierArcApproximation`, map markers, coroutine AI (pattern, not product) | 4.2, 4.3, 5.2 |
| [mspielberg/dv-hud](https://github.com/mspielberg/dv-hud) | Community HUD: grade, stress, upcoming switches | 3.1 analog, 4.3 |
| [mspielberg/dv-remote-dispatch](https://github.com/mspielberg/dv-remote-dispatch) | Track/car/job map in a browser | 5.2 analog |
| [fauxnik/dv-comms-radio-api](https://github.com/fauxnik/dv-comms-radio-api) | Radio mode framework | 5.2, 5.3 maybe |
| [Cysharp/UniTask](https://github.com/Cysharp/UniTask) | Async without `Task` alloc | 4.2 maybe |
| [sinai-dev/UniverseLib](https://github.com/sinai-dev/UniverseLib) | uGUI factory / ScrollPool | 5.2 maybe (3.1 already no) |
| [mspielberg/dv-zcouplers](https://github.com/mspielberg/dv-zcouplers) | Know what **not** to copy | 5.3 |
| [imagitama/derail-valley-switch-next-junction](https://github.com/imagitama/derail-valley-switch-next-junction) | Small next-junction API | 5.2, 5.3 |

Org index: [github.com/derail-valley-modding](https://github.com/derail-valley-modding).

---

## When a story starts

1. Open this file’s row. Do not skip to code.
2. If the row says **read** a repo, ask the user before cloning.
3. Adapt into Type A/B + zero-alloc (Gemini blueprint → Cursor).
4. If the decision changes (e.g. UniTask in), **edit this file in the same ship**.
