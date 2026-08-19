## Release / merge discipline

Versioning is **PM-driven**, not SemVer guesswork. Policy: [docs/Versioning_and_Release_Strategy.md](docs/Versioning_and_Release_Strategy.md). Agent rule: [`.cursor/rules/pm-versioning.mdc`](.cursor/rules/pm-versioning.mdc).

- **`info.json` `Version`:** `2.{Epic}.{Story}` (optional `.Fix` sub-patch). Major locked at **2**.
- **DLL `+BUILD`:** local gitignored `build_number.txt` → `InformationalVersion` `2.1.1+104`. Not committed; not written into `info.json`.
- **Private / testing:** every completed PM_PLAN **story**.
- **GitHub Release:** every completed **epic**, automatically after the close ship is on `main`.
- **Nexus Mods:** only when the mod is playable (first player-facing feature); do not auto-upload.

### Merge-ready (minimum)

Document the real gate in `AGENT_HANDOFF.md` and `TEST_PLAN.md`, then treat it as mandatory:

- Tier 1 is green (`npx --yes markdownlint-cli2` + `dotnet test` + Release build)
- Tier 2 when behavior demands in-world validation ([deploy-before-smoke](.cursor/rules/deploy-before-smoke.mdc))
- `info.json` matches the story just shipped (if it was a numbered story)
- Tracked docs updated (`PM_PLAN`, `docs/PROJECT_STATUS.md`, AGENT_HANDOFF *Current state*)
- Rollback path is clear (a revert commit is usually sufficient)

### Rollback

- Prefer a single revert commit per change (`info.json` reverts with it)
- Re-run the required validation tier(s)
- Do not force-push `main` unless the user explicitly asks
