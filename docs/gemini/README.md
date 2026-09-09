# Gemini drop — SL-55 C4S frog vs HTP first-stop

**Not canonical.** Focused pack (no snapshot). Replies go in `dropzone/`.

**Folded 2026-09-09:** cab **2.13.2.5.8** Past-switch Observe always prefers dest-side `PickLastJunctionId` (C4S must not stay on first-stop `989976`). Removed the cab-vs-graph disagree gold. Global crunch runner is Core tests (`YMS_FROG_MATRIX_CRUNCH=1`), not this 10-file pack. Give Gemini `dropzone/sw-frog-matrix-gemini.txt` and later `matrix-*-gemini.txt`. Rejected: HeadlessYardSimulator / mock-distance fuzzer.

| # | File | Role |
|---|------|------|
| 1 | `README.md` | This |
| 2 | `CONTEXT.md` | Ask: same frog pick as graph walk, not TT-overfit |
| 3 | `PathPlan.cs` | `TryFindJunctionFirstStop` (flip-reapproach, not next-ahead) |
| 4 | `SwitchListRouteLeg.cs` | `PickPinJunctionId` = first-stop else `RequiredFlips[0]` |
| 5 | `RouteStepDestPolicy.cs` | Corridor vs approach pin; relatch only when dests differ |
| 6 | `RoutePinLatch.cs` | Set-dest latch; Recheck must not steal |
| 7 | `RouteCorridorDrive.cs` | HTP `Plan` → `PickPin` |
| 8 | `HtpSetDestAuditTests.cs` | Golds cab `990152` vs replan `1576058` **disagree** |
| 9 | `MapsRouteListener.cs` | Approach-relatch skipped when step dest == corridor dest |
| 10 | `SwitchListPlanner.cs` | After B4L, Past-switch dest **is** `SW-C4S` (same as corridor) |

**Upload:** these 10 files. Cap 10; no `Gemini_Snapshot.txt`.

**Reply:** PASS/WARN/FAIL → `docs/gemini/dropzone/`
