# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-09-03 (**13.4** thin `[x]` on **`main`** @ **`2.13.4.7`**; keep **`feature/13.4-autonomous-transit-thin`**. **Epic 13** still open)

## Summary

**DerailValleyModv2** — Yard Master Suite v2. **Epic 3 Display Shell (infra) closed** at **3.3.1**. **Epic 4 infra closed** at **4.3**. **Epic 6 Diagnostic HUD closed** at **6.21** (`2.6.21.6`; extra pins **6.21.7** `2.13.1.16`). **Epic 7 Governors closed** at **7.5** (`2.7.5.7`). **8.7** on **`main`** (`2.8.7.31`). **9.1** + **9.1.2** + **9.1.3** + **9.1.4** on **`main`** (`2.9.1.40`). **13.1** `[x]` 7-row reverse-to-TT + leave sawtooth (`2.13.1.20`). **13.2.1** `[x]` Prep couple auto-Next (`2.13.2.1`). **13.2.2** `[x]` Prep at-track cue (`2.13.2.2`). **13.6.1** `[x]` remote take (`2.13.6.1`). **13.4** thin `[x]` drive-leg GO (`2.13.4.7`). **Next:** **13.5** auto delivery drop. **13.2.3** HOLD. Do **not** close Epic 13. **9.2** only if flat PID fails. Full v1 map: [V1_FEATURE_COVERAGE.md](V1_FEATURE_COVERAGE.md). Canonical HTP: [HTP.md](HTP.md).

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — **13.4** thin (`2.13.4.7`). |
| **`feature/13.4-autonomous-transit-thin`** | Keep — 13.4 land. |
| **`feature/13.6.1-remote-take`** | Keep — 13.6.1 land. |
| **`feature/13.2.2-prep-track-arrival`** | Keep — 13.2.2 land sha `53536c2`. |
| **`feature/13.2.1-couple-auto-advance`** | Keep — 13.2.1 land sha `7dd7634`. |
| **`feature/9.1.4-next-chip`** | Keep — 9.1.4 land sha `18891b6`. |
| **`feature/13.2.3-filo-pickup-queue`** | Park — WIP stashed; multi-area FILO later. |
| **`feature/13.1-reverse-to-tt`** | Keep — 13.1 archaeology ([Feature_Branch_Archaeology.md](git/Feature_Branch_Archaeology.md)). |
| **`feature/9.1.3-win0-graph-dump`** | **9.1.3** archaeology — **keep**. |
| **`feature/8.7-route-pin-cleared`** | Keep — do not delete. |

---

## Sequence

**Next:** **13.5** auto delivery drop → **13.6** thin → **13.2.4**. Park **13.2.3** WIP; multi-area FILO / **10.2** after walk-in. Do not re-smoke **13.4**. Clear-line pin / CLEARED stop cue deferred to **8.7** revisit. **9.2** only if flat PID fails. **Deferred:** 8.8–8.9, 11 Catalog, 12 Roadside, Epic 14 until after 13. Reverse-cruise gold remains cab **`feature=0`** with desk closed.

### Autonomy tracker (re-baseline)

| Story | Est (days) | Started | Done | Actual | Notes |
|-------|------------|---------|------|--------|-------|
| 8.7 remainder | 0.5–1 | 2026-08-27 | 2026-08-29 | ~2 | `2.8.7.31` CP0 + chords |
| 9.1 PID hold | 2–3 | 2026-08-29 | 2026-08-30 | ~1.5 | `2.9.1.14` CP1 + takeoff/coast |
| 9.1.3 walker + span + refresh | 2–4 | 2026-09-01 | 2026-09-01 | ~1 | `2.9.1.39` Wins 0–5.1 |
| 13.1 | 1.5–2 | 2026-09-01 | 2026-09-02 | ~2 | CMPH `2.13.1.20`; branch kept |
| 13.6.1 | 0.5–1 | 2026-09-02 | 2026-09-02 | ~0.5 | CMPH `2.13.6.1`; branch kept |
| 13.4 thin | 2–3 | 2026-09-03 | 2026-09-03 | ~1 | CMPH `2.13.4.7`; Prep GO approach; branch kept |
| 13.2.1 | 0.5–1 | 2026-09-02 | 2026-09-02 | ~0.5 | `2.13.2.1` |
| 13.2.2 | 1 | 2026-09-02 | 2026-09-02 | ~0.5 | `2.13.2.2` |
| 13.2.3 | 1–1.5 | | | | HOLD / multi-area later |
| 13.2.4 | 2–3 | | | | after 13.6 thin |
| 13.2.5 | 2–3 | | | | |
| 13.2.6 | 1 | | | | |
| 13.3 | 1 | | | | full Validate UI after thin path |
| 13.5 | 3–4 | | | | after 13.4 thin |
| 13.6 | 1.5–2.5 | | | | thin after 13.5; 13.6.1 done |

*Fill **Actual** on ship; adjust **Est** for remaining rows when reality diverges.*
