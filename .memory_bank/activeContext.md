# Active Context

## Текущее направление
Реализация production-ready AudioManager по ТЗ: runtime-ядро, data-модель, пуллинг, snapshots, editor tooling и интеграция в Unity-проект.

## Активные задачи
- REQUIREMENTS_OWNER: формализация задач, критериев приемки и ограничений.
- ARCHITECT: проектирование структуры `Audio/Runtime`, `Audio/Data`, `Audio/Editor`.
- IMPLEMENTER: реализация API, пулов, handles, music crossfade, snapshot control.
- REVIEWER: проверка соответствия ТЗ, fail-safe и git-дисциплины изменений.
- QA_TESTER: сценарная проверка against acceptance criteria.
- DOCS_WRITER: синхронизация `local/README.md` и Memory Bank.

## Последние изменения
- Инициализирована инфраструктура `AGENTS.md` и `.memory_bank`.
- Получено комплексное ТЗ на AudioManager (UI/SFX/Music/Ambience + Mixer + Pooling + Snapshots).

## Следующие шаги
- Реализовать runtime-компоненты и ScriptableObject модели.
- Добавить editor-валидатор и debugger window.
- Обновить документацию API/архитектуры и зафиксировать открытые вопросы.
