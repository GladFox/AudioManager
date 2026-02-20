# AudioManager UPM Modularization Role Plan

## 1) REQUIREMENTS_OWNER
### Задачи
- Зафиксировать уже принятые решения:
  - `com.gladfox.audiomanager`
  - `0.1.0`
  - размещение package source в `/upm`
  - demo и в `Samples~`, и в `AudioManager/`
- Зафиксировать релизный канал для `0.1.x`: `git tags only`.
- Зафиксировать acceptance criteria и этапы миграции.

### Выход
- Утвержденный spec: `audio-upm-modularization-spec.md`.

## 2) ARCHITECT
### Задачи
- Спроектировать финальную структуру репозитория.
- Определить границы package/runtime/editor/sample.
- Утвердить правила зависимостей (`Addressables`, `InputSystem`).

### Выход
- Архитектурная схема:
  - `UPM package` как библиотека,
  - `Unity app` как consumer.

## 3) IMPLEMENTER
### Этапы
1. Создать package scaffold:
- `package.json`, `README`, `CHANGELOG`, `LICENSE`,
- `Runtime/`, `Editor/`, `Samples~/`.

2. Перенести код:
- runtime/editor скрипты из app в package;
- собрать asmdef, исправить references.

3. Подготовить samples:
- demo scene/scripts/assets в `Samples~/AudioDemo`;
- инструкция импорта sample.

4. Переключить demo app на пакет:
- подключить `file:` dependency;
- удалить дубли кода из `Assets`.

5. Стабилизация:
- fix broken GUID references;
- smoke test preload/play/unload/snapshots.

### Выход
- Рабочий UPM пакет + рабочее demo приложение.

## 4) REVIEWER
### Проверки
- package не содержит project-specific мусор.
- runtime assembly чиста от UnityEditor ссылок.
- sample независим и импортируется без ручных правок.
- demo app действительно использует пакет.
- docs/changelog обновлены.

### Выход
- Findings list и список обязательных фиксов.

## 5) QA_TESTER
### Тесты
- install package через `file:`.
- install package через `git`.
- compile runtime/editor/tests.
- import sample + запуск сцены.
- regression по Addressables loading flow.

### Выход
- QA протокол с результатами и known issues.

## 6) DOCS_WRITER
### Задачи
- Обновить package README.
- Обновить `local/README.md` (архитектурный source of truth).
- Обновить `.memory_bank/activeContext.md` и `.memory_bank/progress.md`.
- Подготовить релиз-ноты package версии.

### Выход
- Синхронизированная документация по UPM-модели.

---

## Бэклог задач (implementation-ready)
1. TASK-UPM-001: Зафиксировать в package docs release channel `git tags only` для `0.1.x`.
2. TASK-UPM-002: Создать UPM scaffold и asmdef.
3. TASK-UPM-003: Перенести runtime/editor код в package.
4. TASK-UPM-004: Подготовить `Samples~/AudioDemo`.
5. TASK-UPM-005: Переключить demo app на `file:` package dependency.
6. TASK-UPM-006: Удалить дубли кода библиотеки из app.
7. TASK-UPM-007: Провести compile/install smoke tests (`file` + `git`).
8. TASK-UPM-008: Обновить docs/changelog/memory bank.
9. TASK-UPM-009: Подготовить PR и release tag plan.

## Предварительная оценка
- Реализация миграции: 10-16 часов
- Стабилизация и QA: 5-8 часов
- Документация и релизная подготовка: 2-4 часа
- Итого: 17-28 человеко-часов
