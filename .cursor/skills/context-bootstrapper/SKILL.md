---
name: context-bootstrapper
description: Receiving-agent protocol. Boots a new agent into the minimum correct context (select + isolate) and produces a clear next-step plan without guessing.
---

# Context Bootstrapper — Receiving Agent Protocol

Use this skill when starting work on this repo, resuming after a break, switching to a new “feature-agent”, or when context feels bloated/confusing.

Goal: reach a **confident, bounded next step** using **minimal context**.

---

## Bootstrap order (read in this order)

1. **Project baseline (always-on context)**  
   - `.cursor/rules/always.mdc`
   - `AGENT_HANDOFF.md`

2. **Current phase / feature truth** (choose the one that matches the user’s goal)  
   - `PM_PLAN.md` (phase/scope)
   - `TEST_PLAN.md` (Tier 1 / Tier 2 validation gates)
   - Maps pin / PID / autonomy: also `docs/HTP.md`

3. **Most recent session handoff note** (if present)  
   - `docs/handoff/NNNN-HANDOFF-YYYY-MM-DD_HHmm.md` or `.cursor/handoff/NNNN-handoff-YYYY-MM-DD_HHmm.md` (**highest `NNNN`**)  
   - Expect a **Receiver brief** (Objective, Git, Decisions, In/Out, Acceptance, Next steps, Performance, Filename). Spec: [`.cursor/handoff/_template.md`](../../handoff/_template.md).  
   - Read **that file only** for session delta. Older notes are history; do **not** use them to second-guess a later **Git** block.

4. **If the task is code-touching:** read the smallest set of files necessary to act safely.

## Git truth — do not re-prove the last ship

`AGENT_HANDOFF.md` → *Current state* plus the latest brief **Git** table are the land record. They are not a rumor.

- If **On** is **`origin/main @ sha`**, story **N.M** `[x]`: **start at Next steps.** Do **not** `git log` / `git fetch` / “is this on main?” / re-merge / re-smoke that story. Do **not** narrate “I see it already landed.”
- `git status` is only to see if *your* tree is dirty before you edit.
- Re-check git **only** when Git truth is missing, says **waiting on merge** / **unpushed** / **WIP**, the user asked “is it on main?”, or you are the agent who will merge.

Mismatch (handoff says landed, Current state still names a feature branch): **Current state on the branch you have checked out wins.** Ask the user; do not silently re-do the merge.

---

## Produce the “Receiver Brief” (what you must write next)

After reading, produce a short brief with:

- **Objective**: one sentence; restate the user goal precisely.
- **Git**: one line from Git truth / Current state (`origin/main @ sha` + next story). If that is already landed, do **not** put “confirm it is on main” in Next steps.
- **Scope**:
  - **In scope**:
  - **Out of scope**:
- **Constraints**: branch policy, “no guessing”, validation tier.
- **Acceptance criteria**: 2–5 verifiable checks.
- **Next steps**: 3–7 bite-sized steps, each with a validation hook.
- **Open questions**: only if something blocks safe progress.

If any required inputs are missing, stop and ask before acting.

---

## Token budget guidance (cheap but effective)

Preferred context payload for a new session:

- **Tracked truth**: `AGENT_HANDOFF.md` + the relevant plan doc(s) (project state)
- **One handoff note**: latest only (session delta)
- **Only the files you’re editing** (and their direct dependencies)

Avoid:
- Full transcript dumps
- Large logs unless they directly change decisions
- Repeating old execution details already captured in tracked docs
