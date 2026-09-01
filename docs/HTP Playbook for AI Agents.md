# **HTP Playbook for AI Agents (v2 — Core routing)**

**CRITICAL — every agent on YardMasterSuite:** follow this playbook with [.cursor/rules/htp.mdc](../.cursor/rules/htp.mdc). Vision lives in [HTP.md](HTP.md); this file is the **gather-once / simulate-forever** ritual.

If you are assisting with YardMasterSuite, you must strictly adhere to the "Gather Once, Simulate Forever" workflow detailed in this document. **Do not use Unity `RailTrack` / `Junction` / `TrackPathAhead` for routing logic.** All routing walks a dumped subgraph in `YardMasterSuite.Core` and must be green in `dotnet test` before cab smoke.

## **1\. The Core Philosophy (Graph Walker)**

Train tracks are complex 3D Bezier curves. Unity `TrackPathAhead.TryBuild` truncates during reverse yard moves (9.1.2 Win 7), dropping the long leave seg and blinding Evaluate to the on-path **60**. We **DO NOT** rely on Unity to build the look-ahead path.

Instead, Unity dumps the **raw local node/edge graph**. Core walks thrown junctions from the loco and feeds `PathSegmentAlong[]` into the existing `PostedLimitFunnel.Evaluate()`.

**The Workflow:**

1. **Extract (Gather Graph):** Dump tracks, junctions, and posted boards within **2.5 km** (not full-map cache) to a text file.  
2. **Pathfind (Core):** `CorePathfinder` traverses that graph from the loco, following dumped selected branches, ~1600 m.  
3. **Simulate (HTP):** Evaluate Limit against that generated path in `dotnet test`.  
4. **Deploy (Play):** Cab only after HTP proves the walker reaches the gold boards (**1398156** 40, **1402212** 60).

**Keep (9.1.2 Wins 0–6):** `PostedPathAheadGate` (12 m corridor, polarity, symmetric dual skip, loco/board abs). `PostedLimitFunnel.Evaluate`. Do **not** re-prove that Limit walk; prove the **new path** includes those boards.

**Replace:** Unity `TrackPathAhead` as Evaluate's path provider. Pre-traced `pathN=` board harvest is gather-v1 only — 9.1.3 gather is **raw graph + boards**.

## **2\. Extraction: What to Gather**

* **Trigger:** Player spawns, throws the desired exit switches, sits still. Train **DOES NOT MOVE**.  
* **Payload (local subgraph, not a pre-traced path):**
  * `loco:` X Y Z ForwardX ForwardZ  
  * `tracks:` ID, entry X/Z, exit X/Z, length  
  * `junctions:` ID, stem / left / right track IDs, selected branch (0 or 1)  
  * `boards:` ID, X/Z, through/diverge, facing  
* **Not:** full-map cache. **Not:** `TrackPathAhead` hop list as the source of truth.

## **3\. Simulation: How to Use the HTP**

1. **Parse the Graph:** Codec → immutable `CoreTrack` / `CoreJunction` / `ParsedPostedBoard`.  
2. **Trace the Path:** `CorePathfinder.BuildPath(...)` from loco, dumped junction selection.  
3. **Assert the Route:** Generated `PathSegmentAlong[]` reaches past **1402212** (60).  
4. **Assert the Limits:** Same Evaluate gold as 9.1.2 Win 6 — Next 40 → Active 40 → Next 60; never Next=50.

**Strict Domain Rules (unchanged):**

* **12 m lateral:** `CorridorLateralMeters` is 12.0.  
* **Symmetric junction duals:** through == diverge → must not govern (e.g. **1398162**).  
* **Same-rail behind-take:** ~250 m window.  
* **Never cab-debug** a red Core walk.

## **4\. Cursor's Mandate**

* **NEVER** write pathfinding inside a `MonoBehaviour`. Routing lives in `YardMasterSuite.Core`.  
* **NEVER** ask for pin smoke until the named HTP routing walk is green.  
* **NEVER** parse the entire map at runtime. One-shot 2.5 km dump only.  
* **ALWAYS** write a headless test against the static graph dump before Unity wire.
