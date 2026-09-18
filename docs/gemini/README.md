# Gemini pack — C4S→B4L dest vs C-yard body

**Not canonical.** Focused pack (no snapshot). Replies go in `dropzone/`.

**Do not change code.** Name the missing Core / HTP walks that would go **red** on this cab. Cursor lands after review.

Harvest dumps stay in `YardMasterSuite.Tests/Fixtures/Htp/` (not in this pack).

**8 files** (cap 10). Two Core+test pairs are concatenated; live repo paths stay canonical.

| # | File | Role |
|---|------|------|
| 1 | `README.md` | This |
| 2 | `CONTEXT.md` | Review ask + Player.log |
| 3 | `PathRouteConstraints.Pack.cs` | Occupy/FilterEdges + cab 22.40 goldens |
| 4 | `RouteStepDestPolicy.cs` | Dest-side vs first-stop pin pick |
| 5 | `RoutePinBoardLatch.cs` | `PinIdForStep` + Observe latch |
| 6 | `HtpSwYardDeliveryRouteTests.cs` | SW B4L/C4S pin polarity walks |
| 7 | `HtpSetDestAuditTests.cs` | SL-55 harvest graph B1S/C4S via B4L |
| 8 | `HtpEngineerPinMomentsTests.cs` | What pin-moment CI asserts today |

**Upload:** these 8. No snapshot. No Unity. No dump text. No jpg.

**Reply:** PASS/WARN/FAIL + named tests (inputs → asserts) → `docs/gemini/dropzone/`.
