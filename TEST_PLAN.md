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

**Logging (volume without noise):** lifecycle + one `T2 <topic>` per meaningful transition. Prefer many *named* events over one dump. Forbidden: per-frame HUD/telemetry, string-built payloads on the hot path, “debug” traces left on after the story ships.

After each smoke, harvest any new lock into Core Tier 1 ([TEST_TDD.md](.cursor/skills/TEST_TDD.md) → *Evidence loop*). Append hitch classes to [docs/PERFORMANCE_LOG.md](docs/PERFORMANCE_LOG.md) (`HitchBand`). Do not treat a quiet log after the 100 ms gate as “no hitch.”

### Lifecycle (every session, once Main loads)

- `[YMS v2] Mod Loaded. Awaiting toggle.`
- On → `[YMS v2] Activated. GC Probe running.` then `[YMS v2] HUD running.` then `[YMS v2] Loco listener running.` then `[YMS v2] Control telemetry running.` then `[YMS v2] Consist listener running.` then `[YMS v2] Heading listener running.` then `[YMS v2] AR overlay running.` then `[YMS v2] Mailbox drain running.`
- Off → `[YMS v2] Deactivated cleanly.`
- No YardMasterSuite exceptions / stack traces

---

**Handoff:** Document the exact commands you use for coverage in AGENT_HANDOFF.md so agents can run them consistently.
