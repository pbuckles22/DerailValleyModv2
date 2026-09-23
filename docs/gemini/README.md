# Gemini pack — second Prep stopped at 0.4 m

**Not canonical.** Troubleshooting pack. Replies go in `dropzone/` (do not upload that folder).

**Upload these 10. Do not upload this README.**

| # | File | Role |
|---|------|------|
| 1 | `AutoCoupleAssist.cs` | Green 0.5 m, slide 1 m, partner allow, refuse |
| 2 | `BackupProximityDisplay.cs` | HUD green window (0.5 m) |
| 3 | `PrepCreepPolicy.cs` | 3 km/h until the window; refused partner stays stopped |
| 4 | `PrepCoupleExitGate.cs` | Stop GO on the Prep approach |
| 5 | `YardKissPolicy.cs` | Request drops to 0 inside the green window |
| 6 | `PrepCreepSession.cs` | Latch the refused partner |
| 7 | `PidSpeedTarget.cs` | Yard request uses the kiss policy |
| 8 | `AutoCouplerListener.cs` | Caller: refuse calls Stop GO |
| 9 | `JobConsistProbe.cs` | Partner job id |
| 10 | `HtpCreepToCoupleCp5Tests.cs` | Locked tests, including the C4S short-stop creep |

The cab facts are the header on `AutoCoupleAssist.cs`.
