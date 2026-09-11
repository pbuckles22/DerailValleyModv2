# Gemini pack — consist-tail CLEARED still fails

**Not canonical.** Focused pack (no snapshot). Replies go in `dropzone/`.

**Do not upload** `dropzone/sw-frog-matrix.tsv`. Live dump stays; this pack does **not** wipe it.

**This pack:** cab 2.13.2.5.15 still stops At switch after couple. Length *did* update (`len=44`). Kiss then hard-stop left `rem=7` and never CLEARED.

| # | File | Role |
|---|------|------|
| 1 | `README.md` | This |
| 2 | `CONTEXT.md` | Cab FAIL + Player.log + review ask |
| 3 | `RouteClearanceEval.cs` | Tail-past-frog CLEARED |
| 4 | `RouteClearanceSession.cs` | rem-to-CLEARED; live length max |
| 5 | `YardApproachKinematics.cs` | `(frog + length) − nosePast` |
| 6 | `YardKissPolicy.cs` | Cruise 25 then Stop GO |
| 7 | `YardArrivalStopPolicy.cs` | `d_stop + 15 − 2` kiss trigger |
| 8 | `YardStopKinematics.cs` | Mass-scaled d_stop (86 t) |
| 9 | `SwitchListYardChain.cs` | Kiss then sit in zone (no re-arm) |
| 10 | `ConsistLengthSession.cs` | Couple `ObserveIncrease` |

**Upload:** these 10. No `Gemini_Snapshot.txt`. No TSV.

**Reply:** PASS/WARN/FAIL → `docs/gemini/dropzone/`.
