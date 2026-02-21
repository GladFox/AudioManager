# AI Integration Guide (AudioManager)

## Цель
Этот файл задает единые правила для ИИ-агентов, которые:
- интегрируют `com.gladfox.audiomanager` в Unity-проекты;
- используют библиотеку в геймплейных/UI задачах;
- вносят изменения в текущем репозитории.

## С чего начинать
Перед любой задачей прочитать:
- `/Users/glad/AudioManager/local/README.md`
- `/Users/glad/AudioManager/upm/com.gladfox.audiomanager/README.md`
- `/Users/glad/AudioManager/AGENTS.md`
- `/Users/glad/AudioManager/.memory_bank/productContext.md`
- `/Users/glad/AudioManager/.memory_bank/activeContext.md`
- `/Users/glad/AudioManager/.memory_bank/progress.md`

## Как подключать пакет
Использовать один из вариантов:

1. Local dependency (в этом репо):
`"com.gladfox.audiomanager": "file:../../upm/com.gladfox.audiomanager"`

2. Git dependency (внешний проект):
`https://github.com/GladFox/AudioManager.git?path=/upm/com.gladfox.audiomanager#upm/v0.1.1`

## Минимальный integration checklist
1. Установить пакет.
2. Импортировать sample `Audio Manager Example` через Package Manager.
3. Выполнить `Tools/Audio/Setup/Generate Production Assets`.
4. Выполнить `Tools/Audio/Validate Sound Events`.
5. Проверить, что `AudioConfig` доступен (через `Resources/Audio/AudioConfig`).
6. Проверить Play Mode smoke:
- `PlayUI`, `PlaySFX`, `PlayMusic`
- snapshot transition
- pause/resume
- `SetSoundEnabled(false/true)`
- dynamic preload по Addressables.

## Runtime правила использования
- Использовать публичный фасад `AudioManager` (`PlayUI/PlaySFX/PlayMusic/...`), а не прямые вызовы `AudioSource.Play*` в gameplay/UI коде.
- Не создавать/удалять `AudioSource` вручную для обычного воспроизведения; использовать пул библиотеки.
- Для контента через Addressables сначала preload (`PreloadByIds`, `PreloadByEvents`, `PreloadBank`), затем проигрывание.
- Учитывать policy для `0.1.x`: `SkipIfNotLoaded`.
- Для жизненного цикла контента использовать scope/ref-count API: `AcquireScope` / `ReleaseScope`.
- При выключении/включении звука учитывать автодозагрузку недостающего контента и восстановление текущего музыкального трека.

## Что нельзя делать агентам
- Не дублировать runtime библиотечный код в `Assets` проекта-потребителя.
- Не добавлять новые зависимости без фиксации в документации и обоснования.
- Не менять архитектурные решения без обновления source-of-truth документации.
- Не пропускать обновление changelog/version при релизных изменениях пакета.

## Правила изменений в этом репозитории
- Библиотека: `/Users/glad/AudioManager/upm/com.gladfox.audiomanager`
- Consumer app: `/Users/glad/AudioManager/AudioManager`
- При изменении package API/поведения обновлять:
- `/Users/glad/AudioManager/upm/com.gladfox.audiomanager/README.md`
- `/Users/glad/AudioManager/upm/com.gladfox.audiomanager/CHANGELOG.md`
- `/Users/glad/AudioManager/README.md` (если меняется интеграционный flow)
- `/Users/glad/AudioManager/.memory_bank/*` (active/progress/systemPatterns при необходимости)

## PR/DoD для ИИ-агента
1. План задачи сформирован до реализации.
2. Изменения ограничены целевой областью (без случайных правок).
3. Документация синхронизирована.
4. Валидация выполнена (минимум smoke/checklist выше).
5. Коммит(ы), push, PR с коротким техническим summary.

## Рекомендуемый шаблон отчета агента
- Что сделано.
- Какие файлы изменены.
- Как проверено.
- Риски/ограничения.
- Следующий шаг (если нужен).
