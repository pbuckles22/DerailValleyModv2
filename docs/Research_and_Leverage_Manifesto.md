## **5\. The Event Bus (Pub/Sub)**

* **The Old Way:** Update() loops running 60 times a second to check if playerSpeed \!= lastSpeed.  
* **What to Leverage:**  
  * We will stick to lightweight, native C\# Action delegates as outlined in our Pub/Sub architecture doc. However, we will model our registration pattern after enterprise standards like **MediatR**, ensuring strict decoupling between the Telemetry readers (Publishers) and the HUD elements (Subscribers).

## **6\. The "AI Triad" Workflow Standard**

Before a single line of code is written for a new feature, the developer MUST execute the following three-step workflow to ensure we are not reinventing the wheel:

### **Step 1: The Reconnaissance (GitHub Copilot)**

* **Goal:** Discover open-source solutions and Unity industry standards.  
* **Action:** Query Copilot Chat with specific intent.  
* **Prompt Example:** *"Search public repositories. How do other Unity modders or Derail Valley mods handle \[Feature Name\]? What libraries or native APIs do they use to achieve this without blocking the main thread?"*

### **Step 2: The Architectural Blueprint (Gemini Web)**

* **Goal:** Adapt the findings into the strict YMS v2.0 Zero-Allocation / Pub-Sub ruleset.  
* **Action:** Bring the Copilot findings to the Gemini architectural thread.  
* **Prompt Example:** *"Copilot found that most mods use UniverseLib for UI and UniTask for async. Here is how I want the feature to look. Draft the architectural blueprint and the C\# Interfaces for how this fits into our Event Bus."*

### **Step 3: The Execution (Cursor IDE)**

* **Goal:** Write the actual, compiling code in the repository.  
* **Action:** Feed the Gemini blueprint to Cursor using the @ context tagging.  
* **Prompt Example:** *"@YmsEventBus.cs @Blueprint.md. Implement the publisher class exactly as designed in the blueprint. Ensure strict adherence to zero-allocation rules."*

## **7\. Per-story register**

Do not re-scout from scratch. Open [LEVERAGE_REGISTER.md](LEVERAGE_REGISTER.md) for the story’s **reuse / adapt / invent** decision and the GitHub repos to inspect. Clone those repos only when the user asks. If a story’s wheel changes, update the register in the same ship.
