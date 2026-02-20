# AudioManager Architecture Source of Truth

## Summary
Репозиторий разделен на библиотеку (`UPM package`) и приложение-потребитель (`Unity demo app`).

- Library: `/upm/com.gladfox.audiomanager`
- Demo App: `/AudioManager`

## Implemented Structure
### UPM Package
- `upm/com.gladfox.audiomanager/package.json`
- `upm/com.gladfox.audiomanager/Runtime/*`
- `upm/com.gladfox.audiomanager/Editor/*`
- `upm/com.gladfox.audiomanager/Samples~/AudioDemo/*`
- `upm/com.gladfox.audiomanager/README.md`
- `upm/com.gladfox.audiomanager/CHANGELOG.md`

### Demo App
- `AudioManager/Assets/Scenes/AudioDemoScene.unity`
- `AudioManager/Assets/Audio/Runtime/Components/AudioDemoSceneBootstrap.cs`
- `AudioManager/Assets/Audio/Data/*` (mixer, clips, demo events)
- `AudioManager/Assets/Resources/Audio/AudioConfig.asset`
- `AudioManager/Packages/manifest.json` (local `file:` package dependency)

## Architecture Decisions
- Библиотечный код вынесен в UPM пакет; demo app содержит только пример использования и контент.
- Публичный API и namespace сохранены (`AudioManagement`).
- `SoundEvent` использует только Addressables references.
- Dynamic loading реализован через `AudioContentService` + scope/ref-count + delayed unload.
- On-demand policy для `0.1.x`: `SkipIfNotLoaded`.
- Release channel для `0.1.x`: `git tags only` (`upm/vX.Y.Z`).

## Runtime Guarantees
- Fail-safe поведение при отсутствующем config/event/clip без исключений.
- 2D/3D pooling без `Instantiate/Destroy` в steady-state.
- Music restore после `Sound OFF -> ON` с ретраем до готовности контента.
- Snapshot policy:
  - в одном кадре выигрывает больший приоритет;
  - между кадрами выигрывает последний запрос.

## Demo Strategy
Поддерживаются два варианта демонстрации:
1. Standalone app demo (`AudioManager`).
2. Package sample (`Samples~/AudioDemo`).

## Setup & Validation
- Setup ассетов: `Tools/Audio/Setup/Generate Production Assets`.
- Validation: `Tools/Audio/Validate Sound Events`.
- Runtime diagnostics: `Tools/Audio/Debugger`.
