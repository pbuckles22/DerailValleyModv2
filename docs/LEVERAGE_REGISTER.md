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
| **[x] 3.4–3.5** Speed/Limit | **3.4** labels shipped in **6.8**. **3.5** posted index **6.9** + Next **6.10**; geometry Limit **retired**. Dual numbers through-only. | **adapt** | Dual diverge number |

---

## Epic 6 — Diagnostic HUD (v1 parity)

| Story | Leverage | Decision | Invent only if |
|-------|----------|----------|----------------|
| **[x] 6.1** Always-on extras | v1 `AlwaysOnHudLine` + `TelemetryReader.CurrentClockLabel` (`DV.DateTimeWrapper.Instance.DateTime`). Heading + Clock shipped. Marked / Path → **6.11**; Station → **6.12**. | **adapt** Type A + v1 wrapper | Polling beats events on hitch probe |
| **[x] 6.4** AR stack sync | v1 `ArWaypointOverlay` sticky row (`LastStackBottomGuiY` + `ArStickyRowPlacement`). v2 `HudStackLayout.LastBottomGuiY`. Edge Y under stack; OnObject keeps world Y. Top-band slide stays Later. | **adapt** v1 sticky row | Top-band slide (cut) |
| **[x] 6.3** Consist on look-at | v1 `TelemetryReader` usable-consist walk. v2 `ConsistTopology.PrepareForLoco` + `ReadConsist` on the look-at usable loco (boarded consist still wins). | **adapt** | Polling consist every frame |
| **[x] 6.2** Look-at polish | v1 `TelemetryReader.FormatCarNumber` / `LoadedCargo` / `carLivery`; v1 `Tier2LocalCarDebug` log-on-change (identity only, not analog pipe). **6.14 cut** — cargo folded here; Job chip → **6.13**. | **adapt** | Cargo API missing (it is not — `TrainCar.LoadedCargo`) |
| **[x] 6.5** Mass + Grade | v1 `GradeDisplay` + consist kg→t; v2 `TrainGadgetTelemetry` display-bucket gate | **adapt** Type A + 10 Hz sample, publish on 0.1 % / whole-tonne | Per-frame grade poll |
| **[x] 6.6** Load + Motors + Fluids | v1 `TelemetryReader` `ReadFluidPercent` / `ReadLoadPercent` / `ReadMotorStatus` (TM + fuse + MU temp). No debug overrides. | **adapt** Core gate + `LocoSimReader` | Game drops sim APIs we use |
| **[x] 6.7** MU sync | v1 `TelemetryReader.TryGetConsistFreeMotionSeverity` + `ConsistFreeMotion`. EngineOn + reverser/throttle/brakes vs other locos. Older-save smoke: v1 F11 all-licenses acquire (`SmokeLicenseGrantGate.Enabled`, ship default false). | **adapt** Core gate + consist walk | Game drops `controlsOverrider` |
| **[x] 6.8** Full lever + Speed + Limit | v1 `TrainHudLine` center chips + `SpeedLimitDisplay` bands. Geometry `?? 120` when no curve zone (v1 `GetOrComputeTrackGeometryLimitKmh`). Sample usable loco (not boarded-only) like `TryGetUsableLoco`. Omit `— Speed` / `— Limit`. Posted Next stays **6.10**. | **adapt** Core gate + listeners | Posted board index |
| **[x] 6.9** Posted board index | v1 `WorldSpeedBoardIndex`, `PostedStickyLimit`, `SpeedLimitBoardFacing`, `SignDebug` FoT on `NeedsRefresh`. Posted sticky wins Limit; **120 auth=default** until a take; **no geometry**. Dual diverge / path-ahead / Next → **6.10**. Stress % → **6.19**. | **adapt** v1 index + facing | Dual junction arm + track-resolve retry |
| **[x] 6.10** Next + distance | v1 `NextLimitReveal`, `AheadBoards.NextDifferent`, `TrackPathAhead` (thrown route via `selectedBranch`). Dual board **numbers** stay through-only — path already follows the thrown arm. | **adapt** v1 reveal + path-ahead | Dual `PickKmh(diverging)` from `selectedBranch` |
| **[x] 6.11** Marked + Path | v1 `ParkMarkSession` + `Home`/`Shift+Home`; `PathCheckSession` + `End`/`Shift+End`; `ParkMarkDisplay` / `PathCheckDisplay`. Path edges freeze with **4.2** mapper (no v1 `RoutePlanService`). Look-away keeps last origin. Station stays **6.12**. | **adapt** v1 session + Type A extras | Per-frame PathCheck.Evaluate |
| **[x] 6.12** Station chip | v1 `StationWaypointDisplay` + `StationJobGenerationRange` job zone; office transform (not yard center). `here` = existing `ArOfficeGate` 20 m (same as house hide). Fluids `Next: Farm [km]` stays cut. AR pin/icons stay **6.15–6.17**. | **adapt** v1 format + `StationOfficeAnchor` | Per-frame station scan |
| **[x] 6.13** Active job bar | v1 `JobsManager.currentJobs` / `GetJobOfCar`; `ActiveJobHudLine` + `JobConsistStatusEval` + `BonusTimeDisplay`. Look-at Job chip via `GetJobOfCar`. Preview / license warn / Cancelled flash stay out (v1 Bundle D extras). Job-car AR → **6.21**. | **adapt** v1 format + 4 Hz sample | Per-frame job/task walk |
| **[x] 6.13 on-consist** | v1 `OnConsistControl` + Rewired cab incremental bindings → front loco. Fail closed off-train. Stacked on **6.13** by request (not full auto-coupler). | **adapt** v1 Core + Update listener | Off-train remote |
| **[x] 6.15** Pin AR | v1 `ParkMarkSession` + `TryGetArPinWorldPosition`; hide within 8 m (`ArPinGate`); stand-height lift 0.6 m; amber quad. PNG **6.17**. | **adapt** existing 3-slot buffer + Type A `T2 ar` | — |
| **[x] 6.16** Loco radar | v1 `ArWaypointOverlay`, `LocoRadarSelection`, `LocoRadarScanPolicy`; PNG **6.17** | **adapt** FoT on city/leave/force; licence filter parked | ModSettings toggle; re-arm filter |
| **[x] 6.17** PNG icons | v1 `Icons/` loco/station/pin + dark plate; radar = loco art amber | **adapt** package copy + `Texture2D.LoadImage` once | job-car PNG Later (quad **6.21**) |
| **[x] 6.18** Rear/Front proximity | v1 Reverse Rear / Forward Front; Neutral omit; green ≤0.5 m + couple-scan | **adapt** NonAlloc overlap + caption key | — |
| **[x] 6.19** Derail Risk | consist-max `derailBuildUp` % of game threshold; coupler `TrainStress.stress` cut; always-on cab RAG; T2 `risk=` max `lead=` boarded loco | **adapt** Type A gadget gate + 10 Hz trainset walk | per-car T2 dump |
| **[x] 6.20** Job preview / Cancelled / license | v1 Bundle D: inventory Preview Regular edge (−30 m HUD); Cancelled 8 s; `No license:` codes. Wipe station = job-id origin not `chainOriginYardId` dest (`2.6.20.1`) | **adapt** v1 format + 4 Hz job bar | per-car T2 dump |
| **[x] 6.21** Job-car AR | v1 purple ■ on task cars; one pin per pickup spur; camera FOV + adjacent cosine; hide on taken GO | **adapt** v1 slot + Type A `T2 job-car-ar` | per-car pins; dedicated PNG |

