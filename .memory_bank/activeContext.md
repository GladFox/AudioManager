# Active Context

## Текущее направление
Подготовка реализации `0.0.2`: внедрение динамической загрузки аудио через Addressables с контролируемой выгрузкой.

## Активные задачи
- REQUIREMENTS_OWNER: утвердить финальный spec `audio-addressables-dynamic-loading-spec.md`.
- ARCHITECT: утвердить runtime-модель `AudioContentService + scope/ref-count`.
- IMPLEMENTER: приступить к TASK-ADDR-001..010 после утверждения политики `QueueAndPlay/Skip`.
- REQUIREMENTS_OWNER: утвердить demo spec `audio-addressables-dialog-demo-spec.md`.

## Последние изменения
- Сформировано полное ТЗ на Addressables dynamic loading:
  - `.memory_bank/specs/audio-addressables-dynamic-loading-spec.md`
- Сформирован детальный роль-ориентированный план:
  - `.memory_bank/specs/audio-addressables-role-plan.md`
- Сформировано дополнительное ТЗ на demo-сценарий диалога:
  - `.memory_bank/specs/audio-addressables-dialog-demo-spec.md`
- Сформирован роль-ориентированный план demo:
  - `.memory_bank/specs/audio-addressables-dialog-demo-role-plan.md`
- Утверждены параметры реализации:
  - `OnDemandPlayPolicy = SkipIfNotLoaded`
  - `UnloadDelaySeconds = 15`
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
- После утверждения перейти к реализации TASK-ADDR-001.
- После базовой реализации Addressables перейти к TASK-DEMO-001..006.
