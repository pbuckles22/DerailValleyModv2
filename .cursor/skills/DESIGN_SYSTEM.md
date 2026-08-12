# DESIGN_SYSTEM — DerailValleyModv2

Zero-allocation UI rules for *Yard Master Suite v2*. Product phases: [docs/YMS_v2_Architecture_Plan.md](../../docs/YMS_v2_Architecture_Plan.md) → Phase 3 Display Shell. Pub/Sub: [docs/Unity_PubSub_Best_Practices.md](../../docs/Unity_PubSub_Best_Practices.md).

The Display Shell is **not built yet**. These rules apply as soon as HUD/AR work starts. Do not copy v1 OnGUI string formatting.

## Visuals

- HUD and AR update only when a Type A event fires (or a Type B mailbox is drained on the main thread). No per-frame polling for display values.
- Cached `GUIContent` / pooled `StringBuilder` only. No string concatenation inside a render loop.
- Event payloads are primitives or readonly structs — never class objects or fresh strings.
- AR markers: object pooling. Hide by moving off-screen; do not `Destroy()` / `Instantiate()` icons.

## Motion

- No animation spec yet. When added, keep it off the hot path (no allocs in `Update()`).

## Consistency

- Subscribe in `OnEnable`, unsubscribe in `OnDisable`/`OnDestroy`.
- Research UI libraries (UniverseLib, etc.) before writing a custom IMGUI stack — see [docs/Research_and_Leverage_Manifesto.md](../../docs/Research_and_Leverage_Manifesto.md).
