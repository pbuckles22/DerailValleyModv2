# PM_PLAN — Yard Master Suite v2

Official **backlog**. Cross off here when a story ships; refresh [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md) + [AGENT_HANDOFF.md](AGENT_HANDOFF.md) → *Current state* in the same change.

**Background:** [docs/YMS_v2_Onboarding_Guide.md](docs/YMS_v2_Onboarding_Guide.md) · [docs/HTP.md](docs/HTP.md) · [docs/Versioning_and_Release_Strategy.md](docs/Versioning_and_Release_Strategy.md)

| Mark | Meaning |
|------|---------|
| `[x]` | Done |
| `[~]` | In progress / partial |
| `[ ]` | Backlog |

---

## Fast track (read top → bottom — do not skip)

**North star:** take → **yard/Prep steps 1–5 (**13**)** → stack/validate → **haul steps 6–7 + drop/pay (**15**)** → Maps desk **14** → multi-job **10**.

**Now (2026-09-04):** **13.2.4** `[x]` on **`main`** (`2.13.2.4.3`). Next = Prep handbrake release / **13.2.5** when asked.

| # | Story | Done bar |
|---|-------|----------|
| **1** | **13.4** `[x]` | Cab PASS **`2.13.4.18`**. CMPH 2026-09-04. |
| **2** | **13.2.4** `[x]` | Cab PASS **`2.13.2.4.3`**: Prep creep ~5; auto Stop GO at tip ≤1.5 m; soft couple; sticky hold (no shove / no re-arm). 100% health. CMPH 2026-09-04. Rem→crawl + handbrake release deferred. |
| **3** | **13.2.5–13.2.6** + **13.3** `[ ]` | Two-car stack + Validate before haul. |
| **4** | **15.1** `[ ]` | Haul Transit (step 6). |
| **5** | **15.2** `[ ]` | Auto delivery drop (step 7). *Was 13.5.* |
| **6** | **15.3** `[ ]` | Turn-in + payout. *Was 13.6.* |
| HOLD | **13.2.3** | FILO queue — park until after walk-in. |

**Do not:** start **15** before Prep stack / Validate path is ready; start **9.2** / **14** / **10** / **11** / **12** while this queue is open; re-open **13.2.4** for rem→crawl.

**Critical path:** 8.7 `[x]` → 9.1 `[x]` → 13.1 `[x]` → 13.6.1 `[x]` → **13.4** `[x]` → **13.2.4** `[x]` → **13.2.5** → 13.3 → **15.1–15.3** → 14 → 10.

**HTP CP3 (13.4):** multi-leg Core walk steps 1–5; fail-closed Derail / no path; stop at Prep spur.

---

## Open epics (detail — same order as Fast track)

