# Test plan

Two-tier strategy for *Yard Master Suite v2*. Story IDs match [PM_PLAN.md](PM_PLAN.md). Keep in sync with [AGENT_HANDOFF.md](AGENT_HANDOFF.md).

| Tier | When | Gate |
|------|------|------|
| **1** | Every logic or docs change | Markdown lint + `dotnet test` + Release build |
| **2** | In-world UMM behavior (after packaging) | Deploy + Player.log `T2 …` + on-screen HUD |

**Merge-ready today:** Tier 1 (`npx --yes markdownlint-cli2` + `dotnet test` + Release build). Stories that touch in-world UI also need Tier 2 before checking Done in PM_PLAN. Deploy with `package.ps1 -NoArchive` before asking for smoke. First in-world smoke (**1.4** hitch probe) passed 2026-08-12.

**HTP:** Maps pin / PID / autonomy Tier 1 is the Headless Test Platform ([docs/HTP.md](docs/HTP.md), [.cursor/rules/htp.mdc](.cursor/rules/htp.mdc)) — corridor/tick walks in Core, one-off harvest dump, cab only for chrome. Do not treat a log paste as the suite.

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
- **Log / screens (2026-08-26 FILO restore `2.8.1.16`):** UMM `2.8.1.16`. Sit `next=—` until direction lock at ~5 km/h; rolling takes `auth=posted` (50→40→60). `limit-ahead` km/h-only (~18 lines). Cab hitch typical `feature=0–4`; late windows `15–17` (other cab systems; IsolateLimitTick proved Limit innocent). **Out of 6.10:** parallel Next metre snaps (chord); leftover hitch. Isolates off. Harvest: `PostedLimitFunnelTests` standstill freeze / lock / Observe roster-ignore.

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

**6.13 Active job bar — Quick smoke.** Ships **2.6.13**. Preview edge / license warn / Cancelled flash are **not** this ship. Dual junction Limit numbers stay through-only. Home AR pin / PNG icons stay **6.15–6.17**. UMM shows **2.6.13**.

