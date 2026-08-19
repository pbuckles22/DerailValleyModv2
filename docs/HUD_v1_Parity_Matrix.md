# HUD v1 parity matrix

Living tracker: **v1 chip/behavior → v2 story → Core formatter → Unity listener → Tier 1 test → Tier 2 smoke → status**.

Source of truth for look/feel: v1 [product.md](../../DerailValleyMod/doc/requirements/product.md). Architecture stays v2 Type A/B.

**Explicit v2 cuts (do not restore):** in-HUD Version (UMM only), `Pos` on HUD, Recommended/Brake chips, geometry-ahead limit boards as product authority, `Next: Farm [km]`, yard minimap schematic, top-band AR slide (mid-edge only for now).

---

## Legend

| Status | Meaning |
|--------|---------|
| `[x]` | Shipped + Tier 1; Tier 2 PASS or N/A |
| `[~]` | Code partial; smoke or game API wiring pending |
| `[ ]` | Backlog |

---

## Chrome and stack

| v1 chip / behavior | v1 ref | v2 story | Core | Unity | Tier 1 | Tier 2 | Status |
|--------------------|--------|----------|------|-------|--------|--------|--------|
| Centered single-box bar (`CreateBarStyle`, pad 10/10/4/4, `#1F1F1F` @ 82%) | 4.7 | **3.3.1** | `HudShell`, `MonitorHudStackLayout` | `HudManager` | `HudShellTests` | 3.3.1 smoke | `[x]` |
| Stack order loco → look-at → job → always-on (bottom) | 4.7 | **3.3.1**, **6.1** | `MonitorHudStackLayout` | `HudManager` | stack layout tests | four-bar smoke | `[~]` |
| Separator ` \| ` | — | **3.3.1** | `MonitorHudLine.Separator` | — | join tests | visual | `[x]` |
| AR sticky row under stack | 4.9 | **6.4** | `HudStackLayout`, `ArStickyRowPlacement` | `ArOverlayManager` (Edge pins under stack) | `ArStickyRowPlacementTests` heading-only | heading-only: STN/LOCO **below** Heading | `[x]` |

---

## Loco bar (usable train **4.3**)

| v1 chip / behavior | v1 ref | v2 story | Core | Unity | Tier 1 | Tier 2 | Status |
|--------------------|--------|----------|------|-------|--------|--------|--------|
| Hide bar when no usable loco train | 4.3 | **3.3.1** | `UsableTrainGate` | `UsableTrainListener`, `UsableTrainProbe` | `UsableTrainGateTests` | foot empty yard → heading only | `[x]` |
| Product levers (`TrainBrake`, `Indy`, `Throttle`, `Reverser`) | 1.1 | **3.3.1** | `CabLeverDisplay`, `ReverserDisplay` | `ControlTelemetryListener` | `CabLeverDisplayTests` | cab labels | `[x]` |
| Speed · Limit center-weighted | 1.17, 4.7 | **3.3.1**, **6.8** | `TrainHudLine`, `LocoHudLine`, `SpeedDisplay`, `SpeedLimitDisplay` | speed/limit listeners | `SpeedDisplayTests`, `SpeedLimitDisplayTests` | cab drive | `[~]` |
| Cars | 1.1 | **3.3.1**, **6.3** | `CarsDisplay` | `ConsistTopologyListener` | `HudShellTests`, `ConsistTopologyTests` | consist count on foot | `[x]` |
| Mass | 1.2 | **6.5** | `TonnageDisplay` | `TrainGadgetListener` | `TrainGadgetTelemetryTests`, `HudShellTests` cab | cab | `[x]` |
| Grade | 1.2 | **6.5** | `GradeDisplay` | `TrainGadgetListener` | `TrainGadgetTelemetryTests`, `GradeDisplayTests` | cab | `[x]` |
| Load | 1.7 | **6.6** | `LoadDisplay` | `TrainGadgetListener` (pending sim API) | v1 port | cab | `[ ]` |
| Fuel / Oil paired colors | 1.8–1.9 | **6.6** | `FluidDisplay` | pending | v1 port | cab | `[ ]` |
| Motors | 1.8 | **6.6** | `MotorDisplay` | pending | v1 port | cab | `[ ]` |
| Handbrakes total | 1.1 | **6.6** | `HandbrakeDisplay` | `TrainGadgetListener` | v1 port | consist | `[~]` |
| MU idle / desync | 1.15 | **6.7** | `ConsistFreeMotion` | pending | v1 port | multi-loco | `[ ]` |
| Posted Limit + Next distance | 1.17 | **6.9–6.10** | `NextLimitReveal`, `PostedLimitFilo` (v1) | pending | geometry today | drive | `[ ]` |
| Rear / Front proximity | 4.11–4.12 | **6.18** | `BackupProximityDisplay` | pending | v1 port | shunt | `[ ]` |

