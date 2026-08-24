---
name: session-summarizer
description: Leaving-agent protocol. Produces a compressed, decision-first handoff that preserves intent and next steps while stripping execution noise.
---

# Session Summarizer — Leaving Agent Protocol

Use this skill when ending a session, reducing context, handing work to a new agent, or **CMPH**.

Goal: transfer **working state** with **minimal tokens**, in a shape the **human reads in chat**.

A gitignored file with no matching chat brief is an incomplete handoff.

---

## Chat + file (both required)

1. Paste the **Receiver brief** as the CMPH closing message (same headings as the example in [`.cursor/handoff/_template.md`](../../handoff/_template.md)).
2. Write that **same body** to:
   - Prefer: `docs/handoff/NNNN-HANDOFF-YYYY-MM-DD_HHmm.md`
   - Copy: `.cursor/handoff/NNNN-handoff-YYYY-MM-DD_HHmm.md`
3. Last line of chat **and** note: `**Filename:** \`docs/handoff/NNNN-HANDOFF-YYYY-MM-DD_HHmm.md\``
4. Sync **Git** rows into `AGENT_HANDOFF.md` → *Current state* (tracked).

Do **not** substitute a one-liner (“on main, next 6.18”). CMPH is hitch table **inside** the brief’s **Performance** block — do not print a second hitch table after the brief.

---

## Filename rules (mandatory)

- `NNNN` new and monotonic (`0001`, `0002`, …). Never reuse.
- Never edit a prior handoff to “update” it — write a new file (exception: the user asked to reshape a specific note).
- Timestamp: local `YYYY-MM-DD_HHmm` (24h).

---

## Receiver brief structure (required)

Copy density from the **6.17 closed** example in `_template.md`. Required headings:

- Title: `Receiver brief (N.M closed | open | WIP)`
- **Objective** — one sentence; include “do not re-prove this land” when shipped
- **Git** — five rows: Story (`[x]`/`[ ]`), Version (`info.json`), On (`origin/main @ sha` or not-merged), Do not, Next
- **Decisions** — one dense paragraph (semicolons)
- **In scope next** / **Out**
- **Acceptance** — already met or not yet (Tier 1 count, smoke, UMM version)
- **Next steps** — numbered, verifiable; if next is in-world smoke, player-facing ask
- **Performance** — three-row hitch table ([chat-performance-summary.mdc](../../rules/chat-performance-summary.mdc)); or `no hitch-summary this turn`
- **Filename** — full path as above

**Gates** (review / debt / tests) live in the **file** after the brief, not in chat.

Budget: the chat brief should stay near the 6.17 example length. Strip logs first.

---

## Progressive summarization (what to keep vs strip)

Keep: decisions, rationale, next steps, acceptance, hitch numbers.

Strip: long logs, step-by-step transcripts, duplicate tracked-doc dumps.

---

## “Green and Clean” exit check

- Chat has the full Receiver brief (not only “see docs/handoff”).
- Filename line present; `NNNN` unused.
- Tracked Git truth matches **On**.
- Acceptance and hitch numbers are real, not placeholders.
