# 🚗 Predictive Braking System

> **Production-grade, AI-assisted predictive braking pipeline for autonomous and semi-autonomous vehicles.**  
> Fuses real-time sensor data with a physics-based threat model to anticipate and command brake actuation before a reactive system would detect the hazard.

[![CI Status](https://img.shields.io/github/actions/workflow/status/your-org/predictive-braking/ci.yml?label=CI&logo=github)](https://github.com/your-org/predictive-braking/actions)
[![Coverage](https://img.shields.io/codecov/c/github/your-org/predictive-braking?logo=codecov)](https://codecov.io/gh/your-org/predictive-braking)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-1.0.0-green.svg)](CHANGELOG.md)
[![Python 3.11+](https://img.shields.io/badge/python-3.11%2B-blue.svg)](https://www.python.org/)

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Quick Start — Onboarding Guide](#2-quick-start--onboarding-guide)
3. [Repository Structure & File Descriptions](#3-repository-structure--file-descriptions)
4. [Development Steps](#4-development-steps)
5. [Architecture Summary](#5-architecture-summary)
6. [Configuration Reference](#6-configuration-reference)
7. [Testing](#7-testing)
8. [Repo Conventions](#8-repo-conventions)
9. [AI Tool Comparison](#9-ai-tool-comparison)
10. [Contributing](#10-contributing)
11. [License](#11-license)

---

## 1. Project Overview

### What This System Does

The **Predictive Braking System** computes a real-time **Threat Priority Index (TPI)** for every object within a configurable forward-looking horizon. When a threat crosses the TPI threshold, it sends a rate-limited deceleration command to the brake actuator via the vehicle's CAN bus — all within the latency budget defined in `docs/architecture/constraints.md`.

### Why "Predictive"?

| Reactive Braking | Predictive Braking |
|---|---|
| Detects hazard, then brakes | Anticipates hazard using sensor fusion |
| Response latency dominated by human/system reaction | Response latency dominated only by actuation hardware |
| Stopping distance = reaction distance + braking distance | Stopping distance ≈ braking distance alone |
| No road-surface awareness | Friction coefficient estimated at 100 Hz via Kalman filter |

### Key Performance Targets

| Metric | Target | Source |
|---|---|---|
| End-to-end pipeline latency | ≤ 12 ms | `docs/architecture/constraints.md` |
| Friction estimate update rate | 100 Hz | `config/safety_params.yaml` |
| Stopping distance accuracy (simulation) | ±3% of physics model | `docs/architecture/algorithm_design.md` |
| Unit test coverage | ≥ 90% | CI enforcement |
| HIL test pass rate | ≥ 99.5% | Phase 4 exit criterion |

---

## 2. Quick Start — Onboarding Guide

### 2.1 Prerequisites

| Requirement | Minimum Version | Notes |
|---|---|---|
| Python | 3.11 | Managed via `.python-version` |
| Poetry | 1.8 | Dependency and virtual-env management |
| Docker | 24.0 | Used for reproducible simulation runs |
| CAN tools (`can-utils`) | 2024.x | Required for hardware integration work only |
| Jupyter Lab | 4.x | Notebooks for debug & exploration |

### 2.2 Environment Setup

```bash
# 1. Clone the repository
git clone https://github.com/your-org/predictive-braking.git
cd predictive-braking

# 2. Install Python dependencies (creates isolated virtual env)
poetry install

# 3. Activate the virtual environment
poetry shell

# 4. Copy the example config and set your local overrides
cp config/safety_params.example.yaml config/safety_params.yaml
cp config/actuation_params.example.yaml config/actuation_params.yaml

# 5. Validate all configs against their schemas
python tools/validate_configs.py --all

# 6. Run the full test suite to confirm a clean baseline
pytest --tb=short -q
```

**Expected output:**  
```
....................................................... 347 passed in 8.41s
```

### 2.3 Running the Simulation

```bash
# Run the default physics simulation scenario (dry road, 80 km/h, pedestrian crossing)
python -m src.simulation.runner --scenario scenarios/default.yaml

# Run all simulation scenarios in parallel (requires Docker)
docker compose run simulator --all-scenarios

# Launch the interactive debug notebook
jupyter lab notebooks/debug_session.ipynb
```

### 2.4 Connecting to Hardware (Optional)

> ⚠️ Hardware integration requires access to the physical test rig. Contact the embedded team lead before proceeding.

```bash
# Bring up the virtual CAN interface (Linux only)
sudo modprobe vcan
sudo ip link add dev vcan0 type vcan
sudo ip link set up vcan0

# Launch the middleware stack against the virtual CAN interface
python -m src.middleware.main --can-interface vcan0 --log-level DEBUG
```

### 2.5 Key Documents to Read First

New contributors should read these in order:

1. `docs/requirements/PRD.md` — understand the *what* and *why*
2. `docs/architecture/constraints.md` — understand the non-negotiable limits
3. `docs/architecture/algorithm_design.md` — understand the *how*
4. `TOOL_WORKFLOW_GUIDE.md` — understand the development process
5. `docs/GLOSSARY.md` — align on terminology before writing code

---

## 3. Repository Structure & File Descriptions

```
predictive-braking/
├── .github/
│   ├── workflows/
│   │   ├── ci.yml                  # Main CI: lint → typecheck → test → coverage
│   │   ├── hil.yml                 # Hardware-in-the-loop test pipeline
│   │   └── release.yml             # Release tagging and changelog generation
│   └── PULL_REQUEST_TEMPLATE.md    # PR checklist enforced on every merge request
├── config/
│   ├── safety_params.example.yaml  # Template: TPI_THRESHOLD, d_margin, μ_seed
│   ├── safety_params.yaml          # Local override (git-ignored)
│   ├── actuation_params.example.yaml # Template: a_max, jerk_limit, rate_limit_hz
│   └── actuation_params.yaml       # Local override (git-ignored)
├── docs/
│   ├── architecture/
│   │   ├── algorithm_design.md     # Full mathematical spec of the braking model
│   │   ├── constraints.md          # Hard latency, safety margin, and HW constraints
│   │   └── adr/                    # Architecture Decision Records (numbered, immutable)
│   ├── hardware/
│   │   ├── sensor_map.md           # Sensor IDs, positions, update rates, CAN IDs
│   │   └── expected_signals.yaml   # Reference signal timeline for CAN diffing
│   ├── ops/
│   │   ├── deployment_playbook.md  # Step-by-step production deployment procedure
│   │   ├── monitoring.md           # Dashboard setup, alert thresholds, SLOs
│   │   └── runbook.md              # On-call incident response procedures
│   ├── performance/
│   │   ├── latency_baseline.md     # Phase 3 profiling results (baseline reference)
│   │   └── benchmark_results.md    # Phase 4 benchmark against all target metrics
│   ├── requirements/
│   │   └── PRD.md                  # Product Requirements Document
│   ├── safety/
│   │   └── safety_review.md        # Safety-critical path review outcomes
│   └── GLOSSARY.md                 # Project-wide term definitions
├── notebooks/
│   ├── debug_session.ipynb         # Interactive step-through of TPI and Kalman state
│   ├── param_sweep.ipynb           # Sensitivity analysis across μ and TPI_THRESHOLD
│   └── visualize_stopping.ipynb    # Stopping distance plots vs speed and friction
├── scenarios/
│   ├── default.yaml                # Standard test: dry road, 80 km/h, pedestrian
│   ├── wet_road_high_speed.yaml    # Stress test: μ=0.3, 120 km/h, cut-in vehicle
│   ├── ice_low_speed.yaml          # Edge case: μ=0.1, 30 km/h, static obstacle
│   └── multi_object_urban.yaml     # Complex: 6 simultaneous threats, urban geometry
├── src/
│   ├── core/
│   │   ├── __init__.py
│   │   ├── braking_model.py        # PRIMARY: stopping distance, TPI, actuator cmd
│   │   ├── kalman_filter.py        # Friction estimator — Kalman filter implementation
│   │   └── threat_classifier.py    # Object scoring: TPI calculation and escalation
│   ├── middleware/
│   │   ├── __init__.py
│   │   ├── can_interface.py        # CAN bus read/write; message encode/decode
│   │   ├── sensor_fusion.py        # Wheel slip + IMU + road classifier fusion
│   │   ├── scheduler.py            # Real-time task scheduler (POSIX SCHED_FIFO)
│   │   └── main.py                 # Middleware entry point; wires all components
│   ├── ml/
│   │   ├── __init__.py
│   │   ├── road_classifier.py      # Inference wrapper for road-surface ML model
│   │   └── models/
│   │       └── road_v1.onnx        # Trained ONNX model (camera + LIDAR features)
│   └── simulation/
│       ├── __init__.py
│       ├── runner.py               # Scenario runner; loads YAML, drives sim loop
│       ├── physics_engine.py       # Ground-truth physics model for validation
│       └── sensor_mock.py          # Deterministic sensor data generator
├── tests/
│   ├── conftest.py                 # Shared fixtures, random seeds, test config
│   ├── unit/                       # Pure-function tests; no hardware or I/O
│   ├── integration/                # Full-stack tests against hardware rig
│   ├── simulation/                 # Scenario-based tests using physics engine
│   ├── hil/                        # Hardware-in-the-loop test scripts
│   ├── fault_injection/            # Adversarial and edge-case scenarios
│   ├── regression/                 # Baseline suite locked at each release
│   └── smoke/                      # Post-deployment sanity checks
├── tools/
│   ├── validate_configs.py         # Validates YAML configs against JSON schemas
│   ├── print_effective_config.py   # Prints merged runtime config for debugging
│   ├── log_parser.py               # Filters and formats structured JSON run logs
│   ├── can_capture.py              # Captures live CAN bus traces to .asc files
│   └── can_diff.py                 # Diffs a CAN trace against expected signals
├── .python-version                 # Pins Python version for pyenv
├── CHANGELOG.md                    # Semantic versioned change history
├── CONTRIBUTING.md                 # Contributor guidelines and code of conduct
├── TOOL_WORKFLOW_GUIDE.md          # Full development workflow and debugging protocol
├── LICENSE                         # MIT License
├── Makefile                        # Common developer shortcuts
├── pyproject.toml                  # Poetry config: deps, scripts, tool settings
└── README.md                       # This file
```

---

## 4. Development Steps

Follow these steps when implementing any new feature or fix. Shortcuts that skip steps are the primary source of production incidents on this project.

### Step 1 — Create a Branch

```bash
git checkout main && git pull
git checkout -b feature/your-feature-name
```

Follow the branch naming convention in [Section 8](#8-repo-conventions).

### Step 2 — Understand the Constraint

Before writing code, locate the relevant entry in `docs/architecture/constraints.md`. If none exists, open a discussion or ADR draft in `docs/architecture/adr/` first.

### Step 3 — Write the Test First

For `src/core/` and `src/middleware/`, write the test before the implementation:

```bash
# Create your test file
touch tests/unit/test_your_feature.py

# Run it (it will fail — that's correct at this stage)
pytest tests/unit/test_your_feature.py -v
```

### Step 4 — Implement

Write the minimum implementation to make the test pass. Do not add capabilities not covered by a test.

### Step 5 — Run the Full Test Suite

```bash
# Lint and type-check
poetry run ruff check src/ tests/
poetry run mypy src/

# Full test suite with coverage
pytest --cov=src --cov-report=term-missing -q
```

Coverage must remain at or above 90%. The CI pipeline enforces this and will block the PR if the threshold drops.

### Step 6 — Update Documentation

- If you changed a public API: update docstrings and `docs/architecture/algorithm_design.md`.
- If you changed a config parameter: update `config/*.example.yaml` and `docs/GLOSSARY.md`.
- If you made an architectural decision: create an ADR in `docs/architecture/adr/`.

### Step 7 — Update the Changelog

Add an entry under `[Unreleased]` in `CHANGELOG.md`:

```markdown
### Added
- Trajectory overlap weighting in TPI calculation (`feat(core)`)

### Fixed
- CAN message byte order for brake command on big-endian ECU (`fix(middleware)`)
```

### Step 8 — Open the Pull Request

Push your branch and open a PR against `main`. Fill in every item on the PR checklist (`.github/PULL_REQUEST_TEMPLATE.md`). Tag a second reviewer for any changes in `src/core/`.

### Step 9 — Address Review Feedback

Respond to every comment — either fix it or explicitly note why you're not. Avoid force-pushing after review has started; use new commits so reviewers can see the delta.

### Step 10 — Merge & Clean Up

Once approved and CI is green, use **Squash and Merge** for feature branches. Delete the branch after merge.

```bash
# Clean up local branch after merge
git checkout main && git pull
git branch -d feature/your-feature-name
```

---

## 5. Architecture Summary

The system operates as a real-time pipeline with five concurrent layers:

```
┌─────────────────────────────────────────────────────────────────────┐
│                        SENSOR LAYER (100 Hz)                        │
│  Camera · LIDAR · Wheel-Speed Sensors · IMU · GPS                   │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    SENSOR FUSION LAYER (100 Hz)                      │
│  src/middleware/sensor_fusion.py                                      │
│  Kalman-filtered μ estimate · Road classifier inference              │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    THREAT ASSESSMENT LAYER (50 Hz)                   │
│  src/core/threat_classifier.py · src/core/braking_model.py          │
│  TPI calculation · Horizon check · Threat escalation                 │
└───────────────────────────┬─────────────────────────────────────────┘
                            │ (only when TPI > threshold)
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    ACTUATION COMMAND LAYER (on demand)               │
│  src/core/braking_model.py                                           │
│  a_cmd computation · Jerk limiting · Rate limiting                   │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      CAN BUS INTERFACE LAYER                         │
│  src/middleware/can_interface.py                                      │
│  Message encoding · TX scheduling · Fault detection                  │
└─────────────────────────────────────────────────────────────────────┘
```

For the full mathematical specification of each layer, see `docs/architecture/algorithm_design.md` and `TOOL_WORKFLOW_GUIDE.md § 3`.

---

## 6. Configuration Reference

All runtime behavior is controlled by YAML files in `config/`. Never hardcode values that appear in these files.

### `config/safety_params.yaml`

| Parameter | Default | Description |
|---|---|---|
| `TPI_THRESHOLD` | `0.45` | Minimum TPI score to escalate a threat to the actuator |
| `d_margin` | `2.5` | Safety buffer beyond computed stopping distance (meters) |
| `mu_seed` | `0.8` | Initial friction coefficient estimate (dry road) |
| `mu_min_clamp` | `0.1` | Hard floor for friction estimate (ice) |
| `horizon_update_hz` | `50` | Rate at which the threat horizon is recomputed |

### `config/actuation_params.yaml`

| Parameter | Default | Description |
|---|---|---|
| `a_max` | `-8.5` | Maximum deceleration command (m/s²; negative = braking) |
| `jerk_limit` | `15.0` | Maximum rate of change of acceleration (m/s³) |
| `rate_limit_hz` | `100` | Maximum frequency of actuator command updates |
| `t_reaction_ms` | `8` | System actuation latency offset added to horizon calc (ms) |

---

## 7. Testing

### Test Hierarchy

| Layer | Location | When to Run |
|---|---|---|
| Unit | `tests/unit/` | Every commit; < 10 s |
| Simulation | `tests/simulation/` | Every PR; ~ 60 s |
| Integration | `tests/integration/` | Pre-merge on feature branches; requires hardware rig |
| Fault Injection | `tests/fault_injection/` | Weekly CI and before each release |
| HIL | `tests/hil/` | Every release candidate |
| Regression | `tests/regression/` | Every release |
| Smoke | `tests/smoke/` | Post every production deployment |

### Running Tests

```bash
# Unit tests only (fast)
pytest tests/unit/ -q

# Simulation tests with physics trace
pytest tests/simulation/ --log-cli-level=DEBUG

# Full suite
pytest -q

# With coverage report
pytest --cov=src --cov-report=html
open htmlcov/index.html

# Specific scenario
pytest tests/simulation/ -k "ice_low_speed" -v
```

### CI Pipeline

The GitHub Actions CI pipeline (`ci.yml`) runs on every push and PR:

```
Lint (ruff) → Type-check (mypy) → Unit Tests → Simulation Tests → Coverage Gate (≥90%)
```

HIL tests run on a separate self-hosted runner connected to the test rig, triggered by the `hil.yml` workflow on release branches.

---

## 8. Repo Conventions

### Branch Naming

```
<type>/<short-description>

feature/   — new capabilities
fix/       — bug corrections
docs/      — documentation only
refactor/  — code restructuring without behavior change
perf/      — performance improvements
test/      — test additions or fixes
chore/     — tooling, deps, CI changes
```

### Commit Message Format (Conventional Commits)

```
<type>(<scope>): <short summary in present tense, ≤72 chars>

[optional body — explain the *why*, not the *what*; wrap at 72 chars]

[optional footer — BREAKING CHANGE: <desc> | Fixes #<issue>]
```

**Valid types:** `feat` · `fix` · `docs` · `refactor` · `perf` · `test` · `chore`  
**Valid scopes:** `core` · `middleware` · `ml` · `simulation` · `config` · `ci` · `docs`

### Code Style

| Tool | Config File | Enforced By |
|---|---|---|
| `ruff` | `pyproject.toml [tool.ruff]` | CI (pre-merge) |
| `mypy` | `pyproject.toml [tool.mypy]` | CI (pre-merge) |
| `black` (formatter) | `pyproject.toml [tool.black]` | Pre-commit hook |
| `isort` | Handled by `ruff` | CI (pre-merge) |

Install the pre-commit hooks to catch issues locally:

```bash
pre-commit install
```

### PR Rules

- Minimum **1 reviewer** approval for all PRs
- Minimum **2 reviewer** approvals for changes in `src/core/` or `src/middleware/`
- All CI checks must be green before merge
- Squash-merge only (no merge commits on `main`)
- Branch must be up-to-date with `main` before merge

### Versioning (SemVer)

```
MAJOR — breaking change to braking model API or safety contract
MINOR — new feature, backward-compatible
PATCH — bug fix, documentation, or performance improvement
```

Release tags (`v1.2.3`) are created on `main` only after a completed release checklist in `docs/ops/deployment_playbook.md`.

---

## 9. AI Tool Comparison

The project uses four AI coding assistants at different points in the workflow. This section documents their relative strengths, limitations, and recommended use cases so teams can choose the right tool without duplication of effort or unreviewed output in safety-critical paths.

---

### 9.1 Feature Comparison Matrix

| Capability | Cursor | Gemini | GitHub Copilot | Microsoft Copilot |
|---|---|---|---|---|
| **In-editor code generation** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ |
| **Multi-file context awareness** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| **Long-context document synthesis** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ |
| **Research & literature analysis** | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ |
| **Boilerplate & scaffold generation** | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Test generation** | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ |
| **Docstring & comment completion** | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Refactoring across files** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| **Documentation drafting** | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Architecture trade-off analysis** | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ |
| **IDE integration (native)** | VS Code (native) | Web / IDE plugins | VS Code / JetBrains | Web / Edge / Office |
| **Offline / air-gapped support** | ❌ | ❌ | ❌ | ❌ |
| **Self-hosted option** | ❌ | ❌ | Copilot Enterprise | ❌ |
| **Access model** | Subscription | Free tier + paid | GitHub subscription | Microsoft 365 / free |

---

### 9.2 Recommended Use Cases Per Tool

#### 🖱️ Cursor

**Best for:** Day-to-day development work inside the editor.

- **Refactoring** `src/core/` and `src/middleware/` across multiple files simultaneously — Cursor's codebase-awareness prevents context loss that plagues single-file tools.
- **Quick, precise edits** with natural language: *"rename all occurrences of `tpi_score` to `threat_index` and update the docstrings."*
- **Exploring unfamiliar sections** of the codebase by chatting with the repo.
- **Avoid for:** Anything touching safety-critical algorithm logic without a human review step — Cursor can hallucinate plausible-looking but physically incorrect constants.

#### 🔵 Gemini

**Best for:** Research, analysis, and long-context synthesis.

- **Literature review** on friction estimation algorithms and Kalman filter tuning strategies.
- **Comparing architectural options** — paste the constraints doc and ask for trade-off analysis with citations.
- **Long-context document reading** — Gemini's large context window handles the full PRD + algorithm design doc simultaneously.
- **Summarizing test results** from long CI logs or benchmark reports.
- **Avoid for:** Generating embedded real-time code — Gemini is less reliable on POSIX scheduling semantics and CAN protocol specifics than Cursor or Copilot.

#### 🐙 GitHub Copilot

**Best for:** Accelerating routine coding patterns within the established codebase style.

- **Test scaffolding** — generating `pytest` fixtures and parametrize decorators that match the project's `conftest.py` patterns.
- **Boilerplate generation** — new middleware modules, config loaders, CLI tools.
- **Docstring and type-hint completion** to maintain documentation coverage.
- **Inline suggestions** while implementing known patterns (e.g., new scenario YAML handlers).
- **Avoid for:** Novel algorithmic work — Copilot's suggestions for new TPI formulas or Kalman tuning often look correct but are not physically grounded. Always validate against `algorithm_design.md`.

#### 🪟 Microsoft Copilot

**Best for:** Documentation, stakeholder communication, and project management artifacts.

- **Drafting PRDs, ADRs, and stakeholder summaries** from technical inputs.
- **Generating the `CHANGELOG.md`** entries from a list of PR titles.
- **Producing onboarding guides** and training materials for new team members.
- **Summarizing meeting notes** and translating them into action items for the project board.
- **Avoid for:** Low-level Python or C debugging — Microsoft Copilot does not have the deep code-context access that Cursor or GitHub Copilot provide, making it less effective for specific implementation problems.

---

### 9.3 Decision Guide — Which Tool to Use?

```
Is the task inside the code editor?
  YES → Is it a complex multi-file refactor?
          YES → Cursor
          NO  → Is it boilerplate, tests, or docstrings?
                  YES → GitHub Copilot
                  NO  → Cursor (for precision) or GitHub Copilot (for speed)

  NO  → Is the task research, analysis, or long-context reading?
          YES → Gemini
          NO  → Is the task documentation, comms, or planning artifacts?
                  YES → Microsoft Copilot
                  NO  → Gemini (for technical depth) or Microsoft Copilot (for prose)
```

### 9.4 Non-Negotiable Rule for All AI Tools

> **No AI-generated code enters `src/core/` or `src/middleware/` without:**
> 1. A passing unit test that exercises the changed logic.
> 2. Manual review against `docs/architecture/algorithm_design.md`.
> 3. For safety-critical paths: sign-off from a second engineer in the PR.

AI tools are pair programmers, not solo engineers. The human is always the pilot.

---

## 10. Contributing

1. Read `CONTRIBUTING.md` for the code of conduct and contribution agreement.
2. Follow the [Development Steps](#4-development-steps) above exactly.
3. Open an issue before starting significant work so effort isn't duplicated.
4. Tag `@your-org/safety-team` in any PR touching `src/core/`, `src/middleware/`, or `docs/safety/`.

For questions, open a GitHub Discussion rather than an issue — discussions are the right venue for architecture debates and design questions.

---

## 11. License

This project is licensed under the **MIT License** — see [LICENSE](LICENSE) for full details.

---

<div align="center">

**Predictive Braking System** · Built with precision and safety in mind  
Maintained by Patrick Buckles · San Ramon, CA

</div>
