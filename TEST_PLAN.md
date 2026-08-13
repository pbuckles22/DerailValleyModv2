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

Requires UMM (`Mods\` under the game root) and `package.ps1`. Deploy before asking for smoke ([deploy-before-smoke.mdc](.cursor/rules/deploy-before-smoke.mdc)).

```powershell
dotnet build YardMasterSuite.sln -c Release
powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

### Evidence

| Source | Where | Proves |
|--------|--------|--------|
| **Player.log** | `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log` | Load, toggle, discrete `T2 …`, exceptions |
| **UMM Logs** | Mod Manager → Logs | Same lines (subset) |
| **HUD** | In-world Display Shell | Compass + top bar match bus values; no launcher HUD |

**1.4 hitch probe:** silent on the launcher / during load (no world session). In-world, a hitch **over 100 ms** may emit `T2 hitch-spike: dt=…ms` (optional `gc0=+N`). Yard frames under 100 ms are silent. At most one log per second. No per-frame logs.

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

**Logging (volume without noise):** lifecycle + one `T2 <topic>` per meaningful transition. Prefer many *named* events over one dump. Forbidden: per-frame HUD/telemetry, string-built payloads on the hot path, “debug” traces left on after the story ships.

After each smoke, harvest any new lock into Core Tier 1 ([TEST_TDD.md](.cursor/skills/TEST_TDD.md) → *Evidence loop*).

### Lifecycle (every session, once Main loads)

- `[YMS v2] Mod Loaded. Awaiting toggle.`
- On → `[YMS v2] Activated. GC Probe running.` then `[YMS v2] HUD running.` then `[YMS v2] Loco listener running.` then `[YMS v2] Control telemetry running.` then `[YMS v2] Consist listener running.` then `[YMS v2] Heading listener running.`
- Off → `[YMS v2] Deactivated cleanly.`
- No YardMasterSuite exceptions / stack traces

---

**Handoff:** Document the exact commands you use for coverage in AGENT_HANDOFF.md so agents can run them consistently.
