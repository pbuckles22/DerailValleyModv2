---
name: context-bootstrapper
description: Receiving-agent protocol. If the user pastes a Receiver brief or Filename, execute Next steps — do not write another handoff.
---

# Context Bootstrapper — Receiving Agent Protocol

Use this skill when starting work on this repo, resuming after a break, switching to a new “feature-agent”, or when the user **pastes** a Receiver brief / Git table / `docs/handoff/NNNN-…`.

Goal: reach a **confident, bounded next step** using **minimal context**, then **do that step**.

A pasted brief is **inbound context**, not a request to file another note. Leave (new `NNNN`) is [session-summarizer](../session-summarizer/SKILL.md) only after **UCPH** / **CMPH** / **SWAT** / park. Rule: [handoff-receive.mdc](../../rules/handoff-receive.mdc).

---

## Bootstrap order (read in this order)

1. **The paste itself** (if they pasted a brief). Trust **Git** / **Next** / **Acceptance**. Do not rewrite it to disk.

2. **Project baseline**
   - `.cursor/rules/always.mdc`
   - `AGENT_HANDOFF.md` → *Current state* (only if Git rows are missing from the paste)

3. **Current phase / feature truth** (only files the **Next** step needs)
   - `PM_PLAN.md` / `TEST_PLAN.md` / `docs/HTP.md` as relevant

4. **The named handoff file** if they gave a Filename and you need a line the paste omitted. Highest `NNNN` otherwise. Older notes are history.

5. **If the task is code-touching:** the smallest set of files to act safely.

## Git truth — do not re-prove the last ship

`AGENT_HANDOFF.md` → *Current state* plus the pasted **Git** table are the land record.

- If **On** is **`origin/main @ sha`**, story **N.M** `[x]`: **start at Next steps.** Do **not** `git log` / `git fetch` / “is this on main?” / re-merge / re-smoke that story.
- `git status` is only to see if *your* tree is dirty before you edit.
- Re-check git **only** when Git truth is missing, says **waiting on merge** / **unpushed** / **WIP**, the user asked “is it on main?”, or you are the agent who will merge.

Mismatch (handoff says landed, Current state still names a feature branch): **Current state on the branch you have checked out wins.** Ask the user; do not silently re-do the merge.

---

## Open for execution (chat only — not a new `NNNN`)

After reading, reply with a **short** open (not a second Receiver brief):

- **This turn:** one sentence from **Next** / **Next steps** (the work, not “write a handoff”).
- **Already true:** do not re-prove (from **Do not** / **Acceptance** met).
- **Still open:** smoke, code, or wait — from **Acceptance** not yet.
- **Do now:** 3–7 numbered steps **to execute**, each with a check. If next is in-world smoke: player-facing ask ([deploy-before-smoke.mdc](../../rules/deploy-before-smoke.mdc)). Confirm Mods `info.json` Version if deployable.
- **Out:** copied from the paste. Do not invent Epic 15 / merge `main` / pop stash.

Then **start** that work (deploy verify, tests, code) or stop so they can cab-smoke. Do **not** write `docs/handoff/` or `.cursor/handoff/` on this turn.

If a required input is missing, stop and ask before acting.

---

## Token budget

Preferred payload:

- The pasted brief **or** latest handoff note (session delta)
- `AGENT_HANDOFF.md` Current state only if Git was not in the paste
- **Only the files you’re editing**

Avoid: full transcripts; repeating the inbound Git table as a new document; writing `NNNN+1`.
