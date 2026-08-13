# DESIGN_SYSTEM — DerailValleyModv2

Zero-allocation UI rules for *Yard Master Suite v2*. Product phases: [docs/YMS_v2_Architecture_Plan.md](../../docs/YMS_v2_Architecture_Plan.md) → Phase 3 Display Shell. Pub/Sub: [docs/Unity_PubSub_Best_Practices.md](../../docs/Unity_PubSub_Best_Practices.md).

Do not copy v1 OnGUI string formatting.

## Stack (3.1)

Native Unity **IMGUI** (`MonoBehaviour.OnGUI`) + `GuiContentCache` / `StringBuilderPool`.

**UniverseLib / UGUI** was scouted and deferred. It is a canvas/GameObject kit (UnityExplorer-class menus), allocates, and would be a second shipped dependency. Revisit when a settings or dispatch-desk panel needs widgets. Do not use UMM `modEntry.OnGUI` for the in-world overlay (that hook is the pause-menu settings pane).

**Compass source:** player look (`PlayerManager.ActiveCamera`, else player transform). Unity world **+Z = north**. 16-point abbreviations only. Not loco facing — this chip must work on foot.

## Visuals

- HUD and AR update only when a Type A event fires (or a Type B mailbox is drained on the main thread). No per-frame polling for display values.
- Compass samples camera yaw in `LateUpdate` but publishes Type A only when the 16-point bucket changes.
- Cached `GUIContent` / pooled `StringBuilder` only. No string concatenation inside a render loop.
- Event payloads are primitives or readonly structs — never class objects or fresh strings.
- Draw only while a world session is active (`PlayerTransform` present). Launcher stays blank.
- AR markers: pre-sized buffer (not a growing `ObjectPool<T>` unless hitch-forced). Hide by moving off-screen; do not `Destroy()` / `Instantiate()` icons.

## Motion

- No animation spec yet. When added, keep it off the hot path (no allocs in `Update()`).

## Consistency

- Subscribe in `OnEnable`, unsubscribe in `OnDisable`/`OnDestroy`.
- 3.1 overlay stays native IMGUI. Revisit UniverseLib only for 5.2 desk widgets — [docs/LEVERAGE_REGISTER.md](../../docs/LEVERAGE_REGISTER.md).
