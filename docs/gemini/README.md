# Gemini pack — why CI is green while cab bugs remain

**Not canonical.** Focused pack (no snapshot). Replies go in `dropzone/`.

**Do not wipe** `dropzone/` matrix dumps.

**Do not change code.** Name the **missing Core walks** that would fail on the cab bugs. Cursor will land them after this review.

Harvest dumps for four SW jobs are already in `YardMasterSuite.Tests/Fixtures/Htp/` (not in this 10-file pack).

| # | File | Role |
|---|------|------|
| 1 | `README.md` | This |
| 2 | `CONTEXT.md` | Review ask |
| 3 | `HtpEngineerPinMomentsTests.cs` | What pin-moment CI asserts today |
| 4 | `HtpFixtures.cs` | Dump loaders (SL-55 / SU-34 / FH-82 / SL-52) |
| 5 | `RoutePinLatch.cs` | Observe: this-step `PinIdForStep` on CLEARED-frog steps |
| 6 | `RoutePinBoard.cs` | Collect / Flatten / `PinIdForStep` |
| 7 | `RoutePinBoardSession.cs` | Board session Flatten + `PinIdForStep` wrapper |
| 8 | `RouteCorridorDrive.cs` | Pose → CLEARED (one frog) |
| 9 | `RouteClearanceEval.cs` | Tail-past-envelope + Next = Align |
| 10 | `RouteClearanceSession.cs` | `SawAtSwitch` on any At-switch (120 m) |

**Upload:** these 10. No snapshot. No Unity. No dump text (too large; loaders name the files).

**Reply:** PASS/WARN/FAIL + named tests (inputs → asserts) that would go **red** on the cab bugs below → `docs/gemini/dropzone/`.
