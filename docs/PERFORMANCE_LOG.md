# Performance log

**This is not a clean bill of health.** The 100 ms / world-session hitch gate stops **alert fatigue**. It does **not** prove YMS is innocent. Frames of 40–99 ms are counted in `T2 hitch-summary` (`below=` / `max=`), not as per-frame `T2 hitch-spike` lines. In-world 100–185 ms in the 3.1 session are still **unexplained**.

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

---

## Session 2026-08-13 — story 3.2 Smoke A office AR (`2.3.1` WIP)

**Setup:** Same career + same three other mods. Probe **100 ms**. **15** `T2 hitch-spike` lines (3.1 had 86 at the old **40 ms** gate — counts are not 1:1). No YardMasterSuite exceptions. Board+drive logged **no** hitch on `T2 loco-board` / lever lines.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H16 | Streaming / first world (`T2 ar init`) | 975, 116, 243 | Feature | `[Loading]` streaming + spawn; same class as H7/H8 | **game** | LoadScale / Feature at spawn |
| H17 | StartingItems | **2747** gc0=+1 | LoadScale | Same as H8 2812 | **game** | `Player_create_13s_is_load_scale` |
| H18 | ZCouplers interior type-load + UMM menu | 819, 213 | Feature | `[ZCouplers] ProcessInteriorObject` errors then `UIMenuController` | **other mod** | — |
| H19 | Yard look-around (AR object↔edge) | 174, 121, 120, 110, 144–149 | Feature | Same band as H9 (129–171). AR OnGUI added a quad+label. **Not worse than 3.1 Feature look-around.** 40–99 still silent | **open** | `Cab_look_120ms_is_feature_hitch` |
| H20 | Board DE2 + drive | none on board; none during `T2 controls` | — | Second `Player entering car` / `T2 loco-board` had **no** Feature line (H11 had 116–135). Drive is not a new hitch class | **better than H11** (this sample) | Feature ≥ 100 ms still locked |
| H21 | Pause | 101 | Feature | `UIMenuController` then quit | **game** | — |

**Vs 3.1 baseline:** In-world Feature look-around is the same class, not a new AR-only spike pattern. Board+drive did not add a hitch this session. Still not performance-clean. YMS-only rerun still the next measurement.

---

## Session 2026-08-17 — story 3.2 Smoke B own-loco (`2.3.1` WIP)

**Setup:** Same career + same three other mods. Probe **100 ms**. **17** `T2 hitch-spike` lines (Smoke A had 15). Player: no noticeable skip while driving. No YardMasterSuite exceptions.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H22 | Streaming / StartingItems | 928, 111, 223, **2030** | Feature + LoadScale | Same as H16/H17 | **game** | LoadScale |
| H23 | ZCouplers + UMM | 798, 165 | Feature | Same as H18 | **other mod** | — |
| H24 | Yard look-around (loco+office edge) | 143, 118, 111, **361**, 115–134, 123, 106 | Feature | Same class as H19. One 361 ms is higher than Smoke A’s 110–174; still look-around, not drive. 40–99 silent | **open** | `Cab_look_120ms_is_feature_hitch` |
| H25 | Board + drive | none on `T2 loco-board` / `T2 controls` | — | Matches player “no skip.” Same as H20 | **not worse** | — |
| H26 | Pause | 125, 138 | Feature | `UIMenuController` | **game** | — |

**Vs Smoke A:** Drive is still clean. Look-around Feature count is similar; one 361 ms look frame is the only new sour note. Adding LOCO did not create a drive hitch class.

---

## Session 2026-08-17 — Smoke B rerun, ZCouplers **off**

**Setup:** Same career. ZCouplers logged `To skip (disabled).` Booklet Organizer + Improved Job Overview still on. Probe **100 ms**. **9** `T2 hitch-spike` lines (prior Smoke B: 17). No `ProcessInteriorObject` errors. No YardMasterSuite exceptions.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H27 | Streaming / StartingItems | 939, 113 gc0=+1, 251, **2257** | Feature + LoadScale | Same as H16/H17/H22 | **game** | LoadScale |
| H28 | `[Loading] Done` then UMM menu | **803** | Feature | **Still here with ZCouplers off.** Prior H18/H23 blamed ZCouplers interior errors; those errors are gone, the ~800 ms at load-done remains → **game / menu**, not ZCouplers type-load | **game** | — |
| H29 | Yard look-around | 165, 108 | Feature | **No 361 ms this time.** Same small Feature class as H9/H19 | **open** | `Cab_look_120ms_is_feature_hitch` |
| H30 | Board + drive | none on board / `T2 controls` | — | Clean again | **not worse** | — |
| H31 | Pause / quit | 113, **2529** | Feature + LoadScale | `UIMenuController` then `Quit game requested` | **game** | — |

**Vs ZCouplers-on Smoke B:** Spike count 17 → 9. Look-around lost the 361 ms. The ~800 ms after load **did not** go away, so that one was not the ZCouplers error dump. Drive still clean.

### Next measurement (hitch-summary)

Spike gate stays **100 ms**. 40–99 ms is counted in `T2 hitch-summary` (`below=` count, `max=` ms, `gc0=` gen0 in that band) every ~30 s in-world and on leave-world / mod off. After Smoke C, paste the summary line(s) as H32+. Do not treat a quiet spike log as “no hitch.”

---

## Session 2026-08-17 — Smoke C edge stack + hitch-summary (`2.3.1` WIP)

**Setup:** Same career. Hitch-summary live. Player: drive felt smooth; asked if look-around skip is real. Screenshots: STN+LOCO **side by side** on the right edge (overlap **PASS**). One report: LOCO briefly at **top-left** (HUD collision) — clamp-to-top, not the uncoded top-bar.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H32 | Spawn / first 30 s | summary `n=763 fine=637 below=114 max=97 gc0=1 feature=11 load=1`; spikes 918, 119, 225, **2535**, 874, 220 | BelowGate + Feature + LoadScale | Same load/menu class as H27/H28. **114** hidden 40–97 ms frames in this window | **game** + first below-band count | hitch-summary |
| H33 | On-foot look-around | summaries ~30 s: `below=0–3 max=43–55`; `feature=0–3`; spikes **152, 132, 137, 146, 121, 158** next to `T2 heading change` | Feature | **Not imagination.** ~2 Feature frames per 24 s look window (~0.1% of frames). Rest are `fine` (under 40 ms). Same 110–160 class as H9/H19/H29 | **open** | `Cab_look_120ms_is_feature_hitch` |
| H34 | Board + brake/drive | `n=1435 fine=1433 below=1 max=47 feature=1` then `n=1224 fine=1223 below=1 max=44 feature=0` on `T2 controls` | — | Matches “driving is smooth.” No Feature spike on lever lines | **not worse** | — |
| H35 | Pause | 116, 111 after `UIMenuController` | Feature | Menu, same as H21/H31 | **game** | — |

**Smoke C overlap:** `loco=edge office=edge` with two chips (PASS). Top-left LOCO is `ClampToScreen` parking an off-top projection on the HUD — **not** the upcoming top AR bar. Edge-stack then treated that HUD chip as “left edge” and pinned it to the left margin. Smoke D: off-screen → mid left/right only; `T2 ar-summary` must show `edgeTop=0`.

---

## Session 2026-08-17 — Smoke D HUD clearance + hitch (same career)

**Setup:** Clamp-to-top removed. Player: look-around still felt hitchy. **edgeTop=0** every window (HUD find closed).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H36 | Spawn | 934, 114 gc0=+1, 254, **2685**, 758, 173; first summary `below=83 feature=13` | LoadScale + Feature | Same load class | **game** | — |
| H37 | Look-around / unboard | ar **object↔edge chatter** (many `T2 ar change` per second) then spikes **118, 157** | Feature | **YMS log tax + screen-edge flicker**, not Type A bus. Heading already 2 s throttle; AR change was unthrottled. 3.1 had the same 110–160 look class before AR | **squash** | `Rapid_look_throttles_T2_ar_change`, `Screen_edge_hysteresis_holds_object_when_barely_off` |
| H38 | Cab / drive windows | `feature=0`, `below=0–1`, `edgeTop=0` | — | Drive still clean. HUD clearance **PASS** on log | **not worse** | ar-summary `edgeTop=0` |
| H39 | Pause | 103 | Feature | Menu | **game** | — |

**Not pub/sub:** AR is LateUpdate poll + OnGUI, not `YmsEventBus`. Remaining ≥100 ms with `gc0` absent after squash still needs a YMS-only look-around to prove game vs IMGUI.

---

## Session 2026-08-17 — 3.2 ship PASS (`2.3.2`)

After 2 s AR log throttle + 48 px object/edge hysteresis: on-foot look window `n=1533 fine=1530 below=3 max=71 feature=0`. Three `T2 ar change` lines the whole session. `edgeTop=0`. Load/pause still Feature/LoadScale (accepted). Story **3.2** Tier 2 **PASS**.

---

## Session 2026-08-17 — story 4.1 Type B mailbox (`2.4.1`)

**Setup:** Same career. ZCouplers off (`To skip (disabled).`). Booklet Organizer + Improved Job Overview on. Probe **100 ms**. **7** `T2 hitch-spike` lines. Three activates → three `T2 mailbox: n=1` (no per-frame mailbox). `edgeTop=0`. No YardMasterSuite exceptions. Quit `UnityException` is Bolt `SceneVariables`, not YMS.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H40 | Streaming / StartingItems | 967, 122, 239, **2470** | Feature + LoadScale | Same load class as H27/H36 | **game** | LoadScale |
| H41 | `[Loading] Done` then UMM menu | **799**, 190 | Feature | Same as H28 (~800 ms at load-done) | **game** | — |
| H42 | On-foot / cab windows | summaries `n=1453 fine=1452 below=1 max=42 feature=0` then `n=1346 fine=1344 below=2 max=60 feature=0` | — | Drive + look **feature=0**. Mailbox drain did not add a hitch class | **not worse** | empty drain is silent (`FormatDrain_is_silent_when_empty`) |
| H43 | Pause / quit | 109; last summary `feature=2`; consist peel `3→2→1` | Feature | `UIMenuController` then `Quit game requested`. Peel is known unload debt | **game** | — |

