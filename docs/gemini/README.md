# Gemini drop — TT visual mid vs `along=21`

**Not canonical.** Focused pack (no snapshot). Replies go in `dropzone/`.

| # | File | Role |
|---|------|------|
| 1 | `README.md` | This |
| 2 | `CONTEXT.md` | Ask: visual center vs spline `L/2` |
| 3 | `Player.log` | Cab: `along=21` while player saw mid |
| 4 | `TurntableArrivalGate.cs` | Aim `18.5`, latch `along=` |
| 5 | `LocoTrackProbe.cs` | Bogie `Span` (not consist center) |
| 6 | `SwitchListSession.cs` | Observe rem / OnTable |
| 7 | `YardApproachKinematics.cs` | Off-rail rem synthesis |
| 8 | `HtpTurntableMidSpinTests.cs` | Cab 18.5 vs 21 goldens |
| 9 | `TurntableSpinGovernor.cs` | `TurntableRailTrack` yaw only |
| 10 | `ConsistLengthSession_YardArrivalStopPolicy.cs` | Coupler-sum + closed 2.5 m lead |

**Upload:** these 10 files. Cap 10; no `Gemini_Snapshot.txt`.

**Reply:** PASS/WARN/FAIL → `docs/gemini/dropzone/`
