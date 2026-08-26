# V1 feature coverage (audit)

**Date:** 2026-08-21 (v2 IDs remapped 2026-08-25: leftover work is Epic **7+**)  
**Sources:** v1 [`PM_PLAN.md`](../../DerailValleyMod/PM_PLAN.md) + [`doc/requirements/product.md`](../../DerailValleyMod/doc/requirements/product.md); v2 [`PM_PLAN.md`](../PM_PLAN.md); Gemini download `YardMasterSuite V2 PM Plan.md` (2026-08-21).

v1 is a **reference library**. Port product behavior into v2 Type A/B; do not copy Update loops.

**Verdict on Gemini’s 2026-08-21 plan: do not adopt.** It collides with shipped Epic **6**, invents systems v1 never shipped, and omits most real v1 gameplay.

---

## Gemini errors (do not implement)

| Gemini story | Problem |
|--------------|---------|
| **Epic 6** Route planning (desk, Dijkstra, Align) | **ID collision.** v2 Epic 6 is Diagnostic HUD (**6.1–6.21**, **closed**). |
| **7.1** ConsistManager spawn/delete 50 cars | v1 **never deletes cars** (product non-goal). Spawn is **3.1b iced** license re-rail, not a custom consist manager. **No `ConsistManager.cs` in v1.** |
| **7.2** JobGenerator custom waybills | v1 does **not** generate jobs. Job HUD is inventory-gated (**4.8** / v2 **6.13**). Catalog is order keys/flags (v1 **5.1** / v2 **9.1**). **No `JobGenerator.cs` in v1.** |
| **Epic 8** RCL / `RclController` | v1 has **on-consist cab keys → front loco** (shipped on **6.13**), not a shunting remote. Remote throw **3.3 CUT**. **No `RclController.cs` in v1.** |
| **Epic 9** Overhead map + click-to-switch | v1 yard schematic **4.13 CUT**. Remote junction click **3.3 CUT** (walk/throw is the grind). Align is **Dispatcher-gated 3.5**, not a 2D CTC map. Architecture Phase 5 “2D Map UI” was a shorthand — do not treat as product. |
| **8.2** “polling listener” | Contradicts v2 “polling is dead.” Use change/deadband Type A like existing gadgets. |

Keep Gemini’s **intent** where it matches v1: **Google Maps** Set dest + Type B pathfind + thresholded ETA logs + Align via ThreeGate. That is Epic **8.1–8.2**, not Epic 6.

Gemini **consist spawner / JobGenerator** does **not** help Maps hitch. v1 Maps stutter was stacked Dijkstra/FoT/map-warm/`eta-refresh` after Set dest — already owned by **8.1–8.2** Type B + log cadence. Trickle-spawn only matters if **8.8** ever instantiates many cars (iced). Do not build a ConsistManager to “fix routing.”

---

## Stay cut (v1 already cut — do not restore)

| v1 | Why |
|----|-----|
| **1.16** Recommended Limit / soft Brake chip | Product lock |
| Geometry-ahead Limit | Retired in v2 **6.9** |
| **1.13** Pos on HUD | Bundle B.1 |
| In-HUD Version chip | UMM only |
| **3.2** Comms overlay | Desk hosts tools |
| **3.3** Remote switch / turntable from HUD | Career grind; PgUp/PgDn QOL only |
| **3.4** as a player chore | Internal engine only |
| **4.5** `Next: Farm [km]` | Clutter |
| **4.13** yard mini-map schematic | AR GPS + desk TT |
| Deleting cars / jobs to clear yards | Product non-goal |
| Full autopilot | Product non-goal |

---

## Coverage matrix (v1 → v2)

### HUD / AR (Epic 6)