**Vs 3.2 ship:** Mailbox probe is one line per activate. In-world Feature count is load/menu/quit only. Story **4.1** Tier 2 **PASS**.

---

## Session 2026-08-17 — story 4.2 track graph (`2.4.2`)

**Setup:** Same career. ZCouplers off. Booklet Organizer + Improved Job Overview on. Probe **100 ms**. One `T2 graph start` / one `T2 graph ready` (no per-track spam). `edgeTop=0`. No YardMasterSuite exceptions. Quit Bolt `SceneVariables` + DV `LampBrakeWarningReader` NREs are game teardown.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H44 | Streaming / graph start | 953 then `T2 graph start: units=2637` | Feature | Player transform appears during `[Loading] streaming`; mapping starts then (64/tick). Same load class as H40 | **game** + expected 4.2 start | `FormatStart` |
| H45 | Graph ready | **117** gc0=+1 next to `T2 graph ready: nodes=2073 edges=6804 hops=—` | Feature | Worker A\* + log string on a 2k-node graph. `hops=—` = first/last instance ids are not a connected pair (probe, not a player route) | **open** (YMS probe) | `FormatReady_uses_dash_when_no_path` |
| H46 | StartingItems / load-done | 204, **2549**, **803** | Feature + LoadScale | Same as H40/H41 | **game** | LoadScale |
| H47 | First in-world window | summary `n=838 fine=735 below=85 max=93 feature=15 load=3`; spikes 157, 123, 185, **3667**, 786, **5417** | Feature + LoadScale | LoadScale pair is pause/menu-class; drive later is clean | **open** (look) + **game** (1 s+) | — |
| H48 | On-foot / cab | `n=1375 fine=1369 below=5 max=82 feature=1` then **`n=1448 fine=1448 below=0 feature=0`** | — | Drive **feature=0**. Mapping did not leave a per-frame hitch | **not worse** | pump 64/tick |
| H49 | Pause / quit | 119, **130360**, **4523** | Feature + LoadScale | 130 s is pause/alt-tab; then `Quit game requested` | **game** | — |

**Vs 4.1:** Drive still `feature=0`. Graph built 2073 nodes / 6804 edges without a multi-second freeze. Story **4.2** Tier 2 **PASS**.

---

## Session 2026-08-17 — story 4.3 geometry scanner (`2.4.3`)

**Setup:** Same career. ZCouplers **on** (interior type-load errors return). Booklet Organizer + Improved Job Overview on. Probe **100 ms**. Version `2.4.3`. No YardMasterSuite exceptions. `edgeTop=0` throughout.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H50 | Streaming / graph start | 957 then `T2 graph start: units=2637` | Feature | Same load class as H44 | **game** + 4.2 | `FormatStart` |
| H51 | Graph ready / StartingItems | 116, 222, **2561** then `T2 graph ready` | Feature + LoadScale | Same as H45/H46. No extra hitch on first `T2 geometry` (line 862) | **game** | `FormatReady_uses_dash_when_no_path` |
| H52 | Load-done menu | **779**, 163 | Feature | Same ~800 ms class as H28/H41 | **game** | — |
| H53 | Cab / switch traverse | summaries `feature=0` during drive (`n=1296`, `n=1189`, `n=1050`); one **101** ms near switch geometry burst; final window `feature=2` on quit/menu | Feature | Drive clean. Rapid `T2 geometry` at switches = **one line per new `RailTrack` id** (expected 4.3). Not per-frame | **not worse** | cache-until-segment Tier 1 |

**4.3 smoke (Player.log):** Menu + on-foot before board: **no** `T2 geometry`. First board → one `segment=986842 limit=120`. Same segment silent until unboard → `segment=—`. Re-board + switch run → new segment ids only (`993766`, `984404`, …). Story **4.3** Tier 2 **PASS**.

---

## Session 2026-08-17 — story 3.3.1 HUD v1 chrome (`2.3.5.1`)

**Setup:** Career Session 1. Booklet Organizer + Improved Job Overview on. ZCouplers status not noted. Probe **100 ms**. Version `2.3.5.1`. Formal smoke PASS (empty yard foot, DE2 cab labels, unboard hide, hitch-summary). Informal shunter yard at SW-B3I in same log. No YardMasterSuite exceptions during play. Quit: Bolt `SceneVariables` + DV NRE (game teardown, same class as H44 session).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H54 | Graph / streaming | 959, 2174 (near `T2 graph start`) | Feature + LoadScale | Same load class as H44/H50 | **game** | — |
| H55 | Graph ready | **117** gc0=+1 at `T2 graph ready: nodes=2073 edges=6804 hops=—` | Feature | Worker A\* on 2k-node graph | **open** (YMS probe) | `FormatReady_uses_dash_when_no_path` |
| H56 | First in-world window | summary `feature=15 load=3` early; then cab **`feature=0 load=0`** (`n=1024 fine=994 below=30 max=100`) | — | Mapping/load burst then clean drive | **not worse** | — |
| H57 | Drive / brake stop | `feature=0` through cab roll; late **`dt=122`**, **`128`** ms spikes at hard stop | Feature | Game physics / brake class; below sustained concern | **open** (look) | — |
| H58 | Pause / quit | **`259835`**, summary `feature=5 load=1` | LoadScale | Alt-tab / pause menu | **game** | — |

**3.3.1 smoke:** `T2 usable-train on/off` matches foot/board/look-at. Board → `T2 consist: cars=3 t=74`. Unboard → `T2 loco-unboard` + usable off. **Informal:** look-at shunter showed loco bar without consist event (`Cars 0` orphan chips) — harvested to Tier 1 omit-null; full fix **6.3**. Story **3.3.1** Tier 2 **PASS**.

---

## Session 2026-08-17 — story 6.3 consist on look-at (`2.6.3`)

**Setup:** Career Session 1, SW-B3I shunter yard. Probe **100 ms**. Version `2.6.3`. Formal smoke PASS (on-foot look-at Cars/Mass, cab match, look-away hide). No YardMasterSuite exceptions.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H59 | Graph / streaming | 1023 (`T2 graph start`), 2281 | Feature + LoadScale | Same load class as H54 | **game** | — |
| H60 | First in-world window | summary `feature=17 load=2` (`n=816 fine=684 below=113 max=99`); then look-around `feature=5` → `feature=1` | Feature | Spawn + first usable-train bind (`T2 consist` at line 770 **before** board). Settles to `feature=1` | **not worse** than H56 | consist-on-look-at Core tests |
| H61 | On-foot look / board | board `146`/`133` ms; post-board summary `feature=4`; unboard look `feature=2` then `1` | Feature | Same board class as H11. Consist bus raise is not a new hitch pattern. `T2 look-at bar` still repeats on aim (**6.2**) | **open** (look) + **6.2** log volume | — |

**6.3 smoke:** On foot before `T2 loco-board`: one `T2 consist: cars=3 t=74`. Screenshots: heading-only off-consist; loco bar `Mass 74 t | Cars 3` matching look-at `all cars 74 t`. Story **6.3** Tier 2 **PASS**.

---

## Session 2026-08-17 — story 6.2 look-at polish (`2.6.2`)

**Setup:** Career Session 1, SW-B3I shunter yard. Probe **100 ms**. Version `2.6.2`. Formal smoke PASS (heading-only, Car N/A + Loco DE2, Car 1/2 + Forestry Trailers, look-away hide). Sixteen identity `T2 look-at bar` lines — no hold-still spam. No YardMasterSuite exceptions.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H62 | Graph / streaming | 1006 (with `T2 graph start`), 2468 `gc0=+1` | Feature + LoadScale | Same load class as H54/H59 | **game** | — |
| H63 | First in-world window | summary `feature=13 load=2` (`n=860 fine=740 below=105 max=100`); then look-around `feature=5`–`7` | Feature | Spawn + first consist look (`T2 consist` + `car=2`). Settles to `feature=1` on freight hold | **not worse** than H60 | LookAtBarTelemetry identity tests |
| H64 | On-foot look-at aim | hold-still `feature=1`; later windows `feature=3`/`4`/`2`/`1`; look-away `feature=1` | Feature | Same unexplained look-around class as H9/H61. Identity log is **not** the tax (no repeat `T2 look-at bar` while pipe stayed 4.5 bar) | **open** (look) | — |

**6.2 smoke:** Shunter `car=NA cargo=`; freight `car=1`/`car=2 cargo=Forestry Trailers`; hide on look-away. Quit-time consist peel 3→2→1 after `Application quit` — known debt. Story **6.2** Tier 2 **PASS**.

---

## Session 2026-08-17 — YMS-only isolation (`2.6.2`)

