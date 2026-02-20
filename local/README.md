# AudioManager Architecture Source of Truth

## Summary
Проект содержит production-ready фундамент аудиосистемы Unity: централизованный `AudioManager`, data-driven `SoundEvent/AudioConfig`, `AudioMixer + Snapshots`, 2D/3D pooling, demo scene и editor tooling.

## Implemented Structure
- `Assets/Audio/Data/AudioConfig.cs`
- `Assets/Audio/Data/SoundEvent.cs`
- `Assets/Audio/Data/AudioMain.mixer`
- `Assets/Audio/Data/SoundEvents/*.asset`
- `Assets/Resources/Audio/AudioConfig.asset`
- `Assets/Audio/Runtime/AudioBus.cs`
- `Assets/Audio/Runtime/AudioHandle.cs`
- `Assets/Audio/Runtime/AudioSourcePool.cs`
- `Assets/Audio/Runtime/AudioManager.cs`
- `Assets/Audio/Runtime/Components/UIButtonSound.cs`
- `Assets/Audio/Runtime/Components/AudioSceneEmitter.cs`
- `Assets/Audio/Runtime/Components/AudioDemoSceneBootstrap.cs`
- `Assets/Audio/Editor/AudioValidator.cs`
- `Assets/Audio/Editor/AudioDebuggerWindow.cs`
- `Assets/Audio/Editor/AudioProductionSetup.cs`
- `Assets/Scenes/AudioDemoScene.unity`

## Architecture Decisions
- `AudioManager` — единый facade API для gameplay/UI (`PlayUI/PlaySFX/PlayMusic`, stop/pause/snapshot/volume).
- `SoundEvent` и `AudioConfig` — единственный data layer для маршрутизации, анти-спама, spatial и mixer bindings.
- SFX/UI воспроизводятся через 2D/3D `AudioSourcePool` без create/destroy в steady-state.
- Music использует выделенные A/B каналы для fade/crossfade.
- Snapshot policy:
  - в рамках одного кадра выигрывает больший `Priority`;
  - между кадрами выигрывает последний запрос (last request wins).
- `AudioManager` сначала пытается загрузить `AudioConfig` из `Resources/Audio/AudioConfig`, иначе применяет runtime defaults.

## Runtime Guarantees
- Fail-safe поведение при отсутствующих config/event/clip: warning log (при `EnableDebugLogs`) без падения.
- Анти-спам на `SoundEvent`: `CooldownSeconds` + `MaxInstances`.
- Громкости API `0..1` конвертируются в dB (диапазон из `AudioConfig.MinDb..MaxDb`).
- `AudioHandle` поддерживает `Stop/SetVolume/SetPitch/SetFollowTarget`.
- Поддержана пауза с сохранением работы UI bus и корректным merge состояний pause/focus/appPause.

## Production Setup
- One-click генерация production ассетов:
  - `Tools/Audio/Setup/Generate Production Assets`
  - batch method: `AudioManagementEditor.AudioProductionSetup.GenerateProductionAssetsBatch`
- Генерируется/обновляется:
  - `AudioMain.mixer` (группы, snapshots, exposed params);
  - demo WAV clips;
  - demo `SoundEvent` assets;
  - `Assets/Resources/Audio/AudioConfig.asset`;
  - build settings с `AudioDemoScene`.

## Demo Scene
- `Assets/Scenes/AudioDemoScene.unity` содержит рабочий сценарий проверки.
- `AudioDemoSceneBootstrap` использует новую Input System (`UnityEngine.InputSystem.Keyboard`).
- Горячие клавиши:
  - `1` UI click
  - `2` 3D SFX (position + follow)
  - `3` music toggle
  - `4` pause/resume
  - `5` menu/gameplay snapshot
  - `6` muffled snapshot
  - `7` default snapshot
