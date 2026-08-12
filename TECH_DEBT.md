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

- **Uncompilable `Main.cs`** — `YardMasterSuite/Main.cs` references `YmsEventBus` / `GcCadenceProbe` with no project. **Owned by PM 1.1** (scaffold + those two types so Release build is green). Do not “fix” by deleting Main.

## Accept for now

(Isolated + workaround + revisit trigger.)

- **Dual `docs/` + `doc/`** — YMS background vs agentic governance. Revisit if agents keep writing product docs into the wrong tree.

---

## ROI rubric (quick)

Score each: Impact (0–2) + Frequency (0–2) + RiskReduction (0–2) + Effort (0–2, reverse scale). Sort descending.

