# Audio Addressables Dialog Demo Role Plan

## 1) REQUIREMENTS_OWNER
### Задачи
- Подтвердить формат demo-диалога (intro + line ids).
- Подтвердить UX overlay (текст/проценты/блокировка).
- Подтвердить использование `SkipIfNotLoaded`.

### Выход
- Утвержденный spec: `audio-addressables-dialog-demo-spec.md`.

## 2) ARCHITECT
### Задачи
- Выбрать: новая сцена или расширение текущей demo-сцены.
- Утвердить точки интеграции с `AudioManager.PreloadByIds`.
- Определить минимальный набор компонентов demo UI.

### Выход
- Схема потока:
  - `Scene Start -> Preload -> Overlay Progress -> Unlock -> Play Intro`.

## 3) IMPLEMENTER
### Этапы
1. Создать/обновить demo-сцену под диалоговый сценарий.
2. Реализовать `AudioDialogDemoController`:
- сбор id звуков диалога,
- вызов preload,
- запуск intro по completion.
3. Реализовать `AudioLoadingOverlayView`:
- show/hide,
- отображение процентов.
4. Подключить кнопки реплик и sound toggle.
5. Реализовать дозагрузку missing ids при `Sound On`.

### Выход
- Рабочая интерактивная demo-сцена.

## 4) REVIEWER
### Проверки
- Нет ли прямого доступа к клипам в demo-коде (только ids/events).
- Нет ли обхода preload flow.
- Корректно ли блокируется UI во время загрузки.
- Нет ли гонок при repeated toggle.

### Выход
- findings/fixes list.

## 5) QA_TESTER
### Тесты
- Старт сцены -> preload -> прогресс -> unlock.
- Проверка всех кнопок воспроизведения.
- Toggle off/on и повторный preload.
- Быстрые повторные переключения.
- Проверка отсутствия исключений.

### Выход
- тестовый протокол demo acceptance.

## 6) DOCS_WRITER
### Задачи
- Зафиксировать demo flow в `local/README.md` (раздел использования).
- Обновить `.memory_bank/activeContext.md` и `.memory_bank/progress.md`.

### Выход
- Синхронизированные docs по demo-сценарию.

---

## Бэклог задач (implementation-ready)
1. TASK-DEMO-001: Define demo dialogue ids and bootstrap sequence.
2. TASK-DEMO-002: Implement preload overlay with percent progress.
3. TASK-DEMO-003: Implement intro playback on preload completion.
4. TASK-DEMO-004: Wire UI buttons to play dialogue sound ids.
5. TASK-DEMO-005: Implement sound toggle + reload missing ids on enable.
6. TASK-DEMO-006: QA checklist and regression notes.
