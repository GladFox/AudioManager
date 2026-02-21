# Release Notes

## 0.1.3 - 2026-02-21

### Добавлено
- Demo UI переведен на крупный `uGUI` layout (лучше читаемость на desktop и mobile).
- В demo добавлено отдельное всплывающее окно диалога поверх основного интерфейса:
  - появляется после загрузки диалога;
  - содержит дополнительные кнопки воспроизведения (`intro/line1/line2/line3/music`);
  - позволяет явно закрыть и уничтожить диалог.

### Изменено
- Demo lifecycle диалога теперь явный и предсказуемый:
  - `Resources.Load<GameObject>(...)`
  - `Instantiate(...)`
  - `Destroy(...)`
- Подгрузка звуков диалога переведена на `AcquireScope(scopeId, ids)` по `SoundEvent.Id`, собранным из загруженного `DialoguePrefab`.
- Локальные импортированные копии sample в app-проекте исключены из git:
  - добавлен ignore для `AudioManager/Assets/Samples/Audio Manager/`.

### Исправлено
- Проблема «звук появляется только со второго клика» в demo устранена:
  - preload/hold выполняется до первого воспроизведения.
- При закрытии диалога теперь сразу запускается очистка неиспользуемых клипов:
  - `ReleaseScope(...)` + `UnloadUnused()`.

## 0.1.2 - 2026-02-22

### Добавлено
- Runtime discovery registry: автоматически регистрирует/удаляет `SoundEvent` через lifecycle (`OnEnable/OnDisable`).
- Новый discovery API в `AudioManager`:
  - `CaptureDiscoveryMarker()`
  - `PreloadDiscovered(...)`
  - `PreloadDiscoveredSince(marker, ...)`
- Editor hook для очистки discovery-реестра при переходе в Play Mode (устранение editor-конфликтов статического состояния).
- Диагностика discovery в `AudioDebuggerWindow`:
  - `Discovered Events`
  - `Discovery Revision`
  - `Last Discovered Preload Count`

### Изменено
- Тестовый bootstrap сцены перешел с ручного `PreloadByIds(...)` на `PreloadDiscovered(..., scopeId: "demo.dialogue")`.
- Кнопка `Play Music (4)` больше не зависит от повторного клика из-за пропущенного preload в demo flow.

### Совместимость
- Существующие API `PreloadByIds/PreloadByEvents/PreloadBank` сохранены.
- Политика `OnDemandPlayPolicy = SkipIfNotLoaded` не изменена.

## 0.1.1 - 2026-02-21

### Изменено
- Репозиторий доведен до стабильной UPM-модели поставки:
  - библиотека публикуется как пакет `com.gladfox.audiomanager`;
  - demo-контент вынесен в package sample `Samples~/AudioManager`.
- Sample в UPM переведен на формат полного примера (`Audio Manager Example`):
  - `Data` (mixer),
  - `Demo` (scene + bootstrap + clips + sound events),
  - `Resources` (`AudioConfig`).
- Удален устаревший sample scaffold `Samples~/AudioDemo` в пакете.
- Актуализированы документация и процессные файлы под новый путь примера и UPM flow.

### Документация
- Обновлен корневой `README.md`:
  - добавлена явная ссылка на UPM git dependency;
  - обновлен путь импорта sample под текущую версию.
- Обновлены package docs:
  - `upm/com.gladfox.audiomanager/README.md`
  - `upm/com.gladfox.audiomanager/CHANGELOG.md`
- Синхронизированы `local/README.md` и Memory Bank.

## 0.0.1 - 2026-02-20

### Добавлено
- Централизованный `AudioManager` API:
  - `PlayUI/PlaySFX/PlayMusic`
  - `Stop/StopAllSFX/StopMusic/StopByEventId`
  - `PauseAll`, `MuteAll`, `TransitionToSnapshot`
- Data-driven модель:
  - `SoundEvent` (варианты клипов, anti-spam, spatial, приоритеты)
  - `AudioConfig` (mixer groups, snapshots, volume params, defaults, pools)
- Пуллинг `AudioSource` (2D/3D):
  - `InitialSize`, `MaxSize`, `ExpandStep`
  - `StealPolicy` и авто-освобождение
- Music A/B каналы для fade/crossfade.
- Интеграционные компоненты:
  - `UIButtonSound`
  - `AudioSceneEmitter`
- Editor tooling:
  - `AudioProductionSetup` (генерация production ассетов)
  - `AudioValidator`
  - `AudioDebuggerWindow`
- Демо-сцена `AudioDemoScene` с Input System управлением.

### Изменено
- Версия продукта (`bundleVersion`) обновлена до `0.0.1`.
- Snapshot policy:
  - в одном кадре выигрывает больший приоритет;
  - между кадрами выигрывает последний запрос.
- `AudioManager` сначала пытается загрузить `AudioConfig` из `Resources/Audio/AudioConfig`.

### Исправлено
- Устранено предупреждение Unity при release pooled-source (`AudioSource.time` reset).
- Устранён `ArgumentOutOfRangeException` при обработке fade-jobs в edge-case stop+fade.
- Демо hotkeys переведены на новую Input System (без legacy `Input`).

### Ограничения релиза
- Manual profiler acceptance (flood/perf/audio artifacts) в основном Unity-проекте требует отдельного прогона.