**Setup:** Same career / SW-B3I. **Only YMS on.** Booklet Organizer, Improved Job Overview, ZCouplers: `To skip (disabled).` Probe **100 ms**. Version `2.6.2`. Spawn looked at consist; boarded DE2; unboard; look-around; pause menu.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H65 | Graph / spawn | 1193 (`T2 graph start`), 523, 2402 `gc0=+1`, 794 | Feature + LoadScale | Same load class as H62. Other mods **off** — still here | **game** | — |
| H66 | First in-world window | summary `feature=15 load=2` (`n=726 fine=585 below=124 max=97`) | Feature | Spawn settle; same as H63 with other mods on | **not worse** | — |
| H67 | Cab then on-foot look | cab `n=1236 fine=1233 below=3 max=54` **`feature=0 load=0`**; unboard look spikes **149**, **124**; summary `feature=2 load=0`; menu `102` | Feature | **YMS-only still hitchy on foot.** Other mods are not the cause. Cab remains clean. Next isolation if we fix: HUD OnGUI off, then AR off, then SphereCast off | **open** (look) — YMS and/or vanilla DV | — |

**Isolation:** Look-around Feature **survives** with only YMS. Do not keep blaming Booklet / Job Overview / ZCouplers.

---

## Session 2026-08-17 — story 6.4 AR stack sync (`2.6.4`)

**Setup:** Career Session 1, SW-B3I. Probe **100 ms**. Version `2.6.4`. Formal smoke: heading-only edge STN/LOCO under Heading (not mid-screen); face office → `office=object`; face loco → `loco=object`. Pause overlay still draws HUD (player still in world — not the launcher gate). Exit to main menu after pause. No YardMasterSuite exceptions (Bolt `SceneVariables` on unload is game).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H68 | Graph / spawn | 987 (`T2 graph start`), 579, 275, **2778**, 993 | Feature + LoadScale | Same load class as H62/H65. First window `n=722 fine=576 below=130 max=96 feature=15 load=1` | **game** | — |
| H69 | Heading-only then look / board | windows `feature=1` → `2` → `0` (cab) → `3` (on-foot object+edge) → `0`/`0`/`0` holding still | Feature | Sticky-row Edge did not add a new hitch class. Cab still clean (`feature=0`). On-foot Feature same as H67 | **not worse**; look **open** | `Smoke_heading_only_edge_stn_sits_below_hud_not_beside_heading`; all `T2 ar-summary` `edgeTop=0` |
| H70 | Pause / exit to menu | 163 then `UIMenuController`; **3073**, **49324**; summary `feature=2 load=2`; consist peel `3→2→1` on unload | Feature + LoadScale | Pause/alt-tab + `Exit back to main menu requested`. Peel is known debt | **game** | — |

**6.4 smoke:** `T2 ar-summary` every window `edgeTop=0`. Heading-only: `object=0 edgeMid=…`. Face office/loco: `office=object` / `loco=object`. Instant sticky→object hop (no v1 1 s glide) — Later, not this ship. Story **6.4** Tier 2 **PASS**.

---

## Session 2026-08-18 — story 6.1 always-on Clock (`2.6.1`)

**Setup:** Career Session 1, SW-B3I then office wall clock. Probe **100 ms**. Version `2.6.1`. Formal smoke PASS (Heading + Clock on always-on bar; analog face 11:57 then 12:01). Clock T2 is one line per **game** minute (`init: 11:45` … `change: 12:05`) — not per LateUpdate. No YardMasterSuite exceptions (Bolt `SceneVariables` on unload is game).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H71 | Graph / spawn | 1000 (`T2 graph start`), 564, 278, **2720**, 810 | Feature + LoadScale | Same load class as H68. First window `n=801 fine=671 below=115 max=91 feature=14 load=1` | **game** | — |
| H72 | Yard then office clock | after spawn: `n=1349 fine=1348 below=0 max=0 feature=1`; then `n=1540 fine=1537 below=0 max=0 feature=3`; look spikes **137** / **140** ms near 11:57–11:58 | Feature | Clock minute publish is not a new hitch class. Office hold is cleaner than H69 on-foot look. Same unexplained 110–140 ms look class as H67 | **not worse**; look **open** | `Smoke_office_wall_clock_*` |
| H73 | Exit to menu | **107** then `UIMenuController`; summary `n=988 fine=983 below=2 max=66 feature=3 load=0`; consist peel `3→2→1` on unload | Feature | Pause/menu + known peel. AR windows `edgeTop=0` | **game** | — |

**6.1 smoke:** HUD `Heading ESE | Clock 11:49` then office `Heading N | Clock 11:57` / `Heading NNW | Clock 12:01` matching analog. Story **6.1** Tier 2 **PASS**.

---

## Session 2026-08-18 — story 6.5 Mass + Grade (`2.6.5`)

**Setup:** Career Session 1, SW-B3I then solo DE2 drive. Probe **100 ms**. Version `2.6.5`. Formal smoke PASS (cab Mass + Grade; Grade ticked on slope without pumping a handbrake). Gadget T2 is init / change / hide — not 10 Hz. No YardMasterSuite exceptions (Bolt `SceneVariables` on unload is game).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H74 | Graph / spawn | 1164, 527, 121, 320, **2606** gc0=+1 | Feature + LoadScale | Same load class as H71. First window `n=868 fine=728 below=122 max=99 feature=16 load=2` | **game** | — |
| H75 | Cab held then drive | cab `n=1125 fine=1118 below=7 max=49 feature=0`; later `n=1085 fine=1067 below=17 max=80 feature=1`; look spikes **109** / **101** / **166** / **135** | Feature | Grade publish is not a new hitch class. Cab drive still `feature=0`. Same unexplained 100–170 ms look class as H67/H72 | **not worse**; look **open** | `Smoke_sw_b3i_cab_held_*`, `Smoke_solo_de2_drive_*` |
| H76 | Pause / exit to menu | **116924** (pause/alt-tab), then on-foot `n=1537 fine=1529 below=3 max=81 feature=5`; unboard look **164** / **148** / **106**; menu `n=482 fine=481 below=1 max=43 feature=0` | Feature + LoadScale | Pause + known on-foot look. AR windows `edgeTop=0` | **game** / look **open** | — |

**6.5 smoke:** Cab held `Mass 74 t | Grade +0.4 %`. Solo drive `Mass 38 t | Grade -1.6 %`. Look-away `Heading NE | Clock 12:23`. `T2 gadgets init: grade=+0.4 mass=74` … `change: grade=-1.6 mass=38` … `hide`. Story **6.5** Tier 2 **PASS**.

---

## Session 2026-08-19 — story 6.6 Load + Motors + Fluids (`2.6.6`)

**Setup:** Career Session 1, SW-B3I DE2. Probe **100 ms**. Version `2.6.6`. Formal smoke PASS (cab Fuel / Oil / Load / Motors; Load ticked under power). Gadget T2 is init / change / hide — not 10 Hz. No YardMasterSuite exceptions (Bolt `SceneVariables` on unload is game).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H77 | Graph / spawn | 844, 559, 127, 232, **2055** | Feature + LoadScale | Same load class as H74. First window `n=812 fine=695 below=102 max=99 feature=14 load=1` | **game** | — |
| H78 | Cab held then roll | cab roll `n=1299 fine=1298 below=0 max=0 feature=1`; then `n=1086 fine=1081 below=4 max=47 feature=1 load=0`; look spikes **174** / **155** / **178** / **149** | Feature | Load/fluids publish is not a new hitch class. Cab roll `load=0`. Same unexplained 100–180 ms look class as H67/H72/H75 | **not worse**; look **open** | `Smoke_sw_b3i_cab_emits_T2_gadgets_init_load_0_fuel_96_oil_92_motors_ok`, `Smoke_sw_b3i_cab_load_ticks_to_40_under_power` |
| H79 | Unboard / exit to menu | unboard look **149** / **120**; summary `n=1225 fine=1220 below=4 max=45 feature=1 load=0`; Bolt `SceneVariables` on unload | Feature | Pause/menu + known look. AR windows `edgeTop=0` | **game** / look **open** | — |

**6.6 smoke:** Cab `Fuel 96 % | Oil 92 % | Load 43 % | Motors OK` then idle `Load 0 %` then roll `Load 25 %`. Look-away `Heading S | Clock 13:49`. `T2 gadgets init: grade=+0.4 mass=74 load=0 fuel=96 oil=92 motors=OK` … `change: … load=40 …` … `hide`. Story **6.6** Tier 2 **PASS**.

---

## Session 2026-08-19 — story 6.7 MU sync (`2.6.7`)

**Setup:** Career older save, two DE2s (Mass 76 t). Probe **100 ms**. Version `2.6.7`. Formal smoke PASS (yellow MU idle on Neutral; red MU desync with hose unplugged). Parking handbrake is not this chip. License grant `T2 licenses granted: n=6` (flag off for ship). No YardMasterSuite exceptions (Bolt `SceneVariables` on unload is game).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H80 | Graph / spawn | 1001, 545, 107, 188, 247, **3119** | Feature + LoadScale | Same load class as H77. First window `n=622 fine=474 below=125 max=100 feature=21 load=2` | **game** | — |
| H81 | Cab then MU work | cab `n=1247 fine=1240 below=7 max=67 feature=0 load=0`; later `n=1255` / `n=1318` `feature=1`–`3` `load=0`; look spikes **200** / **171** / **186** / **153** | Feature | MU publish is not a new hitch class. Cab still `feature=0`. Same unexplained look class as H67/H72/H78 (one 200 ms peak) | **not worse**; look **open** | `Smoke_trailing_neutral_*`, `Smoke_unplugged_indy_mismatch_is_mu_desync` |
| H82 | Unboard / look | `n=1030 fine=956 below=68 max=96 feature=6`; then `n=1290 feature=2` | Feature | Known on-foot look. AR windows `edgeTop=0` | **game** / look **open** | — |

**6.7 smoke:** Neutral `MU idle`; hose unplugged fight `MU desync`; Neutral again `MU idle`. `T2 gadgets init: … mu=idle` … `change: … mu=desync` … `mu=idle` … `hide`. Story **6.7** Tier 2 **PASS**.

---

