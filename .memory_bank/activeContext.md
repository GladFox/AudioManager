# Active Context

## Текущее направление
Релиз `0.1.2` реализован: добавлен discovery preload для динамических `SoundEvent` без ручных preload-list.

## Активные задачи
- QA_TESTER: PlayMode smoke и регрессия по dynamic dialog flow.
- DOCS_WRITER: финальная синхронизация README/AI guide/release docs.
- STEWARD: подготовка tag `upm/v0.1.2`.

## Последние изменения
- Добавлен runtime `SoundEventDiscoveryRegistry`.
- `SoundEvent` теперь авто-регистрируется в discovery registry (`OnEnable/OnDisable`).
- В `AudioManager` добавлены API:
  - `CaptureDiscoveryMarker()`
  - `PreloadDiscovered(...)`
  - `PreloadDiscoveredSince(marker, ...)`
- Demo bootstrap переведен с ручного `PreloadByIds` на `PreloadDiscovered(..., scopeId: "demo.dialogue")`.
- Добавлен editor hook для очистки discovery-реестра при входе в Play Mode.
- Обновлены release/docs (`CHANGELOG`, `RELEASE_NOTES`, `README`, `ai.md`).

## Следующие шаги
1. Прогнать manual QA matrix для dynamic dialog и unload-cycle.
2. Создать git tag `upm/v0.1.2`.
3. Подготовить scope следующего релиза (`0.1.3`).
