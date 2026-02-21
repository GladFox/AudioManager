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
- `upm/com.gladfox.audiomanager/Samples~/AudioManager/*`
- `upm/com.gladfox.audiomanager/README.md`
- `upm/com.gladfox.audiomanager/CHANGELOG.md`

### Demo App
- `AudioManager/Assets/AddressableAssetsData/*`
- `AudioManager/Assets/InputSystem_Actions.inputactions`
- `AudioManager/Packages/manifest.json` (local `file:` package dependency)

## Architecture Decisions
- Библиотечный код вынесен в UPM пакет; demo контент поставляется через `Samples~`.
- Публичный API и namespace сохранены (`AudioManagement`).
- `SoundEvent` использует только Addressables references.
- Dynamic loading реализован через `AudioContentService` + scope/ref-count + delayed unload.
- Dynamic discovery реализован через `SoundEventDiscoveryRegistry` + `PreloadDiscovered*` API.
- On-demand policy для `0.1.x`: `SkipIfNotLoaded`.
- Release channel для `0.1.x`: `git tags only` (`upm/vX.Y.Z`).

## Runtime Guarantees
- Fail-safe поведение при отсутствующем config/event/clip без исключений.
- 2D/3D pooling без `Instantiate/Destroy` в steady-state.
- Music restore после `Sound OFF -> ON` с ретраем до готовности контента.
- Динамически появившиеся `SoundEvent` могут быть дозагружены без ручного списка ids/events.
- Snapshot policy:
  - в одном кадре выигрывает больший приоритет;
  - между кадрами выигрывает последний запрос.

## Demo Strategy
Поддерживаются два варианта демонстрации:
1. Consumer app (`AudioManager`) как контейнер проекта/пакетов.
2. Package sample (`Samples~/AudioManager`) как полный demo-контент.

Для git-source-of-truth demo ассетов используется только package sample.
Локальные импортированные копии sample в `AudioManager/Assets/Samples/Audio Manager` считаются тестовыми и не трекаются в git.

## Setup & Validation
- Setup ассетов: `Tools/Audio/Setup/Generate Production Assets`.
- Validation: `Tools/Audio/Validate Sound Events`.
- Runtime diagnostics: `Tools/Audio/Debugger`.