- **Where:** A yard with a job office. Start with **no** taken job. **Mod Manager closed** after confirming **UMM Version** `2.6.13`.
- **You should see:** Bottom bar still Heading + Clock (plus Marked/Station/Path if you set them). **No** extra bar between the look-at line and Heading. Take a haul/shunt job at the office — a **job bar** appears above Heading: `Job SM-FH-12` (or that town’s letters) then **GO**, **HOLD**, or **RED**, then **Bonus m:ss**. With only the loco, it is usually **RED**. Couple the job’s cars and nothing extra — **GO**. Couple a random extra wagon — **HOLD**. Look at one of the job’s cars — the look-at line has a **Job …** chip with the same id. Look at a car with no job — that chip is gone. Look at the sky — the job bar **stays**. Finish or abandon the job — the job bar goes away. No Preview km chip. No license warning. No red Cancelled flash.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.13`. (3) Load a yard on foot with no taken job. (4) Confirm no job bar. (5) Take a job at the office. (6) Read the new bar — id + GO/HOLD/RED + Bonus. (7) Board a loco; note RED until job cars are on. (8) Couple the job cars — GO (or HOLD if extra freight is on). (9) Look at a job car — Job chip on the look-at bar. (10) Look at the sky — job bar stays. (11) Complete or abandon the job — bar gone. (12) Menu — no HUD.
- **PASS if:** Version is `2.6.13`, taking a job shows the bar with id + status + Bonus, look-at Job chip matches on a job car and omits on a non-job car, sky look-away keeps the job bar, finishing/abandoning hides it. **FAIL if:** Version is still `2.6.12`, taking a job never shows a bar, Bonus never appears, look-at never shows Job on a job car, the bar vanishes when you look at the sky, or Preview / license / Cancelled appear.
- **Log:** `[YMS v2] Job bar running.` `T2 job init (hidden)` then `T2 job appear: job=… extra=0 status=RED|HOLD|GO bonus=…` on take; `T2 job change:` on GO/HOLD; `T2 job hide` when the job is gone. Look-at: `T2 look-at bar: … job=SM-FH-12` on a job car. Not every bonus second. Harvest: `Smoke_taken_job_bar_shows_job_go_bonus`, `Smoke_no_taken_job_emits_T2_job_init_hidden`, `Smoke_take_job_emits_T2_job_appear`, `Smoke_complete_job_emits_T2_job_hide`, `Smoke_look_at_job_car_shows_job_chip`, `Smoke_look_at_job_car_logs_job_id`.
- **Performance:** Cab drive should stay `feature=0`. Job bar samples at 4 Hz, not every frame. On-foot look remains H67/H72 class; first look-at FoT ~2.2 s stays open.
- **Log / screens (2026-08-21):** UMM `2.6.13`. Office apron `Job SW-SU-72 | RED`. After couple, Car 5 look-at `job=SW-SU-72`, job bar **GO**. `T2 job appear: job=SW-SU-72 extra=0 status=GO`. `T2 look-at bar: car=5 cargo=Forestry Trailers track=SW-B3I job=SW-SU-72`. Preview / license / Cancelled not shown.

**6.13 on-consist keys — Quick smoke (stacked by request).** Same ship **2.6.13**. This is **not** off-train remote and **not** auto-coupler. UMM still **2.6.13**.

- **Where:** Coupled consist. Stand **on a freight car** (last car is fine), **not** in the front loco cab. **Mod Manager closed.**
- **You should see:** An extra bar under Heading: `On-consist: cab Throttle / Indy / TrainBrake / Reverser → front loco | Numpad . TM fuse`. Cab throttle/brake/reverser keys (the same ones as in the cab) notch the **front loco**. The train can move while you stand at the end. Step onto the ground — that bar vanishes and those keys do nothing. Sit in the front cab — no extra bar (normal cab).
- **Do:** (1) Full game restart after this deploy. (2) Confirm **UMM Version** `2.6.13`. (3) Stand on the last car of your coupled job train. (4) Confirm the On-consist bar. (5) Reverser to F, throttle up with cab keys — loco/train moves. (6) Step off the train — bar gone; keys dead. (7) Optional: Numpad `.` only turns TM fuse **on** (will not kill motors).
- **PASS if:** Version is `2.6.13`, last-car cab keys move the front loco, stepping off disarms. **FAIL if:** keys still do nothing on the last car, keys work from the ground, or the front cab double-applies.
- **Log:** `[YMS v2] On-consist control running.` `T2 on-consist: armed (cab bindings → front loco)` on the car; `T2 on-consist: disarmed` off the train. Not every notch. Harvest: `Smoke_player_on_last_car_still_picks_front_loco`, `Smoke_stand_on_last_car_emits_T2_on_consist_armed`, `Smoke_step_off_train_emits_T2_on_consist_disarmed`.
- **Performance:** Cab `feature=0` still expected. On-consist is Update input, not a new hitch class.
- **Log / screens (2026-08-21):** UMM `2.6.13`. Last-car `T2 on-consist: armed (cab bindings → front loco)`; step off `T2 on-consist: disarmed`. Cab windows `feature=0`. Harvest: `Smoke_player_on_last_car_still_picks_front_loco`, `Smoke_stand_on_last_car_emits_T2_on_consist_armed`.

**6.15 Pin AR — Quick smoke.** Ships **2.6.15**. Amber **PIN** quad at the Home mark (not PNG — **6.17**). Radar / job-car purple stay **6.16** / **6.21**. Dual junction Limit numbers stay through-only. UMM shows **2.6.15**.

- **Where:** A yard on foot. **Mod Manager closed** after confirming **UMM Version** `2.6.15`.
- **You should see:** Bottom bar Heading + Clock. Press **Home** — an **amber square** labeled **PIN** sits in the world where you stood (about waist height), not glued to a car beside you. Walk away — PIN stays at that spot; the bottom bar shows **Marked NE 84m** (bearing back). Turn around — PIN can sit on the left/right mid-edge like STN/LOCO. Walk onto the mark (within about 8 m) — PIN **hides**; chip becomes **Marked here**. **Shift+Home** — PIN gone and Marked gone. No 48px PNG, no dark plate.
- **Do:** (1) Full game restart after deploy. (2) Confirm **UMM Version** `2.6.15`. (3) Load a yard on foot. (4) Press **Home**. (5) Look at the mark — amber PIN in the world. (6) Walk 20–40 m away and turn — PIN still at the mark (or mid-edge if behind you). (7) Walk back onto the mark — PIN hides. (8) **Shift+Home** — no PIN. (9) Optional cab drive 20 s.
- **PASS if:** Version is `2.6.15`, Home plants an amber PIN at the mark, it hides within ~8 m, Shift+Home clears it. **FAIL if:** Version is still `2.6.13`, Home never shows a PIN, the icon sticks to a nearby car, it stays on top of you at the mark, or Shift+Home leaves a PIN.
- **Log:** `[YMS v2] AR overlay running.` After Home: `T2 ar change:` with `pin=object` or `pin=edge` (throttled 2 s). After Shift+Home or standing on the mark: `pin=—`. Harvest: `Smoke_unmarked_hides_pin`, `Smoke_home_mark_away_shows_pin`, `Smoke_standing_on_mark_hides_pin`, `Smoke_standing_within_8m_hides_pin`, `Smoke_home_pin_emits_T2_ar_pin_place`.
- **Performance:** Cab drive should stay `feature=0`. On-foot look remains H67/H72 class. One extra AR slot in the existing AR LateUpdate — not a new hitch class.
- **Log / screens (2026-08-21):** UMM `2.6.15`. Home → `T2 mark init: Marked here` then `T2 ar change: … pin=edge` (PIN also on the rails in-world). Mid-edge stack STN + LOCO + PIN. On the mark / Shift+Home → `pin=—`. Cab `T2 hitch-summary: n=1092 … feature=0 load=0`. Harvest: `Smoke_unmarked_hides_pin`, `Smoke_home_mark_away_shows_pin`, `Smoke_standing_on_mark_hides_pin`, `Smoke_standing_within_8m_hides_pin`, `Smoke_home_pin_emits_T2_ar_pin_place`.

**6.16 Loco radar — Quick smoke.** Ships **2.6.16.14**. Amber **other-loco** quads (not PNG — **6.17**), **v1 4.10 parity — no licence filter**. Licence debug is **F8** (F11 is the game stats overlay). UMM shows **2.6.16.14**.

- **Where:** Load save on car 5 of the DE2 job train (then DE2 cab). **Mod Manager closed** after confirming **UMM Version** `2.6.16.14`.
- **You should see:** On a **freight car**, cyan **LOCO** on the DE2 next to green **STN**, **plus up to 3 amber loco chips** (`S060` / `S282` / …) each showing **name on one line, metres under it** — with **no F8 needed**. F8 does **not** change the amber set (debug grant only). Toast / Save Manager — all AR gone. In the **DE2 cab**, cyan LOCO hidden, amber chips stay. After hop-off from an **MU pair**, the mate you just left is **not** amber (own consist).
- **Do:** (1) Full restart. (2) UMM `2.6.16.14`. (3) On car 5 — STN + cyan LOCO + amber locos immediately. (4) Toast / Save Manager — AR vanishes. (5) Press F8 twice — amber set **unchanged** (licenses toast only). (6) Board DE2 cab — cyan LOCO gone, amber stays. (7) Numpad Enter N→R→F still holds Forward. (8) If you have an MU pair: hop off — the mate is **not** an amber chip.
- **PASS if:** Amber locos on load with **no** F8, names + metres spaced, cyan LOCO on the freight car; MU mate not amber on foot. **FAIL if:** UMM older than `2.6.16.14`, amber empty on load, F8 changes the amber set, or the MU mate you just left is amber.
- **Log:** `T2 loco-radar: scan reason=Forced city=SW excl=1 unlic=0 cands=8 n=1` (n is nearest-within-600 m, not total locos). `unlic` stays **0** (filter parked). Harvest: `Smoke_de2_only_save_shows_unlicensed_locos_without_f11`, `Smoke_save_load_on_freight_car_uses_usable_loco_when_last_null`, `Smoke_last_loco_known_skips_per_frame_usable_probe`, `Smoke_cab_idle_reuses_caption_metre_key`, `Smoke_cab_drive_does_not_retry_overlay_fot_every_two_seconds`, `Smoke_license_debug_hotkey_is_not_f11`, `Smoke_on_foot_last_loco_excludes_mu_mate_from_radar`.
- **Performance:** Cab target `feature=0`. `2.6.16.10` cab windows were `feature=15–19` (fps overlay on). `2.6.16.12` overlay-off cab still `feature=15`. `2.6.16.13` overlay-handle cap: cab reverse with 6 loads **`feature=0`** (H107). On-foot look H67/H72 class. One FoT per world enter / city / leave-loco.
- **Log / screens (2026-08-23):** Product PASS on `2.6.16.10` (amber + cyan). Hitch PASS on `2.6.16.13`: board DE2, reverse, speed 1→39, overlay off; cab windows `feature=0 load=0` (`n=1052/1021/814/712/744`). Pause/quit `feature=2`. Harvest overlay cap + F8 hotkey tests. **6.16.14** PASS: MU pair on turntable — mate has no amber; others (S282A / DE6) stay amber. Log `T2 loco-radar: … LeftLoco city=MF … excl=2 … n=3`. Cab `feature=0`. Harvest `Smoke_on_foot_last_loco_excludes_mu_mate_from_radar`.

**6.17 PNG icons — Quick smoke.** Ships **2.6.17.2**. 48px v1 **loco / house / pin** PNGs + dark plate; radar uses loco art (amber). On-consist throttle/indy/train brake: **one tap = one notch** when standing on a loco. Dual junction Limit numbers stay through-only. UMM shows **2.6.17.2**.

- **Where:** MF roundhouse on foot, then two DE2s MU'd. **Mod Manager closed** after **UMM Version** `2.6.17.2`.
- **You should see:** House PNG on the office (STN), cyan loco PNG on LastLoco after hop-off, amber loco PNG on other locos (max 3), pin PNG at Home. Not the stick-figure placeholders.
- **Do:** (1) Restart. (2) UMM `2.6.17.2`. (3) Yard — STN house, radar locos, Home pin. (4) Hop a DE2 — that unit cyan **LOCO**; MU mate has no amber (own consist). (5) On the MU pair, tap throttle once — HUD **9%** not 18%.
- **PASS if:** v1-style icons + plate; `T2 ar-icons …=png`; one throttle tap = 9%. **FAIL if:** crosshairs/stick figures, `quad` in the log, or 9% then 18% on one tap.
- **Log:** `T2 ar-icons loco=png station=png pin=png radar=png`. Harvest: `Smoke_yard_markers_are_48px_named_pngs_with_dark_plate`, `Smoke_cab_keys_do_not_double_step_when_standing_on_mu_mate`.
- **Performance:** Cab `feature=0` class vs H107. Spawn graph/load OK. On-foot look H67/H72.
- **Log / screens (2026-08-23):** Player PASS on v1 art (`2.6.17.1`) and MU single-step (`2.6.17.2`). Log `T2 ar-icons loco=png station=png pin=png radar=png`. Cab windows `feature=0`. Own-consist radar skip and max-3 confirmed as v1 rules (not bugs).

**6.18 Rear/Front proximity — Quick smoke.** Ships **2.6.18**. Reverse → `Rear`; Forward → `Front`; Neutral omit. Green ≤0.5 m with couple-scan; yellow through 30 m; dash when open. No “Couple ready”. Dual junction Limit numbers stay through-only. UMM shows **2.6.18**.

- **Where:** Cab of a loco with a free travel-end coupler. **Mod Manager closed** after **UMM Version** `2.6.18`.
- **You should see:** `Front …` / `Rear …` on the **loco bar** after Cars. Yellow out to 30 m; green at ≤0.5 m in couple-scan. Neutral: chip gone.
- **Do:** (1) UMM `2.6.18`. (2) Neutral — no Front/Rear. (3) Reverse toward cars — Rear yellow then green. (4) Neutral — gone. (5) Forward — Front not Rear.
- **PASS if:** Neutral omits; Reverse = Rear (green close); Forward = Front; no “Couple ready”. **FAIL if:** chip in Neutral, Front/Rear swapped, or “Couple ready”.
- **Log:** `T2 proximity init: end=Rear tenths=… couple=1` (or Front); `T2 proximity hide` in Neutral. Harvest: `Smoke_reverse_free_tip_caption_is_rear_not_front`, `Smoke_neutral_omits_chip`, `Smoke_reverse_shunt_shows_rear_chip_after_cars`.
- **Performance:** Cab `feature=0` class vs H109. Spawn graph/load OK. On-foot look H67/H72.
- **Log / screens (2026-08-24):** Player PASS. `T2 proximity init: end=Front tenths=5 couple=1`; hide on Neutral; Rear dash `tenths=-1`. Cab windows `feature=0` (`max=42–66`). Spawn `feature=12 load=2 max=100`.

**6.19 Derail Risk — Quick smoke.** Ships **2.6.19.5**. Cab `Derail Risk N %` after Motors while boarded. Consist-max `derailBuildUp` (wagons included). No coupler. Always on (green &lt;15 %; yellow 15–94 %; red ≥95 %). Omit on foot. Dual junction Limit numbers stay through-only. UMM shows **2.6.19.5**.

- **Where:** Cab of a DE2 with cars, then a curve. **Mod Manager closed** after **UMM Version** `2.6.19.5`.
- **You should see:** `Derail Risk N %` on the **loco bar** after Motors, always present (green when safe). Chip is the worst car, not loco-only.
- **Do:** (1) UMM `2.6.19.5`. (2) Board solo — green 0 %. (3) Couple cars, drive a curve until yellow then red. (4) Unboard — chip gone.
- **PASS if:** chip stays on in cab through green/yellow/red; loco bar does not reflow off; red near a tip. **FAIL if:** chip vanishes under 15 %, coupler flicker, or job-bar RED is treated as Derail Risk.
- **Log:** `T2 gadgets change: … risk=99 lead=…` (max vs boarded loco). Vanilla `DERAILED! … TYPE: LocoDE2` at buildup ~0.6 matches ~100 %. Harvest: `Smoke_cab_always_shows_green_when_safe`, `Smoke_wagon_88_beats_lead_12`, `Smoke_loco_de2_L061_trip_at_threshold_is_red_100`, `Smoke_wagon_hotter_than_lead_emits_T2_risk_88_lead_12`.
- **Performance:** Cab `feature=0` class vs H110. Spawn graph/load OK. On-foot look H67/H72.
- **Log / screens (2026-08-24):** Player PASS on `2.6.19.4` (chip 99 % then `DERAILED! LocoDE2 L-061` at 0.600). `lead=` shipped `2.6.19.5` (waived re-smoke). Spawn `feature=16 load=1 max=96`. Cab `feature=0`.

**6.20 Job preview / Cancelled / license warn — Quick smoke.** Ships **2.6.20.1**. Unvalidated ticket: `Preview Nm` to Regular destroy (−30 m HUD buffer). Missing licenses: red `No license: TL2`. Abandon: red `Job … | Cancelled` ~8 s. Taken job still GO/HOLD/RED + Bonus. Wipe station is job-id origin (SW-SU at SW office is meters, not dest OUT). Dual junction Limit numbers stay through-only. Job-car AR stays **6.21**. UMM shows **2.6.20.1**.

- **Where:** SW job office on foot, then a walk toward the Regular edge with a ticket. **Mod Manager closed** after **UMM Version** `2.6.20.1`.
- **You should see:** Empty hands — no extra job bar. Pick up a ticket — `Preview ~900m` at the office (SU and SL tickets the same). LONG II ticket you cannot take — `No license: TL2 | Preview ~900m`. Walk out — yellow under ~200 m, red under ~50 m, then `Preview OUT`. Validate — Job + RED/GO + Bonus. Trash booklet — `Cancelled` ~8 s then hide.
- **Do:** (1) UMM `2.6.20.1`. (2) Empty hands — no Preview. (3) Pick `SW-SU-72` and `SW-SL-*` at the desk — both ~900 m. (4) Pick `SW-SU-34` — license + Preview meters. (5) Walk out with an SL ticket — OUT then step back. (6) Validate — taken bar. (7) Trash — Cancelled.
- **PASS if:** office SU/SL tickets show hundreds of meters; license chip on LONG II; walk-out OUT; take then Cancelled. **FAIL if:** SU tickets OUT at the office, Preview with empty hands, or Preview stays after take.
- **Log:** `T2 job appear: preview=910 license=— yard=SW`; `license=TL2`; `preview=OUT`; `status=Cancelled`; `T2 job hide`. Harvest: `Smoke_hold_overview_emits_T2_job_appear_preview`, `Smoke_preview_out_when_past_regular_edge`, `Smoke_no_license_fh_with_preview_emits_T2`, `Smoke_abandoned_taken_job_emits_T2_cancelled`, `Smoke_sw_su_ticket_at_sw_office_uses_job_id_origin_not_chain_dest`, `Smoke_sw_su_at_sw_office_emits_preview_900_yard_sw`.
- **Performance:** Cab `feature=0` class vs H112. Spawn graph/load OK. On-foot look H67/H72. Job bar still 4 Hz.
- **Log / screens (2026-08-24):** Player PASS. Office `SW-SU-72` / `SW-SU-34` `Preview 910m` (`yard=SW`); walk-out yellow/red/OUT then back; take `Job SW-SL-55 | RED`; trash `Cancelled`. Spawn `feature=17–18 load=1–2 max=96–99`. Cab `feature=0 max=59–78`.

**6.21 Job-car AR — Quick smoke.** Ships **2.6.21.6**. Taken job: purple square on pickup **spurs** (one pin per track, not per car), caption `jobId · spur · meters`. Distinct from green STN / cyan LOCO / amber PIN/radar. Hide when the job is taken and the consist is GO. Pin hops when you reach the **center of the next car** (not glued to lumber every glance). Cab throttle/indy/train stay put after you cut them (Incremental rising-edge). Dual junction Limit numbers stay through-only. UMM shows **2.6.21.6**.

- **Where:** Career yard with a taken shunting job (cars on a nearby spur). **Mod Manager closed** after **UMM Version** `2.6.21.6`.
- **You should see:** One purple square on the pickup spur, caption like `SW-FH-82 · C1O · 4m`. Not on the STN/LOCO stack unless that spur is at the edge.
- **Do:** (1) UMM `2.6.21.6`. (2) Empty hands — no purple job pins. (3) Take a job — purple on the pickup cars. (4) Walk along the consist — pin hops at the next car center. (5) Finish / GO — pins gone. (6) In the cab, notch throttle once then cut it — it stays.
- **PASS if:** one purple pin per spur; hops at next car center; hides on GO; throttle stays after cut. **FAIL if:** pin frozen hundreds of metres away, purple stuck in the STN corner while lumber fills the view, pins stay after GO, or throttle walks itself.
- **Log:** `T2 job-car-ar: scan job=… taken=1 n=…`; `clear (no job in hand)`; `hide job=… reason=ready`. Harvest: `Smoke_beside_consist_pin_stays_on_near_car_in_fov`, `Smoke_mid_flatcar_origin_off_axis_still_beats_far_car`, `Smoke_turn_around_uses_closest_car_in_fov`, `Smoke_walk_along_consist_pin_follows_nearest_car`, `Smoke_on_consist_does_not_write_throttle_indy_train`, `Smoke_cab_incremental_chatter_does_not_reclimb`.
- **Performance:** Cab `feature=0` class vs H114. Spawn graph/load OK. On-foot look H67/H72.
- **Log / screens (2026-08-24):** Player PASS “good enough” on `2.6.21.6` (`scan job=SW-FH-82 taken=1 n=1`). GO hide PASS on `2.6.21.1`. Throttle stay PASS on `2.6.21.4`. Spawn `feature=13 load=2 max=100`. Cab `feature=0 max=42`. On-foot `feature=1–3 max=46–47`.

**7.1 Three-Gate + hotkey / load gate — Quick smoke.** Ships **2.7.1.6**. On-consist Numpad Enter cycles reverser (cab or wagon); Numpad `.` TM fuse ON via Three-Gate. No YMS HUD on loading screen. UI tools: Ctrl+Home/End/F8 (either Control). Never Rewired for mod hotkeys. Dual junction Limit numbers stay through-only. UMM shows **2.7.1.6**.

- **Where:** Career yard. **Mod Manager closed** after **UMM Version** `2.7.1.6`. Steam `-nonvr`.
- **You should see:** No HUD during load; mouse after spawn; Ctrl+Home mark; Numpad Enter moves reverser in cab; Numpad `.` fuse.
- **Do:** (1) UMM `2.7.1.6`. (2) Load — no HUD on bar. (3) Quit/reload — mouse OK. (4) Ctrl+Home / Ctrl+End. (5) Cab Numpad Enter + Numpad `.`. (6) Optional wagon Numpad Enter.
- **PASS if:** load gate + mouse survive reload; Ctrl tools; Numpad Enter/`.` apply; `T2 three-gate: apply write=…`. **FAIL if:** dead mouse after first load, HUD on loading screen, or no three-gate lines.
- **Log:** `T2 three-gate: apply write=reverser` / `tm-fuse`; `T2 mark…` / `T2 path…`. Harvest: `Smoke_loading_screen_hides_hud_before_world_stream_complete`, `Smoke_loading_screen_does_not_poll_on_consist_keys`, `Smoke_tool_keys_require_control_chord`, `Smoke_numpad_enter_cycles_reverser_on_loco_and_wagon`.
- **Performance:** Cab `feature=0` class vs H117. Spawn graph/load OK. On-foot H67/H72.
- **Log / screens (2026-08-25):** Player PASS 1–5. Log: three-gate `reverser`×3 + `tm-fuse`×1; mark/path OK; NRE=0. Spawn `feature=6 load=2`; cab `feature=0 load=0`; mixed `feature=1–3`.

**7.2 Thermal governor — Quick smoke.** Ships **2.7.2**. When HUD Motors is Hot (cab TM TEMP yellow), Three-Gate soft-rolls throttle toward **75%** (Warning) or **55%** (Critical) at 5%/s — not a yank to idle. Cool motors passthrough. No ▼GOV flash / heat-inject this slice. Dual junction Limit numbers stay through-only. UMM shows **2.7.2**.

- **Where:** Career yard, **in the cab** of a DE2. **Mod Manager closed** after **UMM Version** `2.7.2`. Steam `-nonvr`, Cloud off.
- **You should see:** HUD **Motors Hot** (yellow) with cab TM TEMP yellow; HUD throttle eases toward ~75%.
- **Do:** (1) UMM `2.7.2`. (2) Load — no HUD on bar. (3) Board DE2, engine/fuse on, Forward. (4) Hold high throttle until Motors Hot. (5) Watch thr= ease toward 75%. (6) Ease off until Motors OK — auto-drop stops. (7) Short cab drive + on-foot look for hitch.
- **PASS if:** Motors Hot and throttle eases toward 75% (not pinned at 100%, not slammed to idle). **FAIL if:** Hot but throttle stays pinned, yank to idle, or HUD on loading screen.
- **Log:** `T2 thermal: soft-cap → 0.75 (Warning)` / `0.55 (Critical)`; `T2 thermal: cap release`; `[YMS v2] Thermal governor running.` Harvest: `Smoke_warning_hot_soft_rolls_throttle_toward_75`, `Smoke_critical_hot_soft_rolls_throttle_toward_55`, `Smoke_thermal_hot_above_cap_three_gate_applies_soft_write`, `Smoke_cap_release_when_cool`.
- **Performance:** Cab `feature=0` class vs H120. Spawn graph/load OK. On-foot H67/H72.
- **Log / screens (2026-08-25):** Player PASS 1–7. Log: Warning cap + roll `100→81`; Critical also fired; cap release; TMS Dead after dwell in yellow (expected). NRE=0. Spawn `feature=8 load=1 max=100`. Cab `feature=0 load=0 max=43–91`. On-foot `feature=1–2 max=44–97`.

**7.3 Auto-brake governor — Quick smoke.** Ships **2.7.3**. Engine on→off: Three-Gate soft-rolls train + independent toward full and throttle toward idle (~20%/s). Never auto-releases on start. Handbrakes untouched. No ▼GOV flash this slice. Dual junction Limit numbers stay through-only. UMM shows **2.7.3**.

- **Where:** Career yard, **in the cab** of a DE2. **Mod Manager closed** after **UMM Version** `2.7.3`. Steam `-nonvr`, Cloud off.
- **You should see:** On the loco bar, train brake and independent ease toward full, throttle eases to idle, after you shut the engine off. Starting the engine again must not dump that air.
- **Do:** (1) UMM `2.7.3`. (2) Load — no HUD on bar. (3) Board DE2, engine/fuse on, Forward, air released, some throttle. (4) Shut engine off — watch train + indy rise, throttle fall. (5) Start engine — brakes stay applied. (6) Short cab drive + on-foot look for hitch.
- **PASS if:** Shutdown rolls air on and throttle idle; start does not auto-release. **FAIL if:** Shutdown does nothing, levers yank instantly, start dumps the brakes, or HUD on loading screen.
- **Log:** `T2 autobrake: applying`; `T2 autobrake: apply done`; `[YMS v2] Auto-brake governor running.` Harvest: `Smoke_engine_off_falling_edge_starts_apply`, `Smoke_engine_start_does_not_auto_release`, `Smoke_shutdown_soft_rolls_brakes_and_throttle`, `Smoke_shutdown_at_speed_still_applies`, `Smoke_apply_done_when_air_full_and_throttle_idle`.
- **Performance:** Cab `feature=0` class vs H123. Spawn graph/load OK. On-foot H67/H72.
- **Log / screens (2026-08-26):** Player PASS 1–6. Log: two apply cycles `applying` → `apply done` at 100/100/0; start held air until player dumped; apply at ~20 km/h. NRE=0. Spawn `feature=5 load=1 max=92`. Cab `feature=0 load=0 max=45–98` (apply `max=56`). On-foot `feature=1 max=45–90`.

**7.4 Auto-coupler — Quick smoke.** Ships **2.7.4.1**. On-consist, Forward/Reverse: Three-Gate TryCouple only when Rear/Front is **green ≤0.5 m** and speed **≤8 km/h**. Does **not** replace zCouplers (knuckle physics). Never auto-uncouples. Off-train / Neutral do nothing. Dual junction Limit numbers stay through-only. UMM shows **2.7.4.1**.

- **Where:** Career yard, **in the cab** of a DE2 with a free cut. **Mod Manager closed** after **UMM Version** `2.7.4.1`. Steam `-nonvr`, Cloud off. zCouplers may stay on.
- **You should see:** Rear/Front yellow at ~4 m with **no** grab. Green ≤0.5 m crawl takes the couple; look-at **R+** (or F+); Cars/mass step up; Rear chip becomes **—**. No 20 km/h snap.
- **Do:** (1) UMM `2.7.4.1`. (2) Load — no HUD on bar. (3) Board, Reverse toward a cut at crawl. (4) Hold at ~4 m — no couple. Ease to green. (5) Confirm **R+** / Cars without walking the hose. (6) Short cab drive + on-foot look (uncouple walk is optional).
- **PASS if:** Yellow 4 m does not grab; green crawl couples without totaling; ground does not couple. **FAIL if:** grab at ~4 m, speed spike / loco dies, or HUD on loading screen.
- **Log:** `T2 autocouple: couple` → `T2 autocouple: done`; `[YMS v2] Auto-coupler running.` Harvest: `Smoke_rear_four_meters_does_not_couple`, `Smoke_does_not_couple_at_high_speed_after_snap`, `Smoke_in_scan_range_couples_when_on_consist`, `Smoke_off_train_does_not_couple`.
- **Performance:** Cab `feature=0` class vs H126. Spawn graph/load OK. On-foot H67/H72.
- **Log / screens (2026-08-26):** First ship `2.7.4` FAIL: couple at Rear 3.9 m, speed 1→23, loco totaled. Patch `2.7.4.1` PASS: crawl couple 1→2→4→6 at speed 1→0; screenshot Cars 6 **R+** Rear **—**; drive session no extra couple. NRE=0. Spawn `feature=3 load=1 max=96`. Cab drive `feature=0 load=0 max=80`. On-foot `feature=1 max=42–76`.

**7.5 Derail safety net — Quick smoke.** Ships **2.7.5.7**. Cab: when Derail Risk ≥65 %, Three-Gate idle throttle and raise independent + train (never dump). Posted Limit / Next are HUD-only — not a speed cap. Yellow 15 % is the chip; intervene stays 65 %. Dual junction Limit numbers stay through-only. UMM shows **2.7.5.7**.

- **Where:** Career yard, **in the cab** of a DE2. **Mod Manager closed** after **UMM Version** `2.7.5.7`. Steam `-nonvr`, Cloud off.
- **You should see:** ~50–60 km/h with Derail in yellow under 65 % leaves throttle yours (no red lever flash). Derail ~65 %+ idles and raises air until the chip drops back under 65.
- **Do:** (1) UMM `2.7.5.7`. (2) Load — no HUD on bar. (3) Board, run at ~50–60 with Derail under 65. (4) Over on purpose until Derail hits ~65 %+ — watch idle + air + red flash. (5) Ease until under 65 — governor lets go. (6) Short cab drive.
- **PASS if:** Under 65 % Derail is untouched even over a 40 board; ≥65 % yanks; start does not dump air. **FAIL if:** it still forces 40 while Derail is under 65, or HUD on loading screen.
- **Log:** `T2 limit-gov: soft-cap` only with `risk=` ≥65; `T2 limit-gov: cap release` when under; `[YMS v2] Limit auto-throttle running.` Harvest: `Smoke_60kmh_derail_40_does_not_trip`, `Smoke_hud_120_next_40_derail_44_does_not_cap`, `Smoke_derail_65_idles_throttle_and_raises_air`.
- **Performance:** Cab `feature=0` class vs H129 when not intervening. Spawn graph/load OK. On-foot H67/H72.
- **Log / screens (2026-08-26):** Player PASS. `2.7.5.7`: 40→65 km/h at risk 3–59 % with no `soft-cap`; three trips at ≥65 (74 / 72 / 69–101). NRE=0. Spawn `feature=5 load=1 max=83`. Cab `feature=0 max=71–96`. End wreck `feature=2 max=99`.

**8.1 Google Maps desk — Quick smoke.** Ships **2.8.1.1**. Desk is bind + Type A only: **no** Align, Path/ETA/Facing HUD, or switch throws on Set dest. Maps dest must **not** arm the 6.11 Path chip. UMM shows **2.8.1.1**.

- **Where:** Career yard, **Mod Manager closed** after **UMM Version** `2.8.1.1`. Cab or on foot.
- **You should see:** Centered **Dispatch desk (Dispatcher)** with City / Track / **Set dest** / Recheck / Clear / Hide. After Set dest, dest text on the desk — **not** Path N switch, **not** Align arrows.
- **Do:** (1) UMM `2.8.1.1`. (2) Load, close Mod Manager. (3) **Ctrl+Insert**, pick a city track, **Set dest**. (4) Hide. (5) Drive a minute. Do not hunt boards for this pass.
- **PASS if:** Desk sets dest; Path chip stays whatever it was before the click; cab hitch class matches 7.5 (`feature=0` after dest). **FAIL if:** Path N switch appears from Set dest alone, or cab hitch climbs like 2.8.1 dest-armed (`feature=11–32`).
- **Log:** `T2 maps-desk: open`; `catalog cities=` / `tracks=`; `T2 maps: dest set city=… track=…`; **no** `T2 path init` from that click; `[YMS v2] Maps desk running.` Harvest: `Set_yard_and_track_does_not_arm_end_path_check`, `Smoke_maps_dest_does_not_replace_end_path_check`.
- **Performance:** After dest, cab `feature=0` vs H132. Spawn graph/load OK. On-foot H67/H72.
- **Log / screens (2026-08-26):** Player PASS. `2.8.1` dest armed Path (`T2 path init` + cab `feature=11–32`) — **closed** in `2.8.1.1`. Re-smoke: dest `HMB-B7I` then `SM-B1O`; **zero** `T2 path init`; after dest cab `feature=0 max=49`; desk close `feature=2`; spawn `feature=4 load=1 max=98`. Later 45–65 km/h `feature=7–11` is FoT/gov (H87 class), not dest-BFS.

**8.2 Google Maps route + Align — Quick smoke.** Ships **2.8.2**. Desk Path/ETA/Facing on Set dest; **Align Route** throws via ThreeGate (Dispatcher). Maps dest still must **not** arm 6.11 Path chip. Live always-on route HUD while driving is **not** this slice. UMM **2.8.2**.

- **Where:** Career, SW yard. Cab or on foot on a named track (same-yard dest first). **Mod Manager closed** after UMM **2.8.2**.
- **You should see:** Desk **Path N switch** or **Path OK**, **ETA …**, **Set Forward/Reverse** after Set dest. **Align Route** throws switches (`T2 align: threw N`) or **already clear**.
- **Do:** (1) UMM `2.8.2`. (2) **Ctrl+Insert**, SW track e.g. **SW-B1S**, **Set dest**. (3) **Align Route** with Dispatcher license. (4) Short cab drive with desk closed.
- **PASS if:** Desk route chips live; Align throws when misaligned; 6.11 Path unchanged by Maps dest; cab typical **`feature=0–4`** (one **`feature=14`** window OK — H140 class). **FAIL if:** desk stuck **Path …** forever; Align no-op with license + misaligned switches; sustained cab **`feature=23`**.
- **Log:** `T2 route init:` / `T2 route change:`; `T2 align: threw N` or `already clear`; **no** `T2 path init` from Set dest alone. Harvest: `Smoke_route_prefers_through_lane_over_spur`, `RequiredFlips_lists_misaligned_only`.
- **Performance:** Set dest may hitch once (`feature=6 load=1` class). Cab **`feature=0`** gate vs H140. On-foot H67/H72.
- **Log / screens (2026-08-26):** Player PASS. SW→SW-B1S: `Path 6 switch` → Align `threw 6` → `Path OK`. SW TT `#Y`→SM = `no path` (TECH_DEBT / **8.4–8.5**). Cab one window `feature=14 max=100`; else `feature=0–4`. NRE **0**.
- **Log / screens (2026-08-27):** Re-confirm PASS. SW→SW-B1S Align `threw 6`; `SW-C1O`→MF `Path 4 switch` Align `threw 4`; `SW-C1O`→SM `no path` (TECH_DEBT / **8.4–8.5**). Desk `feature=4 max=98`; cab `feature=0–4`. NRE **0**.

