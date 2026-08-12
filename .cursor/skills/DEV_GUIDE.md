# DEV_GUIDE — DerailValleyModv2

## Tech stack

| Layer | Choice |
|-------|--------|
| Game | Derail Valley (Unity) |
| Language | C# |
| Target framework | `net48` (class library) — planned |
| Mod loader | Unity Mod Manager (UMM) |
| Patching | Harmony — Prefix / Postfix only |
| Project shape | [derail-valley-modding/template-umm](https://github.com/derail-valley-modding/template-umm) (not scaffolded yet) |
| IDE | Cursor + C# Dev Kit |
| Inspection | [dnSpy](https://github.com/dnSpy/dnSpy/releases) |

Background: [docs/YMS_v2_Onboarding_Guide.md](../../docs/YMS_v2_Onboarding_Guide.md)  
Roadmap: [docs/YMS_v2_Architecture_Plan.md](../../docs/YMS_v2_Architecture_Plan.md)  
Pub/Sub: [docs/Unity_PubSub_Best_Practices.md](../../docs/Unity_PubSub_Best_Practices.md)  
Plan: [PM_PLAN.md](../../PM_PLAN.md)

v1 [DerailValleyMod](https://github.com/pbuckles22/DerailValleyMod) is a **reference** for game API hooks and math. Do not copy bolt-on `Update()` loops.

## Environment setup

1. **.NET SDK** 8+ with **.NET Framework 4.8 targeting pack** (VS ".NET desktop development" workload).
2. **Unity Mod Manager** installed into Derail Valley (creates `Mods\`).
3. **Cursor** + **C# Dev Kit**; **dnSpy** for inspecting `Assembly-CSharp.dll`.

After a solution exists: copy `Directory.Build.targets.example` → `Directory.Build.targets` and set your game `Managed\` path (file is gitignored).

## Layout (current vs planned)

```
YardMasterSuite/           # UMM entry — Main.cs only today
YardMasterSuite.Core/      # planned: YmsEventBus.cs, GcCadenceProbe.cs (pure + probe)
YardMasterSuite.Tests/     # planned: xUnit Tier 1
docs/                      # YMS v2 background (do not merge into doc/)
doc/                       # agentic governance (PROJECT_STATUS, requirements, handoff)
```

## Build / deploy

Not wired. Intended commands (after scaffold):

```bash
dotnet test YardMasterSuite.sln
dotnet build YardMasterSuite.sln -c Debug
dotnet build YardMasterSuite.sln -c Release
```

Deploy (after `package.ps1` exists):

```powershell
powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

**This machine:** game root `C:\Program Files (x86)\Steam\steamapps\common\Derail Valley`  
**Player.log:** `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log`

## Conventions

- Fail closed on Harmony load failure (`Main.Load` returns false).
- Prefix/Postfix only — no Transpilers without an explicit decision.
- Zero-allocation in hot paths; Type A vs Type B Pub/Sub per `docs/Unity_PubSub_Best_Practices.md`.
- Every `YmsEventBus` subscribe in `OnEnable` must unsubscribe in `OnDisable`/`OnDestroy`. `Main.OnToggle(false)` calls `YmsEventBus.ClearAllSubscriptions()`.
- In-game stories: emit discrete `T2` Player.log lines for checklist fields once Monitor exists.