| v1 | v2 | Status |
|----|----|--------|
| 1.1–1.2 Speed, Mass, Grade | 6.5, 6.8 | `[x]` |
| 1.3–1.6 / 4.2 / 4.4 Look-at integrity, cargo, track, loco type | 6.2 | `[x]` |
| 1.7–1.9 Load, Motors, Fuel/Oil | 6.6 | `[x]` |
| 1.10–1.11 / 1.17 Posted Limit + Next | 6.9–6.10 | `[x]` |
| 1.12 Heading | 6.1 | `[x]` |
| 1.14 Marked | 6.11 | `[x]` |
| 1.15 MU idle/desync | 6.7 | `[x]` |
| Clock | 6.1 | `[x]` |
| Derail Risk (consist-max `derailBuildUp`, **6.19**) | **6.19** | `[x]` — coupler HUD cut; not v1 worse-of Stress |
| 4.1 Targeting 0.15 m / 250 m | 6.3 | `[x]` |
| 4.3 Hide loco bar | 3.3.1 | `[x]` |
| 4.6 Station chip | 6.12 | `[x]` |
| 4.7 Centered stack | 3.3.1 | `[x]` |
| 4.8 Active job GO/HOLD/Bonus | 6.13 | `[x]` |
| 4.8 Preview Nm / Cancelled / license warn | **6.20** | `[x]` — origin yard from job id (`2.6.20.1`) |
| 4.8 Job-car purple AR @ **0.6.16** | **6.21** | `[x]` — one pin per spur; hop at next car center (`2.6.21.6`) |
| 4.9 Pin AR | **6.15** | `[x]` |
| 4.10 Loco radar | **6.16** | `[x]` (PNG **6.17**; licence filter parked) |
| 4.9 PNG + plate | **6.17** | `[x]` |
| 4.11–4.12 Front/Rear proximity | **6.18** | `[x]` |
| End Path check | 6.11 | `[x]` (desk path is Epic **8**) |

### Governors (v1 Epic 2 → v2 Epic 7)

| v1 | v2 | Status |
|----|----|--------|
| 2.1 Three-Gate | **7.1** | `[x]` |
| 2.2 Thermal governor | **7.2** | `[x]` |
| 2.3 Auto-brake on engine off | **7.3** | `[x]` |
| On-consist cab keys (**0.6.81**) | stacked **6.13** | `[x]` |
| Auto-coupler (not a v1 numbered story) | **7.4** | `[ ]` |
| Parking candidate **2.4** Limit auto-throttle | **7.5** | `[ ]` until asked |
| 4.4 PID / 4.5 MPC | **10.1 / 10.2** | v2-new; not v1 |

### Yard / Dispatcher (v1 Epic 3 → v2 Epic 8)

| v1 | v2 | Status |
|----|----|--------|
| 3.5 **Google Maps** Align Route (Set dest, Path/ETA/Facing, Dispatcher throw) | **8.1–8.2** | `[ ]` |
| Dispatch Desk Set dest / Recheck | **8.1** (Google Maps desk) | `[ ]` |
| 3.6 Digital Switch List | **8.3** | `[ ]` |
| Town Turntable Set dest | **8.4** | `[ ]` |
| 3.7 Multi-step Maps (TT inject, reverse-into, leg AR) | **8.5** | `[ ]` |
| 3.1 Move cars here / teleport (never delete) | **8.6** | `[ ]` |
| 3.1 follow-on place ghost / Snap under-mesh | **8.9** | `[ ]` |
| 3.6 parking couple auto-advance / arrival-track split | **8.10** | `[ ]` |
| Maps/SL pin + CLEARED (latched frog; length-aware) | **8.7** | `[ ]` |
| 3.1b license spawn | **8.8 iced** | iced until 8.5 |
| Sticky yard / MFMB fence Core | retain with desk/Limit | not a player epic |

### Catalog / parking (v1 PM parking lot)

| v1 | v2 | Status |
|----|----|--------|
| 5.1 Digital Catalog | **9.1** | `[ ]` |
| Session reset hotkey | Later | `[ ]` |
| Player headlamp | Later | `[ ]` |
| Anti-Wheelslip, Startup Assist, Auto-Shop | Later | `[ ]` |
| Manual Transmission Override (DM3) | Later | `[ ]` |
| Mounting Suite / precision mounting | Later | `[ ]` |
| Engine Temp Soft Governor | Later | `[ ]` |
| PgUp/PgDn turntable QOL | Later | `[ ]` |
| Flight-sim HUD | Later | `[ ]` |
| Coupler tension HUD | Later (cut from **6.19**) | `[ ]` |
| AR in-view-only (no false edge-stick) | Later (4.9 follow-on) | `[ ]` |
| 0.4 Graceful fail (Harmony self-disable) | Later | `[ ]` |
| 2.2b ▼GOV flash / F5–F9 inject | Later (tester) | `[ ]` |
| 1.17 #4 behind-seed ice | Later | `[ ]` |

---

## Recommended build order (after this audit)

1. Governors: **7.1 Three-Gate → 7.2 Thermal → 7.3 Auto-brake**; **7.4** auto-coupler last among the v1 trio; **7.5** only if asked.
2. Dispatcher: **8.1 desk UI → 8.2 Align → 8.3 Switch List → 8.4 TT → 8.5 multi-step → 8.7 CLEARED pin** (8.6 teleport when yard friction demands; **8.9** ghost after 8.6; **8.10** after 8.3).
3. **9.1** Catalog.
4. **10.1 PID** only after Limit honest + user spec.

Do **not** start **7.4** until the user asks.
