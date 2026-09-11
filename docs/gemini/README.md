# Gemini pack — W1+W2 dest itinerary

**Not canonical.** Focused pack (no snapshot). Replies go in `dropzone/`.

**Do not wipe** `dropzone/` matrix dumps. This pack only replaces the **root** 10-file upload.

W1 Gemini PASS is already in. This pack is **W2** (FH-82) plus the helper/tests so you can check anti-overfit. Do not re-litigate W1.

| # | File | Role |
|---|------|------|
| 1 | `README.md` | This |
| 2 | `CONTEXT.md` | W2 review ask + tables |
| 3 | `RouteStepDestPolicy.cs` | `TryMapsDestForListProgress` |
| 4 | `SwitchListDestItineraryTests.cs` | W1 + W2 goldens |
| 5 | `SwitchListPlanner.cs` | Job template → label dests |
| 6 | `RoutePinLatch.cs` | Live Observe (W0; not this review) |
| 7 | `MapsDeskPanel.cs` | `ApplyStepDest` **not wired** to helper |
| 8 | `SwitchListRunner.cs` | All Transit = pin-leg (recorded) |
| 9 | `RoutePinLatchTests.cs` | W0 Set + spent-dismiss |
| 10 | `SwitchListStepDisplay.cs` | English from label dest |

**Upload:** these 10. No `Gemini_Snapshot.txt`.

**Reply:** PASS/WARN/FAIL on **W2** + go/no-go **W3 walk** → `docs/gemini/dropzone/`.
