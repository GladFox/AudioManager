# AudioManager 0.1.2 - Dynamic Discovery Role Plan

## 1) REQUIREMENTS_OWNER
### Задачи
- Утвердить целевой контракт: preload без ручных списков для динамических диалогов.
- Зафиксировать API:
  - `CaptureDiscoveryMarker`
  - `PreloadDiscovered`
  - `PreloadDiscoveredSince`
- Утвердить acceptance matrix для dialog flow.

### Выход
- Утвержденный spec: `audio-0.1.2-dynamic-soundevent-discovery-spec.md`

## 2) ARCHITECT
### Задачи
- Спроектировать discovery registry без breaking changes текущих API.
- Утвердить жизненный цикл `SoundEvent` register/unregister.
- Утвердить scope-политику выгрузки для discovered preload.

### Выход
- Runtime схема: `SoundEvent -> DiscoveryRegistry -> AudioManager -> AudioContentService`

## 3) IMPLEMENTER
### Этапы
1. Реализовать `SoundEventDiscoveryRegistry`.
2. Подключить register/unregister в `SoundEvent` lifecycle.
3. Добавить новые методы в `AudioManager`.
4. Интегрировать фильтр `CanLoadEventForCurrentSettings`.
5. Добавить debug counters.
6. Обновить demo bootstrap под marker-based preload.

### Выход
- Компилируемый runtime с discovery preload.

## 4) REVIEWER
### Проверки
- Нет ли дублей/утечек в реестре.
- Нет ли race при unload/load на тех же событиях.
- Не изменено ли поведение legacy preload API.
- Документация и memory bank синхронизированы.

### Выход
- findings list + фикс-лист.

## 5) QA_TESTER
### Тесты
- Dynamic dialog create -> preload discovered since marker.
- First-click play check (music/sfx/ui).
- Scope release -> delayed unload.
- Re-enter dialog 5-10 циклов без роста loaded clips.
- Sound OFF/ON with discovered events.

### Выход
- QA протокол + known issues.

## 6) DOCS_WRITER
### Задачи
- Обновить package README (новый preload сценарий).
- Обновить `RELEASE_NOTES.md` для `0.1.2`.
- Обновить `.memory_bank/activeContext.md` и `.memory_bank/progress.md`.
- Обновить `upm/.../CHANGELOG.md`.

### Выход
- Документация в состоянии release-ready.

## Бэклог задач (implementation-ready)
1. TASK-012-001: Discovery registry service.
2. TASK-012-002: SoundEvent lifecycle auto-register.
3. TASK-012-003: AudioManager discovery API.
4. TASK-012-004: Marker-based preload flow.
5. TASK-012-005: Debug counters/logging.
6. TASK-012-006: Demo scenario update.
7. TASK-012-007: QA matrix execution.
8. TASK-012-008: Docs/release sync.

## Оценка
- Реализация: 6-9 часов
- QA и стабилизация: 3-5 часов
- Документация и релизная упаковка: 1-2 часа
- Итого: 10-16 человеко-часов