- [ ] **Epic 13 — Autonomous yard / Prep loop (single job)** — **Phase C.** After **8.7** + **9.1**. Switch List **steps 1–5** (yard through Prep approach). Ships as **2.13.x**. Haul (step 6) + Delivery (step 7) live on **Epic 15**. **Simulator:** State machine on Topology + Physics; each story has a named Core gate before cab smoke.

  - [x] **13.1 Step runner (GO / Human / Done)** — **GO** = PID + Maps; **Human** = pause until **Done**; **Next** on HumanHold when a later row exists (last Human is Done-only). Cab PASS **`2.13.1.20`**: 7-row SW-FH-82 (Past switch B4L → Set Forward to TT → TT spin → leave `#Y-#S1512#T` CLEARED → Prep C1O → Transit → Delivery). CMPH 2026-09-02. Do **not** treat this land as closing **Epic 13**.
    > As a dispatcher, I want GO on transit and to mark human-only steps done myself.
    >
    > **Simulator gate (CP2):** Same corridor + PID ticks. GO runs the Physics loop on a Transit step; HumanHold **Next** if PeekNext; last Human Done-only. Fail-closed if no path / not CLEARED for Align.
  - [x] **13.1.15 Harvest logging** — Shipped **`2.13.1.15`**. Change-only T2 (and Core formatters) so Player.log + HTP can prove: extra job-car pins, dest remaining / dest-yard behind, and which writer moved throttle while Cruise is off. No product HUD/AR/PID behavior change this ship.
    > As a maintainer, I want discrete log lines I can fold into named Core tests so the next cab trip does not guess at throttle, pins, or a missed dest yard.
    >
    > **Ship:**
    > 1. `T2 job-car-ar: n=K ids=…` on pin-count / id change (not per-frame).
    > 2. `T2 route: rem=Nm dest=…` on km buckets while a Maps/Switch List dest is set (desk open **or** closed).
    > 3. `T2 route: dest-yard behind` when the dest yard is behind the consist.
    > 4. `T2 writer: pid|thermal|derail-gov|none thr= spd= limit= risk=` when a governor actually writes, or when throttle drops and Cruise/GO are off.
    >
    > **Simulator gate:** Named tests for interned / change-only strings and the dest-behind + job-car n= gates (pure inputs). Do not wait on a new graph dump.
    >
    > **Out of scope:** Fixing purple pins (**6.21.7**), remote take (**13.6.1**), Next chip (**9.1.4**), always-on Rem HUD chrome (**14.3**).
  - [ ] **13.2 Yard prep — stack job cars** — **Split into sub-stories** (each = own ship + smoke). Parent absorbs deferred **8.10**. Full stack = **13.2.1** … **13.2.6** PASS.

    - [x] **13.2.1 Couple auto-advance** — On **7.4** success during **Prep** step, auto **Next**. Cab PASS **`2.13.2.1`**: Prep → Transit on `autocouple: done` (no Next press). CMPH 2026-09-02. *Was **8.10**.*
      > As a dispatcher, I want the checklist to move when I couple, not only when I press Next.
      >
      > **Simulator gate (CP4):** Couple-success input → step index++. No tick loop required.
    - [x] **13.2.2 Prep track arrival** — Loco on prep leg dest track → T2 `prep: at track` + desk `· at track`; at-spur latch (fail-closed if ambiguous). Does **not** Next the list. Cab PASS **`2.13.2.2`**. CMPH 2026-09-02.
      > As a shunter, I want to know I am on the right pickup track before I reverse to the cars.
      >
      > **Simulator gate:** Along-track position on dest track id → at-track; ambiguous track → no advance.
    - [ ] **13.2.3 FILO pickup queue** — Core order of task cars; desk “Next pickup: …”; **6.21** pin follows active queue head (Tier 1 named smoke scenario).
      > As a yard master, I want to know which car is next in FILO order.
      >
      > **Simulator gate (CP5 part):** Queue head identity from job cars; pin/target follows head after couple-advance.
    - [x] **13.2.4 Creep-to-couple** — CMPH **`2.13.2.4.3`** on **`main`** (2026-09-04). Prep GO creep **5 km/h**; tip ≤1.5 m / mech → auto Stop GO + sticky hold (no yard-chain re-arm shove); **7.4** couple; cab PASS soft couple (100% health). **Out (deferred):** rem→crawl approach polish; consist handbrake release after couple; multi-car (**13.2.5**).
      > As a shunter, I want the loco to inch up to the job car without me on the throttle.
      >
      > **Simulator gate (CP5 part):** Creep ticks toward a stubbed car pose; speed ≤ `AutoCoupleAssist.MaxCoupleSpeedKmh`; green/scan clearance → Stop GO; refuse slam speed.
    - [ ] **13.2.5 Between-car shunt** — After couple, short pull-forward to clear knuckle; queue advances; repeat **13.2.4** for car 2 on **same spur** (two-car smoke max).
      > As a yard master, I want space to reach the next job car without uncoupling what I already have.
      >
      > **Simulator gate (CP6 part):** After couple, pull-forward distance; queue head = car 2; second creep+couple.
    - [ ] **13.2.6 Prep complete** — All task cars in consist → auto-advance Prep boundary; arms **13.3** Validate. Tier 1: consist ⊆ job task cars.
      > As a dispatcher, I want Prep to finish when every job car is coupled, not when I guess.
      >
      > **Simulator gate (CP6 part):** Consist ⊆ task cars → Prep complete; missing car → stay on Prep.
  - [ ] **13.3 Validate gate** — Confirm consist vs job; **Validate** arms haul **GO** (**Epic 15** / step 6).
    > As an engineer, I want to sign off the train before the mod drives away.
    >
    > **Simulator gate (CP7):** Match → haul Transit GO armed (**15.1**); mismatch → fail-closed (no GO).
  - [x] **13.4 Autonomous yard / Prep transit** — CMPH **`2.13.4.18`** on **`main`** (2026-09-04). Cab PASS: held → Load → steps **1–5** → designed crash at cars (manual TT + Next). Locks: CLEARED crawl-stop; sticky **OnTable**; snap indy go-stop; rem≤d_stop mid/Prep aim; **yard crawl 10** on to-TT + Prep. Fail-closed Derail / no path. **Out (deferred):** haul 6–7 → **Epic 15**; auto-couple (**13.2.4**); auto TT spin; rem→crawl (Gemini A).
    > As an engineer, I want the yard Switch List through Prep to drive itself so I only handle the couple.
    >
    > **Simulator gate (CP3):** Multi-leg walk steps 1–5; CLEARED + Align + Facing + GO; stop on TT; stop at Prep spur; fail-closed Derail / no path. Not one-leg-only.
  - [x] **13.6.1 Remote take** — CMPH **`2.13.6.1`** on **`main`**. Cab PASS: GO/desk took Preview (`src=go`); job bar RED→GO. *Stays on Epic **13** (take paperwork). Auto turn-in moved to **15.3**.*
    > As a dispatcher, I want to take the job from the desk when I start the trek so I do not miss payout because I forgot the station machine.
    >
    > **Simulator gate:** Preview + desk/GO arm → taken=true when the API allows; refuse when office required.
    >
    > **Out of scope:** Auto turn-in / payout (**15.3**); Validate (**13.3**).

- [ ] **Epic 15 — Haul + delivery autonomy** — **Phase C2.** After **13.4** through Prep `[x]` (and ideally **13.3** Validate). Switch List **steps 6–7** + turn-in. Ships as **2.15.x**. *Was **13.5** / **13.6**.* Before **Epic 14** Maps desk.

  - [ ] **15.1 Haul Transit GO** — Autonomous road leg (Switch List **step 6** / Transit → delivery yard). Align, CLEARED, Facing, fail-closed Derail / no path. TakeJob arm on haul GO after Prep stays as today.
    > As an engineer, I want the loaded train to drive the haul without me on the throttle.
    >
    > **Simulator gate (CP8 part):** Haul Transit GO walk after Prep complete / Validate; CLEARED + PID ticks; fail-closed Derail.
  - [ ] **15.2 Auto delivery drop** — Length-aware **fully in stall**; stop; uncouple/handbrake; advance to turn-in. *Was **13.5**.*
    > As an engineer, I want to know when the train is fully in the delivery track.
    >
    > **Simulator gate (CP8 part):** Consist envelope vs stall span → fully in; then stop + uncouple/handbrake; step → turn-in.
  - [ ] **15.3 Turn-in + payout** — Auto or one-click complete; T2 payout line. *Was **13.6**.* **13.6.1** remote take already shipped under Epic **13**.
    > As an engineer, I want to get paid without walking every UI step if the drop was correct.
    >
    > **Simulator gate (CP9):** Turn-in complete event from a valid drop; payout UI stays Tier 2. **CP10** chains CP0–CP9.

