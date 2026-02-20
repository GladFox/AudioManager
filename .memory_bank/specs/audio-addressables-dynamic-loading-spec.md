# Audio Addressables Dynamic Loading Spec

## 1. Цель
Реализовать в AudioManager динамическую загрузку/выгрузку аудио через Addressables с пакетным preload, прогрессом для UI и управлением памятью без слома текущего публичного API воспроизведения.

Целевой релиз: `0.0.2`.

## 2. Базовые решения (утверждено)
- `SoundEvent` НЕ хранит сериализованные `AudioClip[]`.
- Источник клипов: только `AssetReferenceT<AudioClip>[]`.
- Музыка НЕ использует `Streaming` load type.
- `PlayById` поддерживается, но не должен превращать кэш в "навсегда загружено".
- Память освобождается через явную модель владения и unload policy.
- `OnDemandPlayPolicy`: `SkipIfNotLoaded`.
- `UnloadDelaySeconds`: `15`.

## 3. Область работ
### Входит
- Data model для addressable-контента.
- Runtime content-loader service.
- Batch preload + progress API.
- Load by ids/events/banks.
- Интеграция в `AudioManager` play path.
- Управление unload/ref-count/scope.
- Debug/validator/docs.

### Не входит
- FMOD/Wwise.
- Полная переработка архитектуры Runtime/Editor.
- Внешние стриминговые плееры.

## 4. Требования к данным
### 4.1 SoundEvent
`SoundEvent` должен содержать:
- `string id`
- clip selection настройки (random/sequence/weighted)
- routing/spatial/priority/cooldown/maxInstances
- `AssetReferenceT<AudioClip>[] clipReferences`

Ограничение:
- сериализованного `AudioClip[]` поля нет.

### 4.2 AudioBank (новый SO)
`AudioBank` содержит:
- `string bankId`
- `SoundEvent[] events`
- флаги назначения (music/sfx/ui/dialogue/scene)

Назначение:
- группировать preload/unload по бизнес-сценариям (сцена, диалог, экран).

### 4.3 AudioConfig
Дополнить:
- `AudioBank[] banks`
- `OnDemandPlayPolicy` (default: `SkipIfNotLoaded`)
- `UnloadDelaySeconds` (default: `15`)
- `EnableAddressablesLogs`

## 5. Runtime архитектура
### 5.1 AudioContentService (новый)
Единая точка управления Addressables.

Функции:
- загрузка `SoundEvent` клипов
- загрузка списков id
- загрузка банков
- progress handle
- unload
- ref-count
- защита от double-load/double-release

Внутренний реестр:
- key: `AssetGUID`
- fields: `state`, `handle`, `refCount`, `lastAccessTime`, `isInUseByVoice`

Состояния:
- `Unloaded`
- `Loading`
- `Loaded`
- `Failed`

### 5.2 Модель владения
Ввести scope-владение:
- `AcquireScope(scopeId, ids/events/bank)`
- `ReleaseScope(scopeId)`

Правила:
- unload возможен только при `refCount == 0` и клип не играет
- опциональная задержка выгрузки через `UnloadDelaySeconds`

### 5.3 Интеграция в AudioManager
Play path:
1. `Play*` получает `SoundEvent` (напрямую или через id lookup).
2. Проверяется `AudioContentService`:
- Loaded -> выбрать клип и играть сразу
- Loading/Unloaded -> политика `OnDemandPlayPolicy`

Политики:
- `SkipIfNotLoaded`: возврат `AudioHandle.Invalid` (основной режим)
- `QueueAndPlay`: опционально, если будет включен override

## 6. Публичный API (минимум)
Добавить в `AudioManager`:
- `AudioLoadHandle PreloadBank(string bankId)`
- `AudioLoadHandle PreloadByIds(IReadOnlyList<string> ids)`
- `AudioLoadHandle PreloadByEvents(IReadOnlyList<SoundEvent> events)`
- `float GetLoadProgress(AudioLoadHandle handle)`
- `void AcquireScope(string scopeId, IReadOnlyList<string> ids)`
- `void ReleaseScope(string scopeId)`
- `void UnloadBank(string bankId)`
- `void UnloadUnused()`

`AudioLoadHandle`:
- `IsValid`, `IsDone`, `Status`, `Progress`, `Error`

## 7. Поведение при включении/выключении звука
### 7.1 Sound OFF
- новые загрузки не запускаются
- queued-on-demand воспроизведение отменяется
- неиспользуемые клипы могут быть выгружены (по policy)

### 7.2 Sound ON
- проверяется кэш и активные scope
- дозагружаются missing clips пакетно
- UI может подписаться на progress

## 8. Music ограничения
- `AudioImporter`/runtime policy для music: только `Compressed In Memory` или `Decompress On Load`
- `Streaming` запрещён
- кроссфейд A/B каналов остается совместимым

## 9. Debug/Diagnostics
Расширить `AudioDebuggerWindow`:
- loaded/loading/failed clip counts
- memory occupied by loaded clips
- active scopes
- queued play requests

Расширить logs:
- load/unload события с `id/guid/scope`
- причины skip/fail

## 10. Validator
Расширить `AudioValidator`:
- `SoundEvent.clipReferences` не пустой (для addressable event)
- дубли id
- битые `AssetReference`
- warning/error при невозможной политике

## 11. Acceptance Criteria
1. При выключенном звуке preload не стартует.
2. `PreloadBank` грузит все клипы банка одним групповым запросом.
3. `PreloadByIds` грузит только запрошенные ids.
4. UI получает прогресс `0..1` до completion.
5. `QueueAndPlay`: звук стартует после загрузки.
6. `SkipIfNotLoaded`: звук не играет и не падает.
7. После `ReleaseScope + delay` неиспользуемые клипы выгружаются.
8. Играющие клипы не выгружаются.
9. После 10 циклов load/unload нет утечек handle.
10. Музыка воспроизводится без Streaming.

## 12. Риски
- race conditions в пересечении preload/play/unload
- double-release Addressables handles
- рост времени первого воспроизведения при плохом preload
- memory thrash при слишком агрессивном unload

## 13. Definition of Done (для этой фичи)
- Реализованы API preload/load-by-id/unload/scope
- Обновлен validator и debugger
- Обновлены docs (`local/README.md`, Memory Bank)
- Пройден acceptance checklist
- Изменения запушены в git
