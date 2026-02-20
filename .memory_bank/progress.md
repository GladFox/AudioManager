# Progress

## Что работает
- Централизованный API `PlayUI/PlaySFX/PlayMusic` (event/id/clip варианты).
- 2D/3D source pooling с лимитами, expansion и `StealPolicy`.
- `SoundEvent` с clip-selection (random/sequence/weighted), anti-spam, spatial параметрами.
- Mixer volume control (`0..1` -> dB), mute и snapshot transitions с приоритетом.
- Music A/B каналы с fade/crossfade и поддержкой stop/pause.
- `AudioHandle` для runtime управления playback.
- `UIButtonSound`, `AudioSceneEmitter`, `AudioValidator`, `AudioDebuggerWindow`.
- `AudioProductionSetup` для автоматической генерации production audio assets.
- Тестовая сцена `AudioDemoScene` с рабочим runtime-демо `PlayUI/PlaySFX/PlayMusic/PauseAll/Snapshots`.
- Перевод demo hotkeys на новую Input System.
- Версионирование продукта: `bundleVersion = 0.0.1`.
- Подготовлены релизные документы:
  - `README.md`
  - `RELEASE_NOTES.md`
  - `EFFORT_REPORT_0.0.1.md`
- Динамическая загрузка через Addressables:
  - `AudioContentService` (registry state + preload + unload),
  - `AudioLoadHandle` (progress/status/error),
  - `AudioBank` и preload по bank/ids/events.
- `SoundEvent` полностью переведен на `AssetReferenceT<AudioClip>[]` / weighted references.
- `AudioManager` поддерживает:
  - `PreloadBank`, `PreloadByIds`, `PreloadByEvents`,
  - `AcquireScope`, `ReleaseScope`, `UnloadUnused`,
  - `SetSoundEnabled` с автопрелоадом банков.
- Демо-сцена реализует preload overlay с процентом и дозагрузку диалоговых звуков при `Sound ON`.
- Исправлен дефект ref-count при повторяющихся GUID в одном scope.

## В работе
- Финальный ручной прогон acceptance в Unity Editor (playmode/profiler/addressables).
- Подготовка PR и ревью для merge в `main`.

## Известные проблемы
- Основной проект сейчас открыт в Unity, поэтому batchmode-проверки по этому же `projectPath` блокируются lock-файлом.
- Финальные метрики flood/perf и аудио-артефакты (click/pop) требуют ручного прогона в редакторе.
- `QueueAndPlay` объявлен в enum, но целевой режим релиза — `SkipIfNotLoaded`.

## Эволюция решений
- От базовой реализации к production-базису:
  - стабилизирован API (`StopByEventId`, id-overloads, pause merge policy);
  - добавлен автоматический bootstrap ассетов и mixer;
  - выполнен formal compliance review против базового ТЗ.
- Для релиза `0.0.2` реализована стратегия:
  - `SoundEvent` без сериализованных `AudioClip[]`;
  - source контента только `AssetReferenceT<AudioClip>[]`;
  - lifecycle через scope/ref-count + delayed unload (`15s`);
  - on-demand поведение `SkipIfNotLoaded`.

## Контроль изменений
last_checked_commit: af7e6e2cb4231201de7dc23194d22c9455a6bb14
last_checked_date: 2026-02-21 01:13:37 +0700