### After haul path

- [ ] **Epic 14 — Maps desk upgrade** — **After 13+15, before 10.** IMGUI desk stays through yard/Prep + haul autonomy so GO/Human/Done exist before a rewrite. Ships as **2.14.x**. **Not HTP** (HTP stays inside **8.7** / **9.1** / **13.x** / **15.x**). UniverseLib only if hitch probe fails IMGUI and the player accepts a second mod.

  - [ ] **14.1 Desk Close chrome** — Hide → Close. *Was **8.11**.*
    > As a dispatcher, I want Close to mean the desk is gone so I do not reopen it while rolling.
  - [ ] **14.2 Track amenity filter** — Omit dead turntable / service picks. *Was **8.12**.*
    > As a dispatcher, I want the track list to skip amenities I cannot use.
  - [ ] **14.3 Live always-on route HUD** — Rem/ETA with the desk closed (TECH_DEBT from **8.2**).
    > As an engineer, I want Path/ETA without keeping the desk open.
  - [ ] **14.4 uGUI Maps desk** — Native uGUI or UniverseLib after IMGUI hitch **fails** `GcCadenceProbe`. One rewrite after **13** chrome exists.
    > As a dispatcher, I want a Maps desk that does not hitch the cab and can skip Layout.

- [ ] **Epic 10 — Multi-job Maps + optimizer** — **Phase D.** After **13+15** PASS (and **14** if the desk rewrite has started). Ships as **2.10.x**. **Simulator:** reuse Epic **13/15** state machine on N jobs; no new physics engine.

  - [ ] **10.1 Multi-job tour board** — N jobs; one board (multi-job license).
  - [ ] **10.2 Pickup order optimizer** — FILO / nearest / yard-cluster.
  - [ ] **10.3 Tour Align + Next** — Shared **13.1** GO/Human semantics.
  - [ ] **10.4 Job + route profit optimizer** — Pick jobs for max payout; feeds **10.2** + **13**.

### Deferred (not on fast track)

- [ ] **Epic 9 — Speed / brake brains** — **Critical path: 9.1** after **8.7** PASS. Ships as **2.9.x**. **Simulator:** Physics expansion (tick loop) lives in **9.1**; do not invent a second physics engine for **13**.

  - [x] **9.1 PID speed governor** — **Hold + takeoff PASS (`2.9.1.14`, 2026-08-30).** Three-Gate throttle to target km/h on active Maps/Switch List leg. **Target cap = min(request, Posted Limit)**. **No derail/grade term in PID v1** — **7.5** separate; **7.2** thermal ceiling. Never dump air. DE2 HUD notches + `MUOverride` write path. **Patches closed:** takeoff slew (`ThrottleRaisePerSecond` 0.05); ±2 km/h coast band (thr off at/above target; indy only above `target+2` — accepted, not a bug); world-leave clears dest/list so PID does not auto-arm on reload. Optional **9.1.1** derail-aware target trim only if **13.4** trips **7.5** a lot (not MPC).
    > As an engineer, I want the loco to hold a safe speed on a Maps leg so I am not babysitting throttle between switches.
    >
    > **Simulator gate (Physics — CP1):** Tick-based 1-D loop in Core — **green**. Cab: idle until Set dest → bleed → gradual takeoff → hold ~25 (±2 coast) → CLEARED; Motors OK.
  - [x] **9.1.2 Path Limit look-ahead** — Posted signs on the Maps corridor become Limit/Next (SW leave: **40 then 60**, never throat **50**; tunnel **30** on long run). Math **Wins 0–6** `[x]`. **Win 7** Unity pin smoke **parked** — path provider pivot **9.1.3**; product lock met on **`2.9.1.37`** + **`2.9.1.39`** smoke. Learnings: [docs/9.1.2_Path_Limit_Learnings.md](docs/9.1.2_Path_Limit_Learnings.md). **13.1** unblocked after **9.1.3** CMPH.
    - [x] **Win 0** — Ladder documented (learnings + this walk).
    - [x] **Win 1** — `CorridorLateralMeters` **12** + synthetic tests (`2.9.1.15`).
    - [x] **Win 2** — Board+path harvest codec + one-shot dump (`2.9.1.16`). Folded `Fixtures/Htp/boards-sw-2026-08-31.txt`.
    - [x] **Win 3** — HTP eligibility walk (geometry only) (`2.9.1.17`).
    - [x] **Win 4** — Symmetric junction dual must not govern (`2.9.1.18`).
    - [x] **Win 5** — Polarity remaining + same-rail behind-take ~250 m (`2.9.1.19`).
    - [x] **Win 6** — Evaluate = Maps authority; HTP Limit walk 40→60, never Next=50 (`2.9.1.20`).
    - [~] **Win 7** — Unity wire + pin smoke (`2.9.1.21`–`.22`). **FAIL** at pin — `TryBuild` truncates in reverse. **Superseded:** **9.1.3** Evaluate + **`2.9.1.37`** cab smoke PASS (40→60).
    > As an engineer, I want Limit/Next to follow the signs on my thrown Maps path so PID caps on real posted speed.
  - [x] **9.1.3 Core graph walker** — Core `CorePathfinder` + live `TrackPathAhead` feed `PostedLimitFunnel.Evaluate`. **Keep** `PostedPathAheadGate` + Evaluate. Dump **raw local graph** (≤2.5 km); HTP walks thrown junctions → `PathSegmentAlong[]` → Evaluate. **Bezier span** for distance (not chord dot-product). Cab smoke **40 then 60** PASS **`2.9.1.37`**; tunnel **30** PASS **`2.9.1.39`** (Win **5.1** travel roster refresh).
    - [x] **Win 0** — `TrackGraphDump` one-shot: tracks + junctions + boards in 2.5 km (`2.9.1.23`). Player sits still, switch thrown.
    - [x] **Win 1** — Graph codec → `CoreTrack` / `CoreJunction` / boards (`2.9.1.24`).
    - [x] **Win 2** — `CorePathfinder` walks dumped graph 1600 m from loco (`2.9.1.25`).
    - [x] **Win 3** — HTP routing walk: path includes harvest **60** (`2.9.1.26`).
    - [x] **Win 4** — Feed walker output into existing Evaluate (`2.9.1.27`).
    - [x] **Win 5** — Bezier span distance + `BoardTakeDetector`; cab smoke take **40** then **60** (`2.9.1.37`). `HtpCurvedSweepTests` ordered sweep green.
    - [x] **Win 5.1** — Travel roster refresh (~1 km driven + XZ); `SeedRefreshBehind`; tunnel **30** cab smoke PASS (`2.9.1.39`; `.38` XZ-only trigger missed winding SW→FH).
    > As an engineer, I want look-ahead path built in Core with true arc distance so curved rail takes signs and long runs still see new boards.
  - [x] **9.1.4 Next-chip** — HUD Limit/Next from Evaluate. Cab PASS **`2.9.1.40`**: `take 40@0` → `sticky=40 next=60` with meters (not dash). Increase boards show Next; behind-take still updates Limit. CMPH 2026-09-02. **Out:** PID cap; **9.2**.
    > As an engineer, I want the next posted speed on the chip with km when it is ahead, and Limit to follow the board I actually take — not a dash while a 60 is 100 m out.
    >
    > **Simulator gate:** Named HTP walk: sticky **40**, path span includes **60@100 m** → Next **60** with meters (not dash); increase boards still show Next; behind-take still updates Limit. Reuse `PostedLimitFunnel.Evaluate` + existing SW board fixture — do not cab-debug while this walk is red.
    >
    > **Out of scope:** PID cap change; **9.2** derail-stress cruise; Win 7 Unity pin smoke.
  - [ ] **9.2 Predictive speed (look-ahead)** — **After 13.4** (keep panacea order: **9.1** → **13** → then **9.2** if flat PID is not enough). **Not brake-only:** (1) **predictive brake** into Posted / curves / pin; (2) **predictive throttle** when an upcoming grade needs momentum. **North star (player, 2026-09-02):** Posted Limit is a **HUD suggestion**; cruise should chase **predicted derail stress + grade**, max safe speed — do not treat posted as the hold target forever. **Look-ahead entry gate (worry here, not earlier):** before MPC cab work, Core must **read** upcoming corridor grade/profile along the Maps path and replay it in the Physics walk. If we cannot harvest look-ahead then, **9.2 is blocked** — do not discover that mid-cab. Posted path-ahead (**6.10**) is not full grade look-ahead. Do not shove grade/derail into **9.1** “when ready.”
    > As an engineer, I want the loco to brake and power for what is ahead so hold speed survives hills without thrashing.

