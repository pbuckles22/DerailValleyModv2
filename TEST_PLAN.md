# Test plan

Two-tier strategy for *Yard Master Suite v2*. Story IDs match [PM_PLAN.md](PM_PLAN.md). Keep in sync with [AGENT_HANDOFF.md](AGENT_HANDOFF.md).

| Tier | When | Gate |
|------|------|------|
| **1** | Every logic change | `dotnet test` + Release build |
| **2** | In-world UMM behavior (after packaging) | Deploy + Player.log `T2 …` + on-screen HUD |

**Merge-ready today:** Tier 1 (`dotnet test` + Release build). Stories that touch in-world UI also need Tier 2 before checking Done in PM_PLAN. Deploy with `package.ps1 -NoArchive` before asking for smoke. First in-world smoke (**1.4** hitch probe) passed 2026-08-12.

---

## Tier 1 — Fast feedback

```bash
dotnet test YardMasterSuite.sln
dotnet build YardMasterSuite.sln -c Release
```

**Pass (intended):** All unit tests green; 0 build errors; `build/YardMasterSuite.dll` present.

Pure helpers live in `YardMasterSuite.Core` (no Unity/game refs). Smoke-found gates must land here ([.cursor/rules/smoke-gates-tier1-ci.mdc](.cursor/rules/smoke-gates-tier1-ci.mdc)).

---

## Tier 2 — In-game smoke

