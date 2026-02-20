# Audio Addressables Role Plan

## План по ролям (обязательная последовательность)

## 1) REQUIREMENTS_OWNER
### Задачи
- Зафиксировать scope релиза `0.0.2`.
- Утвердить 4 ключевых решения:
  - `OnDemandPlayPolicy`: `SkipIfNotLoaded` (утверждено)
  - `UnloadDelaySeconds`: `15` (утверждено)
  - формат `AudioBank`
  - стратегия preload при `Sound ON`
- Утвердить acceptance checklist.

### Выход
- Утвержденный spec: `audio-addressables-dynamic-loading-spec.md`

## 2) ARCHITECT
### Задачи
- Спроектировать изменения без слома текущего API.
- Утвердить новый runtime-сервис `AudioContentService`.
- Утвердить модель scope/ref-count/unload.
- Обновить `systemPatterns.md` при изменениях принципов.

### Выход
- Архитектурная схема runtime взаимодействий:
  - `AudioManager <-> AudioContentService <-> Addressables`

## 3) IMPLEMENTER
### Этапы
1. Data layer
- обновить `SoundEvent` под `AssetReferenceT<AudioClip>[]`
- добавить `AudioBank`
- расширить `AudioConfig`

2. Runtime content service
- реализовать `AudioContentService`
- реализовать registry состояний
- добавить group preload и progress handle

3. AudioManager integration
- добавить preload/load-by-id API
- интегрировать `OnDemandPlayPolicy`
- интегрировать scope acquire/release

4. Unload policy
- безопасный unload only-if-not-playing
- delay unload

5. Editor tooling
- validator checks
- debugger counters/load state/memory

### Выход
- Компилируемый runtime/editor код

## 4) REVIEWER
### Проверки
- Нет ли прямых сериализованных `AudioClip[]` в `SoundEvent`.
- Нет ли путей double-release handle.
- Нет ли unload активных клипов.
- Сохранена ли обратная совместимость `Play*` API.
- Обновлены ли docs и Memory Bank.

### Выход
- Список findings и фикс-лист

## 5) QA_TESTER
### Тесты
- preload bank и progress
- preload by ids
- toggle sound off/on
- play while loading (`QueueAndPlay`/`Skip`)
- unload after release scope
- stress: repeated load/unload cycles
- memory regression checks

### Выход
- Протокол тестирования и known issues

## 6) DOCS_WRITER
### Задачи
- Обновить `local/README.md`
- Обновить `.memory_bank/activeContext.md`
- Обновить `.memory_bank/progress.md`
- Добавить release notes для `0.0.2`

### Выход
- Полная синхронизация документации

---

## Бэклог задач (implementation-ready)
1. TASK-ADDR-001: `SoundEvent` migration to `AssetReferenceT<AudioClip>[]`
2. TASK-ADDR-002: `AudioBank` SO + config integration
3. TASK-ADDR-003: `AudioContentService` state registry
4. TASK-ADDR-004: Group preload handles + progress
5. TASK-ADDR-005: `AudioManager` preload/unload/scope API
6. TASK-ADDR-006: Play policy implementation (`QueueAndPlay/Skip`)
7. TASK-ADDR-007: Safe unload (not-playing guard + delay)
8. TASK-ADDR-008: Validator updates for Addressables
9. TASK-ADDR-009: Debugger counters and memory telemetry
10. TASK-ADDR-010: QA matrix + docs sync

## Предварительная оценка
- Реализация: 10-14 часов
- Тестирование и стабилизация: 4-6 часов
- Документация и финализация: 2-3 часа
- Итого: 16-23 человеко-часа