- [ ] **Epic 11 — Digital Catalog** — **DEFER** until **10** or player asks. **2.11.x**.

  - [ ] **11.1 Digital Catalog** — Order keys / flags / tools.

- [ ] **Epic 12 — Roadside Assist** — **DEFER** until autonomous loop stable. **2.12.x**.

  - [ ] **12.1 Emergency fuel/oil call-out** — Paid call-out at stranded loco.

## Later (not on panacea critical path)

v1 parking lot + **deferred 8.x tester tools**. Desk chrome lives on **Epic 14**. Promote only when autonomous loop (**13**) blocks or user asks.

- **8.8 License spawn** — iced; tester spawn trickle.
- **8.9 Place ghost / Snap polish** — Bring/Turn UX polish.
- **Pickup “clear to couple”** — Rear/Front green at job car (**13.2** may subsume).
- **Multi-step shunting jobs (complex)** — after **13** single-job PASS; may extend **13.1** before **10**.
- **Epic 11 Catalog / Epic 12 Roadside** — see epic headers (**DEFER**).

- **Desk auto-height (14.5 candidate)** — Switch List window grows with step count + wrapping coach lines (player 2026-09-02). Parked; not in the Now queue.
- **UMM AR toggles (6.22 candidate)** — Show nearest locos / show job-car pins. Still waits on ModSettings; **6.21.7** is hide-phantoms, not a toggle.
- **Consist length HUD (6.23 candidate)** — `Length Nm` chip; Mass beside Cars. Length already used for frog pin; chip not painted.
- **Align-on-Next** — Stay **manual**. Align throws only after CLEARED (**8.7**). Optional “Next at CLEARED = Align then advance” is in scope for full **13.4** (steps 1–5), not a separate story.
- **UMM ModSettings** — when the first player toggle exists (loco radar **6.16** “Show nearest locos”).
- **Job-car PNG** — **6.21** shipped a purple quad; dedicated art waits like **6.17**.
- **Job-car pin glued to lumber** — **6.21** accepted hop at the next car **center** (`2.6.21.6`). Tighten FOV only if asked.
- **On-consist wagon lever writes** — disabled **2.6.21.3** (Rewired chatter). Cab Incremental is Harmony rising-edge (no hold-repeat). Numpad Enter reverser + Numpad . TM fuse stay.
- **Motors Off chip** — engine stall vs TM Dead; parked during **6.21** throttle hotfix.
- **Loco radar licence filter** — **6.16** requirement 1, parked behind `LocoRadarLicenseGate.FilterEnabled`. Filtering alone empties the radar at low career stages; promote with a "dim the unlicensed" or UMM-toggle design, not as a plain hide.
- **Top-band AR slide** — v1 4.9 (sticky row under HUD for now).
- **AR sticky ↔ object glide** — v1 `ArMarkerTransition` (~1 s ease). v2 hops. Not a 6.4 blocker.
- **Pause overlay hide** — Esc pause keeps HUD/AR (player still in world). Launcher hide is `HudWorldSession`. Hide-on-pause only if product asks.
- **AR in-view-only** — no false edge-stick on occluded targets (v1 4.9 / 4.10 follow-on; house @ 120 m on a freight flank).
- **Coupler tension HUD** — v1 parked; cut from Derail Risk. Consist-max `derailBuildUp` shipped **6.19**.
- **PgUp/PgDn turntable** — local QOL (v1 Epic 4); not remote CTC (**3.3** cut).
- **Session reset hotkey** — e.g. Shift+F6: time ~07:00, weather, invalidate/refresh jobs (sandbox).
- **Player headlamp** — camera-mounted spot (concept `L`); hands-free vs flashlight.
- **Anti-Wheelslip**
- **Startup Assist** — needs **7.1** Three-Gate.
- **Auto-Service / Auto-Shop** — overlap check vs **11.1** Catalog; paid emergency liquids → **12.1** Roadside Assist.
- **Manual Transmission Override (DM3)** — reverser must leave Neutral to unlock throttle; DM3 has no MU (knowledge note, not a ship).
- **Mounting Suite / precision mounting**
- **Engine Temp Soft Governor** — only if distinct from **7.2** TM thermal.
- **Flight-sim HUD** — v1 parking (not the Monitor stack).
- **Harmony missing-target self-disable** — v1 **0.4** still `[~]`: log + disable on broken signatures, no session crash.
- **Thermal ▼GOV flash / F5–F9 inject** — tester; v1 Motors heat inject parked @ **0.6.21**. F11 all-licenses already has `SmokeLicenseGrantGate`.
- **1.17 #4 behind-seed** — ice if Limit stays empty when a board is &lt;600 m behind.
- **Do not restore:** Recommended/Brake chips, Pos HUD, in-HUD Version, Comms overlay, remote throw, yard schematic, `Next: Farm [km]`, delete-cars, RCL remote, custom JobGenerator, click-to-switch 2D CTC map, Stress-as-occupancy (Limit stays posted).

