# **Yard Master Suite v2.0 — The Clean Room Rewrite**

**Philosophy:** We are abandoning the "Bolt-On Update Loop" paradigm. YMS v2.0 is an Event-Driven, Zero-Allocation, highly cohesive Unity Mod. The previous codebase is now strictly a reference library for game API hooks and raw mathematical formulas.

## **The Rebuild Sequence (Bottom-Up Architecture)**

### **Phase 1: The Heartbeat (Core Infrastructure)**

*Before we read a single train variable, we need the systems that keep the mod alive and performant.*

1. **The GC Probe:** Implement GcCadenceProbe.cs immediately. We run this constantly during development to ensure no new feature introduces memory leaks.  
2. **The Event Bus (Pub/Sub):** Create a central YmsEventBus. Instead of scripts running Update() to check if the player boarded a train, they will subscribe to YmsEventBus.OnPlayerBoardedTrain.  
3. **The String / UI Cacher:** Implement GuiContentCache and pooled StringBuilders. No strings will be concatenated inside a render loop.

### **Phase 2: The Senses (Event-Driven Telemetry)**

*How the mod perceives the Derail Valley world without polling.*

1. **Loco State Listener:** A script that hooks into the vanilla game's boarding/unboarding events. It caches the CurrentUsableLoco and broadcasts changes.  
2. **Control Telemetry:** Reading throttle, brake, and reverser states *only* when the player actually moves the levers, not 60 times a second.  
3. **Trainset Topology:** A listener that updates our cached consist length and weight *only* when a Coupler.OnCoupled or OnUncoupled event fires natively in the game.

### **Phase 3: The Display Shell (Zero-Alloc UI)**

*Building the visual layer before we hook up the heavy math.*

1. **The HUD Manager:** Rebuild the Top Bar and Always-On compass.  
2. **AR Overlay Engine:** Rebuild the 3D world-space markers (Office, Loco, Pins) using strict object pooling (don't Destroy() and Instantiate() icons; move them off-screen when hidden).  
3. *Checkpoint:* The mod should now display basic speed and mass with absolute zero GC stutter.

### **Phase 4: The Heavy Engines (Time-Sliced Brains)**

*Porting over our masterclass math algorithms, wrapped in performance guards.*

1. **Type B Mailbox:** `ConcurrentQueue<T>` of readonly structs. Workers enqueue; a main-thread drain publishes Type A. Build this before any graph/MPC work so those stories do not invent a queue as a side effect.  
2. **Track Graph Builder:** Port PathGraphBuilder, but convert it to a Unity Coroutine or Job. It must yield across multiple frames so evaluating 500 track edges doesn't freeze the game. Publish results through the mailbox.  
3. **Geometry Scanner (A116):** Port the track curvature math, caching the results heavily so we only recalculate when the train enters a new track segment.  
4. **Predictive Braking (MPC):** Port the PredictiveBrakeController (Feed-Forward Stress math).

### **Phase 5: The Tools & Governors (The Hands)**

*Now that the foundation is rock solid, we build the actual gameplay features.*

1. **The Thermal Governor:** Hook the engine temperature events to the throttle clamp.  
2. **Dispatch Desk & Switch List:** Bring back the 2D Map UI and the Epic 5 multi-step routing logic.  
3. **Auto-Coupler / Remote Tools:** The QOL features.