## Session 2026-08-20 — story 6.8 full lever + Speed + Limit (`2.6.8`)

**Setup:** Career two DE2s (Mass 76 t, MF-T13P). Probe **100 ms**. Version `2.6.8`. Formal smoke PASS (cab Speed 0 + Limit 120; roll Speed 5; look-at levers + Speed + Limit 60; sky hides bar). No Next. First DE2 `oil=0` is the tank, not a HUD omit. Bolt `SceneVariables` on unload is game.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H83 | Graph / spawn | 917, 563, 112, 249, **2405** | Feature + LoadScale | Same load class as H80. First window `n=719 fine=572 below=124 max=93 feature=22 load=1` | **game** | — |
| H84 | Cab held then roll | held `n=1331 fine=1329 below=2 max=47 feature=0 load=0`; roll `n=1195 fine=1188 below=7 max=55 feature=0`; first brake-bleed window `n=1001 fine=863 below=136 max=69 feature=2` | Feature | Speed/Limit publish is not a new hitch class. Clean cab still `feature=0`. Brake-bleed `T2 controls` chatter sits in the known below/feature band | **not worse**; look **open** | `Smoke_mf_t13p_cab_held_speed_0_limit_120`, `Smoke_cab_roll_speed_5_limit_120_load_35`, `Smoke_cab_roll_publishes_speed_0_then_5` |
| H85 | On-foot look / unboard | spikes **203** / **193** / **168** / **157**; later `n=1280 feature=5` then `n=1379 feature=6` | Feature | Same unexplained look class as H67/H72/H81. AR windows `edgeTop=0` | **game** / look **open** | `Smoke_look_at_usable_loco_shows_levers_speed_and_limit` |

**6.8 smoke:** Cab `Speed 0 km/h | Limit 120` then roll `Speed 5 km/h`. Look-at DE2 `Speed 0 km/h | Limit 60`. Sky `Heading NW | Clock 20:43`. `T2 speed init: 0` … `T2 speed change: 5`. `T2 limit init: 120 auth=geometry` … `T2 limit change: 60 auth=geometry`. No Next. Story **6.8** Tier 2 **PASS**.

---

## Session 2026-08-20 — story 6.9 posted board index (`2.6.9`)

**Setup:** Career DE2, probe **100 ms**. Version `2.6.9`. Formal smoke PASS (Limit sticky on facing posted signs; curves do not move Limit; look-at Fuel/Oil follow usable loco). Geometry scanner ripped — no `T2 geometry` / `auth=geometry`. Bolt `SceneVariables` on unload is game.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H86 | Graph / spawn | first window `n=722 fine=575 below=127 max=94 feature=19 load=1` | Feature + LoadScale | Same load class as H83 (`feature=22 load=1`) | **game** | — |
| H87 | Cab held then posted-sign drive | many windows `feature=0` (n=1357 / 1121 / 1245 / 1366 / 1412 / 1313); one drive window `n=949 fine=875 below=62 max=100 feature=12` near `T2 boards fot` | Feature | Posted Limit publish is not a new hitch class. Clean cab still `feature=0`. FoT refresh bumps `below`/`feature` in one window — same band as 6.8 brake-bleed chatter, not a cab-drive class change | **not worse**; FoT **open** | posted sticky / facing Core smokes |
| H88 | On-foot look / pause | spikes **100–170**; pause **115819** | Feature + LoadScale | Same unexplained look class as H67/H72/H85. Look-at flicker also chatters `auth=none` ↔ `default` + repeated `T2 boards fot` (WARN log noise) | **game** / look **open** | look-at fluid gate |

**6.9 smoke:** Limit `120 auth=default` then takes `120/90/60/80 auth=posted`. Look-at Oil 0 % vs 97 %. `[YMS v2] Posted board index running.` No Geometry scanner. Story **6.9** Tier 2 **PASS**.

---

## Session 2026-08-20 — story 6.10 Next + distance (`2.6.10`)

**Setup:** Career DE2, probe **100 ms**. Version `2.6.10`. Formal smoke PASS after a first-try FAIL (nearby 6 skipped; look-at FoT 2.2–2.3 s). Dual junction numbers waived (through-only). Bolt `SceneVariables` on unload is game.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H89 | Graph / spawn | first window `n=710 fine=583 below=115 max=98 feature=9 load=3`; board FoT spikes 2457 / 863 | Feature + LoadScale | Same spawn/load class as H86 (`feature=19 load=1`). One `T2 boards fot` after load | **game** | — |
| H90 | Cab posted drive + Next | `feature=0` (`n=865` / `894`); take window `n=999 feature=5` near `80 next=50` | Feature | Next publish is not a new cab class. Raise `next=80` omits meters until ~115 m; drop `next=50 579m` is MaxReveal | **not worse** | NextDifferent / reveal / FormatHud Next smokes |
| H91 | On-foot look | L→R PASS; R→L stutter; spikes **100–144**; one window `n=797 below=204 feature=1 max=88`; pause **88344** | Feature | **No** 2.2 s FoT (roster kept). Remaining is H67/H72 look class plus usable-train on/off chatter (`60 next=80` ↔ `auth=none`) when the spherecast loses the loco | **better** than first 6.10 try; RTL **open** | `Smoke_nearby_posted_6_is_kept_when_board_track_unknown` |

**6.10 smoke:** Next chip + distance. Dual numbers through-only (player PASS). Look-at FoT fix held (one `T2 boards fot`). Story **6.10** Tier 2 **PASS**.

---

## Session 2026-08-20 — story 6.11 Marked + Path (`2.6.11`)

**Setup:** Career night yard, probe **100 ms**. Version `2.6.11`. Formal smoke PASS after look-away `Path —` FAIL; sticky origin re-smoke PASS.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H92 | Graph / spawn | first window `n=788 fine=670 below=106 max=98 feature=10 load=2`; earlier session `feature=13 load=2` | Feature + LoadScale | Same spawn/load class as H89 (`feature=9 load=3`) | **game** | — |
| H93 | First look-at loco | `T2 boards fot: raw=48 parsed=43` then **2210** | LoadScale | One-shot posted-board FoT on first look after load (same class as first 6.10 try). Player felt a pause; not every look | **open** (FoT) | roster keep from 6.10; not a Path chip gate |
| H94 | Cab / look after dest | cab `feature=0`; look spikes **104–157**; re-smoke window `n=1490 feature=1` | Feature | Path sticky origin does not add a cab class. Look remains H67/H72 | **not worse** | `Smoke_look_away_keeps_path_ok_when_dest_matches_last_origin` |

**6.11 smoke:** Home mark + End Path check. Look-away keeps Path OK. Story **6.11** Tier 2 **PASS**.

---

## Session 2026-08-20 — story 6.12 Station chip (`2.6.12`)

**Setup:** Career night, probe **100 ms**. Version `2.6.12`. Formal smoke PASS (CP zone chip, apron `here`, leave hides, look-away Path OK).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H95 | Graph / spawn | first window `n=588 fine=453 below=120 max=99 feature=13 load=2`; spikes 956, 581, **2640**, **2516** | Feature + LoadScale | Same spawn/load class as 6.11 first session (`feature=13 load=2`). First `T2 boards fot: raw=48 parsed=43` then **2516** | **game** / FoT **open** | — |
| H96 | Cab drive into CP | windows `n=1002 feature=0` and `n=1027 feature=0` | — | Station chip on always-on does not add a cab hitch class vs H94 | **not worse** | `Smoke_cab_drive_shows_station_cp_ssw_640m` |
| H97 | On-foot look / apron | spikes **100–180**; one **344** next to second `T2 boards fot: raw=52 parsed=46`; pause **30586** / **47425** | Feature + LoadScale | Look remains H67/H72. 344 ms is a FoT refresh while rolling, not the 2.2 s first-look class. Pause is the menu | **not worse**; FoT **open** | `Smoke_office_apron_shows_station_cp_here`, `Smoke_look_away_keeps_station_and_path_on_always_on` |

**6.12 smoke:** In-zone Station chip + office `here`. Dual numbers through-only. Story **6.12** Tier 2 **PASS**.

---

## Session 2026-08-21 — story 6.13 Job bar + on-consist (`2.6.13`)

**Setup:** Career SW, probe **100 ms**. Version `2.6.13`. Formal smoke PASS (job RED→GO, look-at Job chip, last-car cab keys).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H98 | Graph / spawn | first window `n=519 fine=309 below=184 max=99 feature=24 load=2`; spikes 907, 628, **2016**, **2211** | Feature + LoadScale | Spawn/load class; `feature=24` is a busy first window (SW mill + graph). First look FoT **2211** next to `T2 boards fot: raw=31 parsed=29` | **game** / FoT **open** | — |
| H99 | Cab / on-consist drive | windows `n=1077 feature=0`, `n=1162 feature=0`, `n=1067 feature=0` | — | Job bar + on-consist redirect do not add a cab hitch class vs H96 | **not worse** | `Smoke_taken_job_bar_shows_job_go_bonus`, `Smoke_stand_on_last_car_emits_T2_on_consist_armed` |
| H100 | On-foot look / last car | spikes **100–160**; FoT **2211** | Feature + LoadScale | Look remains H67/H72. 2211 ms is the known first-look FoT class, not a new on-consist hitch | **not worse**; FoT **open** | `Smoke_look_at_job_car_logs_job_id` |

**6.13 smoke:** Taken job bar GO/HOLD/RED + Bonus; look-at Job chip; on-consist cab keys. Dual numbers through-only. Story **6.13** Tier 2 **PASS**.

---

## Session 2026-08-21 — story 6.15 Pin AR (`2.6.15`)

