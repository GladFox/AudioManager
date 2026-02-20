# AudioManager Architecture Source of Truth

## Summary
Проект содержит production-ready аудиоподсистему Unity с централизованным `AudioManager`, `AudioMixer + Snapshots`, 2D/3D pooling и динамической загрузкой клипов через Addressables.

## Implemented Structure
- `Assets/Audio/Data/AudioConfig.cs`
- `Assets/Audio/Data/SoundEvent.cs`
- `Assets/Audio/Data/AudioBank.cs`
- `Assets/Audio/Runtime/AudioBus.cs`
- `Assets/Audio/Runtime/AudioHandle.cs`
- `Assets/Audio/Runtime/AudioLoadHandle.cs`
- `Assets/Audio/Runtime/AudioContentService.cs`
- `Assets/Audio/Runtime/AudioSourcePool.cs`
- `Assets/Audio/Runtime/AudioManager.cs`
- `Assets/Audio/Runtime/Components/UIButtonSound.cs`
- `Assets/Audio/Runtime/Components/AudioSceneEmitter.cs`
- `Assets/Audio/Runtime/Components/AudioDemoSceneBootstrap.cs`
- `Assets/Audio/Editor/AudioProductionSetup.cs`
- `Assets/Audio/Editor/AudioValidator.cs`
- `Assets/Audio/Editor/AudioDebuggerWindow.cs`
- `Assets/Resources/Audio/AudioConfig.asset`
- `Assets/Audio/Data/Banks/*.asset`
- `Assets/Scenes/AudioDemoScene.unity`

## Architecture Decisions
- `AudioManager` — единый facade API (`PlayUI/PlaySFX/PlayMusic`, preload/scope, stop/pause/snapshot/volume).
- `SoundEvent` хранит только addressable-ссылки на клипы (`AssetReferenceT<AudioClip>[]` / weighted references), без сериализованных `AudioClip[]`.
- `AudioContentService` управляет lifecycle адресуемых клипов: `Loading/Loaded/Failed/Unloaded`, preload, unload, progress, guard от double-load/release.
- Для памяти используется модель scope/ref-count + delayed unload (`UnloadDelaySeconds`).
- Политика on-demand playback: `SkipIfNotLoaded` (если клип не загружен, play пропускается без исключений).
- Snapshot policy:
  - в рамках одного кадра выигрывает больший `Priority`;
  - между кадрами выигрывает последний запрос (last request wins).

## Runtime Guarantees
- Fail-safe: при отсутствии config/event/clip — warning log (при `EnableDebugLogs`) и graceful skip.
- Play-path без `Instantiate/Destroy` в steady-state: звук идет через пулы или music A/B channels.
- Анти-спам `SoundEvent`: `CooldownSeconds` + `MaxInstances`.
- `AudioHandle` поддерживает `Stop/SetVolume/SetPitch/SetFollowTarget/IsValid`.
- Громкости API `0..1` конвертируются в dB (`AudioConfig.MinDb..MaxDb`) и сохраняются через `PlayerPrefs`.
- `SetSoundEnabled(false/true)` управляет остановкой/возобновлением и автопрелоадом банков для текущих настроек.

## Production Setup
- One-click генерация production ассетов:
  - `Tools/Audio/Setup/Generate Production Assets`
  - batch method: `AudioManagementEditor.AudioProductionSetup.GenerateProductionAssetsBatch`
- Генерируется/обновляется:
  - `AudioMain.mixer` (groups/snapshots/exposed params);
  - demo WAV clips;
  - demo `SoundEvent` assets с `AssetReferenceT<AudioClip>`;
  - demo `AudioBank`;
  - `Assets/Resources/Audio/AudioConfig.asset`;
  - Addressables entry для demo clip assets;
  - build settings с `AudioDemoScene`.

## Demo Scene
- `Assets/Scenes/AudioDemoScene.unity` содержит сценарий preload диалоговых звуков с блокирующим overlay и progress.
- `AudioDemoSceneBootstrap` использует новую Input System (`UnityEngine.InputSystem.Keyboard`).
- Горячие клавиши:
  - `1` line 1 (UI)
  - `2` line 2 (3D follow SFX)
  - `3` line 3 (UI)
  - `4` music toggle
  - `5` pause/resume
  - `6` menu/gameplay snapshot toggle
  - `7` sound on/off (с повторной дозагрузкой)
