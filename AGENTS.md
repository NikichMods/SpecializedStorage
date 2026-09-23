# Specialized Storage — Project Rules

The global engineering baseline for this repository is `NikichMods/DevRules`. Before substantive implementation, read `ENGINEERING_RULES.md`, `CI_POLICY.md`, `GIT_WORKFLOW.md`, and `PROJECT_BOOTSTRAP.md` there. This file contains only project-specific additions and explicit exceptions.

## Project identity and scope

- Public project: **Specialized Storage**.
- Game: `Graveyard Keeper 1.407`.
- Repository: `NikichMods/SpecializedStorage`.
- Canonical project: `SpecializedStorage.csproj`.
- Runtime source: `src/`.
- Stable BepInEx GUID: `nikich.graveyardkeeper.specializedstorage`.
- Scope: themed storage furniture may grant larger stack capacity to verified appropriate item categories, with the accepted compact increased-stack marker UI.

Do not turn this into a general inventory overhaul, arbitrary stack-size framework, or storage replacement without explicit user approval.

## Accepted behavior and compatibility

Preserve the established semantics unless a new change is explicitly accepted:

- supported specialized storages use verified classification logic;
- specialized stack capacity remains `min(vanilla × 4, 200)` and vanilla stack-size-1 items remain unchanged;
- unrelated storages/items retain vanilla limits;
- the increased-stack marker remains informational only and must not interfere with quantity labels, mouse/gamepad input, drag-and-drop, or save data;
- the accepted marker design is the 7×5 three-bar pixel marker at the accepted lower-left placement;
- mod-created UI must not duplicate after UI recreation;
- no custom save-data format is introduced without a separately approved migration;
- optional GYK Quick Stack compatibility remains narrow and evidence-based.

Never infer item/storage internals from display text when repository/game evidence can establish actual identifiers or metadata.

## Compile-reference boundary

The public project uses hand-authored minimal compile-time API reference projects under `references/` for the Graveyard Keeper/NGUI types required by production source. These reference projects contain signatures only and are not runtime replacements.

- Keep their assembly identities/signatures aligned with verified Graveyard Keeper 1.407 metadata used by the accepted build.
- Never package the reference-stub assemblies with the mod.
- If production code starts using a new game member, verify its declaring assembly, signature, and required enum values before adding it to the reference layer.
- Research evidence and raw game/decompilation material belong in the private research/legacy layer, not in this public repository.

## Repository and release contract

- `main` is accepted stable public state.
- Runtime/build candidates stay on a development branch until the acceptance gate is satisfied.
- Every numbered DLL handed to the user is immutable and tied to exact source.
- `docs/TEST_BUILD_LOG.md` is the durable build/test record.
- Public stable binaries are published through GitHub Releases after acceptance.
- Candidate/test binaries remain Actions artifacts.
- Candidate/test handoff uses a ready raw, versioned DLL so the exact build is obvious.
- The installed public payload uses the stable canonical filename `SpecializedStorage.dll`; a surrounding archive/store entry may carry the version.

## CI policy

Follow `DevRules/CI_POLICY.md` and `DevRules/GIT_WORKFLOW.md`.

- Hosted CI is used at coherent candidate/handoff boundaries, not for routine documentation/bookkeeping.
- A clean Release build is required before a new DLL is handed to the user.
- The current public reference-stub build path must be proven before it replaces the legacy private-reference build path.
- After acceptance, publish the exact tested artifact to GitHub Releases; do not rebuild different bytes under the same version.

## Long-lived sources of truth

Use `README.md`, `CHANGELOG.md`, `docs/MIGRATION_PROVENANCE.md`, `docs/TEST_BUILD_LOG.md`, `docs/POST_AUDIT_RESEARCH_2026-09-19.md`, `references/README.md`, canonical production source/project files, and current public history. Historical pre-public evidence remains available in `NikichMods/SpecializedStorage-legacy-private`.

When chat memory conflicts with accepted repository evidence, investigate the conflict before changing code.

## Shared Graveyard Keeper research

Cross-project Graveyard Keeper 1.407 host/runtime research is centralized in `NikichMods/GraveyardKeeperResearch`.

Before starting a fresh investigation into vanilla/game-engine/UI/NGUI/data/lifecycle behavior:

1. read this repository's own canonical verified-data / architecture docs first;
2. consult `NikichMods/GraveyardKeeperResearch/docs/RESEARCH_INDEX.md` and the linked shared knowledge documents;
3. search accepted local/shared test evidence and relevant history if the result has not yet been promoted;
4. perform new static/runtime research or a probe only if the question remains open.

Project-specific mechanics, product/UX decisions, release state, and build acceptance remain canonical in this repository. Reusable host/runtime facts that can serve multiple Graveyard Keeper mods should be promoted back into the shared research repository after acceptance rather than left only in chat, commit history, or a test log.