**Setup:** Career MF, probe **100 ms**. Version `2.6.15`. Formal smoke PASS (Home amber PIN, world + mid-edge, hide ~8 m, Shift+Home clear). Cab drive ~16 km/h then stop.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H101 | Graph / spawn | first window `n=543 fine=387 below=135 max=97 feature=19 load=2`; spikes 942, 639, **2576** (StartingItems), **1682** after `T2 boards fot: raw=31 parsed=29` | Feature + LoadScale | Same spawn/load class as H98 (`feature=19` vs 24). 2576 is StartingItems; 1682 is first FoT + ZCouplers interior type-load | **game** / FoT **open** / **other mod** | — |
| H102 | Cab drive | window `n=1092 fine=1078 below=14 max=63 feature=0 load=0` while Speed 1–16 | — | Pin slot in existing AR LateUpdate does not add a cab hitch class vs H99 | **not worse** | `Smoke_home_pin_emits_T2_ar_pin_place` |
| H103 | On-foot look / PIN walk | spikes **103–222** (typical **110–170**); after stop `n=964 feature=4` then `n=1053 feature=0` | Feature | Look remains H67/H72. 191–222 ms while walking the yard with PIN on. Pause 168 ms then menu | **not worse** | `Smoke_home_mark_away_shows_pin`, `Smoke_standing_within_8m_hides_pin` |

**6.15 smoke:** Home amber PIN at the mark; mid-edge with STN/LOCO; hides on the mark; Shift+Home clears. PNG stays **6.17**. Story **6.15** Tier 2 **PASS**.

---

## Session 2026-08-23 — story 6.16 Loco radar (`2.6.16.10` product PASS; `2.6.16.11` hitch fix not re-logged)

**Setup:** Career SW, probe **100 ms**. UMM `2.6.16.10`. Formal smoke PASS (cyan LOCO on freight car, amber S060, F11 does not change amber, cab hides cyan). DV fps/mem overlay **on**.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H104 | Graph / spawn | first window `n=411 fine=128 below=249 max=98 feature=31 load=3`; radar FoT `fotMs=100` | Feature + LoadScale | Same spawn/load class as H101 (`feature=31` vs 19). Overlay inflates Feature count | **game** / overlay | — |
| H105 | Cab / in-world | windows `feature=15–19`, `max=46–98ms`; radar FoT 85–118 on Forced/LeftLoco only | Feature | Overlay `FindObjectOfType` every 2 s (missing notification root). Closed by H107 | **closed** (H107) | `Smoke_cab_drive_does_not_retry_overlay_fot_every_two_seconds` |
| H106 | On-foot look | typical 100–200 ms class in later windows (`below` high, `max` 47–99) | Feature | Look remains H67/H72. Not treated as a new class | **not worse** | `Smoke_de2_only_save_shows_unlicensed_locos_without_f11` |

**6.16 smoke:** v1 4.10 parity (no licence filter). Cyan LOCO fallback on freight car. Amber S060 156 m then 63 m in cab. F11 does not change amber (`unlic=0 n=1` before and after). Story **6.16** Tier 2 **PASS**. Landed on `main` 2026-08-23.

---

## Session 2026-08-23 — 6.16 hitch isolation (`2.6.16.13`)

**Setup:** Career SW, probe **100 ms**. UMM `2.6.16.13`. DV fps/mem overlay **off**. DE2 cab reverse with ~6 loaded cars.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H107 | Cab reverse drive | windows `n=1052 feature=0 max=71`; `n=1021 feature=0 max=45`; `n=814 feature=0 max=60`; `n=712 feature=0 max=61`; `n=744 feature=0 max=81`; speed 1–39 | — | Overlay `FindObjectOfType` retry every 2 s was the `feature=15` class. Cap 2 lookups/world. Pause/quit `feature=2` | **not worse** vs H102 | `Smoke_cab_drive_does_not_retry_overlay_fot_every_two_seconds` |

**Hitch:** Overlay-off cab on `2.6.16.12` was `feature=15`. `2.6.16.13` cab drive **`feature=0`**. Spawn/yard walk still `feature=16 load=2`. Look class H67/H72 unchanged. H105 closed by H107.

---

## Session 2026-08-23 — 6.17 PNG icons (`2.6.17.2`)

**Setup:** Career MF. UMM `2.6.17.1` then `2.6.17.2`. v1 Icons PNGs + dark plate. Probe 100 ms.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H109 | Cab / yard after PNG | cab windows `feature=0` (`max=43–98`); spawn earlier `feature=29 load=3 max=98`; look `below` small `max` 59–98 | — | PNG load once in `EnsureStyle`. Cab class matches H107 | **not worse** vs H107 | `Smoke_yard_markers_are_48px_named_pngs_with_dark_plate` |

**6.17 smoke:** v1 loco/house/pin art PASS. MU one-tap 9% PASS (`2.6.17.2`). `T2 ar-icons …=png`. Radar max 3 + own-consist skip = v1. Landed on `main`.

---

## Session 2026-08-23 — 6.16.14 LastLoco trainset exclude (`2.6.16.14`)

**Setup:** Career MF, probe **100 ms**. UMM `2.6.16.14`. Two DE2s MU'd on the turntable; hop-off; drive out of station zone.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H108 | Cab / yard MU | cab windows `feature=0 load=0` (`max=48–81`); spawn `feature=16 load=2`; look `below` high `max` 51–99 | — | Trainset exclude is HashSet id adds on scan, not per-frame. Yard `feature=1–6` is look/overlay class | **not worse** vs H107 | `Smoke_on_foot_last_loco_excludes_mu_mate_from_radar` |

**6.16.14 smoke:** MU mate has no amber; distant S282A / DE6 stay amber. Cyan LOCO on own DE2. Log `T2 loco-radar: … LeftLoco city=MF … excl=2 n=3`. Story **6.16.14** Tier 2 **PASS**.

---

## Session 2026-08-24 — 6.18 Rear/Front proximity (`2.6.18`)

**Setup:** Career SW then MF. UMM `2.6.18`. Probe 100 ms. Cab Front/Rear chip.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H110 | Cab shunt with proximity | cab windows `feature=0` (`max=42–66`); spawn `feature=12 load=2 max=100` (later `feature=20 load=1`); look 110–196 ms | — | 10 Hz NonAlloc overlap only while chip shown; caption key holds. Cab class matches H109 | **not worse** vs H109 | `Smoke_reverse_free_tip_caption_is_rear_not_front`, `Observe_does_not_allocate_when_key_holds` |

**6.18 smoke:** Neutral omit PASS. Front yellow/green PASS. Rear yellow/green PASS. Log `T2 proximity init: end=Front tenths=5 couple=1`. Landed on `main`.

---

## Session 2026-08-24 — 6.19 Derail Risk (`2.6.19.4` product PASS; ship `2.6.19.5`)

**Setup:** Career SW, DE2 then flats. Probe **100 ms**. UMM `2.6.19.4` for the derail; `lead=` logging is `2.6.19.5`.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H111 | Graph / spawn | first window `n=514 fine=166 below=331 max=96 feature=16 load=1` | Feature + LoadScale | Same spawn/load class as H110 (`feature=12 load=2 max=100`) | **game** | — |
| H112 | Cab curve to derail | many windows `feature=0` (`max` ~47–88); one `feature=1` | Feature | Consist walk is 10 Hz + change-only T2. Cab class matches H110 | **not worse** vs H110 | `Smoke_loco_de2_L061_trip_at_threshold_is_red_100`, `Smoke_wagon_88_beats_lead_12` |

**6.19 smoke:** Always-on Derail Risk PASS. Yellow then red; `risk=99` then vanilla `DERAILED! LocoDE2 L-061` at buildup 0.600. Hide-below-15 rejected earlier (bar reflow). Coupler HUD stays cut. Landed on `main`.

---

## Session 2026-08-24 — 6.20 Job preview (`2.6.20` product; ship `2.6.20.1`)

**Setup:** Career SW office + walk to Regular edge. Probe **100 ms**. UMM `2.6.20` then `2.6.20.1` origin-yard patch.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H113 | Graph / spawn | `feature=18 load=2 max=96` then reload `feature=17 load=1 max=99` | Feature + LoadScale | Same spawn class as H111 (`feature=16 load=1 max=96`) | **game** | — |
| H114 | Cab / walk with Preview | cab windows `feature=0` (`max` ~59–78); walk `feature=0–5` | Feature | 4 Hz inventory + change-only T2. Cab class matches H112 | **not worse** vs H112 | `Smoke_hold_overview_emits_T2_job_appear_preview`, `Smoke_preview_out_when_past_regular_edge` |
| H115 | Office re-smoke 2.6.20.1 | office `feature=3–4` `max=42–55` | Feature | Same on-foot class | **not worse** | `Smoke_sw_su_ticket_at_sw_office_uses_job_id_origin_not_chain_dest` |

**6.20 smoke:** Preview / license / Cancelled PASS. Office SU dest-yard OUT was a find; `2.6.20.1` `yard=SW` `preview=910`. Landed on `main`.

---

## Session 2026-08-24 — 6.21 Job-car AR (`2.6.21.6`)

**Setup:** Career SW. Probe **100 ms**. UMM `2.6.21.6` (FOV hop accepted). Throttle chatter hotfix is the same ship (`2.6.21.3`/`2.6.21.4`).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H116 | Graph / spawn | `feature=13 load=2 max=100` (earlier 6.21 ships `feature=15 load=1–2 max=95–99`) | Feature + LoadScale | Same spawn class as H113 | **game** | — |
| H117 | Cab Incremental after chatter fix | cab windows `feature=0` (`max=42`; earlier 6.21.4 `max=42–90`) | Feature | Harmony Prefix is per Tick, not per-frame alloc. Cab class matches H114 | **not worse** vs H114 | `Smoke_cab_incremental_chatter_does_not_reclimb`, `Smoke_on_consist_does_not_write_throttle_indy_train` |
| H118 | On-foot job-car walk | `feature=1–3` `max=46–47` | Feature | 0.25 s Keep rebuild + overlay slots. Look class matches H67/H72 (open) | **not worse** | `Smoke_walk_along_consist_pin_follows_nearest_car`, `Smoke_beside_consist_pin_stays_on_near_car_in_fov` |

