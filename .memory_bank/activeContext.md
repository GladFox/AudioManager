# Active Context

## Текущее направление
Реализация `0.0.2`: стабилизация внедренной динамической загрузки аудио через Addressables и подготовка PR.

## Активные задачи
- REVIEWER: финальная проверка runtime-path preload/play/unload и ref-count.
- QA_TESTER: ручной playmode/profiler прогон в Unity Editor (проект сейчас открыт в UI).
- DOCS_WRITER: синхронизация release-коммуникации перед merge.

## Последние изменения
- Добавлена Addressables-зависимость в `Packages/manifest.json`.
- Реализованы новые runtime-компоненты:
  - `AudioContentService` (load/preload/unload, scope/ref-count, in-use guard),
  - `AudioLoadHandle` (status/progress/error),
  - `AudioBank` (группировка событий).
- Миграция `SoundEvent` на `AssetReferenceT<AudioClip>[]` и weighted references без `AudioClip[]`.
- Расширен `AudioConfig`:
  - `banks`,
  - `enableAddressablesLogs`,
  - `onDemandPlayPolicy`,
  - `unloadDelaySeconds`.
- `AudioManager` интегрирован с dynamic loading API:
  - `PreloadBank/PreloadByEvents/PreloadByIds`,
  - `AcquireScope/ReleaseScope/UnloadUnused`,
  - `SetSoundEnabled` + автопрелоад банков,
  - debug counters по Addressables.
- Обновлены editor-инструменты:
  - `AudioProductionSetup` создает demo bank и addressable entries,
  - `AudioValidator` проверяет addressable clip refs,
  - `AudioDebuggerWindow` показывает addressables counters и память.
- `AudioDemoSceneBootstrap` реализует диалоговый preload flow:
  - блокирующий overlay с прогрессом,
  - интро после preload,
  - дозагрузка при `Sound ON`,
  - управление через новую Input System.
- Исправлен ref-count leak при `AcquireScope` для повторяющихся clip GUID.

## Следующие шаги
1. Прогнать ручной acceptance в Unity Editor (PlayMode + Profiler + Addressables groups/build).
2. Подготовить и опубликовать PR с checklist по `audio-addressables-dynamic-loading-spec.md`.
3. После ревью обновить `RELEASE_NOTES.md` под `0.0.2` и выполнить merge в `main`.
