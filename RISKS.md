## Risk register (top 5)

This file is intentionally short. Keep only the highest leverage, current risks.

- **Risk**: Merge-ready gates drift from reality (agents think “green” without running the real gate)  
  **Impact**: High  
  **Likelihood**: Med  
  **Trigger**: PRs merge without the documented Tier 1 command; flaky CI ignored  
  **Mitigation**: keep `AGENT_HANDOFF.md` + `TEST_PLAN.md` aligned; enforce handoff checklist  
  **Rollback**: revert the merge; restore last known-good tag/commit

- **Risk**: Hitch compounding while the probe stays quiet (v1 failure mode)  
  **Impact**: High  
  **Likelihood**: High  
  **Trigger**: `Feature` / `BelowGate` rows in [docs/PERFORMANCE_LOG.md](docs/PERFORMANCE_LOG.md) grow after a story; YMS-only session never run  
  **Mitigation**: 100 ms + world-session gate; append a log row after every hitch smoke; `HitchBand` Tier 1 tests  
  **Rollback**: restore prior `SpikeSeconds` only if alert fatigue returns; do not drop the log

- **Risk**: Context bloat causes agent drift / incorrect assumptions  
  **Impact**: Med  
  **Likelihood**: High  
  **Trigger**: handoff notes exceed ~500 words; repeated long logs in chat; conflicting “truth” sources  
  **Mitigation**: use session-summarizer; keep durable truth in tracked docs; bootstrap with minimal context  
  **Rollback**: create a compressed handoff note; re-establish source-of-truth docs; re-scope work