**6.21 smoke:** Purple spur pin PASS (good enough — hops at next car center). GO hide PASS (`2.6.21.1`). Throttle stay PASS (`2.6.21.4`). Log `T2 job-car-ar: scan job=SW-FH-82 taken=1 n=1`. Epic **6** closed.

---

## Session 2026-08-25 — 7.1 Three-Gate (`2.7.1.6`)

**Setup:** Career after wipe/restore; YMS + Booklet + IJO + ZCouplers. Probe **100 ms**. UMM `2.7.1.6`. Steam `-nonvr`.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H119 | Graph / spawn | first summary `feature=6 load=2 max=94`; later `feature=9 load=1` | Feature + LoadScale | Same spawn/load class as H116 (`feature=13 load=2`) | **game** | — |
| H120 | Cab Three-Gate / Numpad | cab windows `feature=0 load=0` (`max` ~67–68) | Feature | Soft writes + Unity KeyCode after world-ready. Cab class matches H117 | **not worse** vs H117 | `Smoke_numpad_enter_cycles_reverser_on_loco_and_wagon`, ThreeGateWrite tests |
| H121 | On-foot / mixed | `feature=1–3` `max` ~90 | Feature | Ctrl chords + HUD. Look class matches H67/H72 (open) | **not worse** | `Smoke_tool_keys_require_control_chord`, `Smoke_loading_screen_hides_hud_before_world_stream_complete` |

**7.1 smoke:** Three-Gate apply PASS (`reverser`×3, `tm-fuse`×1). Load HUD gate + mouse reload PASS. Ctrl+Home/End PASS. Numpad Enter in cab PASS. NRE/Rewired-uninit **0**. Duplicate save UID spam still open (not this ship). Landed on `main`.

---

## Session 2026-08-25 — 7.2 Thermal governor (`2.7.2`)

**Setup:** Career SW DE2. Probe **100 ms**. UMM `2.7.2`. Steam `-nonvr`. Cloud off.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H122 | Graph / spawn | first summary `feature=8 load=1 max=100` | Feature + LoadScale | Same spawn/load class as H119 (`feature=6 load=2 max=94`) | **game** | — |
| H123 | Cab thermal cap | cab windows `feature=0 load=0` (`max` 43–91; thermal window `max=54`) | Feature | Cached Three-Gate write delegate; cap tick alloc-free. Cab class matches H120 | **not worse** vs H120 | `Smoke_warning_hot_soft_rolls_throttle_toward_75`, `Smoke_thermal_hot_above_cap_three_gate_applies_soft_write` |
| H124 | On-foot / mixed | `feature=1–2` `max` 44–97 | Feature | Look class matches H67/H72 (open) | **not worse** | `Smoke_cap_release_when_cool` |

**7.2 smoke:** Warning soft-cap PASS (`100→81`, `T2 thermal: soft-cap → 0.75 (Warning)`). Critical also logged. Cap release PASS. TMS Dead after dwell in yellow (cap eases, does not immortalize). NRE **0**. Landed on `main`.

---

## Session 2026-08-26 — 7.3 Auto-brake governor (`2.7.3`)

**Setup:** Career SW DE2. Probe **100 ms**. UMM `2.7.3`. Steam `-nonvr`. Cloud off.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H125 | Graph / spawn | first summary `feature=5 load=1 max=92` | Feature + LoadScale | Same spawn/load class as H122 (`feature=8 load=1 max=100`) | **game** | — |
| H126 | Cab auto-brake apply | cab windows `feature=0 load=0` (`max` 45–98; apply window `max=56`) | Feature | Cached Three-Gate write delegate; park tick alloc-free. Cab class matches H123 | **not worse** vs H123 | `Smoke_shutdown_soft_rolls_brakes_and_throttle`, `Smoke_shutdown_three_gate_applies_soft_write` |
| H127 | On-foot / mixed | `feature=1` `max` 45–90 | Feature | Look class matches H67/H72 (open) | **not worse** | `Smoke_engine_start_does_not_auto_release` |

**7.3 smoke:** Shutdown apply PASS (`T2 autobrake: applying` → `apply done` at train+indy 100 / thr 0; two cycles). Start does not dump air. Apply at ~20 km/h still rolled full. NRE **0**. Landed on `main`.

---

## Session 2026-08-26 — 7.4 Auto-coupler (`2.7.4.1`)

**Setup:** Career SW DE2. Probe **100 ms**. UMM `2.7.4.1`. Steam `-nonvr`. Cloud off. ZCouplers 2.3.5 still loaded (knuckle physics; not this story).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H128 | Graph / spawn | first summary `feature=3 load=1 max=96` | Feature + LoadScale | Same spawn/load class as H125 (`feature=5 load=1 max=92`) | **game** | — |
| H129 | Cab drive after couple | window `feature=0 load=0 max=80` | Feature | 10 Hz couple tick idle when linked. Cab class matches H126 | **not worse** vs H126 | `Smoke_already_linked_does_not_write`, `Observe_does_not_allocate_when_couple_holds` |
| H130 | On-foot / mixed | `feature=1` `max` 42–76 | Feature | Look class matches H67/H72 (open) | **not worse** | `Smoke_off_train_does_not_couple` |

**7.4 smoke:** `2.7.4` FAIL (TryCouple at Rear 3.9 m; speed 1→23; DE2 totaled). `2.7.4.1` PASS: green ≤0.5 m + ≤8 km/h; crawl couple `couple`→`done` (1→2→4→6, speed 1→0); screenshot Cars 6 **R+** Rear **—**; drive had no extra couple. NRE **0**.

---

## Session 2026-08-26 — 7.5 Derail safety net (`2.7.5.7`)

**Setup:** Career SW DE2. Probe **100 ms**. UMM `2.7.5.7`. Steam `-nonvr`. Cloud off. Limit stayed `120 auth=default next=40` (posted take never 60).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H131 | Graph / spawn | first summary `feature=5 load=1 max=83` | Feature + LoadScale | Same spawn class as H128 (`feature=3 load=1 max=96`) | **game** | — |
| H132 | Cab without intervene | windows `feature=0 load=0` (`max` 71–96) | Feature | Derail-only gate; no Next-40 yank. Cab class matches H129 | **not worse** vs H129 | `Smoke_60kmh_derail_40_does_not_trip`, `Smoke_hud_120_next_40_derail_44_does_not_cap` |
| H133 | Cab while intervening | 2.7.5.5 gov-on was `feature=14 max=100`; 2.7.5.7 summaries after yank stayed `feature=0` | Feature | Change-only T2 + Three-Gate. End wreck `feature=2 max=99` | **not worse** vs H129 | `Smoke_derail_65_idles_throttle_and_raises_air` |

**7.5 smoke:** PASS. Under 65 % Derail at 50–65 km/h no `soft-cap`. Three trips when risk crossed 65 (74 / 72 / 69→101). Posted/Next unused as cap. Speed-hold deferred to **10.1**. NRE **0**.

---

## Session 2026-08-26 — 8.1 Google Maps desk (`2.8.1.1`)

**Setup:** Career. Probe **100 ms**. First drive UMM `2.8.1` (dest armed 6.11 PathCheck). Re-smoke UMM `2.8.1.1`. Steam `-nonvr`. Cloud off.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H134 | Graph / spawn | `2.8.1.1` first summary `feature=4 load=1 max=98` | Feature + LoadScale | Same spawn class as H131 (`feature=5 load=1 max=83`) | **game** | — |
| H135 | Cab after Maps dest (`2.8.1`) | windows `feature=11–32` `max=100` after `T2 path init: Path 6 switch` on the same click as dest set | Feature | Maps dest was a facade over `PathCheckSession`; `#Y` origin changes rebuilt adjacency + BFS | **closed** in `2.8.1.1` | `Set_yard_and_track_does_not_arm_end_path_check`, `Smoke_maps_dest_does_not_replace_end_path_check` |
| H136 | Cab after Maps dest (`2.8.1.1`) | after dest `feature=0` `max=49`; desk close `feature=2` | Feature | PathCheck uncoupled from Maps dest. Cab class matches H132 | **not worse** vs H132 | same as H135 |
| H137 | Cab 45–65 km/h (`2.8.1.1`) | `feature=7–11` with look-at `#Y` + 7.5 `soft-cap` at Derail ~63 % | Feature | Posted FoT / gov band (H87 class), **not** dest-BFS | **not** dest-armed class | H87 / 7.5 harvest |

**8.1 smoke:** Desk PASS (`open` / catalog 22/288 / dest / recheck / close). `2.8.1` hitch **worse** (H135). `2.8.1.1` re-smoke: dest `HMB-B7I` then `SM-B1O`; **zero** `T2 path init`; after dest cab `feature=0`. Facing 60 board / Next metres miss is **6.9** (TECH_DEBT), not this ship. NRE **0**.

---

## Session 2026-08-26 — 6.10 FILO posted Limit (`2.8.1.16`)