Requires UMM (`Mods\` under the game root) and `package.ps1`. Deploy before asking for smoke ([deploy-before-smoke.mdc](.cursor/rules/deploy-before-smoke.mdc)). **How to ask:** that rule → *How to ask* (where / what they see / steps / PASS vs FAIL / log / UMM Version). Do not only name `T2` lines.

```powershell
dotnet build YardMasterSuite.sln -c Release
powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

### Evidence

| Source | Where | Proves |
|--------|--------|--------|
| **Player.log** | `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log` | Load, toggle, discrete `T2 …`, exceptions |
| **UMM Logs** | Mod Manager → Logs | Same lines (subset) |
| **HUD** | In-world Display Shell | Compass + top bar; STN on office; LOCO on last loco when on foot; no launcher HUD |

**1.4 hitch probe:** silent on the launcher / during load (no world session). In-world, a hitch **over 100 ms** may emit `T2 hitch-spike: dt=…ms` (optional `gc0=+N`). Yard frames under 100 ms are silent on that line. At most one spike log per second. No per-frame logs. Every ~30 s in-world (and when leaving the world / toggling the mod off) emit one `T2 hitch-summary: n=… fine=… below=… max=…ms gc0=… feature=… load=…` so the 40–99 ms band is countable. `below` is 40–99 ms; `fine` is faster than 40 ms.

**2.1 loco state listener:** after activate, `[YMS v2] Loco listener running.` Board a locomotive → `T2 loco-board: id=…`. Leave it (on foot or onto non-loco) → `T2 loco-unboard: id=…`. Same loco is silent. No per-frame logs.

**2.2 control telemetry:** after activate, `[YMS v2] Control telemetry running.` Board a loco. Move **one lever at a time**:

- throttle → `thr=` changes; `indy` / `train` / `eng` stay put
- independent (indy) → `indy=` changes; `train` stays put
- train brake → `train=` changes; `indy` stays put
- engine / dynamic brake (if the loco has one) → `eng=` changes; DE2 usually logs `eng=na`
- reverser → `rev=` changes (`50` = neutral)

`raw=` is the 0–1 values read from the game that tick. Still levers are silent. Unboard stops sampling.

**2.3 trainset topology:** after activate, `[YMS v2] Consist listener running.` Board a loco → `T2 consist: cars=… t=…` (tonnes). Couple a car → `cars` goes up and `t` changes. Uncouple (including **on foot** after leaving the cab) → `cars` goes down **before** reboard. Cargo load without couple is silent. Unboard does **not** drop consist sampling; a different loco or deactivate does. Reboard of the same consist is silent.

**3.1 HUD manager:** after activate, `[YMS v2] HUD running.` and `[YMS v2] Heading listener running.` Load into the world (not the menu):

- Compass bar at the top: `Heading N` (16-point; no degrees). Look around → chip changes. Log: `T2 heading init: N` then `T2 heading change: …` at most every 2 s (HUD updates immediately; logs are throttled).
- Board a loco → top bar `cars=… t=… | thr=… indy=… train=… eng=… rev=…` matching the latest consist/controls `T2` lines. DE2 usually `eng=na`.
- Unboard → cab chips drop; consist `cars=` / `t=` stay (on-foot pin-pulls). Couple/uncouple still updates the top bar.
- Launcher / main menu: no HUD. Confirm ship **2.3.1** in **UMM Version**, not an in-HUD chip.

**3.2 AR overlay — Smoke A office glyph (PASS 2026-08-13).** Shipped **2.3.2**. Always-on in the yard job zone.

- **Where:** In a town/yard, **Mod Manager closed**. Marker is the **job office**, not the car beside you (it can sit on whatever is in the line of sight to the office).
- **You should see:** Green square + white **STN**. Looking at the office → STN on that building (`office=object`). Looking away → STN on the **left or right screen edge, mid-height** (`office=edge`). That mid-edge cue is this slice. A top-of-screen slide is **upcoming / not coded** — not part of this smoke.
- **Do:** (1) Load a yard. (2) Face the office. (3) Turn away. (4) Walk onto the office apron (~20 m) — STN hides. (5) Walk away — STN returns. (6) Menu — no STN.
- **PASS if:** STN is visible, tracks office vs edge as above, hides on the apron, silent on the menu. **FAIL if:** no marker while `office=object`/`edge` in the log, or it only appears on nearby cars and never as an edge cue when you turn away.
- **Log:** `[YMS v2] AR overlay running.` then `T2 ar init: loco=— office=object|edge pin=—` and `T2 ar change` at most every 2 s (same throttle as heading). No per-frame / per-meter lines. Hitch: append [PERFORMANCE_LOG.md](docs/PERFORMANCE_LOG.md) (H16–H21 this session).

**3.2 AR overlay — Smoke B own loco (PASS 2026-08-17).** Shipped **2.3.2**. STN from Smoke A stays.

- **Where:** Yard, **Mod Manager closed**. You need a locomotive you have boarded at least once this session (`LastLoco`).
- **You should see:** Cyan square + white **LOCO** on *your* engine when you are **on foot**. Same left/right mid-edge cue as STN when you look away from it. Green **STN** can be on screen at the same time (office).
- **Do:** (1) Board the loco — **LOCO hides** (you are on it). Cab HUD still shows `cars=` / levers. (2) Get out and walk away — **LOCO** appears on that engine (`loco=object`) or on a screen edge (`loco=edge`) if you look away. (3) Walk around; STN still tracks the office. (4) Menu — no markers.
- **PASS if:** LOCO is gone in the cab, back on the engine on foot, edge cue when you turn away from the loco, STN still works. **FAIL if:** LOCO stays on screen while you are in that cab, never appears on foot after unboard, or STN disappears because LOCO was added.
- **Log:** `T2 ar change: loco=— …` when you board; `loco=object` or `loco=edge` on foot. At most one `T2 ar change` per 2 s. Drive a few meters for hitch; append [PERFORMANCE_LOG.md](docs/PERFORMANCE_LOG.md) if `T2 hitch-spike` during that drive.

**3.2 AR overlay — Smoke C edge stack + hitch-summary (PASS 2026-08-17).** Shipped **2.3.2**. Same session as Smoke B is fine.

- **Where:** Yard, **Mod Manager closed**, **on foot**, with both the job office and your last loco in the area.
- **You should see:** When you look **away** so both markers are off-screen on the **same** left or right side: green **STN** and cyan **LOCO** sit **next to each other** on that mid-height edge (one slightly inward). Both labels readable. This is still the mid-edge cue — **not** a top-of-screen bar.
- **Do:** (1) Stand so office and loco are both behind you / off to one side. (2) Confirm two chips, not one mashed label. (3) Face one of them — that one jumps onto the object; the other may stay on the edge. (4) Stay in the world ~30 s or open the pause menu / leave to the station menu.
- **PASS if:** the two edge chips are separated and readable; STN/LOCO still hide on the menu. **FAIL if:** both labels sit on the same pixel (unreadable overlap) while `loco=edge office=edge`.
- **Log:** `T2 ar change: loco=edge office=edge pin=—` while overlapped-side is showing. After ~30 s in-world or on leave/pause-to-menu: `T2 hitch-summary: n=… fine=… below=… max=…ms gc0=… feature=… load=…` (one line, not per-frame). Paste that summary into [PERFORMANCE_LOG.md](docs/PERFORMANCE_LOG.md).

**3.2 AR overlay — Smoke D HUD clearance (PASS 2026-08-17).** Shipped **2.3.2**. Harvest from Smoke C: LOCO at top-left was `ClampToScreen` into the heading bars, then edge-stack pinned that chip to the left margin. Off-screen is now mid left/right only. Look-around object/edge chatter throttled (2 s) + 48 px hysteresis.

- **Where:** Yard, Mod Manager closed, on foot.
- **You should see:** STN/LOCO on the objects when in view; when off-screen, **only** mid-height left/right chips. Nothing in the top-left over `Heading` / `cars=`.
- **Do:** (1) Walk close to the loco and look slightly down / around so the engine wants to leave the top of the view. (2) Look away so both chips share a side (Smoke C still). (3) Stay ~30 s.
- **PASS if:** no marker sits on or above the two HUD bars; shared-side chips still readable mid-edge. **FAIL if:** LOCO or STN appears in the heading / `cars=` corner.
- **Log:** `T2 ar-summary: n=… object=… edgeMid=… edgeTop=0 hidden=…` every ~30 s. **FAIL the log** if `edgeTop` is not 0. Also hitch-summary as in Smoke C.

**Upcoming (not coded — do not treat as this smoke):** top-of-screen slide for off-FOV markers. Pin finder later.

**4.1 Type B mailbox — Smoke A drain probe (PASS 2026-08-17).** Shipped **2.4.1**. No new HUD/AR chrome — this is the worker → queue → main-thread Type A path.

- **Where:** Main menu or yard, **Mod Manager closed** after you confirm Version.
- **You should see:** The same compass / top bar / STN / LOCO as **2.3.2**. Nothing new on screen. No hitch from the mailbox itself.
- **Do:** (1) Enable the mod (or load the game with it on). (2) Confirm **UMM Version** `2.4.1`. (3) Stay on the menu a few seconds, or load a yard — HUD/AR behave as before. (4) Toggle the mod off, then on again — one probe line per activate, not a stream.
- **PASS if:** existing HUD/AR still work; no new marker or chip; one `T2 mailbox: n=1` shortly after activate. **FAIL if:** the game throws, HUD/AR vanish, mailbox lines spam every frame, or Version is still `2.3.2`.
- **Log:** `[YMS v2] Mailbox drain running.` then `T2 mailbox: n=1` once per activate (may be a frame or two later). Empty frames silent. Off → `[YMS v2] Deactivated cleanly.` No YardMasterSuite exceptions.

**4.2 Track graph — Smoke A map + A\* probe (PASS 2026-08-17).** Shipped **2.4.2**. No new HUD/AR chrome. Mapping is silent on the menu; it starts when you load a yard.

- **Where:** Yard, **Mod Manager closed** after you confirm Version. Same compass / STN / LOCO as before.
- **You should see:** Nothing new on screen. No freeze while the world loads. HUD/AR still work.
- **Do:** (1) Confirm **UMM Version** `2.4.2`. (2) Stay on the main menu a few seconds — no `T2 graph start` yet. (3) Load a career/yard. (4) Wait a couple of seconds in the world. (5) Drive or walk; HUD/AR unchanged.
- **PASS if:** the world loads without a hitch freeze from mapping; HUD/AR still work; one `T2 graph start` then one `T2 graph ready` (or `T2 graph fail` if the registry is empty). **FAIL if:** the game hitch-locks for seconds on load, graph lines spam every frame, Version is still `2.4.1`, or HUD/AR vanish.
- **Log:** `[YMS v2] Track graph running.` On world enter: `T2 graph start: units=…` then `T2 graph ready: nodes=… edges=… hops=…` once (`hops=—` if first/last nodes are disconnected — still PASS). No per-track lines. Hitch-summary as usual; paste if `feature` spikes during the first seconds in-world.

**4.3 Geometry scanner — Smoke A current-segment cache (PASS 2026-08-17).** Shipped **2.4.3**. Limit chip arrives in **3.5** (not this smoke).

- **Where:** Yard, **Mod Manager closed** after you confirm Version. Same compass / STN / LOCO as before.
- **You should see:** Nothing new on screen. No freeze when you board or roll onto a new track.
- **Do:** (1) Confirm **UMM Version** `2.4.3`. (2) Stay on the main menu a few seconds — no `T2 geometry` yet. (3) Load a yard on foot — still no geometry line (scanner waits for a boarded loco). (4) Board a locomotive — one `T2 geometry` line. (5) Sit still or roll a few meters on the **same** track — no more geometry lines. (6) Drive onto a **different** track (through a switch or off a yard lead) — one new `T2 geometry` line. (7) Get out — one `T2 geometry: segment=—`.
- **PASS if:** HUD/AR still work; geometry logs only on board / new track / unboard; menu is silent. **FAIL if:** Version is still `2.4.2`, a Limit chip appears, geometry lines spam every frame, or HUD/AR vanish.
- **Log:** `[YMS v2] Geometry scanner running.` After board: `T2 geometry: segment=… limit=… start=… end=…` or `T2 geometry: segment=… limit=—` (straight / no sustained curve — still PASS). Unboard: `T2 geometry: segment=—`. No per-frame lines. Hitch-summary as usual; paste if `feature` spikes on the first board.

**3.3.1 HUD v1 chrome parity — Quick smoke (PASS 2026-08-17).** Ships **2.3.5.1**. Epic **6** matrix: [docs/HUD_v1_Parity_Matrix.md](docs/HUD_v1_Parity_Matrix.md).

- **Where:** Empty yard on foot, then DE2 cab. **Mod Manager closed** after confirming **UMM Version** `2.3.5.1`.
- **You should see:** **Single-box** centered bars (no box-on-box). **On foot in empty yard:** bottom bar **`Heading …` only** — **no** loco bar, **no** `cars=` debug. **In cab:** product labels (`TrainBrake`, `Throttle`, `Speed`, `Limit`, `Cars`) — not `thr=` / `cars=`. Optional look-at bar when crosshair on a car. STN/LOCO AR unchanged.
- **Do:** (1) Full game restart after deploy. (2) Toggle mod once; confirm Version. (3) On foot in empty yard — heading only. (4) Board DE2 — loco bar with product labels. (5) Unboard — loco bar hides again. (6) Drive ~30 s — one `T2 hitch-summary` (`feature=0` expected).
- **PASS if:** single-box chrome, foot hides loco bar, cab product labels, AR OK, hitch-summary clean. **FAIL if:** double bar on heading, debug telemetry labels, consist memory on foot, or `feature` spike.
- **Log (Player.log 2026-08-17):** `T2 usable-train on/off` on look/board/unboard; board → `T2 consist: cars=3 t=74`; unboard → `T2 loco-unboard` + `T2 usable-train off`; cab drive `T2 hitch-summary feature=0 load=0`. No YardMasterSuite exceptions in session.

**Reference smoke — SW-B3I shunter yard (informal, not 3.3.1 gate).** Extra photos from same session while exploring elsewhere. Harvested into **6.3**.

- **Where:** On foot, crosshair on coupled shunter + flatcar consist at **SW-B3I** (log stacks nearby).
- **Observed (3.3.1):** Loco bar visible (`T2 usable-train on`) with misleading **`Mass 0 t | Cars 0`** while look-at bar showed correct per-car / all-cars mass (`Car 18 t | all cars 74 t`). In cab: **`Cars 3 | Mass 74 t`** — correct. `T2 consist` only after board.
- **Tier 1 lock (3.3.1):** `HudShellTests.Smoke_look_at_usable_train_omits_cars_and_mass_when_consist_unknown`. **6.3:** `ConsistTopologyTests.Smoke_shunter_yard_on_foot_look_at_binds_consist_anchor` + `HudShellTests.Smoke_look_at_usable_train_shows_cars_and_mass_when_consist_known`.
- **Other notes:** `T2 look-at bar` repeats while aiming (harvested into **6.2** identity log). `T2 controls: thr=… raw=…` debug lines still in cab listener logs (not HUD product labels). End-of-session pause spike `dt=259835ms` + game Bolt/DV teardown NREs — not YMS.

**6.1 Always-on Clock — Quick smoke (PASS 2026-08-18).** Ships **2.6.1**. Heading stays; **Clock HH:MM** is in-game world time, not your PC clock. Marked / Station / Path are **not** this ship (**6.11–6.12**). UMM shows **2.6.1** even though **2.6.4** already shipped — that is story **6.1**, not a downgrade bug.

- **Where:** Yard on foot, Heading bar visible. **Mod Manager closed** after confirming **UMM Version** `2.6.1`.
- **You should see:** Bottom always-on bar `Heading …  |  Clock HH:MM` (padded, e.g. `Clock 09:05`). No Marked / Station / Path chips yet. Looking away still parks STN/LOCO under Heading (6.4). Launcher / main menu: no Heading, no Clock.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.1` (not `2.6.4`). (3) Load a yard. (4) Read the bottom bar — Heading plus Clock. (5) Compare Clock to an in-world analog clock or the sky (day vs night), not the PC taskbar. (6) Wait for the in-game minute to tick (or sleep / wait in-game) — the chip should change. (7) Exit to Main Menu — bar gone.
- **PASS if:** Clock is on the Heading bar, looks like world time, updates on the minute, menu hides it, Version is `2.6.1`. **FAIL if:** Version is still `2.6.4`, no Clock chip, Clock matches the PC clock but not the world, Clock spam-changes every second, or Heading disappeared.
- **Log / screens (2026-08-18):** Steps 1–7 PASS. Yard: `Heading ESE | Clock 11:49` with loco + look-at bars. Office wall clock: HUD `Heading N | Clock 11:57` then `Heading NNW | Clock 12:01` matching the analog face. Harvest: `Smoke_office_wall_clock_*`. Expected Player.log: `[YMS v2] Clock running.` then `T2 clock init: HH:MM` and one `T2 clock change` per minute tick.

**6.3 Consist on look-at usable train — Quick smoke (PASS 2026-08-17).** Ships **2.6.3**. Fixes the SW-B3I on-foot `Cars 0` / `Mass 0 t` find.

- **Where:** Yard on foot (SW-B3I shunter + flatcar if you still have that consist, or any coupled loco + cars). **Mod Manager closed** after confirming **UMM Version** `2.6.3`.
- **You should see:** Aim at sky → bottom **`Heading …` only** (no loco bar). Aim at the coupled consist → loco bar with **`Cars N`** and **`Mass … t`** that match the look-at bar’s **all cars** total — **not** `Cars 0` / `Mass 0 t`, and **not** missing Cars/Mass while the look-at bar shows a consist total. Board that same consist → same Cars/Mass as on foot.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.3`. (3) On foot, look at empty yard — heading only. (4) Walk up and put the crosshair on a coupled loco consist (shunter + cars is ideal). (5) Read loco-bar Cars/Mass vs look-at **all cars**. (6) Board that loco — Cars/Mass stay the same. (7) Get out while still looking at it — Cars/Mass still correct. (8) Look at the sky — loco bar hides.
- **PASS if:** on-foot look-at shows real Cars/Mass matching the consist; cab matches; look-away hides the loco bar. **FAIL if:** Version is still `2.3.5.1`, on-foot look-at shows `Cars 0` / `Mass 0 t` / omitted chips while look-at bar has an all-cars total, or cab disagrees with the on-foot loco bar.
- **Log (Player.log 2026-08-17):** Version `2.6.3`. On foot before board: `T2 consist: cars=3 t=74` then `T2 usable-train on` (world spawn looking at SW-B3I). One consist line only — board silent (same numbers). Unboard → `T2 loco-unboard` then usable off/on with look. Screenshots: heading-only on gondolas; loco bar `Mass 74 t | Cars 3` on flatbed and shunter matching `all cars 74 t`. No YardMasterSuite exceptions.

**6.2 Look-at polish — Quick smoke (PASS 2026-08-17).** Ships **2.6.2**. **6.14 cut** — cargo is this ship; Job chip stays **6.13**.

- **Where:** Yard on foot at a coupled loco + freight (SW-B3I shunter + flats is ideal). **Mod Manager closed** after confirming **UMM Version** `2.6.2`.
- **You should see:** Crosshair on the **shunter** → look-at bar with **Car N/A** and a **Loco …** type (not FlatbedEmpty). Crosshair on a **freight car** in that consist → **Car 1** or **Car 2** (not Car XX), **Empty Cargo** or a named cargo, **Track SW-B3I** (or that yard’s track id). Pipe / handbrake / couplers still there. Aim at the sky → look-at bar hides; heading stays.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.2`. (3) On foot, look at empty yard — heading only. (4) Aim at the shunter — Car N/A, loco type, no Empty Cargo on the loco. (5) Aim at the first freight, then the second — car number changes 1 → 2; cargo is Empty or a real type, never FlatbedEmpty as a loco chip. (6) Hold aim on one car for ~5 seconds — HUD pipe may tick; you should **not** get a stream of look-at log lines. (7) Look at the sky — look-at bar hides.
- **PASS if:** freight shows a real car number; loco shows N/A + loco type; cargo is Empty or named (not a car-type fake); look-at log is one line per aim change / hide. **FAIL if:** Version is still `2.6.3`, freight is Car XX, freight shows Loco FlatbedEmpty, cargo never appears, or `T2 look-at bar` repeats every fraction of a second while you hold still.
- **Log (Player.log 2026-08-17):** Version `2.6.2`. Spawn looking at consist: `T2 consist: cars=3 t=74` then `T2 look-at bar: car=2 cargo=Forestry Trailers track=SW-B3I`. Shunter: `car=NA cargo= track=SW-B3I`. Freight: `car=1` / `car=2` with `cargo=Forestry Trailers`. Look-away: `T2 look-at bar: hide`. Sixteen identity lines total — no hold-still spam. Screenshots: heading-only; loco `Car N/A | Loco DE2`; Car 1 / Car 2 + Forestry Trailers. No YardMasterSuite exceptions. Bolt teardown NRE on quit — not YMS.

**6.4 AR stack sync — Quick smoke (PASS 2026-08-17).** Ships **2.6.4**. Edge STN/LOCO sit **under** the HUD stack, not beside Heading at mid-screen. On-object markers stay on the office / loco. Not the cut top-band slide.

- **Where:** Yard on foot, Heading-only (look at sky/empty track so no loco/look-at bars). Need a last loco (boarded once) and a job office in view-range. **Mod Manager closed** after confirming **UMM Version** `2.6.4`.
- **You should see:** Green **STN** and cyan **LOCO** on the **left or right edge**, in a row **just below** the Heading bar — not halfway down the screen beside the Heading text. Face the office → STN sits **on the building**. Face your parked loco → LOCO sits **on the engine**. Turn away again → they return under Heading on the edge.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.4`. (3) On foot, look at empty yard — heading only. (4) Turn until STN and/or LOCO are edge cues. (5) Check they sit under Heading, not mid-height. (6) Face the office — STN on that building. (7) Face the loco — LOCO on that engine. (8) **Launcher / main menu** (Exit to Main Menu) — no Heading/STN/LOCO. Esc pause while still in the yard may still show them (player is still in the world).
- **PASS if:** edge chips are under the Heading bar; facing the office/loco puts the marker on that object; Version is `2.6.4`. **FAIL if:** Version is still `2.6.2`, edge STN/LOCO sit beside Heading at mid-screen, STN stays glued under the bar while you stare at the office, or chips overlap the Heading text. Pause overlay with HUD still up is **not** a fail (in-world session).
- **Log (Player.log 2026-08-17):** Version `2.6.4`. Every `T2 ar-summary` has `edgeTop=0`. Heading-only: `object=0 edgeMid=…`. Face office/loco: `office=object` / `loco=object`. No YardMasterSuite exceptions. Pause overlay still showed Heading/LOCO (in-world session — not a fail). Instant sticky→object hop is Later. Hitch H68–H70.

**Epic 6 wave smokes** — one session per wave when that wave’s matrix rows ship; do not re-smoke the full v1 matrix each time.

**Logging (volume without noise):** lifecycle + one `T2 <topic>` per meaningful transition. Prefer many *named* events over one dump. Forbidden: per-frame HUD/telemetry, string-built payloads on the hot path, “debug” traces left on after the story ships.

After each smoke, harvest any new lock into Core Tier 1 ([TEST_TDD.md](.cursor/skills/TEST_TDD.md) → *Evidence loop*). Append hitch classes to [docs/PERFORMANCE_LOG.md](docs/PERFORMANCE_LOG.md) (`HitchBand`). Do not treat a quiet log after the 100 ms gate as “no hitch.”

### Lifecycle (every session, once Main loads)

- `[YMS v2] Mod Loaded. Awaiting toggle.`
- On → `[YMS v2] Activated. GC Probe running.` … `[YMS v2] Speed telemetry running.` then `[YMS v2] Limit display running.` then `[YMS v2] Clock running.`
- Off → `[YMS v2] Deactivated cleanly.`
- No YardMasterSuite exceptions / stack traces

---

**Handoff:** Document the exact commands you use for coverage in AGENT_HANDOFF.md so agents can run them consistently.