---

## Epic 4 — Heavy engines

| Story | Leverage | Decision | Invent only if |
|-------|----------|----------|----------------|
| **[x] 4.1** Type B mailbox | BCL `ConcurrentQueue<T>` of readonly structs; drain on main thread → Type A. Pattern already in [Unity_PubSub_Best_Practices.md](Unity_PubSub_Best_Practices.md). No UniTask / Jobs in this story. | **adapt** (thin Core queue + tests) | A zero-alloc DV mailbox exists (none known) |
| **[x] 4.2** Track graph builder | Graph: **A\*** (standard). Game: `RailTrack` graph. Community: [WallyCZ/DVRouteManager](https://github.com/WallyCZ/DVRouteManager) `PathFinder.cs` (A\* over `RailTrack`, turntables, yard penalty). v1 `PathGraphBuilder` + `PathGraphBuildPump` (already time-sliced). Yield: **native Update tick** every 64 units (not UniTask). Publish via **4.1**. | **adapt** A\* + v1 pump | Coroutine still hitch-spikes → then inspect UniTask **or** Job System (Jobs struggle if nodes are managed `RailTrack` objects) |
| **[x] 4.3** Geometry scanner (A116) | Shipped then **retired for Limit in 6.9** — scanner + `SpeedLimitGeometry*` deleted. Posted boards own Limit. | **cut** from Limit | Revisit only if stress/derail needs arc math |

**UniTask** ([Cysharp/UniTask](https://github.com/Cysharp/UniTask)): allocation-free async. Default **do not ship**. Native coroutine matches “yield across frames” without a DLL. Revisit only if **4.2** hitch-fails.

---

## Epic 7 — Governors (was 5)

| Story | Leverage | Decision | Invent only if |
|-------|----------|----------|----------------|
| **[x] 7.1** Three-Gate | v1 `ThreeGate.TryApply` (Integrity → State Registry → Safety → Soft Write). v2 Core stub already exists. | **adapt** v1 path; this story owns wiring + T2 abort | Per-governor private write |
| **[x] 7.2** Thermal governor | v1 `ThermalGovernor` + `ThermalThrottleCap`. Soft-roll on FixedUpdate while Hot (5%/s); Three-Gate write. Type A temp-only would miss the roll. | **adapt** v1 cap + **7.1** | Game adds a vanilla thermal limiter we should subscribe to instead |
| **[x] 7.3** Auto-brake | v1 `AutoBrakeGovernor`: engine off soft-rolls train + indy toward full, throttle toward idle; never auto-release on start. | **adapt** v1 + **7.1** | — |
| **[x] 7.4** Auto-coupler / remote tools | Vanilla coupler / remote APIs. v1 `CouplerLinkStatus` and related. [mspielberg/dv-zcouplers](https://github.com/mspielberg/dv-zcouplers) is **knuckle physics** — different product; **do not copy**. Small junction analog: [imagitama/derail-valley-switch-next-junction](https://github.com/imagitama/derail-valley-switch-next-junction). | **adapt** vanilla + v1 QOL | We want radio-based remote (then CommsRadioAPI) |
| **[ ] 7.5** Limit auto-throttle | Same Three-Gate pattern as **7.2**; % of posted Limit (**6.9–6.10**). | **adapt** when asked | — |
| **Later** UMM ModSettings | `UnityModManager.ModSettings` (template-umm / every DV UMM mod). v1 `ModSettings.cs`. | **adapt** when the first player toggle exists | Not a 3.3 Display Shell story |

---

## Epic 8 — Dispatcher (was 7)

| Story | Leverage | Decision | Invent only if |
|-------|----------|----------|----------------|
| **[ ] 8.1–8.5** Dispatch desk & switch list | **Product logic:** v1 `SwitchListPlanner`, `SwitchListSession`, `PathPlan`, `MapsClearUiGate`, yard minimap. **Graph/API analog:** DVRouteManager (path + map markers) — we are **not** an AI driver. **Map analog:** [mspielberg/dv-remote-dispatch](https://github.com/mspielberg/dv-remote-dispatch) is a **browser dispatcher**, different product; steal coordinate/track-id ideas only. **Widgets:** revisit UniverseLib here (panels, scroll pools) if IMGUI cannot do a desk. **Radio:** [fauxnik/dv-comms-radio-api](https://github.com/fauxnik/dv-comms-radio-api) only if the desk is a Comms Radio mode. | **adapt** v1 planner; **read** those three repos before drawing UI | IMGUI cannot do the desk **and** user OK on UniverseLib and/or CommsRadioAPI |

---

## Epic 9 — Catalog (was 8)

| Story | Leverage | Decision | Invent only if |
|-------|----------|----------|----------------|
| **[ ] 9.1** Digital Catalog | v1 catalog order keys/flags/tools. Not custom job generation. | **adapt** v1 | — |

---

## Epic 10 — PID / MPC (was 4.4–4.5)

| Story | Leverage | Decision | Invent only if |
|-------|----------|----------|----------------|
| **[ ] 10.1** PID speed governor | **Blocked on user spec.** Posted Limit already honest (**6.9–6.10**). | **adapt** after spec | — |
| **[ ] 10.2** Predictive braking (MPC) | **v1 is the source of truth** for our feed-forward stress math (`AutoBrakeGovernor` / related Core; architecture name “PredictiveBrakeController”). Academic MPC / ROS = analogy only — do not vendor. Type B mailbox (**4.1**). | **adapt** (port math, new bus) | v1 formula is wrong in current DV physics; then re-derive from game, not ROS |

---

## Third-party libraries (default: out)

| Library | Role | When it gets in |
|---------|------|-----------------|
| UniverseLib | Runtime uGUI / UnityExplorer-class menus | 8.1 desk widgets after IMGUI fails; never for 3.1 overlay |
| UniTask | Zero-alloc async/await | 4.2 after coroutine hitch-fails |
| Unity Job System + Burst | Parallel blittable math | 10.2 / graph **if** data is structs, not `RailTrack` objects |
| CommsRadioAPI | Extra radio modes | 8.x / 7.4 if UX is radio, not a HUD desk |
| Mirror / Netcode UI pooling | Networked canvas | **Never** — wrong domain |

---

## GitHub to inspect (do not clone until asked)

Read-only reconnaissance. Clone or add as a sibling only after the user says so.

| Repo | Why | Stories |
|------|-----|---------|
| [pbuckles22/DerailValleyMod](https://github.com/pbuckles22/DerailValleyMod) | Already local — hooks + math | all |
| [derail-valley-modding/template-umm](https://github.com/derail-valley-modding/template-umm) | Already used for scaffold/packaging | 1.x done |
| [WallyCZ/DVRouteManager](https://github.com/WallyCZ/DVRouteManager) | `RailTrack` A\*, `BezierArcApproximation`, map markers, coroutine AI (pattern, not product) | 4.2, 4.3, 8.x |
| [mspielberg/dv-hud](https://github.com/mspielberg/dv-hud) | Community HUD: grade, stress, upcoming switches | 3.1 analog, 4.3 |
| [mspielberg/dv-remote-dispatch](https://github.com/mspielberg/dv-remote-dispatch) | Track/car/job map in a browser | 8.x analog |
| [fauxnik/dv-comms-radio-api](https://github.com/fauxnik/dv-comms-radio-api) | Radio mode framework | 8.x / 7.4 maybe |
| [Cysharp/UniTask](https://github.com/Cysharp/UniTask) | Async without `Task` alloc | 4.2 maybe |
| [sinai-dev/UniverseLib](https://github.com/sinai-dev/UniverseLib) | uGUI factory / ScrollPool | 8.1 maybe (3.1 already no) |
| [mspielberg/dv-zcouplers](https://github.com/mspielberg/dv-zcouplers) | Know what **not** to copy | 7.4 |
| [imagitama/derail-valley-switch-next-junction](https://github.com/imagitama/derail-valley-switch-next-junction) | Small next-junction API | 8.x, 7.4 |

Org index: [github.com/derail-valley-modding](https://github.com/derail-valley-modding).

---

## When a story starts

1. Open this file’s row. Do not skip to code.
2. If the row says **read** a repo, ask the user before cloning.
3. Adapt into Type A/B + zero-alloc (Gemini blueprint → Cursor).
4. If the decision changes (e.g. UniTask in), **edit this file in the same ship**.