**Setup:** Career SW DE2. Probe **100 ms**. UMM `2.8.1.16`. Steam `-nonvr`. Isolates off. Limit takes `auth=posted`.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H138 | Spawn / FoT warm | `fotMs=107` first summary `feature=4 load=1` | Feature + LoadScale | Spawn `FindObjectsOfType<SignDebug>` once | **game** | `ShouldEmptyFot` false |
| H139 | Cab Limit tick (isolated) | `2.8.1.15` IsolateLimitTick on still `feature=4–15` | Feature | Limit SetTravel/Tick not the leftover | **exonerated** | `Smoke_standstill_tick_freezes_along_and_sticky` |
| H140 | Cab with HUD on (`2.8.1.16`) | typical `feature=0–4`; late `15–17` `max≈100` | Feature | Quiet EventBus + km/h LogAhead; late windows AR/gadgets/controls | **not** 2.8.1.13 `feature=23`; leftover **out of 6.10** | `Smoke_observe_ignores_roster_count_only_change`, `ShouldLogAhead_only_on_sticky_or_next_kmh_change` |

**6.10 FILO smoke:** Chips PASS (takes 50/40/60; Next after lock). Hitch not 23. Parallel Next metres **deferred**. Landed `main` @ CMPH 2026-08-26. NRE **0**.

---

## Session 2026-08-26 — 8.2 Google Maps route + Align (`2.8.2`)

**Setup:** Career SW. Probe **100 ms**. UMM `2.8.2`. Steam `-nonvr`. Desk + Align + cab drive.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H141 | Desk open / first Set dest | first summary `feature=6 load=1 max=94` | Feature + LoadScale | Graph warm + route worker; same class as H138 | **game** | — |
| H142 | Cab after Align + drive | typical `feature=0–4`; one window `feature=14 max=100` | Feature | H140 leftover class (AR/gadgets), not route/Align | **not worse** vs H140; **not** 23 | `Smoke_route_prefers_through_lane_over_spur` |
| H143 | Cab steady windows | most summaries `feature=0–1` `max=41–69` | Feature | Gold gate held after desk close | **not worse** | — |

**8.2 smoke:** PASS. SW→SW-B1S route + Align `threw 6`. `#Y` TT→SM `no path` logged — **8.4–8.5** / TECH_DEBT. Desk Path/ETA/Facing OK; live always-on route HUD **not shipped**. NRE **0**.

---

## Session 2026-08-27 — 8.3 Digital Switch List (`2.8.3` / `2.8.3.1`)

**Setup:** Career SW. Probe **100 ms**. UMM `2.8.3` then polish `2.8.3.1`. Steam `-nonvr`. Desk Per job + Align/Next. Job SW-FH-82.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H144 | Spawn / settle | `feature=4 load=0 max=95` → `feature=0` | Feature | Same spawn class as H141 (lighter) | **not worse** | — |
| H145 | Desk Switch List Load / Align / Next | `feature=4–7 max≈99`; Align-threw window `feature=3` | Feature | Desk + route worker; H141 class | **same class** | `SwitchListPlannerTests` |
| H146 | Cab / idle after SL | `feature=0–3`; several windows **`feature=0`** | Feature | Gold 8.x gate held | **not worse**; gold met | — |

**8.3 smoke:** PASS. Load 3 steps; Prep Align clear; Next→Transit/Delivery; Delivery Align `threw 6`. Manual Next only. Per job footer job-id polish in `2.8.3.1`. NRE **0**.

---

## Session 2026-08-27 — 8.4 Town turntable dest (`2.8.4`)

**Setup:** Career SW DE2 on SW-B3I. Probe **100 ms**. UMM `2.8.4`. Steam `-nonvr`. Desk Route Turntable Set dest + Align + cab.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H147 | Spawn / settle | `feature=4 load=0 max=89` | Feature | Same class as H144 | **not worse** | — |
| H148 | Desk Set dest TT (FoT once) | `feature=3–4`; Align window `feature=3` | Feature | Cached FoT; single Compute (no v1 multi-leg stack) | **same class** as H145 | `Smoke_SetDest_Turntable_binds_session_yard_and_anonymous_track` |
| H149 | Cab after Align | `feature=0–1`; gold **`feature=0`** | Feature | 8.x gold gate held | **not worse** | — |

**8.4 smoke:** PASS. `TT FoT=11` · dest `SW`/`#Y-#S1774#T` · Path OK · Align `already clear`. Set Reverse = cab→pin gear only (multi-leg → **8.5**). Fat ETA (`cost=639s`) = `#Y` inflation accept. NRE **0**.

---

## Session 2026-08-27 — 8.5 Multi-step Maps (`2.8.5` / `2.8.5.1`)

**Setup:** Career SW DE2. Probe **100 ms**. UMM `2.8.5` then Clear patch `2.8.5.1`. Steam `-nonvr`. Per job Load SW-FH-82 + Clear.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H150 | Desk Load Switch List (inject + path) | `feature=3 max=54`; one `hitch-spike` 207 ms on load | Feature | Inject + sync PathPlan; H145/H148 class | **same class** | `SwitchListTurnAroundTests` |
| H151 | Desk after Clear / idle | `feature=1–2 max=42–67` | Feature | Quiet after wipe | **not worse** | `Smoke_Clear_also_drops_switch_list_steps` |
| H152 | On-foot look (desk session) | spikes ~100–155; one window `feature=8 max=88` | Feature | H67/H72 look class | **open** | — |

**8.5 smoke:** PASS. `inject TurnAround → #Y-#S1774#T` · `loaded SW-FH-82 · 4 steps` · Path OK / Set Reverse · Clear → `dest clear` + `switch-list: cleared`. YMS NRE **0** (DV BrakeWarning OnDestroy + Bolt SceneVariables on quit ignore). Cab gold not re-measured this desk-only recheck; gate remains `feature=0`.

---

## Session 2026-08-27 — 8.6 Loco turn + Bring (`2.8.6.4`)

**Setup:** Career SW. Probe **100 ms**. UMM through `2.8.6.4`. Turn DE2 + drive; Bring DH4 earlier; coupled refuse edge.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H153 | Yard / desk Turn+Bring | `feature` 1–8; max ~63–100 | Feature | Desk/look class | **same class** | `LocoRerailPolicyTests` |
| H154 | Cab after MoveToTrack turn | `feature=0 load=0` | Feature | 8.x gold gate held | **not worse** | — |
| H155 | On-foot look | open (not re-measured) | Feature | H67/H72 | **open** | — |

**8.6 smoke:** PASS. `turn · DE2 · MoveToTrack` · drive correct order · coupled refuse · Bring DH4 Lock/place earlier PASS. Prior fails: TeleportTrainset spin; on-rails Rerail no-op. YMS NRE **0**.

---

## Session 2026-08-29 — 8.7 route pin + CLEARED (`2.8.7.31`)

**Setup:** Career SW. Probe **100 ms**. UMM `2.8.7.31`. Steam `-nonvr`. B4L→TT / Path 1 switch. Desk closed after reverse hide. Align/Next via **Ctrl+PageUp** / **Ctrl+PageDown**.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H156 | Spawn / graph | first summary `feature=4 load=0 max=82`; spikes 924 / 220 | Feature + LoadScale | Same spawn class as H147 | **not worse** | — |
| H157 | Cab reverse to pin (desk hid) | windows `feature=0` `max=58–77` | — | Reverse force-close + pin AR diet; isolation also `feature=0` through frog | **not worse** vs H154 / 2.8.7.29 cruise | `ArPinHitchGateTests` |
| H158 | Align/Next chords | window `feature=6 max=100` | Feature | Throw + Path, not open-desk IMGUI (old Align-with-desk `26`) | **not worse** vs desk Align | `Smoke_8_7_align_next_chords_are_tool_keys` |
| H159 | Forward after Next | window `feature=8 max=98 below=21` | Feature | Leftover look/path/FILO class; not Maps desk | **worse** than reverse gold; **out of 8.7** | isolate later |
| H160 | On-foot look | not this cab session | Feature | H67/H72 | **open** | — |

**8.7 smoke:** PASS. `latch 990152 reverse=1` · `hitch hide reverse` · `CLEARED` · `chord align` threw 1 · `chord next` `hide next`. Isolation: switch alone `feature=0`; open desk = mush `below`; dest+reverse+closed desk frog `feature=0`. Play ritual = Set dest stopped → close desk → roll; chords at CLEARED. NRE **0**.

---

## Session 2026-08-30 — 9.1 PID hold (`2.9.1.12`)

**Setup:** Career SW. Probe **100 ms**. UMM `2.9.1.12`. Steam `-nonvr`. B4L→TT / Path 1 switch. Desk closed after Set dest.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H161 | Spawn / graph | first summary `feature=6 load=0 max=93`; spikes 843 / 172 | Feature + LoadScale | Same spawn class | **not worse** | — |
| H162 | Cab reverse hold | windows `feature=0` `max=51–70` | — | PID FixedUpdate 32000 + MUOverride | **not worse** vs H157 | `HtpPidStraightHoldTests`, `PidSpeedWriteTests` |
| H163 | On-foot look | not this cab session | Feature | H67/H72 | **open** | — |

**9.1 smoke:** PASS. `gear` → bleed → `thr-on` → climb → `apply thr=0 indy=27` → hold → `CLEARED`. Known debt: thr 9→100 by ~10 km/h (slip); `motors=Dead` after CLEARED; snappy thr↔indy. NRE **0** (YMS).

---

## Session 2026-08-30 — 9.1 takeoff / coast (`2.9.1.14`)

**Setup:** Career SW. Probe **100 ms**. UMM `2.9.1.14`. Steam `-nonvr`. Desk SW+Turntable default; mouse RequirePointer while open.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H164 | Spawn / cab | hitch-summary **not pasted** (player: smooth) | — | Expect same class as H161/H162 | **assumed not worse** | `HtpPidStraightHoldTests` takeoff/deadband; `YmsRouteSessionsTests` |
| H165 | On-foot look | not this cab session | Feature | H67/H72 | **open** | — |

