# Test plan

Two-tier strategy for *Yard Master Suite v2*. Story IDs match [PM_PLAN.md](PM_PLAN.md). Keep in sync with [AGENT_HANDOFF.md](AGENT_HANDOFF.md).

| Tier | When | Gate |
|------|------|------|
| **1** | Every logic or docs change | Markdown lint + `dotnet test` + Release build |
| **2** | In-world UMM behavior (after packaging) | Deploy + Player.log `T2 …` + on-screen HUD |

**Merge-ready today:** Tier 1 (`npx --yes markdownlint-cli2` + `dotnet test` + Release build). Stories that touch in-world UI also need Tier 2 before checking Done in PM_PLAN. Deploy with `package.ps1 -NoArchive` before asking for smoke. First in-world smoke (**1.4** hitch probe) passed 2026-08-12.

---

## Tier 1 — Fast feedback

```bash
npx --yes markdownlint-cli2
dotnet test YardMasterSuite.sln
dotnet build YardMasterSuite.sln -c Release
```

**Pass (intended):** Markdown lint clean (see `.markdownlint.json`); all unit tests green; 0 build errors; `build/YardMasterSuite.dll` present.

Pure helpers live in `YardMasterSuite.Core` (no Unity/game refs). Smoke-found gates must land here ([.cursor/rules/smoke-gates-tier1-ci.mdc](.cursor/rules/smoke-gates-tier1-ci.mdc)).

**Performance regression (CI):** When you add or change a Core hot-path helper (telemetry `Observe`, format/bucket used from `LateUpdate`), add or extend a test that it does not allocate — see [TEST_TDD.md](.cursor/skills/TEST_TDD.md) → *Performance regression*. Frame-time stays Tier 2 (`GcCadenceProbe`); do not fake a Unity profile in `dotnet test`.

---

## Tier 2 — In-game smoke