**8.3 Digital Switch List — Quick smoke.** Ships **2.8.3.1**. Taken/held job → **Per job** tab → Load → Prep/Transit/Delivery; **Align step** / manual **Next** reuse **8.2**. No couple auto-advance (**8.10**). No arrive/CLEARED gate (**8.7**). UMM **2.8.3.1**.

- **Where:** Career, taken freight job (e.g. SW-FH-82). On foot or cab near origin track. **Mod Manager closed** after UMM **2.8.3.1**.
- **You should see:** Desk **Route | Per job**. Per job: job dropdown (taken + held), **Load Switch List**, step list with `▶`, **Align step** / **Next**. Footer shows **job id** (not `N cities / M tracks`).
- **Do:** (1) UMM `2.8.3.1`. (2) **Ctrl+Insert** → **Per job** → select job → **Load Switch List**. (3) **Align step** on Prep. (4) **Next** → Transit → Align. (5) Optional Delivery Align.
- **PASS if:** 3 steps load; Align throws or already clear; Next advances `▶`; footer = job id. **FAIL if:** no Per job tab; Load empty; Align no-op with license; Next stuck.
- **Log:** `T2 switch-list: loaded … · 3 steps`; `align step … Prep`; `next · step 2: Transit`; `T2 align: threw N` or `already clear`. Harvest: `SwitchListPlannerTests` / `SwitchListSessionTests`.
- **Performance:** Desk Load/Align `feature=4–7` (H141 class). Cab **`feature=0`** gate. On-foot H67/H72.
- **Log / screens (2026-08-27):** Player PASS on `2.8.3`. SW-FH-82 Prep→SW-C1O / Transit·Delivery→GF-D5I; Align Prep clear; Delivery `threw 6`; Next through complete. Polish `2.8.3.1` Per job footer = job id. NRE **0**.

