# Gemini drop — HTP process review (2026-08-28)

**Not canonical.** ≤10 files. Start with **`CONTEXT.md`**.

Focused pack (gather vs Core vs cab). Full repo dump is still gitignored `Gemini_Snapshot.txt` at repo root if you also want it — **do not** put that file in this folder for this ask.

Live dump fixtures stay in the repo at `YardMasterSuite.Tests/Fixtures/Htp/` (too large for this pack). Tests replay them.

| # | File | Role |
|---|------|------|
| 1 | `README.md` | This |
| 2 | **`CONTEXT.md`** | Learning-pass ask + what we gathered vs simulated |
| 3 | `HTP.md` | North star + dump protocol |
| 4 | `htp.mdc` | Hard rule |
| 5 | `RouteCorridorDrive.cs` | Topology walk |
| 6 | `SwitchListRouteLeg.cs` | Pin arm (Path OK + JunctionFirstStop) |
| 7 | `RouteHarvestDump.cs` | One-off gather writer |
| 8 | `HtpFixtures.cs` | Load dump + pose helpers |
| 9 | `HtpSwTurntableLiveDumpTests.cs` | Live-dump scenarios |
| 10 | `SwTurntableCorridorTests.cs` | Sketch polarity only |

Canonical: `docs/HTP.md`. Snapshots here are copies.
