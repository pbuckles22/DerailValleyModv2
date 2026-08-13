# Performance log

**This is not a clean bill of health.** The 100 ms / world-session hitch gate stops **alert fatigue**. It does **not** prove YMS is innocent. Frames of 40–99 ms are now **invisible** in Player.log. In-world 100–185 ms in the 3.1 session are still **unexplained**.

v1 died on silent hitch compounding. After every Tier 2 smoke that shows `T2 hitch-spike` (or a frame you felt), **append a row**. Then harvest any Unity-free gate into Core tests ([TEST_TDD.md](../.cursor/skills/TEST_TDD.md) → Evidence loop). Bands: `GcCadence.Classify` (`HitchBand`).

## Bands (locked in Tier 1)

| Band | dt | Probe (in world session) | Core |
|------|-----|--------------------------|------|
| `BelowGate` | &lt; 100 ms | Silent | `GcCadence.Classify` |
| `Feature` | 100 ms – &lt; 1 s | `T2 hitch-spike` | same |
| `LoadScale` | ≥ 1 s | Silent if no world session; otherwise logs | same |

## How to add a row

1. Quote `dt=` (and `gc0=` if present) and nearby game lines (`[Loading]`, `Autosaving`, `Player entering car`).
2. One **hypothesis** (game / other mod / YMS). Unproven stays **open**.
3. Name the **Tier 1** test if the decision is Unity-free. Else write **YMS-only rerun** as the next measurement.

---

## Session 2026-08-13 — story 3.1 HUD (`2.3.1`)

**Setup:** Career Session 1. Mods also on: Booklet Organizer 1.1.2, Improved Job Overview 0.1.1, ZCouplers 2.3.5. Probe was still **40 ms** (pre-harvest). 86 `T2 hitch-spike` lines. No YardMasterSuite exceptions.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H1 | OpenVR init fail → non-VR | 410 | Feature | Game VR bootstrap, not YMS | **game** | — |
| H2 | Unload unused assets / leave menu | 5573 then 41–44 | LoadScale + BelowGate | Unity `Unloading 386 unused Assets` (350 ms inside a 5.5 s frame) | **game** | `Player_create_13s_is_load_scale` (band) |
| H3 | Switch into world / `TrainCarRegistry` | 6771 | LoadScale | Scene switch + save init | **game** | LoadScale ≥ 1 s |
| H4 | Loading steps 2–6 (veg, terrain, railway, content) | 1969 gc0=+3, 1367, 1332 gc0=+3 | LoadScale | `[Loading]` steps; gen0 from game instantiate | **game** | LoadScale |
| H5 | `[Loading] creating player` (step 7) | **13096** | LoadScale | Player spawn. Largest spike. World session may already be true | **game** (open if YMS OnEnable runs here) | `Player_create_13s_is_load_scale` |
| H6 | `[Loading] initializing car pool` (step 8) | 141, **1296**, 264, 393, 448, 479, 512, 516 | Feature + LoadScale | Car pool burst until `Car pool initialized` | **game** | `Streaming_1003ms_is_load_scale` |
| H7 | Terrains + texture streaming (steps 10–12) | 863, 1003 | Feature / LoadScale | `[Loading] waiting for terrains` / streaming budget | **game** | LoadScale at 1.003 s |
| H8 | First in-world frames (`T2 heading init`) + StartingItems | 72, 83, 60, then **2812** | BelowGate + LoadScale | Spawn settle + `StartingItemsController initializing` | **game** (2812) / **open** (60–83 ms) | BelowGate 50 ms |
| H9 | Yard look-around, on foot | many **40–70**; also 129–171 | BelowGate + Feature | DV yard + 3 other mods + IMGUI `OnGUI`. **Not proven YMS.** 40–99 ms will **not** log after the 100 ms gate | **open** | `Yard_play_50ms_is_below_gate`, `Cab_look_120ms_is_feature_hitch` |
| H10 | gen0 during look / uncouple | 66, 53, 54 ms with **gc0=+1** | BelowGate | Alloc: IMGUI internals, or `T2 controls` strings while dragging levers, or other mods. **Now silent** (under 100 ms) | **open** | Observe still silent for gc0 without ≥100 ms (`Observe_silent_when_gc0_increases_without_a_frame_spike`) |
| H11 | Board DE2 | 80–135 around `T2 loco-board` | BelowGate + Feature | Game `Player entering car` + consist bind. 80 ms now silent; 116–135 ms still log | **open** | Feature ≥ 100 ms |
| H12 | Lever drag | no extra hitch line beyond H9; dense `T2 controls` | — | Each percent change allocates a log string (story 2.2). Can contribute to H10 | **open** (YMS log alloc) | do not log per-frame; 2.2 already change-only |
| H13 | Autosave | 141 next to `Autosaving` | Feature | Game save | **game** | `Autosave_141ms_is_feature_hitch` |
| H14 | Unboard + pin-pull (`cars=3→2`) | 47–185 | BelowGate + Feature | Coupler events + consist rebind. 185 ms still logs | **open** | Feature |
| H15 | Pause / quit | 51–92 then unload | BelowGate | Menu + `Application quit`; `cars=1 t=38` is quit peel (TECH_DEBT) | **game** | — |

### What the 100 ms gate **hides** next session

H9/H10/H11 **BelowGate** rows. If those grow as we add AR (3.2) or graph (4.2), Player.log will not show it. **Revisit trigger:** YMS-only session (other three mods off). If `Feature` spikes remain on a quiet look-around, treat IMGUI / listeners as guilty until disproven.

### Next measurement

YMS-only in-world look-around + board, other mods off. Append H16+ with `Feature` spike counts. **3.1 HUD is a product PASS; it is not a performance-clean PASS.** Every later story that touches Update/OnGUI/couplers must add rows here. Prefer the YMS-only session before **3.2** ships.