**8.4 Town turntable dest — Quick smoke.** Ships **2.8.4**. Route Track **Turntable** → resolve `#Y-…` in sticky city; same **8.2** single path + one Align. **Not** multi-leg Align/Next (**8.5**). Set Reverse = gear hint only. UMM **2.8.4**.

- **Where:** Career SW (or any town with a TT). Cab or on foot in that yard. **Mod Manager closed** after UMM **2.8.4**.
- **You should see:** Track dropdown **Turntable** first; after Set dest **Path OK** (or Path N switch); Align threw / already clear. Reverse→forward driving is expected; no Switch List steps for TT.
- **Do:** (1) UMM `2.8.4`. (2) **Ctrl+Insert** → City **SW** → Track **Turntable** → **Set dest**. (3) **Align Route**. (4) Hide; short cab drive.
- **PASS if:** dest `#Y-…`; Path OK / Align works same-yard; cab `feature=0`. **FAIL if:** no Turntable token; `no turntable in …`; Path stuck; cab hitch climbs after Set dest.
- **Log:** `T2 maps: TT FoT=…`; `T2 maps: dest set city=SW track=#Y-…`; `T2 route:… Path OK`; `T2 align:…`. Harvest: `Smoke_SetDest_Turntable_binds_session_yard_and_anonymous_track`, `TurntableTrackResolverTests`.
- **Performance:** Desk Set dest `feature=3–4`. Cab **`feature=0`**. On-foot H67/H72. ETA may look fat (`#Y` cost inflation — accept).
- **Log / screens (2026-08-27):** Player PASS. SW Turntable → `#Y-#S1774#T`; Path OK; Align `already clear`; Set Reverse gear-only (no multi-leg). Cab gold `feature=0`. NRE **0**.

