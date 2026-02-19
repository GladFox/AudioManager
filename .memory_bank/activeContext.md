# Active Context

## Текущее направление
Финализация AudioManager до production baseline: закрытие разрывов по ТЗ, стабилизация API, генерация production ассетов и фиксация соответствия.

## Активные задачи
- QA_TESTER: ручной play-mode/profiler прогон acceptance matrix в основном Unity-проекте.
- DOCS_WRITER: зафиксировать метрики после ручного прогона.

## Последние изменения
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
- Зафиксировать метрики flood/perf и аудио-поведение (crossfade/snapshots/pause/persistence).
- После подтверждения метрик перевести статус задачи в fully verified production.
