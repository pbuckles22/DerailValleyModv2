## Technical debt (tracked backlog)

This is the durable home for technical debt across sessions. Handoff notes can mention debt, but anything that persists should be recorded here.

### Cadence

- **Every handoff**: run the tech-debt-evaluator skill and record “Do first” items in the handoff note.
- **Promote persistent debt**: if a “Do first” item persists across 2+ handoffs (or blocks work), add it here and rank it.

---

## Fix now

(Blocking, unsafe, or no-rollback debt.)

- (none)

## Fix soon

(High ROI; frequent pain; not blocking.)

- (none)

## Accept for now

(Isolated + workaround + revisit trigger.)

- **Dual `docs/` + `doc/`** — YMS background vs agentic governance. Revisit if agents keep writing product docs into the wrong tree.
- **NU1702** — `YardMasterSuite.Tests` (net10.0) references `YardMasterSuite.Core` (net48), same as v1. Revisit if tests need APIs that do not flow across that TFM gap.
- **No `package.ps1`** — Release copies `build/YardMasterSuite.dll` only. Deploy/smoke waits until packaging is in scope.

---

## ROI rubric (quick)

Score each: Impact (0–2) + Frequency (0–2) + RiskReduction (0–2) + Effort (0–2, reverse scale). Sort descending.