**8.5 Multi-step Maps — Quick smoke.** Ships **2.8.5.1**. Per job Load injects TurnAround before Prep when face-into-Exit; Align step / Next per leg; Clear wipes dest **and** list. Route tab stays single dest (TT NoPath may bind pivot→TT list). UMM **2.8.5.1**.

- **Where:** Career SW, taken job (e.g. SW-FH-82), loco facing into Exit. **Mod Manager closed** after UMM **2.8.5.1**.
- **You should see:** Per job list `Turn around → #Y-…` then Prep / Transit / Delivery; after Clear, empty hint (no stale legs).
- **Do:** (1) UMM `2.8.5.1`. (2) **Ctrl+Insert** → **Per job** → Load. (3) Confirm TurnAround first when facing into Exit. (4) Clear → list gone.
- **PASS if:** 4-step inject + Clear wipe. **FAIL if:** Prep-only when face-into-Exit; Clear leaves legs.
- **Log:** `T2 switch-list: inject TurnAround`; `loaded … 4 steps`; `T2 maps: dest clear` + `T2 switch-list: cleared`. Harvest: `SwitchListTurnAroundTests`, `Smoke_Clear_also_drops_switch_list_steps`.
- **Performance:** Desk Load `feature=3` class; cab gold **`feature=0`**; on-foot H67/H72.
- **Log / screens (2026-08-27):** Player PASS. SW-FH-82 TurnAround `#Y-#S1774#T` → Prep `SW-C1O` → GF-D5I; Clear wipe confirmed on `2.8.5.1`. YMS NRE **0** (DV quit NRE ignore).

