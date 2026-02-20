# Active Context

## Текущее направление
Формирование релиза `0.0.1`: фиксация версии продукта, публикация release notes и продуктовой документации.

## Активные задачи
- QA_TESTER: ручной play-mode/profiler прогон acceptance matrix в основном Unity-проекте.
- DOCS_WRITER: обновить релизные документы после manual acceptance.

## Последние изменения
- Обновлена версия продукта (`bundleVersion`) до `0.0.1`.
- Добавлены релизные документы:
  - `README.md` (продуктовое описание и quick start)
  - `RELEASE_NOTES.md` (изменения версии `0.0.1`)
  - `EFFORT_REPORT_0.0.1.md` (оценка трудозатрат и токенов)
- `AudioManager`:
  - auto-load `AudioConfig` из `Resources/Audio/AudioConfig`;
  - добавлены overloads `PlayMusic(string, fadeIn, crossfade, restartIfSame)` и `PlaySFX(string, Transform)`;
  - добавлены `StopByEventId` / `StopByEvent`;
  - переработана pause-state логика (`user/focus/app` merge);
  - snapshot policy: same-frame priority, cross-frame last request wins.
- `AudioDemoSceneBootstrap` переведен на новую Input System (`Keyboard.current`), использует вызовы по id.
- Добавлен `AudioProductionSetup` (editor automation генерации mixer/config/events/clips/build settings).
- Усилен `AudioValidator` (проверки mixer/snapshots/catalog).
- Сгенерированы production ассеты:
  - `AudioMain.mixer`
  - `Assets/Resources/Audio/AudioConfig.asset`
  - demo `SoundEvent` assets
  - demo WAV clips
- Выполнена headless валидация в временной копии проекта:
  - production setup completed;
  - validator passed with no issues.

## Следующие шаги
- Прогнать manual acceptance в основном проекте (Play Mode + Profiler).
- При необходимости подготовить patch release `0.0.2` по итогам ручного тестирования.
