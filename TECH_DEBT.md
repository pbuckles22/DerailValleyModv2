## Technical debt (tracked backlog)

This is the durable home for technical debt across sessions. Handoff notes can mention debt, but anything that persists should be recorded here.

### Cadence

- **Every handoff**: run the tech-debt-evaluator skill and record “Do first” items in the handoff note.
- **Promote persistent debt**: if a “Do first” item persists across 2+ handoffs (or blocks work), add it here and rank it.

---

## Fix now

(Blocking, unsafe, or no-rollback debt.)

- (none)

## Fix soon

(High ROI; frequent pain; not blocking.)

- **In-world hitch unexplained** — Look-around Feature 110–170 ms since 3.1 (H9). Cab/drive is `feature=0` through H102 and **H107** (`2.6.16.13` overlay-handle cap). **YMS-only 2026-08-17 (H67):** other mods off; on-foot still `149`/`124` ms (`feature=2`); cab `feature=0`. Accept look class; escalate only a *new* cab `feature>0` class or look spikes clearly worse than 170 ms. See [docs/PERFORMANCE_LOG.md](docs/PERFORMANCE_LOG.md).

## Accept for now

(Isolated + workaround + revisit trigger.)

- **Upstream `doc/` vs this repo `docs/`** — AgenticTemplate still uses `doc/`. On `git merge upstream/main`, keep this repo’s `docs/` paths.
- **NU1702** — `YardMasterSuite.Tests` (net10.0) references `YardMasterSuite.Core` (net48), same as v1. Revisit if tests need APIs that do not flow across that TFM gap.
- **Quit-time consist peel** — last-loco coupler binds stay live after unboard, so world unload can emit `T2 consist` 6→5→4→… Ignore lines after `Application quit`. HUD hides when `PlayerTransform` is missing (`HudWorldSession`); revisit if unload still paints dropping `cars=` while the player object is alive.
- **Dead AR clamp APIs** — `ClampToScreen` / `ApplyBehindCameraEdge` are still public; live overlay uses `ApplyBehindCameraHorizontalEdge` only. Calling the clamp helpers parks chips on the HUD top (`edgeTop≠0`). Tests still cover them. Intern or delete after pin/top-band if still unused.
- **Office scan while out of zone** — `StationOfficeAnchor.TryGet` rescans (`GetComponent`) every `LateUpdate` when `_range` is null (open map / between towns). Not a 3.2 blocker; revisit if a YMS-only hitch pass still points at AR.
- **4.2 A\* probe is not a player route** — `PathGraphSearch` probe still A\*s first/last registry id (H45). Player routes use **8.2** `PathPlan` on string graph; do not treat probe `hops` as Align Route.
- **`#Y` turntable origin → cross-city NoPath** — Smoke `2.8.2`: origin `#Y-#S1774#T` at SW TT → SM tracks logs `T2 route: no path`. **8.4** same-yard Set dest TT. **8.5** TT dest NoPath → pivot multi-leg + Switch List TurnAround inject. Remaining: pathfind from `#Y` origin to another city without a pivot (accept until a player hit).
- **8.2 live always-on route HUD** — Desk shows Path/ETA/Facing while open; in-cab updating rem/ETA with desk closed deferred to **8.3+** / route pin (**8.7**). Always-on **Path OK** remains **6.11** End-dest check.
- **6.16 place-caption leftovers** — `LocoRadarDisplay.FormatPlace` / `TrackIncludesCity` / `IsUsableCityYardId` are test-only; live captions are type + metres. Delete with the unused `placeLabel` arg if **6.17** PNGs keep that caption shape.
- **6.16 overlay-handle cap** — `ScreenOverlayHandlePolicy` stops after 2 `FindObjectOfType` misses per world (H107 cab `feature=0`). Late save/toast roots may miss hide; pause still uses `IsPauseMenuOpen`. Revisit if a modal leaves AR up.
- **6.16 licence eval while filter parked** — `LocoRadarProbe` still calls `EvaluateLocoLicense` / `LicenseManager` on each FoT even though `LocoRadarLicenseGate.FilterEnabled` is false. Skip the query until the filter is re-armed (piggyback when touching the probe).
- **6.16 tutorial overlay reflect** — `ScreenOverlayGate.TutorialFloatieActive` does cached `FieldInfo.GetValue` on each overlay check. Cheap vs FoT; skip when handle lookup has given up if a hitch pass still points here.
- **6.21 Keep rebuild poll** — inventory identity is pickup/swap/drop; live cars and GO-hide still `Rebuild` every 0.25 s on `Keep`. Revisit if on-foot look grows a new hitch class vs H118.
- **Posted sticky miss (facing / reverse)** — Boards the player can see are not always in the posted roster / path-ahead. **7.5** never logged `auth=posted` / Limit 60 (`120 auth=default next=40`). **8.1** re-smoke: facing **60** on a **straight**, toward the loco; HUD **Limit 50**, Next stayed **40**; log never had `60`. Next metres can look wrong for the same miss. Governor does not use posted. Revisit **6.9** take/index if the chip must match boards; **not** an **8.1** / Align / dest gate.
- **6.10 parallel Next metres (chord snap)** — `2.8.1.16` smoke: Next km/h updates but metres snap (e.g. `7m` → `497m`) when chord picks a parallel board. Needs **facing + path** (likely with **8.2** route graph), not more FILO tuning. **Out of 6.10.**
- **6.10 cab leftover hitch (`feature` 15–17)** — With Limit tick + EventBus HUD on, typical cab drive is `feature=0–4`; some late windows hit **15–17** (`max≈100`). **IsolateLimitTick** (`2.8.1.15`) and full HUD restore (`2.8.1.16`) exonerated Limit (H139/H140). Suspects: AR stack, gadgets, controls, look-at, limit-gov. **AR isolate** is a future hitch story — **not** a 6.10 or **8.2** gate unless cab regresses above gold `feature=0` sustained.

---

## ROI rubric (quick)

Score each: Impact (0–2) + Frequency (0–2) + RiskReduction (0–2) + Effort (0–2, reverse scale). Sort descending.
