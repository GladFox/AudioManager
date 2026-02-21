# Active Context

## Текущее направление
Подготовка релиза `0.1.2`: автосбор динамически появляющихся `SoundEvent` и preload без ручных списков для диалогов.

## Активные задачи
- REQUIREMENTS_OWNER: утвердить spec `0.1.2` на discovery preload.
- ARCHITECT: согласовать lifecycle `SoundEvent` registry + marker-based preload.
- IMPLEMENTER: реализовать discovery API в `AudioManager` и runtime реестр.

## Последние изменения
- Выявлен функциональный гэп: для динамических диалогов нужен ручной список ids/events для preload.
- Подготовлен release-spec `0.1.2` для закрытия гэпа:
  - `audio-0.1.2-dynamic-soundevent-discovery-spec.md`
  - `audio-0.1.2-dynamic-soundevent-discovery-role-plan.md`
- В `CHANGELOG` добавлен planned section для `0.1.2`.

## Следующие шаги
1. Реализовать discovery registry и API `PreloadDiscovered*`.
2. Обновить demo-сценарий под marker-based preload.
3. Выполнить QA matrix и выпустить `0.1.2`.