**8.6 Loco turn + Bring — Quick smoke.** Ships **2.8.6.4**. Desk **Loco** → **Turn** (look-at solo → `MoveToTrack` 180°) / **Bring** (type → Lock → place). Coupled refuse. UMM **2.8.6.4**.

- **Where:** Career SW yard on foot. **Mod Manager closed** after UMM **2.8.6.4**.
- **You should see:** Loco tab Turn/Bring; `LOOK · DE2`; after Turn, nose reversed on same spot; Bring lands DH4 on locked rail.
- **Do:** (1) UMM `2.8.6.4`. (2) **Ctrl+Insert** → **Loco** → **Turn** → point at solo loco → **Turn look-at loco**. (3) Drive forward in the new nose direction. (4) Optional: couple a car → Turn aborts. (5) **Bring** → pick type → Lock → Bring now.
- **PASS if:** in-place reverse + drive OK; coupled refuse; Bring places on-rails loco. **FAIL if:** clear-space spin; no-op Turn; Bring moves derailed-only / wrong type.
- **Log:** `T2 loco-rerail: turn · … · MoveToTrack`; `place source · … · derailed=False`; `place started` / `place complete`. Harvest: `Smoke_Turn_refuses_coupled_consist`, `LocoRerailPolicyTests`, `Smoke_poll_miss_keeps_last_aim_lock_freezes`.
- **Performance:** Cab after turn **`feature=0`**. Desk/yard `feature` 1–8 class OK.
- **Log / screens (2026-08-27):** Player PASS. Turn DE2 `MoveToTrack`; drive correct order; coupled refuse edge PASS; Bring DH4 earlier PASS on `2.8.6.x`.

