# Active Context

## Текущее направление
Закрепление UPM-модели: полный demo-контент переносится в package sample, consumer app остается контейнером проекта и зависимостей.

## Активные задачи
- IMPLEMENTER: перенести `AudioManager/Assets/AudioManager` в `upm/com.gladfox.audiomanager/Samples~/AudioManager` целиком.
- REVIEWER: проверить отсутствие устаревшего sample `Samples~/AudioDemo` и битых путей в docs/package metadata.
- QA_TESTER: smoke-проверка импорта sample из Package Manager и запуска `AudioDemoScene`.

## Последние изменения
- Ветка `main` получила commit `672b208` с рефакторингом структуры примера.
- Актуализируется модель demo: canonical source демо-ассетов хранится в package sample.
- Удаляется дублирование между app `Assets/AudioManager` и `Samples~`.

## Следующие шаги
1. Обновить `package.json` sample path и sample README.
2. Синхронизировать `README.md`, `local/README.md`, `.memory_bank/progress.md`.
3. Выполнить commit/push в отдельной ветке и открыть PR в `main`.