---

## Shipped archive (closed — do not re-smoke)

- [x] **Epic 0 — Repo bootstrap** — Folder layout, public GitHub repo, AgenticTemplate + v1 rules delta. **Closed 2026-08-12.**

  - [x] **0.1 Docs layout** — Move YMS background into `docs/`; archive Predictive Braking templates.
  - [x] **0.2 Public repo** — `pbuckles22/DerailValleyModv2` on `main`.
  - [x] **0.3 Agentic overlay** — Upstream AgenticTemplate; v1 delta rules; stack-specific handoff/docs.

- [x] **Epic 1 — Phase 1 Heartbeat** — Core infrastructure before any train telemetry. **Closed 2026-08-12.**

  - [x] **1.1 Solution scaffold** — `YardMasterSuite.sln`, csproj, `info.json`, `Directory.Build.targets.example`.
    > As a maintainer, I want a net48 UMM project that builds so Phase 1 code has a home.
  - [x] **1.2 YmsEventBus** — Central Type A `Action` bus with `ClearAllSubscriptions()`.
    > As a subscriber, I receive primitive/struct payloads with zero alloc and can unsubscribe on disable.
  - [x] **1.3 package.ps1** — Deploy Release DLL + `info.json` into `Mods\YardMasterSuite\`; optional zip.
    > As a maintainer, I can install a local build so later stories can smoke in-world.
  - [x] **1.4 GcCadenceProbe** — Silent frametime monitor that logs GC/stutter warnings.
    > As a developer, I am warned when a feature introduces a hitch.
  - [x] **1.5 GuiContentCache / StringBuilder pool** — No concatenated strings in render loops.
    > As a player, the HUD does not hitch from string allocs.

- [x] **Epic 2 — Phase 2 Senses** — Event-driven telemetry (no polling). **Closed 2026-08-12.**

  - [x] **2.1 Loco state listener** — Board/unboard → cached current loco + bus event. (`info.json` **2.2.1**, Tier 2 PASS 2026-08-12)
  - [x] **2.2 Control telemetry** — Throttle, indy, train brake, engine/dynamic brake, reverser only when levers move. (`info.json` **2.2.2**, Tier 2 PASS 2026-08-12)
  - [x] **2.3 Trainset topology** — Consist length/weight on coupler events only (keeps listening on foot). (`info.json` **2.2.3**, Tier 2 PASS 2026-08-12)

- [x] **Epic 3 — Phase 3 Display Shell (infra)** — Zero-alloc HUD/AR shell before heavy math / v1 parity. **Closed 2026-08-17** at **3.3.1**. Full v1 diagnostic look/feel → **Epic 6**.

  - [x] **3.1 HUD manager** — Top bar + always-on compass. (`info.json` **2.3.1**, Tier 2 PASS 2026-08-13)
  - [x] **3.2 AR overlay engine** — Fixed 3-slot buffer; office STN + own-loco LOCO; mid-edge fan; hitch-summary. (`info.json` **2.3.2**, Tier 2 PASS 2026-08-17)
  - [x] **3.3 Centered HUD stack** — v1 `MonitorHudStackLayout`; centered bars. (`info.json` **2.3.3**)
  - [x] **3.3.1 HUD v1 chrome parity (stop-state patch)** — v1 bar chrome; product labels; **4.3** `UsableTrainGate`; four-bar stack slots; AR sticky Y publish. (`info.json` **2.3.5.1**, Tier 2 PASS 2026-08-17)
  - [x] **3.4 Speed telemetry + chip** — Event path + product labels in **6.8**. (`info.json` **2.6.8**, Tier 2 PASS 2026-08-20)
  - [x] **3.5 Limit display** — Posted authority in **6.9**; Next in **6.10**. Geometry Limit **retired**.

- [x] **Epic 4 — Phase 4 Heavy Engines (infra)** — Time-sliced brains (Job/coroutine). **Closed 2026-08-25** at **4.3**. PID/MPC → **Epic 9**.

  - [x] **4.1 Type B mailbox** — `ConcurrentQueue<T>` drain to Type A on the main thread. (`info.json` **2.4.1**, Tier 2 PASS 2026-08-17)
    > As a heavy engine, I can push a struct off the worker and the HUD receives it without touching Unity APIs from that thread.
  - [x] **4.2 Track graph builder** — Yield across frames; publish via **4.1**. (`info.json` **2.4.2**, Tier 2 PASS 2026-08-17)
  - [x] **4.3 Geometry scanner (A116)** — Shipped 2026-08-17; **retired for Limit** in **6.9** (posted boards only). Scanner + Core curve ladder removed.

- [x] **Epic 6 — Diagnostic HUD (v1 parity)** — Player-visible match to v1 **1.17 + Epic 4 HUD QOL** (minus explicit v2 cuts). Matrix: [docs/HUD_v1_Parity_Matrix.md](docs/HUD_v1_Parity_Matrix.md). **Closed 2026-08-24** at **6.21** (`info.json` **2.6.21.6**).

  - [x] **6.1 Always-on bar** — Heading + Clock (`DateTimeWrapper` world time). Marked / Path → **6.11**; Station → **6.12**. (`info.json` **2.6.1**, Tier 2 PASS 2026-08-18).
  - [x] **6.2 Look-at bar** — Pipe / Handbrake / Couplers / Car / Track / Cargo / Loco type; identity-only `T2 look-at bar`. Job chip → **6.13**. (`info.json` **2.6.2**, Tier 2 PASS 2026-08-17).
  - [x] **6.3 Usable target** — Spherecast + look-at wins + usable consist walk + consist publish on look-at (`info.json` **2.6.3**, Tier 2 PASS 2026-08-17).
  - [x] **6.4 AR stack sync** — Edge STN/LOCO sit **below** the HUD stack (`HudStackLayout.LastBottomGuiY`). OnObject stays on the world object. (`info.json` **2.6.4**, Tier 2 PASS 2026-08-17). Glide + pause-hide → Later.
  - [x] **6.5 Mass + Grade** — Cab Mass + Grade; change-only gadget gate (`info.json` **2.6.5**, Tier 2 PASS 2026-08-18).
  - [x] **6.6 Load + Motors + Fluids** — Cab Fuel / Oil / Load / Motors from sim (`info.json` **2.6.6**, Tier 2 PASS 2026-08-19).
  - [x] **6.7 MU sync** — Cab yellow `MU idle` / red `MU desync`; quiet when synced (`info.json` **2.6.7**, Tier 2 PASS 2026-08-19).
  - [x] **6.8 Full lever + Speed + Limit** — Live levers + Speed + Limit chip; omit dashes; no Next (`info.json` **2.6.8**, Tier 2 PASS 2026-08-20). Geometry Limit later retired in **6.9**.
  - [x] **6.9 Posted board index** — Posted sticky Limit; geometry scanner ripped. (`info.json` **2.6.9**, Tier 2 PASS 2026-08-20).
  - [x] **6.10 Next + distance** — Next chip on Limit; meters when close (`NextLimitReveal`). Dual numbers stay through-only. (`info.json` **2.6.10**, Tier 2 PASS 2026-08-20). **FILO funnel restore** (`2.8.1.16`, 2026-08-26): EventBus HUD; takes while rolling; Next after direction lock. Parallel Next metres + leftover cab hitch (`feature` 15–17) **out of 6.10**.
  - [x] **6.11 Marked** — Home / Shift+Home return chip; End / Shift+End Path check (sticky origin on look-away). (`info.json` **2.6.11**, Tier 2 PASS 2026-08-20).
  - [x] **6.12 Station chip** — In-zone `Station SM NE 84m` / `here` from office transform. Omit outside job-generation zone. Fluids `Next: Farm [km]` stays cut. AR pin/icons/radar → **6.15–6.17**; job-car ■ → **6.21**. (`info.json` **2.6.12**, Tier 2 PASS 2026-08-20).
  - [x] **6.13 Active job bar** — taken `Job · GO/HOLD/RED · Bonus`; look-at Job chip (`GetJobOfCar`). Preview / license / Cancelled → **6.20**; job-car AR → **6.21**. On-consist cab keys stacked by request. (`info.json` **2.6.13**, Tier 2 PASS 2026-08-21).
  - ~~**6.14 Track + Cargo**~~ — **Cut.** Folded into **6.2**. Look-at Job chip is **6.13**.
  - [x] **6.15 Pin AR slot** — Home mark world PIN (amber quad); hide within 8 m; Shift+Home clears. PNG stays **6.17**. (`info.json` **2.6.15**, Tier 2 PASS 2026-08-21).
  - [x] **6.16 Loco radar** — v1 **4.10** parity: nearest **other locos** (licence filter **parked**) as amber AR, **≤600 m**, up to 3, own consist excluded. Caption is **loco name / meters** (two lines). Cyan LOCO uses `LastLoco` **else usable consist loco**. Hide AR on loading / pause / save overlays. F8 is licence **debug** (not F11 — game stats overlay). Cab hitch: overlay `FindObjectOfType` capped at 2 lookups/world (`2.6.16.13`, cab `feature=0`). On-foot LastLoco excludes the whole trainset (`2.6.16.14`). (`info.json` **2.6.16.14**, Tier 2 PASS 2026-08-23).
    > As a yard master, I want to see where other locos are so I can walk to one and MU without searching the whole yard.
    >
    > **Deferred on top of v1 parity** (user, 2026-08-23):
    > 1. **Licence filter** — parked: `LocoRadarLicenseGate.FilterEnabled = false`. DE2-only save saw **0 of 9** nearby locos when filtering. Needs dim-unlicensed or UMM toggle before re-arm.
    > 2. **UMM “Show nearest locos”** — waits on ModSettings.
    > 3. **PNG icons** — shipped **6.17**.
  - [x] **6.17 PNG icons** (48px + dark plate) — v1 **4.9**: loco / house / pin under `Mods/.../Icons/`; radar reuses loco art with amber tint (v1 4.10). Tint secondary. `2.6.17.2` also stops on-consist lever redirect when standing on any loco (MU double-notch). (`info.json` **2.6.17.2**, Tier 2 PASS 2026-08-23).
  - [x] **6.18 Rear/Front proximity** — v1 **4.11–4.12**: Reverse → `Rear N.Nm`; Forward → `Front …`; Neutral omit. Green ≤0.5 m + couple-scan; yellow through 30 m; open tip `Front —` / `Rear —`. No “Couple ready”. (`info.json` **2.6.18**, Tier 2 PASS 2026-08-24).
    > As a driver reversing to pick up a train, I want distance before impact and a clear cue when I am close enough to brake and couple.
  - [x] **6.19 Derail Risk** — cab chip after Motors while boarded: consist-max `derailBuildUp` % of game threshold (worst car, wagons included). No coupler. Always on in cab (green &lt;15 %; yellow 15–94 %; red ≥95 %). Omit on foot. Fail-closed `— Derail Risk`. **Not** Limit occupancy. (`info.json` **2.6.19.5**, Tier 2 PASS 2026-08-24).
    > As an engineer, I want Derail Risk on the cab bar for the worst car in my train so I can slow down before a tip-over anywhere in the consist.
  - [x] **6.20 Job preview / Cancelled / license warn** — v1 **4.8** remainder: inventory `Preview Nm` to Regular destroy (−30 m HUD buffer); Abandoned/Expired → red Cancelled ~8 s; `No license: TL2` (etc.). Wipe station is job-id origin (`SW-SU-72` → SW), not dest. Taken job still **6.13**. Job-car AR → **6.21**. (`info.json` **2.6.20.1**, Tier 2 PASS 2026-08-24).
  - [x] **6.21 Job-car AR** — v1 **4.8** @ **0.6.16**: purple ■ on taken-job **task cars**, **one pin per pickup spur**. Distinct from STN / LOCO / PIN / radar. Quad this story (PNG Later). Hide on taken GO. Pin hops at the next car center (accepted). Cab Incremental rising-edge (chatter hotfix). (`info.json` **2.6.21.6**, Tier 2 PASS 2026-08-24).
    > As a yard master with a job in hand, I want the cars I still need marked in the world so I am not reading numbers off the look-at bar.
  - [x] **6.21.7 Extra purple pins** — Shipped **`2.13.1.16`**. Hide pins once those task cars are **on the consist**, even if paperwork is Preview. Never spawn a pin on `#Y` connector / turntable tracks. Still **one pin per real pickup spur**. Uses **13.1.15** `T2 job-car-ar: n=` change lines.
    > As an engineer on the road, I want only the cars I still need to pick up marked, not extra purple pins on anonymous tracks after I have already coupled.
    >
    > **Simulator gate:** Core `ShouldShowAr` / slot pick: `taken=0` + all expected cars on consist → **0** pins; `#Y-*` track ids do not create slots; pickup spur still one pin. Named test from the smoke captions.
    >
    > **Out of scope:** UMM toggle (**6.16** Later); FILO queue-head pin (**13.2.3**); PNG art.