**8.7 Route pin + CLEARED — Quick smoke.** Ships **2.8.7.31**. SW sawtooth: latched pin → reverse past frog → **CLEARED** → **Ctrl+PageUp** Align → **Ctrl+PageDown** Next (desk stays closed). UMM **2.8.7.31**.

- **Where:** Career SW, **in the cab**, Mod Manager closed after UMM **2.8.7.31**. Steam `-nonvr`.
- **You should see:** After Set dest, a pin on the conflict switch (not the first flip). Desk **hides on reverse roll**. At CLEARED, Align throws without opening the desk. Next hides the pin; step 2 Set Forward.
- **Do:** (1) UMM `2.8.7.31`. (2) Stopped: **Ctrl+Insert** → Set dest Turntable (or B4L→TT list). (3) **Close desk**. (4) Set Reverse; roll through the pin. (5) At CLEARED: **Ctrl+PageUp**, then **Ctrl+PageDown**. (6) Set Forward; short roll.
- **PASS if:** `latch … reverse=1`; reverse cruise `feature=0`; `CLEARED`; `chord align` + threw; `chord next` + `hide next`. **FAIL if:** CLEARED while the pin is still in the windshield; Align needs the desk open; pin hides at CLEARED not Next.
- **Log:** `T2 route-pin: latch 990152 reverse=1`; `T2 maps-desk: hitch hide reverse`; `T2 route-pin: CLEARED`; `T2 maps-desk: chord align`; `T2 align: threw N`; `T2 maps-desk: chord next`; `T2 route-pin: hide next`. Harvest: `HtpSwTurntableLiveDumpTests`, `ArPinHitchGateTests`, `Smoke_8_7_align_next_chords_are_tool_keys`, `RouteClearanceEvalTests`.
- **Performance:** Reverse to pin **`feature=0`**. Align/Next window may `feature=6` (throw). Forward leftover `feature=8` is not 8.7 gold — isolate later. Spawn graph/load OK. On-foot H67/H72.
- **Log / screens (2026-08-29):** Player PASS on `2.8.7.31`. Latch 990152; reverse hide; CLEARED; chord align threw 1; chord next hide. Reverse windows `feature=0`. Align window `feature=6 max=100`. Next 30 s Forward `feature=8 max=98 below=21`. NRE **0**.

**9.1 PID speed hold — Quick smoke.** Ships **2.9.1.12** (hold) / **2.9.1.14** (takeoff + coast). Gear → bleed → notch ramp → hold ~25 (±2 coast). `MUOverride` write; do not Hud-round bleed. UMM **2.9.1.14**.

- **Where:** Career SW, **in the cab** of a DE2, Mod Manager closed after UMM **2.9.1.14**. Steam `-nonvr`. **Close Maps desk** after Set dest.
- **You should see:** No auto-drive until Set dest; reverse; indy bleeds; throttle notches gradually; near 25 thr coasts (indy only above ~27); Motors OK through CLEARED.
- **Do:** (1) UMM `2.9.1.14`. (2) Load — loco idle. (3) Desk defaults SW + Turntable; mouse pointer mode while open. (4) Set dest; close desk. (5) Watch takeoff → hold → CLEARED.
- **PASS if:** idle before Set dest; thr modest by ~10 km/h; hold near 25; Motors OK. **FAIL if:** auto-drive on load; thr≈100 by ~10; Motors=Dead after CLEARED.
- **Log:** `gear` → `brakes` → bleed → `hold`/`thr-on` → climb → coast/hold → `CLEARED`. Harvest: `HtpPidStraightHoldTests` (takeoff/deadband/chatter), `YmsRouteSessionsTests`, `MapsDeskDefaultsTests`, `PidSpeedWriteTests`.
- **Performance:** Cab **`feature=0`** desk closed. Spawn graph/load OK.
- **Log / screens (2026-08-30):** PASS on `2.9.1.12` hold. PASS on `2.9.1.14` takeoff/coast/desk/mouse; ±2 coast at ~27 accepted. Hitch not pasted (player: smooth).

**9.1.3 Path Limit span — Quick smoke.** Ships **`2.9.1.37`** (Win 5). Bezier span distance; SW leave **40 then 60**. Win **5.1** (roster refresh) is a separate ship.

