# Gemini pack — spatial before more SL-55 patches

**Not canonical.** Focused pack (no snapshot). Replies go in `dropzone/`.

**Do not change code.** Say whether 16.2 is the next backlog item, and which bugs A* will not fix. Cursor lands after review.

**8 files** (cap 10). Live repo paths stay canonical.

| # | File | Role |
|---|------|------|
| 1 | `README.md` | This |
| 2 | `CONTEXT.md` | Cab + the question |
| 3 | `PathPlan.cs` | A* heuristic already in Find |
| 4 | `RoutePinBoard.cs` | Step 6 and step 8 share pin 1002848 |
| 5 | `RoutePinBoardArProbe.cs` | Forced respawn skips the duplicate hide |
| 6 | `RouteStepDestPolicy.cs` | Loader-spot text rule that copies the corridor frog |
| 7 | `PrepSameDestOrigin.cs` | 2.16.16 live-track origin (got the 4th car) |
| 8 | `HtpPrepSameDestOriginTests.cs` | What CI locks today (throat → C4S, not C4S → B4L) |

**Upload:** these 8. No snapshot. No jpg.

**Reply:** PASS/WARN/FAIL + named tests (inputs → asserts) → `docs/gemini/dropzone/`.