- [x] **Epic 7 — Governors (v1 Epic 2)** — Soft writes via Three-Gate. **Closed 2026-08-26** at **7.5** (`2.7.5.7`).

  - [x] **7.1 Three-Gate helper** — v1 **2.1**: Integrity → State Registry → Safety → Soft Write; fail closed. On-consist reverser/TM fuse are the first writers. Loading-screen world gate + Ctrl/Numpad hotkey policy (no Rewired). (`info.json` **2.7.1.6**, Tier 2 PASS 2026-08-25).
    > As a maintainer, I want one write path so every governor aborts the same safe way.
  - [x] **7.2 Thermal governor** — v1 **2.2**: soft-roll throttle when Motors Hot (Warning 75% / Critical 55%) via Three-Gate. (`info.json` **2.7.2**, Tier 2 PASS 2026-08-25).
    > As an engineer, I want the mod to soft-cap throttle when motors overheat so I avoid TM Offline.
  - [x] **7.3 Auto-brake governor** — v1 **2.3**: engine on→off soft-rolls train + independent toward full and throttle toward idle; never auto-release on start. (`info.json` **2.7.3**, Tier 2 PASS 2026-08-26)
    > As an engineer, I want air applied when I shut down so an unpowered loco is not free to roll.
  - [x] **7.4 Auto-coupler** — fail-closed on-consist couple assist (not zCouplers physics, not RCL). Green ≤0.5 m + ≤8 km/h Three-Gate TryCouple; finish hose/cocks if already knuckled. (`info.json` **2.7.4.1**, Tier 2 PASS 2026-08-26)
    > As a shunter, I want a fail-closed couple assist without a full RCL remote (v1 never shipped RCL).
  - [x] **7.5 Limit auto-throttle** — v1 parking **2.4** evolved: Derail Risk ≥65 % Three-Gate idle + raise air (never dump). Posted / Next stay HUD-only. Speed-hold / look-ahead → **9.1**. (`info.json` **2.7.5.7**, Tier 2 PASS 2026-08-26)
    > As an engineer, I want a consist-safety net when I am not watching Derail Risk, without a posted-speed cop.

