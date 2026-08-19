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

- **In-world hitch unexplained** — Look-around Feature 110–170 ms since 3.1 (H9). Cab/drive `feature=0`. **YMS-only 2026-08-17 (H67):** other mods off; on-foot still `149`/`124` ms (`feature=2`); cab `feature=0`. Other mods are not the cause. **Accept and continue Epic 6** — do not re-list as Fix now each handoff. **Escalate** only a *new* class (cab `feature>0`, or look spikes clearly worse than 170 ms). Next isolation only if we open a hitch story: HUD OnGUI off → AR off → SphereCast off. See [docs/PERFORMANCE_LOG.md](docs/PERFORMANCE_LOG.md).

## Accept for now

(Isolated + workaround + revisit trigger.)

- **Upstream `doc/` vs this repo `docs/`** — AgenticTemplate still uses `doc/`. On `git merge upstream/main`, keep this repo’s `docs/` paths.
- **NU1702** — `YardMasterSuite.Tests` (net10.0) references `YardMasterSuite.Core` (net48), same as v1. Revisit if tests need APIs that do not flow across that TFM gap.
- **Quit-time consist peel** — last-loco coupler binds stay live after unboard, so world unload can emit `T2 consist` 6→5→4→… Ignore lines after `Application quit`. HUD hides when `PlayerTransform` is missing (`HudWorldSession`); revisit if unload still paints dropping `cars=` while the player object is alive.
- **Dead AR clamp APIs** — `ClampToScreen` / `ApplyBehindCameraEdge` are still public; live overlay uses `ApplyBehindCameraHorizontalEdge` only. Calling the clamp helpers parks chips on the HUD top (`edgeTop≠0`). Tests still cover them. Intern or delete after pin/top-band if still unused.
- **Office scan while out of zone** — `StationOfficeAnchor.TryGet` rescans (`GetComponent`) every `LateUpdate` when `_range` is null (open map / between towns). Not a 3.2 blocker; revisit if a YMS-only hitch pass still points at AR.
- **4.2 A\* probe is not a player route** — `PathGraphSearch` is Dijkstra (h=0) O(n²) over ~2k nodes; mapper A\*s first/last `GetInstanceID()` so smoke logged `hops=—`. 117 ms `gc0=+1` at ready (H45). Revisit when **4.4** / **5.2** has a real origin→dest; do not treat `hops` as Align Route.

---

## ROI rubric (quick)

Score each: Impact (0–2) + Frequency (0–2) + RiskReduction (0–2) + Effort (0–2, reverse scale). Sort descending.
