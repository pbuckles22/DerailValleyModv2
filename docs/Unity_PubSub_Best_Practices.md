# **Architecture Guideline: Event-Driven Unity Modding**

## **1\. The Core Constraint: The Main Thread Rule**

Unlike enterprise backends (Kafka, RabbitMQ), Unity is fundamentally a single-threaded game engine.

* **The Rule:** You can calculate *math* on any thread, but you can only touch *Unity APIs* (Transforms, GameObjects, UI) on the Main Thread.  
* **The Implication:** Our Event Bus must support both lightning-fast synchronous Main Thread events, and a thread-safe "Mailbox" for background tasks to safely pass data back to the Main Thread.

## **2\. The Two Types of Publishers**

We will implement two distinct Pub/Sub patterns depending on the payload.

### **Type A: The Synchronous System.Action Bus (For 90% of the Mod)**

For events that happen entirely on the Main Thread (e.g., player boards a train, player flips a lever), we use pure C\# delegate / System.Action.

* **Why:** It executes instantly and allocates **zero garbage memory**.  
* **How it works:** UI components subscribe to the Action. When the Telemetry script detects a state change, it invokes the Action. All UI components instantly update in the exact same frame.  
* **Payload Rule:** Event payloads MUST be primitive types (int, float) or readonly struct. We will *never* pass class objects or strings as event payloads, as that triggers Garbage Collection.

// GOOD: Zero Allocation  
public static event Action\<int, float\> OnLocoSpeedChanged;

// BAD: Allocates memory every tick  
public static event Action\<SpeedChangeEventArgsClass\> OnLocoSpeedChanged;

### **Type B: The Thread-Safe "Mailbox" (For Heavy Math)**

For systems like the Dijkstra Map Routing or the Predictive Brake Horizon, we will offload the math to System.Threading.Tasks.Task or the Unity JobSystem.

* **The Problem:** The background thread finishes the route, but it cannot directly update the Dispatch Desk UI (Main Thread crash).  
* **The Solution:** We build a ConcurrentQueue\<RouteResultStruct\>. The background publisher pushes the result into the queue. A lightweight Update() loop on the Main Thread peeks at the queue, dequeues the result, and publishes it via a Type A synchronous event to the UI.

## **3\. Feature Implementation Examples**

Here is how our massive bolt-on features will be rewritten under the Pub/Sub model:

### **Feature: The Loco Radar (Yielding Coroutine)**

* **Old Way:** Every frame, find all trains, calculate distance, build strings. (Massive GC Stutter).  
* **New Way:** A Unity Coroutine runs in the background, checking one train per frame (time-slicing). When it finishes scanning the whole yard, it triggers YmsEventBus.OnRadarScanComplete(RadarResultStruct). The AR UI is subscribed to this event and just repaints the icons.

### **Feature: Train Speed & Mass HUD (Type A Pub/Sub)**

* **Old Way:** OnGUI constantly asks loco.GetSpeed() and formats a string 60 times a second.  
* **New Way:** A LocoStatePublisher script runs in FixedUpdate (physics tick). It compares currentSpeed to lastSpeed. If the difference is \> 0.1 km/h, it fires OnSpeedChanged(newSpeed). The HUD subscribes, receives the float, updates a cached GUIContent, and renders it. If the speed doesn't change, no code runs.

### **Feature: Dispatch Desk Routing (Type B Pub/Sub)**

* **Old Way:** Player clicks "Set Dest", the game freezes for 2 seconds while the graph builds, then the UI updates.  
* **New Way:** Player clicks "Set Dest". UI shows a loading spinner. The click fires a background Task. When the Dijkstra algorithm finds the path, it puts the PathPlanResult struct into the Concurrent Mailbox. The Main Thread reads the mailbox, fires OnPathFound(PathPlanResult), the UI spinner stops, and the path draws.

## **4\. The "Unsubscribe" Mandate (Memory Leaks)**

In C\#, if a UI element subscribes to a static Event Bus and is later destroyed without unsubscribing, the Event Bus keeps the UI element alive in memory forever (a classic C\# memory leak).

**Rule:** Every script that subscribes to YmsEventBus in OnEnable() MUST unsubscribe in OnDisable() or OnDestroy().