- [ ] **Epic 8 — Google Maps / Dispatcher** — **8.7 `[x]`.** Rest deferred to Later. Type B Dijkstra + Three-Gate throws. **Not** a 2D click-map. **Simulator:** Topology expansion lives in **8.7** (see Headless foundation).

  - [x] **8.1 Google Maps desk** — v1 Dispatch Desk Route tab: city / track / **Set dest** / Recheck. **Ctrl+Insert**. Click publishes Type A (`YmsEventBus.OnMapsDestCommand`). **No pathfind / Align / switch throws on the click.** Maps dest does **not** arm 6.11 Path check (`2.8.1.1`). Align + Path/ETA/Facing HUD + Three-Gate throws = **8.2**. (`info.json` **2.8.1.1**, Tier 2 PASS 2026-08-26).
    > As a licensed dispatcher, I want Google Maps–style Set dest (city → track) without hitching the cab.
  - [x] **8.2 Google Maps route + Align** — v1 **3.5** “Google Maps Align Route”: Type B pathfind (`RoutePlanReady` mailbox); desk Path / ETA / Facing (bucket T2); **Align Route** throws via ThreeGate. Dispatcher-gated. Through-lane bias. Live always-on route HUD + `#Y` turntable→cross-city → **8.4–8.5** / TECH_DEBT. (`info.json` **2.8.2**, Tier 2 PASS 2026-08-26).
    > As a licensed dispatcher, I want the path drawn like Maps, then Align so I am not hiking every lever.
  - [x] **8.3 Digital Switch List** — v1 **3.6**: taken job → Prep / Transit / Delivery; each step uses **8.2** Align + Next. Manual Next only (couple auto-advance / arrival-track split → **8.10**). Per job footer shows job id (not Route catalog counts). (`info.json` **2.8.3.1**, Tier 2 PASS 2026-08-27).
    > As a dispatcher, I want the job Switch List so I do not re-pick city/track three times.
  - [x] **8.4 Town turntable dest** — v1 Town TT: Set dest **Turntable** in sticky yard (same Maps engine as **8.1**). Single dest + one Align; multi-leg Align/Next → **8.5**. FoT cached (v1 0.6.49 stutter). (`info.json` **2.8.4**, Tier 2 PASS 2026-08-27).
    > As an engineer in town, I want Set dest to the yard turntable.
  - [x] **8.5 Multi-step Maps** — v1 **3.7**: TurnAround inject, reverse-into leg, current-leg AR on the Switch List (Route tab stays single dest). Clear wipes dest + list (`2.8.5.1`). (`info.json` **2.8.5.1**, Tier 2 PASS 2026-08-27).
    > As an engineer facing the wrong way, I want Switch List to send me to the turntable then reverse into the spur.
  - [x] **8.6 Loco turn + re-rail place** — Loco-only. **Turn** = look-at solo loco → `MoveToTrack` same footprint 180° (not TeleportTrainset spin; not on-rails `Rerail` no-op). **Bring** = type dropdown → on-rails source → Lock aim → `TeleportTrainset`. Coupled refuse; derailed sources refuse. Bring Flip removed. (`info.json` **2.8.6.4**, Tier 2 PASS 2026-08-27).
    > As an engineer or tester, I want to reverse a loco where it sits (nose where the toes were) so I do not need a turntable and do not slide into neighbors.
    > As an engineer or tester, I want a dropdown of loco types, then place one from anywhere on the map onto the rail I am looking at (e.g. DH4 into SW when the yard has none).
  - [x] **8.7 Route pin + CLEARED + switch-back coach** — **ON CRITICAL PATH (closed).** Length-aware frog; pin latch (Dijkstra first-stop); reverse travel at Set dest; Align/Next Ok only on CLEARED; pin hides on **Next**. Closed-desk **Ctrl+PageUp** / **Ctrl+PageDown**. HTP CP0 live SW dump + sketch walks green. Ritual: Set dest stopped → **close desk** → roll; chords at CLEARED (do not reopen desk). (`info.json` **2.8.7.31**, Tier 2 PASS 2026-08-29).
    > As a driver on a sawtooth, I want CLEARED only after I am past the throw switch, then Align throws, then Facing tells me which way to go.
    >
    > **Simulator gate (Topology — CP0):** Named Core walk on a harvested or frozen corridor (SW→TT first): pin = conflict switch not first flip; pose sequence stays At-switch while the pin is in the windshield; CLEARED only after leading edge + consist length past the frog; Align/Next Ok only on CLEARED. Fold live `graph.txt` / `corridor.txt` into fixtures when a dump exists — do not keep cab-debugging the pin while this walk is red. Cab smoke confirms world chrome only.
  - [ ] **8.8 License spawn** — **DEFER** → Later.
  - [ ] **8.9 Place ghost / Snap polish** — **DEFER** → Later.
  - [ ] **8.10 Switch List couple auto-advance** — **DEFER** standalone; absorb into **13.2**.
  - [ ] **8.11 Desk Close chrome** — **DEFER** → **14.1**.
  - [ ] **8.12 Track amenity filter** — **DEFER** → **14.2**.

**HTP / renumber notes:** see git history before 2026-09-03 for thin-13.4 wording; haul/delivery moved to Epic **15**. Clear-line pin → **8.7** revisit.
