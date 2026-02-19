# AudioManager Implementation Plan

## Ownership (Multi-Agent Simulation)
- REQUIREMENTS_OWNER: формализует acceptance criteria и список задач.
- ARCHITECT: утверждает структуру `Audio/Data`, `Audio/Runtime`, `Audio/Editor`.
- IMPLEMENTER: реализует runtime/editor код и API.
- REVIEWER: проверяет соответствие ТЗ, anti-spam, pooling, snapshot policy.
- QA_TESTER: выполняет сценарные проверки из acceptance checklist.
- DOCS_WRITER: обновляет `local/README.md` и Memory Bank.

## Task Breakdown
1. Data Layer
- `SoundEvent` (SO) с clip selection, anti-spam полями, spatial и routing.
- `AudioConfig` (SO) с mixer/group/snapshot bindings и pool settings.

2. Runtime Core
- `AudioManager` facade API.
- `AudioSourcePool` с 2D/3D пулами, stealing policy, auto release.
- `AudioHandle` для runtime-контроля инстансов.

3. Integration
- `UIButtonSound` для UI-потока.
- `AudioSceneEmitter` для сценовых emitters.

4. Tooling
- `AudioValidator` (Editor menu validation).
- `AudioDebuggerWindow` (runtime monitor).

## Current Status
- Data Layer: Done.
- Runtime Core: Done.
- Integration: Done.
- Tooling: Done (including production asset bootstrap).
- QA acceptance run in Unity profiler/editor: Partially done (headless validation complete, manual profiler checks pending).

## Open Questions for Product/Tech Lead
1. Addressables для клипов: подтверждаем `No` на текущем этапе?
2. WebGL target: нужен ли официальный support в этом релизе?
3. Snapshot priority matrix: подтвердить финальную матрицу приоритетов для runtime (сейчас same-frame by priority, cross-frame last request wins).
4. Persistence backend: `PlayerPrefs` достаточно или нужен project storage abstraction?
