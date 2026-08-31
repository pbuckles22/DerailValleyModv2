# **HTP Playbook for AI Agents**

**CRITICAL — every agent on YardMasterSuite:** follow this playbook with [.cursor/rules/htp.mdc](../.cursor/rules/htp.mdc). Vision lives in [HTP.md](HTP.md); this file is the **gather-once / simulate-forever** ritual.

If you are assisting with YardMasterSuite, you must strictly adhere to the "Gather Once, Simulate Forever" workflow detailed in this document. Do not suggest Unity-dependent code, MonoBehaviour updates, or in-game frame-by-frame calculations for Core logic. All logic must be mathematically verifiable headlessly before it ever touches the game.

## **1\. The Core Philosophy**

Train tracks are complex 3D Bezier curves. Calculating look-aheads, speed limit boundaries, and junction facing-states in real-time requires heavy 3D engine overhead.

We **DO NOT** rebuild Unity's physics or routing inside our C\# Core.

Instead, we extract a 1D, flattened snapshot of the track ahead, and run our C\# logic against that static text file in milliseconds.

**The Workflow:**

1. **Extract (Gather):** Let Unity flatten the 3D world into a 1D text file.  
2. **Simulate (HTP):** Use C\# to mock a train moving down that 1D list and assert the outcomes.  
3. **Deploy (Play):** Only boot the game once the C\# simulation is 100% green.

## **2\. Extraction: What, When, and How to Gather**

**When to Extract:**

* Do **NOT** cache the entire map. It causes massive frame hitches and bloats the AI context window.  
* **Trigger:** The player spawns in the cab, sets the desk (destination), throws the exit switch for the path they want, and waits for the game to settle. The train **DOES NOT MOVE**.  
* A single, one-shot script is triggered to dump the corridor ahead.

**What to Extract (The Payload):**

The dump file (e.g., boards-sw-b4l-2026-08-30.txt) represents a \~1600m forward-looking corridor. It must contain:

* **Header Summary:** pathN=, boardN=, facingN=, dualN= (for quick AI verification).  
* **Path Segments:** Segment count, entry XZ, hint, length, and entryAbs (absolute distance along the path).  
* **Speed Boards:** ID, X/Z coordinates, through/diverge limits (or dual flags), facing directions, and nearby junction data.

## **3\. Simulation: How to Use the HTP**

Once the text file is generated, the AI (Cursor) takes over to build the Headless Test Platform (HTP) fixtures inside YardMasterSuite.Tests.

**How to Simulate:**

1. **Parse the Dump:** Write a codec to read the text file into immutable C\# structs (PathSegment, SpeedBoard).  
2. **Mock the Movement:** Write unit tests that manually increment a fake locoAbs (locomotive absolute position) down the parsed path.  
3. **Calculate Remaining Distance:** Always use reverse-travel polarity math: (boardAbs \- locoAbs) \* pathPolarity.  
4. **Assert State Changes:** Verify the Evaluate() function output as the train "moves."  
   * *Example:* Assert Approach Next 40, then Active 40, then Next 60\.

**Strict Domain Rules for HTP Tests:**

* **12m Lateral Corridor:** CorridorLateralMeters is exactly 12.0. Boards further away are ignored.  
* **Symmetric Junction Duals:** If a board is a dual, near a junction, not diverging, and through\_speed \== diverging\_speed, it **MUST NOT** govern. (e.g., Board 1398162).  
* **Same-Rail Behind-Take:** Promotions behind a board are strictly restricted to the same rail within a \~250m window to prevent ghost limits.

## **4\. Cursor's Mandate (Read Carefully)**

* **NEVER** ask the user to test logic in the cab ("pin smoke") until the HTP walk tests are written, executed (dotnet test), and passing.  
* **NEVER** write logic that attempts to parse the entire map at runtime. You only work with the one-shot corridor dump.  
* **ALWAYS** refer back to this document if you are confused about how the YardMasterSuite gets its spatial data.
