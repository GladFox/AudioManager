# AudioManager 0.1.2 - Dynamic SoundEvent Discovery Spec

## 1. Цель релиза
Убрать обязательное ручное составление `List<SoundEvent>`/`List<string>` для динамических диалогов.

Требуемое поведение:
- если `SoundEvent` появился в рантайме (создан/загружен/поднят сценой), он автоматически попадает в реестр discoverable событий;
- в конце загрузки диалога можно вызвать один preload-метод без ручного списка;
- загрузка идет пакетно, с прогрессом, через Addressables;
- после завершения диалога память освобождается через scope/ref-count модель.

Целевой релиз: `0.1.2`.

## 2. Проблема текущей версии (0.1.1)
- Поддерживаются только явные сценарии:
  - `PreloadByEvents(...)`
  - `PreloadByIds(...)`
  - `PreloadBank(...)`
- Если диалог строится динамически и не дает список событий явно, библиотека сама их не собирает.
- Из-за `SkipIfNotLoaded` первый play может быть пропущен, если preload не был вызван корректно.

## 3. Область работ
### Входит
- runtime-discovery реестр `SoundEvent`;
- API для preload discovered событий (все/новые/по scope);
- интеграция с существующей `AudioContentService` (без слома текущего preload API);
- scope lifecycle для автоматической выгрузки ненужных клипов;
- debug-индикаторы количества discovered/preloaded events;
- документация и acceptance тесты.

### Не входит
- смена `OnDemandPlayPolicy` по умолчанию (`SkipIfNotLoaded` остается);
- смена базовой архитектуры Addressables loader;
- внешний middleware (FMOD/Wwise).

## 4. Архитектурное решение
### 4.1 Discovery реестр
Добавить runtime реестр `SoundEventDiscoveryRegistry`:
- хранит `HashSet<SoundEvent>` активных discoverable событий;
- дедупликация по ссылке;
- фиксирует `discoveryRevision` (монотонный счетчик) при изменении реестра.

Источник данных:
- `SoundEvent.OnEnable` -> register;
- `SoundEvent.OnDisable` -> unregister.

Это покрывает:
- статические SO, загруженные через сцену/ресурсы/addressables;
- динамически созданные SO (`CreateInstance`) при активации.

### 4.2 Snapshot-модель для "новых" событий
Добавить lightweight маркер ревизии:
- `int CaptureDiscoveryMarker()`
- `AudioLoadHandle PreloadDiscoveredSince(int marker, bool acquireScope, string scopeId = null)`

Сценарий диалога:
1. перед стартом загрузки диалога: `marker = CaptureDiscoveryMarker()`;
2. диалог создает/поднимает `SoundEvent`;
3. в конце загрузки диалога: `PreloadDiscoveredSince(marker, acquireScope: true, scopeId: dialogScopeId)`.

### 4.3 Полный preload discovered
Добавить:
- `AudioLoadHandle PreloadDiscovered(bool acquireScope = false, string scopeId = null)`

Использование:
- загрузка всех известных в текущий момент событий без ручного списка.

### 4.4 Scope и выгрузка
Для discovered preload использовать текущую модель:
- `AcquireScope`/`ReleaseScope`;
- unload only при `refCount == 0` и `not in use`;
- `UnloadDelaySeconds` сохраняется.

## 5. Публичный API (добавить в AudioManager)
- `int CaptureDiscoveryMarker()`
- `AudioLoadHandle PreloadDiscovered(bool acquireScope = false, string scopeId = null)`
- `AudioLoadHandle PreloadDiscoveredSince(int marker, bool acquireScope = false, string scopeId = null)`
- `int GetDiscoveredEventCount()`

Требования совместимости:
- существующие API `PreloadByIds/Events/Bank` не менять;
- поведение play-path не ломать.

## 6. Поведение и правила
1. Если звук выключен (`!soundEnabled && !musicEnabled`) -> discovered preload возвращает completed-handle без запуска загрузки.
2. В discovered preload учитываются только события, подходящие под текущие настройки (`soundEnabled/musicEnabled` + bus rules).
3. Если `acquireScope = true`, `scopeId` обязателен; иначе -> failed handle с error.
4. Повторные вызовы на тех же событиях не дублируют Addressables load и не раздувают refCount некорректно.
5. `ReleaseScope(scopeId)` инициирует стандартный delayed unload.

## 7. Debug и диагностика
Расширить debug данные:
- `DiscoveredEventCount`
- `LastDiscoveryRevision`
- `LastDiscoveredPreloadCount`

Логи (опционально через debug flag):
- сколько событий найдено в discovered preload;
- сколько реально ушло в загрузку;
- сколько было пропущено фильтрами/дедупом.

## 8. Acceptance Criteria
1. Динамический диалог создает N новых `SoundEvent`; ручные списки не формируются.
2. В конце загрузки диалога `PreloadDiscoveredSince(marker, true, scopeId)` загружает нужные клипы пакетно.
3. UI получает корректный прогресс `0..1` через `AudioLoadHandle`.
4. Первый play после preload воспроизводится без второго клика.
5. `ReleaseScope(scopeId)` + delay выгружает неиспользуемые клипы.
6. Повторный вход в диалог не создает утечек ref-count/handles.
7. Старые интеграции с `PreloadByIds/Events/Bank` работают без изменений.

## 9. Риски
- лишняя регистрация editor-only объектов при domain reload;
- гонки между `OnDisable` и активной загрузкой;
- ошибки scopeId дисциплины в gameplay-коде (не release scope).

## 10. Definition of Done (0.1.2 scope)
- реализован discovery registry + новый API;
- updated demo scenario для dynamic dialog preload без ручных списков;
- обновлены `README`/`CHANGELOG`/Memory Bank;
- acceptance checklist пройден;
- изменения запушены в git.
