# Gemini pack — close the Prep couple without rewriting locks

**Not canonical.** Two files. Replies go in `dropzone/` (do not upload that folder).

**Do not change code.** Name one additive Core gate. A finished Prep consist grow must arm Stop GO, and a lost laser must not request 25 while that hold is set. Existing locked tests stay the same.

| # | File | Role |
|---|------|------|
| 1 | `README.md` | This |
| 2 | `CONTEXT.md` | The hole, the locks, the question |
| 3 | `ConsistTopologyListener.cs` | Grow latches hold, then tries Stop GO |
| 4 | `PrepCreepSession.cs` | `TryStopGoIfNeeded` requires `WantsCoupleStop` |
| 5 | `PrepCoupleExitGate.cs` | `ShouldLatchHoldOnConsistGrow` |
| 6 | `YardKissPolicy.cs` | Missing laser returns 25 |
| 7 | `PidSpeedTarget.cs` | `RequestForYardStep` calls `RequestKmh` |
| 8 | `SwitchListYardChain.cs` | Hold blocks a new Arm GO only |
| 9 | `PrepCreepPolicy.cs` | 3 km/h walk and the 10 m zone |
| 10 | `HtpYardTaperKissTests.cs` | Locked smoke tests that must stay green |

**Upload:** these 10. Live repo paths stay canonical; these copies are the pack.

**Reply:** PASS / WARN / FAIL, one function, one new test name → `docs/gemini/dropzone/`.