Requires UMM (`Mods\` under the game root) and `package.ps1`. Deploy before asking for smoke ([deploy-before-smoke.mdc](.cursor/rules/deploy-before-smoke.mdc)). **How to ask:** that rule → *How to ask* (where / what they see / steps / PASS vs FAIL / log / UMM Version / **performance**). Do not only name `T2` lines. A smoke PASS writeup that omits hitch-summary vs the last session is incomplete.

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
- **Log / screens (2026-08-18):** Steps 1–7 PASS. Yard: `Heading ESE | Clock 11:49` with loco + look-at bars. Office wall clock: HUD `Heading N | Clock 11:57` then `Heading NNW | Clock 12:01` matching the analog face. Harvest: `Smoke_office_wall_clock_*`. Player.log: `[YMS v2] Clock running.` `T2 clock init: 11:45` then one `T2 clock change` per game minute through `12:05`. Hitch H71–H73: spawn `feature=14`; office `fine=1348` / `feature=1` then `feature=3`. All `T2 ar-summary` `edgeTop=0`.

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

**6.5 Mass + Grade — Quick smoke (PASS 2026-08-18).** Ships **2.6.5**. Cab loco bar adds **Grade**; **Mass** stays. Fuel / Oil / Load / Motors follow in **6.6**. Handbrakes count may already appear from the gadget snapshot. UMM shows **2.6.5**.

- **Where:** Board a locomotive (yard DE2/shunter is fine). **Mod Manager closed** after confirming **UMM Version** `2.6.5`.
- **You should see:** The loco bar still has levers, Speed, Limit, Cars, and **Mass … t**. New chip: **Grade 0.0 %** on flat track, or **Grade +1.2 %** / **Grade -0.5 %** (sign + one decimal) when the loco is pitched on a slope. Heading + Clock stay on the bottom bar. Sitting still, Grade should hold — not flicker every fraction of a second.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.5`. (3) Load a yard and board a loco. (4) On flat ground, read Mass and Grade on the loco bar. (5) Drive onto a hill, ramp, or crest so the loco tilts — Grade should change (you do **not** need to pump a handbrake). (6) Sit still on that slope for ~5 seconds — Grade holds. (7) Get out and look at empty sky — loco bar hides. (8) Exit to Main Menu — no HUD.
- **PASS if:** Mass and Grade are on the cab bar, Grade moves when the loco tilts, Grade stays still when you sit, menu hides it, Version is `2.6.5`. **FAIL if:** Version is still `2.6.1`, no Grade chip, Grade only appears after you change handbrakes, Grade flickers constantly, or Mass disappeared.
- **Log / screens (2026-08-18):** Steps 1–8 PASS. Cab held SW-B3I: `Mass 74 t | Grade +0.4 %` with `Handbrakes 1` / TrainBrake 100 % / Speed 0. Solo DE2 drive: `Mass 38 t | Grade -1.6 %` (Handbrakes 0) — Grade ticked without pumping a handbrake. Look-away: `Heading NE | Clock 12:23`. Harvest: `Smoke_sw_b3i_cab_held_*`, `Smoke_solo_de2_drive_*`. Player.log: `[YMS v2] Train gadgets running.` `T2 gadgets init: grade=+0.4 mass=74` then `T2 gadgets change` on slope (incl. `grade=-1.6 mass=38`), `T2 gadgets hide` on unboard/look-away. Not 10 Hz.
- **Performance (H74–H76, not worse):** Spawn `n=868 fine=728 below=122 max=99 feature=16 load=2` — same graph/load class as H71 (`feature=14`). Cab drive `n=1125 fine=1118 below=7 max=49 feature=0 load=0`. On-foot look 101–166 ms is the existing H67/H72 class. Pause `dt=116924ms` is game. All `T2 ar-summary` `edgeTop=0`.

**6.6 Load + Motors + Fluids — Quick smoke (PASS 2026-08-19).** Ships **2.6.6**. Cab loco bar adds **Fuel**, **Oil**, **Load**, and **Motors** (Mass / Grade / Handbrakes stay). Steam locos may omit some chips if the sim has no diesel TM / fuel tank — that is OK. UMM shows **2.6.6**.

- **Where:** Board a **DE2** (yard shunter is fine). **Mod Manager closed** after confirming **UMM Version** `2.6.6`.
- **You should see:** Loco bar still has levers, Speed, Limit, Cars, Mass, Grade, Handbrakes. New chips: **Fuel … %**, **Oil … %**, **Load … %**, **Motors OK** (green) with TM knife up. Sitting still, those percents should hold — not flicker. Throttle up while rolling → **Load** should rise. TM knife down (if you know the cab switch) → **Motors Dead**. Low fuel/oil is not required for this smoke.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.6`. (3) Load a yard and board a DE2. (4) Read Fuel, Oil, Load, Motors on the loco bar (with Mass / Grade still there). (5) Sit still ~5 seconds — chips hold. (6) Notch throttle and roll a few meters — Load should change from idle. (7) Get out and look at empty sky — loco bar hides. (8) Exit to Main Menu — no HUD.
- **PASS if:** Fuel, Oil, Load, and Motors are on the cab bar, Load moves when you apply power, chips stay still when you sit, menu hides them, Version is `2.6.6`. **FAIL if:** Version is still `2.6.5`, those four chips never appear on a DE2, Load never leaves 0 % under power, chips flicker every fraction of a second, or Mass / Grade disappeared.
- **Log / screens (2026-08-19):** Steps 1–8 PASS. SW-B3I DE2 cab: `Fuel 96 % | Oil 92 % | Mass 74 t | Grade +0.4 % | Load 43 % | Motors OK` (throttle 9 %, TrainBrake 100 %, Speed 0). Idle: `Load 0 %`. Rolling: `Load 25 %` at 4 km/h, Oil 91 %, TrainBrake 0. Look-away: `Heading S | Clock 13:49`. Harvest: `Smoke_sw_b3i_cab_emits_T2_gadgets_init_load_0_fuel_96_oil_92_motors_ok`, `Smoke_sw_b3i_cab_load_ticks_to_40_under_power`, `Smoke_sw_b3i_cab_shows_fuel_96_oil_92_load_43_motors_ok`. Player.log: Version `2.6.6`. `[YMS v2] Train gadgets running.` `T2 gadgets init: grade=+0.4 mass=74 load=0 fuel=96 oil=92 motors=OK` then `T2 gadgets change: … load=40 …` under power, `T2 gadgets hide` on unboard. Not 10 Hz. Bolt `SceneVariables` on unload — game, not YMS.
- **Performance (H77–H79, not worse):** Spawn `n=812 fine=695 below=102 max=99 feature=14 load=1` — same graph/load class as H74 (`feature=16 load=2`). Cab roll `n=1299 fine=1298 below=0 max=0 feature=1 load=0` then `n=1086 fine=1081 below=4 max=47 feature=1 load=0`. On-foot / cab-look 104–178 ms is the existing H67/H72 class. All `T2 ar-summary` `edgeTop=0`.

**6.7 MU sync — Quick smoke (PASS 2026-08-19).** Ships **2.6.7**. This is **not** the 6.6 Fuel / Oil / Load / Motors check. Those stay. The new chip only appears when **two locos** are in the same consist and they disagree. A solo DE2 must look like 6.6 (no MU text). UMM shows **2.6.7**.

- **Where:** A yard with **two locomotives** you can couple (two DE2s is fine). **Mod Manager closed** after confirming **UMM Version** `2.6.7`.
- **You should see:** After coupling, board one loco. Fuel / Oil / Load / Motors / Mass / Grade stay. If both locos match (MU hose plugged, same gear/brakes) → **no** yellow or red MU chip. Park the other in Neutral or engine-off with brakes matching → yellow **MU idle**. **Unplug the MU hose**, keep both in Forward, then mismatch **Indy** or **Throttle** → red **MU desync**. The parking **handbrake wheel is not** this chip (it only changes `Handbrakes N`). Match them again → the chip vanishes. Look at empty sky → loco bar hides.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.7`. (3) Couple two locos. (4) Board one — confirm Fuel/Oil/Load/Motors still there, and **no** MU chip if they match. (5) Set the other loco Neutral or engine-off (brakes the same) — yellow **MU idle**. (6) **Unplug the MU hose.** Put both reversers in Forward. In your cab drop **Indy to 0 %** (leave the other loco at Indy 100 %) **or** notch **Throttle** on yours only — red **MU desync**. Do **not** use the parking handbrake wheel for this step. (7) Plug MU again / match levers — chip gone. (8) Get out, look at empty sky — bar hides. Exit to Main Menu — no HUD.
- **PASS if:** Version is `2.6.7`, solo/synced consist has no MU chip, Neutral/off shows yellow MU idle, unplugged Indy/Throttle fight shows red MU desync, chip clears when they match, 6.6 chips did not disappear. **FAIL if:** Version is still `2.6.6`, a lone DE2 shows MU idle/desync, two matched locos show a chip, Neutral never yellows, unplugged Indy/Throttle fight never reds, or Fuel / Oil / Load / Motors vanished. Parking-handbrake-only with no red is **not** a fail (harvest 2026-08-19).
- **Log / screens (2026-08-19):** Steps 1–5 + unplug-hose red PASS. Two DE2s, Mass 76 t / Cars 2. Neutral → yellow `MU idle`. Hose unplugged + fight → red `MU desync`. Neutral again → yellow `MU idle`. Parking handbrake is **not** this chip (`Smoke_one_handbrake_on_is_not_mu_desync`). Harvest: `Smoke_two_de2s_synced_omits_mu_chip`, `Smoke_trailing_neutral_shows_mu_idle`, `Smoke_unplugged_indy_mismatch_is_mu_desync`. Player.log: Version `2.6.7`. `T2 licenses granted: n=6`. `T2 gadgets init: … mu=idle` then `change: … mu=desync` then `mu=idle`. `T2 gadgets hide` on unboard. Not 10 Hz. Bolt `SceneVariables` on unload is the game, not YMS.
- **Performance (H80–H82, not worse):** Spawn `n=622 fine=474 below=125 max=100 feature=21 load=2` — same graph/load class as H77 (`feature=14 load=1`); busier first window, still spawn. Cab `n=1247 fine=1240 below=7 max=67 feature=0 load=0`. On-foot look 101–200 ms is the existing H67/H72 class (one 200 ms peak). MU chip publish is not a new hitch class.

**6.8 Full lever + Speed + Limit — Quick smoke (PASS 2026-08-20).** Ships **2.6.8**. This is **not** the 6.7 MU check. Fuel / Oil / Load / Motors / Mass / Grade stay. Posted **Next** is **not** this ship (6.10). UMM shows **2.6.8**.

- **Where:** A yard DE2 you can board and roll a few meters. Also look at that loco from the ground. **Mod Manager closed** after confirming **UMM Version** `2.6.8`.
- **You should see:** In the cab, the loco bar still has Rev / Throttle / Indy / TrainBrake, then **Speed N km/h** and **Limit N** in the middle (not `— Speed` / `— Limit`). Sitting still: **Speed 0 km/h** and a Limit number (often 120 on straight track, or a curve number). Rolling: Speed ticks up in whole km/h. If you get close to / over Limit, Limit turns yellow then red. Get out and look at the loco: levers + Speed + Limit stay. Look at empty sky: loco bar hides. No `Next` on Limit.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.8`. (3) Load a yard and board a DE2. (4) Sit still — read Speed 0 and a Limit number; 6.6 chips still there. (5) Notch throttle and roll — Speed should change; levers still live. (6) Get out and look at that loco — levers / Speed / Limit still on the bar. (7) Look at empty sky — loco bar hides. (8) Exit to Main Menu — no HUD.
- **PASS if:** Version is `2.6.8`, cab shows live levers + Speed 0 then ticking Speed + a Limit number (no dashes, no Next), look-at still shows those chips, 6.6 chips did not disappear. **FAIL if:** Version is still `2.6.7`, Speed never leaves 0 while rolling, Limit is `— Limit` the whole time in cab, levers vanish on look-at, Next appears, or Fuel / Oil / Load / Motors vanished.
- **Log / screens (2026-08-20):** Steps 1–8 PASS. MF-T13P DE2 cab: `Speed 0 km/h | Limit 120` (TrainBrake 100 %, Throttle 0 %). Other DE2 full fluids: Fuel/Oil 100 %. Roll: `Speed 5 km/h | Limit 120` (Throttle 18 %, Indy 43 %, TrainBrake 36 %, Load 35 %). Look-at loco: levers + `Speed 0 km/h | Limit 60`. Sky: `Heading NW | Clock 20:43`. Harvest: `Smoke_mf_t13p_cab_held_speed_0_limit_120`, `Smoke_cab_roll_speed_5_limit_120_load_35`, `Smoke_cab_roll_publishes_speed_0_then_5`, `Smoke_curve_geometry_is_limit_60`, `Smoke_look_at_usable_loco_shows_levers_speed_and_limit`. Player.log: Version `2.6.8`. `T2 speed init: 0` … `T2 speed change: 5`. `T2 limit init: 120 auth=geometry` then `T2 limit change: 60 auth=geometry`. `T2 gadgets hide` on look-away. First DE2 `oil=0` is the tank. Bolt `SceneVariables` on unload — game, not YMS.
- **Performance (H83–H85, not worse):** Spawn `n=719 fine=572 below=124 max=93 feature=22 load=1` — same graph/load class as H80 (`feature=21 load=2`). Cab held `n=1331 fine=1329 below=2 max=47 feature=0 load=0`; roll `n=1195 feature=0`. On-foot look 102–203 ms is the existing H67/H72/H81 class. All `T2 ar-summary` `edgeTop=0`.

**6.9 Posted board index — Quick smoke (PASS 2026-08-20).** Ships **2.6.9**. This is **not** the 6.8 Speed check (Speed still ticks). Limit follows **posted number signs only** (geometry scanner **ripped**). **Next** is **not** this ship (6.10). UMM shows **2.6.9**.

- **Where:** A yard DE2 you can board and drive onto the mainline past a **white speed-limit number sign** beside the track (a single digit like 6 means 60 km/h). **Mod Manager closed** after confirming **UMM Version** `2.6.9`.
- **You should see:** In the cab, Speed still ticks. **Limit stays 120** until you pass a posted sign — curves must not tick 60/90 by themselves. Drive past a speed sign that faces you on your track → **Limit changes to that sign's number** (6 → 60, 4 → 40) and stays there until the next sign. No `Next` on the bar. Get out and look at each loco: Fuel/Oil must match **that** unit (green tank stays green). Look at empty sky: loco bar hides.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.9`. (3) Load a yard and board a DE2. (4) Sit still — Speed 0, Limit 120; 6.6 chips still there. (5) Roll out of the yard **before** a posted sign — Limit must stay 120 (no 60/90 from curves). (6) Pass a posted speed sign on your right that faces you — Limit matches that sign (digit × 10). (7) Keep rolling with no new sign — Limit must not jump. (8) Get out. Look at the empty-tank loco, then the full-tank loco — Fuel/Oil should follow the one in the crosshair. Look at empty sky — bar hides. Exit to Main Menu — no HUD.
- **PASS if:** Version is `2.6.9`, Limit stays **120** until you pass a facing posted sign (curves must not tick 60/90/120 on their own), then Limit matches that sign, no Next, look-at keeps Limit, looking at a full-tank DE2 shows **that** unit’s Fuel/Oil (not the empty one you boarded last). No `[YMS v2] Geometry scanner running.` / no `T2 geometry`. **FAIL if:** Version is still `2.6.8`, Limit jumps to 60/90 on a curve with no sign, Limit never leaves 120 after a facing sign, Next appears, look-at of the green-tank loco still shows Oil 0 %, Geometry scanner still logs, or Speed / fluids vanish.
- **Log / screens (2026-08-20):** UMM `2.6.9` confirmed. Multiple facing signs → Limit sticky correct (120 → posted takes 120/90/60/80). Look-at empty vs full tank Oil 0 % vs 97 % PASS. No `T2 geometry` / `auth=geometry`. Harvest: posted sticky Core tests + look-at fluid gate. Player.log: `[YMS v2] Posted board index running.` `T2 boards fot: raw=… parsed=…` `T2 limit … auth=default` then `auth=posted`. Hitch H86–H88: spawn `feature=19 load=1`; cab often `feature=0`; one drive window `feature=12` near FoT; look 100–170 ms known class. Look-at flicker still chatters `auth=none` ↔ `default` (WARN, not a product fail).
- **Performance:** Cab drive should stay `feature=0` like 6.8 (H84). First board scan (`T2 boards fot`) may be one Feature hitch — same class as other rare scans, not a new cab-drive class. Spawn graph/load OK. On-foot 100–200 ms is known debt (H67/H72/H85).

**6.10 Next + distance — Quick smoke.** Ships **2.6.10**. This is **not** a re-check of 6.9 Limit sticky. Dual junction **numbers** are still through-only (the thrown track is used for *which* sign is Next). UMM shows **2.6.10**.

- **Where:** Same DE2 drive onto the mainline past **two** facing speed signs on your track (for example 8 then 5, meaning 80 then 50). **Mod Manager closed** after confirming **UMM Version** `2.6.10`.
- **You should see:** After you pass the first sign, Limit matches that number **and** the bar grows `Next` for the sign still ahead (`Limit 80 | Next 50`). While that next sign is far, there is **no** meter count. When you get close (hundreds of meters, not kilometres), meters appear: `Next 50 (85m)`. Passing that sign makes Limit become 50 and Next updates or drops. Curves still must not move Limit by themselves.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.10`. (3) Load a yard and board a DE2. (4) Sit still — Speed 0, Limit 120; no Next yet unless a different number is already ahead on your track. (5) Roll toward a facing sign — before you pass it, Limit may still be 120 with `Next` showing that sign. (6) Pass it — Limit matches; if another different number is ahead, `Next` shows that. (7) Keep rolling until meters appear on Next, then pass that sign. (8) Look-at the loco from the ground — Limit + Next stay. Empty sky — loco bar hides. Menu — no HUD.
- **PASS if:** Version is `2.6.10`, Next appears for a different posted number ahead, meters show only when close, Limit still only changes when you pass a facing sign (not on curves), look-at keeps Limit/Next. **FAIL if:** Version is still `2.6.9`, Next never appears with two facing signs ahead, meters show kilometres away, Limit jumps on a curve with no sign, or Speed / fluids vanish.
- **Log:** `[YMS v2] Posted board index running.` `T2 boards fot: raw=… parsed=…` `T2 limit … auth=posted next=…` (optional `50m` / `0.1km` when close). No `T2 geometry` / `auth=geometry`. No per-frame next lines.
- **Log / screens (2026-08-20):** UMM `2.6.10`. First try: nearby 6 skipped (off-route when track attach failed) + look-at FoT **2.2–2.3 s** FAIL. Fix: corridor if track unknown; keep roster across look-away; T2 not every 10 m. Re-smoke PASS: `60 next=80` then `next=80 115m` then `80 next=50 579m` (raise hides meters until ~120 m; drop shows meters out to ~600 m). Dual numbers through-only (waived). One `T2 boards fot` after load. Harvest: `Smoke_nearby_posted_6_is_kept_when_board_track_unknown`, `Smoke_branch_board_is_ignored_when_on_other_path_track`, `Smoke_after_6_next_8_omits_meters_until_close`, `Smoke_take_8_shows_next_5_meters_when_drop_is_inside_reveal`. Look L→R PASS; R→L stutter is usable-train flicker + H67/H72 class (`below=204`), not 2 s FoT.
- **Performance (H89–H91, not worse cab; look better than first 6.10 try):** Spawn `feature=9 load=3`. Cab `feature=0`; take window `feature=5`. Look spikes 100–144 ms (old class). RTL still open. Pause ~88 s is the menu.

**6.11 Marked + Path — Quick smoke.** Ships **2.6.11**. Station chip is **not** this ship (**6.12**). Dual junction Limit numbers stay through-only. UMM shows **2.6.11**.

- **Where:** Yard on foot, Heading + Clock visible. A car on a yard track you can look at. **Mod Manager closed** after confirming **UMM Version** `2.6.11`.
- **You should see:** Bottom bar still `Heading … | Clock HH:MM` until you press **Home**. Then **`Marked here`** appears between Heading and Clock. Walk away — chip becomes **`Marked NE 84m`** (bearing toward the mark + meters). **Shift+Home** removes Marked. Aim at a car and press **End** — **`Path OK`** (same track) stays when you look at the sky (origin is sticky). **Shift+End** removes Path. Path is **not** a second Home pin — no `NE 30m` on Path. No Station chip. No Home AR pin yet (**6.15**).
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.11`. (3) Load a yard on foot. (4) Confirm no Marked / Path chips. (5) Press **Home** — `Marked here`. (6) Walk ~20 m — bearing + meters; Clock still there. (7) **Shift+Home** — Marked gone. (8) Aim at a car, press **End** — Path chip appears. (9) Look at the sky — Path stays (not `Path —`). (10) **Shift+End** — Path gone. (11) Menu — no HUD.
- **PASS if:** Version is `2.6.11`, Home sets Marked here, walking shows bearing+meters, Shift+Home clears, End shows Path OK, look-away keeps Path OK, Shift+End clears Path, Clock/Heading stay. **FAIL if:** Version is still `2.6.10`, Home does nothing, look-away after End shows `Path —`, Marked never leaves after Shift+Home, or Station appears.
- **Log:** `[YMS v2] Marked running.` `T2 mark init: Marked here` then `T2 mark change: Marked …` on bearing change (not every meter). `T2 path init: Path OK` on End; look-away must **not** spam `T2 path change: Path —`; `T2 path cleared` on Shift+End. Harvest: `Smoke_look_away_keeps_path_ok_when_dest_matches_last_origin`.
- **Log / screens (2026-08-20):** UMM `2.6.11`. Home / walk / Shift+Home PASS (`Marked here` → `Marked N` → clear). End on loco `Path OK`; first try look-away `Path —` FAIL; sticky origin re-smoke PASS (no `T2 path change: Path —`). One-time first look-at pause = `T2 boards fot` + `dt=2210ms` (known FoT, not a 6.11 fail).
- **Performance (H92–H94, not worse cab):** Spawn `feature=10 load=2` (first session `feature=13 load=2`). Cab `feature=0`. On-foot look 104–157 ms (H67/H72). First look FoT 2.2 s open.

**6.12 Station chip — Quick smoke.** Ships **2.6.12**. Fluids `Next: Farm [km]` is **not** this ship (cut). Dual junction Limit numbers stay through-only. Home AR pin / PNG icons stay **6.15–6.17**. UMM shows **2.6.12**.

- **Where:** On foot at a yard with a job office (green **STN** square). Start out of town if you can, then walk in. **Mod Manager closed** after confirming **UMM Version** `2.6.12`.
- **You should see:** Far from town — bottom bar is `Heading … | Clock HH:MM` (plus Marked/Path only if you set them). Walk into the station zone — **`Station SM NE 84m`** (yard letters + bearing toward the office + meters). That bearing points at the **job office** (same place as green STN), not the yard middle. Walk to the office door / apron — chip becomes **`Station SM here`**. Walk back out of town — Station disappears. No new Home pin in the sky. Path/Home still work as in 6.11.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.12`. (3) Load a yard on foot, outside the town if possible. (4) Confirm no Station chip. (5) Walk toward the job office until Station appears — letters + bearing + meters. (6) Keep walking to the office door — `Station … here`. Green STN may hide on the apron (already shipped). (7) Walk back out of the zone — Station gone. (8) Optional: Home / End still work; look at the sky after End still keeps Path OK. (9) Menu — no HUD.
- **PASS if:** Version is `2.6.12`, Station appears in-zone with yard letters + bearing/meters, office apron shows `here`, leaving the zone hides Station, Clock/Heading stay, no `Next: … km` chip. **FAIL if:** Version is still `2.6.11`, Station never appears in town, chip uses raw map coords, `here` never shows at the office, Station stays after leaving town, or a Home AR pin appears.
- **Log:** `[YMS v2] Station running.` `T2 station init: Station SM …` when you enter the zone; `T2 station change: Station SM here` on the apron; `T2 station change: — Station` when you leave. Not every meter. Harvest: `Smoke_in_zone_shows_station_bearing_on_always_on`, `Smoke_office_apron_shows_station_here`, `Smoke_outside_zone_omits_station`.
- **Log / screens (2026-08-20):** UMM `2.6.12`. Spawned outside town. Cab `Heading S | Station CP SSW 640m`. On foot `Station CP NW 43m` with green STN on the office. Apron `Station CP here` (STN hidden). Leave zone hides Station. Sky look-away `Heading SE | Marked NNW 28m | Station CP NNW 41m | Path OK`. `T2 station init: Station CP SSW` then `change: Station CP here` then `change: — Station`. Harvest: `Smoke_cab_drive_shows_station_cp_ssw_640m`, `Smoke_office_apron_shows_station_cp_here`, `Smoke_enter_cp_zone_emits_T2_station_init_ssw`, `Smoke_office_apron_emits_T2_station_change_here`, `Smoke_look_away_keeps_station_and_path_on_always_on`.
- **Performance (H95–H97, not worse cab):** Spawn `feature=13 load=2` (same class as 6.11 first session). Cab `feature=0`. On-foot look 100–180 ms (H67/H72); one 344 ms FoT refresh. First look FoT 2516 ms open. Pause 30 s / 47 s is the menu.

**Epic 6 wave smokes** — one session per wave when that wave’s matrix rows ship; do not re-smoke the full v1 matrix each time.

**Logging (volume without noise):** lifecycle + one `T2 <topic>` per meaningful transition. Prefer many *named* events over one dump. Forbidden: per-frame HUD/telemetry, string-built payloads on the hot path, “debug” traces left on after the story ships.

After each smoke, harvest any new lock into Core Tier 1 ([TEST_TDD.md](.cursor/skills/TEST_TDD.md) → *Evidence loop*). Append hitch classes to [docs/PERFORMANCE_LOG.md](docs/PERFORMANCE_LOG.md) (`HitchBand`). Do not treat a quiet log after the 100 ms gate as “no hitch.”

### Lifecycle (every session, once Main loads)

- `[YMS v2] Mod Loaded. Awaiting toggle.`
- On → `[YMS v2] Activated. GC Probe running.` … `[YMS v2] Posted board index running.` then `[YMS v2] Limit display running.` … `[YMS v2] Clock running.` then `[YMS v2] Marked running.` then `[YMS v2] Station running.` then `[YMS v2] Train gadgets running.`
- Off → `[YMS v2] Deactivated cleanly.`
- No YardMasterSuite exceptions / stack traces

---

**Handoff:** Document the exact commands you use for coverage in AGENT_HANDOFF.md so agents can run them consistently.
