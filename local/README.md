# AudioManager Architecture Source of Truth

## Summary
В проекте реализована централизованная подсистема аудио на Unity Audio stack.

## Implemented Structure
- `Assets/Audio/Data/AudioConfig.cs`
- `Assets/Audio/Data/SoundEvent.cs`
- `Assets/Audio/Runtime/AudioBus.cs`
- `Assets/Audio/Runtime/AudioHandle.cs`
- `Assets/Audio/Runtime/AudioSourcePool.cs`
- `Assets/Audio/Runtime/AudioManager.cs`
- `Assets/Audio/Runtime/Components/UIButtonSound.cs`
- `Assets/Audio/Runtime/Components/AudioSceneEmitter.cs`
- `Assets/Audio/Editor/AudioValidator.cs`
- `Assets/Audio/Editor/AudioDebuggerWindow.cs`

## Architecture Decisions
- `AudioManager` выступает facade для gameplay/UI.
- `SoundEvent` и `AudioConfig` обеспечивают data-driven управление.
- Воспроизведение SFX/UI идет через 2D/3D pools с release и steal policy.
- Music использует отдельный A/B канал для fade/crossfade.
- Snapshot conflicts решаются приоритетом `AudioConfig.SnapshotBinding.Priority`.

## Runtime Guarantees
- Fail-safe вызовы API при `null`/missing config/event.
- Анти-спам: `Cooldown` + `MaxInstances` на `SoundEvent`.
- Громкость API `0..1` переводится в dB для exposed mixer params.
- `AudioHandle` поддерживает stop/volume/pitch/follow операции.

## Team Workflow
1. Перед задачей читать `.memory_bank/productContext.md`, `.memory_bank/activeContext.md`, `.memory_bank/progress.md`.
2. Сначала формируется план (REQUIREMENTS_OWNER/ARCHITECT).
3. После реализации обязательна синхронизация docs и Memory Bank.