---

## Look-at bar

| v1 chip / behavior | v1 ref | v2 story | Core | Unity | Tier 1 | Tier 2 | Status |
|--------------------|--------|----------|------|-------|--------|--------|--------|
| Pipe, Handbrake, Couplers (colors) | 4.2, 4.4 | **6.2** | `BrakePipeDisplay`, `HandbrakeDisplay`, `CouplingDisplay` | `LocalCarTelemetryListener` | `LocalCarHudLine` (v1 tests port) | look-at car | `[x]` |
| Car number, Track | 4.2 | **6.2** | `CarNumberDisplay`, `TrackDisplay` | `LocalCarTelemetryListener` | `CarNumberDisplayTests` | yard | `[x]` |
| Cargo, Loco type | 4.2, 4.4 | **6.2** | `CargoDisplay`, `LocoTypeDisplay` | `LocalCarTelemetryListener` (`LoadedCargo` / livery id) | `CargoDisplayTests`, `LocoTypeDisplayTests` | yard | `[x]` |
| Job chip on look-at | 4.2 | **6.13** | `JobDisplay` | pending DV job API | — | job zone | `[ ]` |

---

## Always-on bar

| v1 chip / behavior | v1 ref | v2 story | Core | Unity | Tier 1 | Tier 2 | Status |
|--------------------|--------|----------|------|-------|--------|--------|--------|
| Heading 16-point | 1.12 | **6.1** | `HeadingDisplay`, `AlwaysOnHudLine` | `HeadingListener`, `HudManager` | `HudShellTests` | on foot | `[x]` |
| Clock (in-game) | 1.12 | **6.1** | `ClockDisplay`, `ClockTelemetry` | `AlwaysOnHudListener` (`DateTimeWrapper`) | `ClockTelemetryTests`, `HudShellTests` | office wall clock | `[x]` |
| Marked (Home) | 1.14 | **6.11** | `ParkMarkDisplay` | pending | — | mark smoke | `[ ]` |
| Station chip | 4.6 | **6.12** | `NextStationDisplay` | pending | — | STN zone | `[ ]` |
| Path check | 3.4 | **6.11** | `PathCheckDisplay` | pending | `PathCheck` | End dest | `[ ]` |

---

## Job bar

| v1 chip / behavior | v1 ref | v2 story | Core | Unity | Tier 1 | Tier 2 | Status |
|--------------------|--------|----------|------|-------|--------|--------|--------|
| Active job GO/HOLD | 4.8 | **6.13** | `ActiveJobHudLine` | `JobBarListener` (API TBD) | v1 tests port | job taken | `[ ]` |

---

## AR visual

| Aspect | v1 | v2 today | v2 story | Status |
|--------|-----|----------|----------|--------|
| Icon size | 48px PNG | 28px color quads | **6.17** | `[ ]` |
| Label plate | dark quad | text only | **6.17** | `[ ]` |
| Pin slot | visible | hidden | **6.15** | `[ ]` |
| Loco radar ≤600m | amber | — | **6.16** | `[ ]` |
| Sticky Y from HUD stack | `LastStackBottomGuiY` | `HudStackLayout.LastBottomGuiY` | **6.4** | `[x]` Edge under stack; OnObject on world; glide Later |

---

## Targeting

| v1 behavior | v1 ref | v2 story | Core | Unity | Status |
|-------------|--------|----------|------|-------|--------|
| Spherecast 0.15m / 250m look-at | 4.1 | **6.3** | `LookAtTargeting`, `TargetCarSelection` | `UsableTrainProbe` | `[x]` |
| Look-at wins over standing | 4.2 | **6.3** | `TargetCarSelection` | `UsableTrainProbe` | `[x]` |
| Usable consist walk | 4.3 | **6.3** | `CouplingLink`, `ConsistTopology.ResolveConsistAnchor` | `CouplerProbe`, `UsableTrainProbe`, `ConsistTopologyListener` | `[x]` |

---

## Maintenance

- Update a row when a story ships or smoke locks behavior.
- Tier 1 must name the smoke scenario ([smoke-gates-tier1-ci.mdc](../.cursor/rules/smoke-gates-tier1-ci.mdc)).
- Do not re-smoke the full matrix per chip — one smoke per wave.