- **Where:** Career SW, **in the cab**, Maps dest set past windshield **60**, Mod Manager closed after UMM **`2.9.1.37`**.
- **You should see:** Next **40** with distance counting down (not frozen in the teens). Pass the **40** → Limit **40**, Next **60**. Pass the **60** → Limit **60**. Never **50** on that rail.
- **Do:** (1) UMM **`2.9.1.37`**. (2) Set dest; close desk. (3) Leave SW; drive past **40** then **60**. (4) Optional long run: boards only scanned within **2500 m** of spawn until Win **5.1** — tunnel **30** may not appear.
- **PASS if:** `take 40` then `take 60` in log with `src=span`; Limit matches; cab `feature=0`. **FAIL if:** Next freezes; Limit stays **120**; no `take` lines.
- **Log:** `T2 limit filo: take 40@… src=span` · `take 60@… src=span` · `limit change: 40 auth=posted next=60` · `walker-path n=10`. Harvest: `HtpCurvedSweepTests`, `HtpWalkerReverseWalkTests`, `HtpLiveHopEvaluateTests`.
- **Performance:** Cab **`feature=0`**; spawn `feature=7–11` class; maps open spike known. See PERFORMANCE_LOG H166.
- **Log / screens (2026-09-01):** PASS on **`2.9.1.37`**. `take 40@0` → `take 60@0`; later `take 40`/`take 60`/`take 50` on long run. Tunnel **30** not in log (roster never refreshed). NRE **0**.

**9.1.3 Win 5.1 travel roster refresh — Quick smoke.** Ships **`2.9.1.39`**. Re-scan posted signs within 2500 m after ~1 km **driven** (not only XZ). Gold: tunnel **30**.

- **Where:** Career SW, **in the cab**, same Maps dest as Win 5. Mod Manager closed after UMM **`2.9.1.39`**.
- **You should see:** After ~1 km driving, log `T2 limit filo: warm · travel · …`. Tunnel **30** becomes Next then Limit **30** (`take 30@0 src=span`). Win 5 **40→60** still holds near SW.
- **Do:** (1) UMM **`2.9.1.39`**. (2) Set dest; close desk. (3) Drive SW→FH past Win 5 boards and through tunnel **30**. (4) Pause/unpause is OK — does not reset roster.
- **PASS if:** `warm · travel` in log; `take 30@0`; Limit **30**; cab `feature=0` class. **FAIL if:** No `travel` after 1+ km driven; **30** never in HUD/log.
- **Log:** `T2 limit filo: warm · travel ·` · `take 30@0 src=span` · `limit change: 30 auth=posted`. Harvest: `NeedsTravelRefresh` tests, `Win5_1_seed_refresh_behind_blocks_ghost_take`.
- **Performance:** Cab `feature=0` windows; see PERFORMANCE_LOG H169.
- **Log / screens (2026-09-01):** PASS on **`2.9.1.39`**. `warm · travel` ×2 · `take 30@0` · `limit change: 30`. NRE **0**.

**13.1 Step runner (GO / Human / Done) — Quick smoke.** Ships **`2.13.1`**. Desk Switch List: **GO** on Transit arms PID (even with Cruise off); **Done** on Prep/Delivery; **Next** blocked during GO or Human hold.

- **Where:** Career SW yard, **in the cab** on a DE2, Maps desk open (**Ctrl+Insert**). UMM **`2.13.1.x`**.
- **You should see:** Load a job → Switch List. Face-into-Exit SW-FH-82: step 1 **Set Reverse · Past switch → SW-B4L until CLEARED** with pin **990152**; after CLEARED, **Next** → TT; then Prep **SW-C1O**. **Done** on Prep/Delivery (no **Next** until Done). On **Transit**: **GO** button; loco holds ~25 km/h with Cruise off.
- **Do:** (1) UMM **`2.13.1.10`** (inbound pin). (2) Take SW-FH-82; face into Exit; **Load Switch List**. (3) Reverse through 990152 until CLEARED; Align/Next → TT. (4) After TT, **Next** to Prep — **known gap:** no switch-back pin on TT → C1O (**Path 7 switch**). (5) **Done** on Prep; **GO** on Transit when that smoke is in scope.
- **PASS if (inbound, `2.13.1.10`):** Step 1 dest **SW-B4L** (not `#Y-#S989#T`); pin **990152**; Align blocked until CLEARED; Next → TT. **FAIL if:** dest S989; **Path OK / already clear** with no pin on step 1.
- **Log:** `T2 switch-list: inject … approach SW-B4L` · `dest list-load pin-corridor → #Y-#S1774#T` · `T2 route-pin: latch 990152`. Harvest: `Smoke_SW_FH_82_TT_step1_dest_is_loco_side_of_990152_not_S989`, `Smoke_SW_FH_82_list_load_past_switch_must_not_Recheck_Maps_to_B4L`, `HtpStepRunnerCp2Tests`.
- **Performance:** Cab `feature=0` class with desk closed during GO drive.
- **Log / screens (2026-09-01):** Inbound TT pin PASS on **`2.13.1.10`**. Step 1 B4L + 990152 CLEARED → TT. Step 3 Prep **Path 7 switch** / no pin = next slice (not a FAIL of inbound).

**Cab hitch isolation (2.6.16.13) — PASS 2026-08-23.** Overlay off, DE2 cab, reverse with consist. Feel: no once-per-second stutter. Log: drive `feature=0`; prior overlay-off drive `feature=15`.

**Epic 6 wave smokes** — one session per wave when that wave’s matrix rows ship; do not re-smoke the full v1 matrix each time.

**Logging (volume without noise):** lifecycle + one `T2 <topic>` per meaningful transition. Prefer many *named* events over one dump. Forbidden: per-frame HUD/telemetry, string-built payloads on the hot path, “debug” traces left on after the story ships.

After each smoke, harvest any new lock into Core Tier 1 ([TEST_TDD.md](.cursor/skills/TEST_TDD.md) → *Evidence loop*). Append hitch classes to [docs/PERFORMANCE_LOG.md](docs/PERFORMANCE_LOG.md) (`HitchBand`). Do not treat a quiet log after the 100 ms gate as “no hitch.”

### Lifecycle (every session, once Main loads)

- `[YMS v2] Mod Loaded. Awaiting toggle.`
- On → `[YMS v2] Activated. GC Probe running.` … `[YMS v2] Posted board index running.` then `[YMS v2] Limit display running.` … `[YMS v2] Clock running.` then `[YMS v2] Marked running.` then `[YMS v2] Station running.` then `[YMS v2] Job bar running.` then `[YMS v2] On-consist control running.` then `[YMS v2] Three-Gate write path running.` then `[YMS v2] Thermal governor running.` then `[YMS v2] Auto-brake governor running.` then `[YMS v2] Auto-coupler running.` then `[YMS v2] Limit auto-throttle running.` then `[YMS v2] PID speed governor running.` then `[YMS v2] Train gadgets running.` then `[YMS v2] Rear/Front proximity running.` then `[YMS v2] Maps desk running.`
- Off → `[YMS v2] Deactivated cleanly.`
- No YardMasterSuite exceptions / stack traces

---

**Handoff:** Document the exact commands you use for coverage in AGENT_HANDOFF.md so agents can run them consistently.
