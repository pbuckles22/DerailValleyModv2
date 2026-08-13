# **Yard Master Suite v2.0: Master Onboarding & Architecture Guide**

**Date Established:** August 2026

**Role:** Tech Lead / Architect

## **1\. Project Philosophy (The "Clean Room" Principles)**

YMS v2.0 is a complete rewrite abandoning the "bolt-on" hacking of v1.0.

* **Zero-Allocation:** No new objects, lists, or un-cached string generation in Update() loops.  
* **Event-Driven (Pub/Sub):** Polling is dead. Systems only compute when the game state actually changes.  
* **Research First:** We do not reinvent the wheel. We leverage open-source Unity modding standards.

## **2\. The Development Workflow (The "AI Triad")**

Before writing a feature, developers MUST execute this three-step workflow:

1. **The Scout (GitHub Copilot):** Query Copilot Chat to find open-source libraries and industry standards for the feature (e.g., "How do Unity mods render UI in 2026 without OnGUI?").  
2. **The Architect (Gemini Web):** Feed Copilot's findings to Gemini. Adapt them to fit our strict Pub/Sub and Zero-Allocation rules. Generate the architectural blueprint.  
3. **The Builder (Cursor IDE):** Use Cursor (Ctrl+K / @files) to execute the Gemini blueprint and write the actual compiling C\# code.

## **3\. Documentation Index (The Source of Truth)**

These documents must be kept in the /docs/ folder and fed to AI assistants to establish project context:

* **YMS\_v2\_Architecture\_Plan.md:** The phased roadmap for rebuilding the mod from the ground up (Phase 1: Heartbeat \-\> Phase 5: Gameplay Tools).  
* **Unity\_PubSub\_Best\_Practices.md:** The engineering "Bible." Defines the rules for Type A (Synchronous UI updates) and Type B (Thread-safe heavy math) Pub/Sub routing.  
* **Research\_and\_Leverage\_Manifesto.md:** The operational rules for finding and implementing open-source libraries.  
* **LEVERAGE\_REGISTER.md:** Per-story reuse / adapt / invent log. Read the row before writing code.

## **4\. The Code Foundation (Phase 1\)**

The v2.0 repository begins with these two immutable pillars:

* **YardMasterSuite.Core/YmsEventBus.cs:** The central nervous system. All inter-script communication happens via these static Actions.  
* **YardMasterSuite.Core/GcCadenceProbe.cs:** A silent Unity MonoBehaviour that monitors frametimes and logs warnings if we accidentally introduce a Garbage Collection stutter.