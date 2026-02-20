# System Patterns

## Архитектурные принципы
- Централизация: один `AudioManager` как facade для аудио-операций.
- Data-driven: настройки звука в `ScriptableObject`, код не хранит hardcoded микс-параметры.
- Performance-first: пуллинг `AudioSource`, без `Destroy` при release.
- Fail-safe API: отсутствующий event/clip/config не ломает выполнение.
- Predictable mix: volume API принимает `0..1`, внутренне конвертирует в dB.
- Dynamic content: аудиоконтент загружается через Addressables по требованию и выгружается по policy.

## Журнал решений
- Решение: разделить runtime на фасад (`AudioManager`) + инфраструктуру (`AudioSourcePool`, `AudioHandle`) + data (`AudioConfig`, `SoundEvent`).
  Причина: изоляция ответственности и простота сопровождения.
- Решение: для музыки использовать выделенный A/B канал для кроссфейда.
  Причина: контролируемые fade/crossfade без конкуренции с SFX-пулом.
- Решение: при конфликте snapshot использовать приоритет из `AudioConfig`.
  Причина: явная политика для одновременных запросов состояний.
- Решение: политика snapshot-конфликтов разделена на 2 уровня:
  - в одном кадре выигрывает больший приоритет;
  - между кадрами выигрывает последний запрос.
  Причина: избежать deadlock состояния и сохранить предсказуемость возврата к `Default/Gameplay`.
- Решение: убрать сериализованные `AudioClip[]` из `SoundEvent` и использовать только `AssetReferenceT<AudioClip>[]`.
  Причина: контролируемая загрузка контента и уменьшение стартового memory footprint.
- Решение: ввести `AudioContentService` с registry (`Unloaded/Loading/Loaded/Failed`) и delayed unload.
  Причина: единая точка lifecycle-контроля Addressables и защита от double-load/double-release.
- Решение: применить `OnDemandPlayPolicy = SkipIfNotLoaded` как дефолт.
  Причина: предсказуемое поведение без скрытых задержек воспроизведения и без очередей в MVP.
- Решение: добавить `AudioBank` и автопрелоад банков при `SetSoundEnabled(true)`.
  Причина: пакетная загрузка нужных звуков с прогрессом и безопасной дозагрузкой при включении звука.
