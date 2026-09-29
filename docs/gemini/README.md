# Gemini pack — coast shove and blind reverse (2.16.34.7)

**Not canonical.** Troubleshooting pack. Replies go in `dropzone/` (do not upload that folder).

**Upload these 10. Do not upload this README.**

| # | File | Role |
|---|------|------|
| 1 | `RouteCommandExecutor.cs` | The ask, plus the 1 km/h coast that replaced the indy-100 dump |
| 2 | `PidSpeedHold.cs` | A 1 km/h target below current speed turns the throttle back on |
| 3 | `PidSpeedGovernorListener.cs` | Passes that request straight into the hold |
| 4 | `BackupProximityListener.cs` | Null clearance when the tip coupler is already coupled |
| 5 | `ProximityEnd.md` | Reverse selects the rear tip of the whole trainset |
| 6 | `RouteCommandExecutorListener.cs` | The tick's only car gap is that clearance |
| 7 | `AutoCoupleAssist.cs` | Foreign-partner uncouple, and the 3 km/h couple gate |
| 8 | `AutoCouplerListener.cs` | Writes `uncouple-foreign` when that gate trips |
| 9 | `RouteCommandExecutorTests.cs` | Locks the 1 km/h coast and the "no indy-100 at 3 km/h" rule |
| 10 | `RouteExecLog.txt` | The C4S shove and the B1S 24 km/h hit |