**9.1.14 smoke:** PASS. Idle until Set dest; gradual takeoff; ±2 coast (~27 before indy) accepted; desk SW/TT + mouse; Motors OK. NRE **0** (YMS).

---

## Session 2026-09-01 — 9.1.3 span (`2.9.1.37`)

**Setup:** Career SW → FM dest. Probe **100 ms**. UMM **`2.9.1.37`**. Steam `-nonvr`. Long drive past SW leave boards.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H166 | Spawn / graph | `feature=7–11 load=0–1 max=89–97` | Feature + LoadScale | Same spawn class as H161 | **not worse** | — |
| H167 | Cab drive (span fix) | windows `feature=0` `max=41–84` | — | Span cached; no per-frame Bezier search | **not worse** vs H162 | `HtpCurvedSweepTests` |
| H168 | Maps desk open | spike **5666** at harvest | LoadScale | Graph harvest 2637 units | **game** | — |

**9.1.37 smoke:** PASS SW leave **40→60**. `take 40@0` · `take 60@0` · all `src=span`. Long run: additional takes; tunnel **30** absent (roster not refreshed). NRE **0** (YMS).

---

## Session 2026-09-01 — 9.1.3 Win 5.1 (`2.9.1.39`)

**Setup:** Career SW → FM dest. Probe **100 ms**. UMM **`2.9.1.39`**. Long drive; tunnel **30** gold.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H169 | Spawn / first window | `feature=6 max=92` | Feature | Same spawn class as H166 | **not worse** | — |
| H170 | Cab drive (travel refresh) | windows `feature=0` `max=73–87`; one `feature=1` | — | Travel refresh ~146 ms FoT | **not worse** vs H167 | `NeedsTravelRefresh` |
| H171 | Pause tail | `feature=2 max=79` | Feature | Pause menu class | **game** | — |

**9.1.39 smoke:** PASS tunnel **30**. `warm · travel` ×2 · `take 30@0 src=span` · `limit change: 30`. NRE **0** (YMS).

---

## Session 2026-09-01 — 9.1.3 CMPH land (`2.9.1.39`)

**Setup:** Merge **`feature/9.1.3-win0-graph-dump`** → **`main`**; feature branch kept on origin.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H172 | CMPH land (no new cab session) | — | — | Prior H169–H171 | **not worse** | — |

---

## Session 2026-09-01 — 13.1 inbound TT pin (`2.13.1.10`)

**Setup:** Career SW-FH-82, face into Exit. UMM **`2.13.1.10`**. Desk Per job Load. Player reported product PASS (inbound pin); no `T2 hitch-summary` pasted.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H173 | 13.1 inbound TT cab | not measured | — | Product PASS only | no hitch-summary | `TryPinCorridorDest` / loco-side approach |

**13.1.10 smoke:** PASS inbound. Step 1 **Past switch → SW-B4L**, pin **990152**, CLEARED, Next → TT. Step 3 Prep **Path 7 switch** / no pin = next slice.

---

## Session 2026-09-02 — 13.1 reverse-to-TT + leave sawtooth (`2.13.1.20`)

**Setup:** Career SW-FH-82, face into Exit Load. Probe **100 ms**. UMM **`2.13.1.20`**. Desk Switch List through 7/7 Align.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H174 | Spawn / first window | `feature=6` `load=0` class | Feature | Same spawn class as H169 | **not worse** | — |
| H175 | Cab drive (list open / closed) | windows `feature=0` `max=41–74` | — | Switch List Next/Align; PID idle | **not worse** vs H170 | `Smoke_13_1_*` planner/runner |
| H176 | Transit Align throw | `feature=4` `load=1` | Feature + LoadScale | Align throw burst | **same class** (not a new cab drive class) | — |
| H177 | On-foot look | not this cab session | Feature | H67/H72 | **open** | — |

**13.1.20 smoke:** PASS 7/7. `inject TurnAround → #Y-#S1774#T (face into Exit)` · `loaded SW-FH-82 · 7 steps` · leave `#Y-#S1512#T` · `align step 7 Delivery` · `T2 align: already clear`. NRE **0** (YMS).

---

## Session 2026-09-02 — 13.1 CMPH land (`2.13.1.20`)

**Setup:** Merge **`feature/13.1-reverse-to-tt`** → **`main`**; feature branch **kept** on origin. No new cab session.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H178 | CMPH land (no new cab session) | — | — | Prior H174–H177 | **not worse** | — |

---

## Session 2026-09-02 — 9.1.4 Next-chip (`2.9.1.40`)

**Setup:** Career, Route dest **CS-A3L**, Cruise on, desk **Hide**. Probe **100 ms**. UMM **`2.9.1.40`**.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H179 | Spawn / first window | `feature=4` `load=2` max=99 | Feature | Same spawn class as H174 | **not worse** | `HtpNextChipWalkTests` |
| H180 | Cab drive (desk closed) | windows `feature=0` `max=44–67` | — | Next-chip Evaluate; PID hold | **not worse** vs H175 | `HtpNextChipWalkTests` |
| H181 | On-foot look | 102–198 ms | Feature | H67/H72 | **open, not worse** | — |

**9.1.4 smoke:** PASS. `take 40@0` → `sticky=40 next=60 376m` → `next=60 120m` → `take 60@0`. Zero `next=—`. NRE **0**. Pause spikes (~195 s) are menu.

---

## Session 2026-09-02 — 13.2.1 couple auto-advance (`2.13.2.1`)

**Setup:** Career SW-FH-82, Maps desk Switch List, Prep reverse into one car. Probe **100 ms**. UMM **`2.13.2.1`**.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H182 | Spawn / first window | `feature=4` `load=1` max=96 | Feature | Same spawn class as H179 | **not worse** | `HtpCoupleAutoAdvanceCp4Tests` |
| H183 | Cab drive (couple window) | `feature=0` `max=48` | — | 7.4 Done + list Next | **not worse** vs H180 | `HtpCoupleAutoAdvanceCp4Tests` |
| H184 | On-foot look | 101–130 ms spikes | Feature | H67/H72 | **open, not worse** | — |

**13.2.1 smoke:** PASS. `autocouple: couple` → `done` → `couple-next` → `next · step 6 Transit`. Prep → Transit. Cars: 5. NRE **0**.

---

## Session 2026-09-02 — 13.2.2 Prep track arrival (`2.13.2.2`)

**Setup:** Career SW-FH-82, Maps desk Per job, Prep dest SW-C1O before couple. Probe **100 ms**. UMM **`2.13.2.2`**.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H185 | Spawn / first window | `feature=5` `load=2` max=96 | Feature | Same spawn class as H182 | **not worse** | `HtpPrepTrackArrivalTests` |
| H186 | Cab drive (arrival window) | `feature=0` `max=57` | — | 10 Hz dest-track poll | **not worse** vs H183 | `HtpPrepTrackArrivalTests` |
| H187 | On-foot look | 138–157 ms spikes | Feature | H67/H72 | **open, not worse** | — |

**13.2.2 smoke:** PASS. `T2 prep: at track`. Desk **`▶ 5/7 · Set Reverse · Prep → SW-C1O · at track`**. List stayed on Prep. NRE **0**.

---

## Session 2026-09-02 — 13.6.1 remote take (`2.13.6.1`)

**Setup:** Career SW-FH-82 Preview, Load Switch List 7 steps. Probe **100 ms**. UMM **`2.13.6.1`**. GO on Transit (Take button not used).

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H188 | Spawn / first window | `feature=7` `load=2` then `feature=6` `load=0` | Feature | Same spawn class as H174 | **not worse** | — |
| H189 | Cab drive after take | windows `feature=0` `max=41–71` | — | Taken job bar; PID idle | **not worse** vs H186 | `Smoke_13_6_1_*` |
| H190 | On-foot look | not this cab session | Feature | H67/H72 | **open** | — |

**13.6.1 smoke:** PASS. `T2 job-take: request job=SW-FH-82 src=go` · `taken=1` · job bar RED→GO. No Preview OUT. NRE **0** (YMS).

---

## Session 2026-09-03 — 13.4 autonomous transit thin (`2.13.4.7`)

**Setup:** Career SW-FH-82, Maps desk Per job, Prep GO approach + haul Transit take. Probe **100 ms**. UMM **`2.13.4.7`**.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H191 | Spawn / cab / on-foot | no hitch-summary this turn | — | Player PASS without pasted summary | **unknown** | `Smoke_13_4_prep_approach_go_arms_pid_without_take` |

**13.4 smoke:** PASS. Prep approach GO; haul take after Prep; expected crash noted. Clear-line pin deferred to **8.7** revisit. NRE not re-checked this writeup.

---

## Session 2026-09-04 — 13.4 yard chain CLEARED crawl-stop (`2.13.4.10`)

**Setup:** Career SW-FH-82, held + Load, step 1 Past-switch → CLEARED → step 2 to-TT. Probe **100 ms**. UMM **`2.13.4.10`**.

| Id | What was slow | dt (ms) | Band | Hypothesis | Status | TDD |
|----|---------------|---------|------|------------|--------|-----|
| H192 | Cab after CLEARED stop | feature=0–1 max=65–82 | Feature/Below | Yard chain crawl-stop then ArmGo; not worse vs prior cab | **not worse** | `Smoke_13_4_yard_chain_*` goStopActive gate |

**13.4.10 smoke:** CLEARED real stop PASS (`go-stop` → `go-stop done` → `arm-go · step 2`). Overshot TT (no rail stop yet) — expected; **`2.13.4.11`** adds Stop GO on TT.
