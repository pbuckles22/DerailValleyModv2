# Test plan

Two-tier strategy for *Yard Master Suite v2*. Story IDs match [PM_PLAN.md](PM_PLAN.md). Keep in sync with [AGENT_HANDOFF.md](AGENT_HANDOFF.md).

| Tier | When | Gate |
|------|------|------|
| **1** | Every logic change (after scaffold) | `dotnet test` + Release build |
| **2** | In-world UMM behavior (after packaging) | Deploy + Player.log `T2 …` + on-screen HUD |

**Merge-ready today:** docs/rules only (no solution). After Epic 1.1: Tier 1 always. Stories that touch in-world UI also need Tier 2 before checking Done in PM_PLAN.

---

## Tier 1 — Fast feedback

```bash
# After YardMasterSuite.sln exists:
dotnet test YardMasterSuite.sln
dotnet build YardMasterSuite.sln -c Release
```

**Pass (intended):** All unit tests green; 0 build errors; `build/YardMasterSuite.dll` present.

Pure helpers live in `YardMasterSuite.Core` (no Unity/game refs). Smoke-found gates must land here ([.cursor/rules/smoke-gates-tier1-ci.mdc](.cursor/rules/smoke-gates-tier1-ci.mdc)).

---

## Tier 2 — In-game smoke

Requires UMM (`Mods\` under the game root) and `package.ps1` (not created yet). Do not ask for smoke until those exist.

```powershell
# After packaging exists:
dotnet build YardMasterSuite.sln -c Release
powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

### Evidence

| Source | Where | Proves |
|--------|--------|--------|
| **Player.log** | `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log` | Load, toggle, discrete `T2 …`, exceptions |
| **UMM Logs** | Mod Manager → Logs | Same lines (subset) |
| **HUD** | In-world only (when Display Shell exists) | Matches latest `T2` line |

**Logging:** lifecycle + discrete `T2` on meaningful change. No per-frame spam.

### Lifecycle (every session, once Main loads)

- `[YMS v2] Mod Loaded. Awaiting toggle.`
- On → `[YMS v2] Activated. GC Probe running.`
- Off → `[YMS v2] Deactivated cleanly.`
- No YardMasterSuite exceptions / stack traces

---

**Handoff:** Document the exact commands you use for coverage in AGENT_HANDOFF.md so agents can run them consistently.
