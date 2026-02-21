# Release Notes

## 0.1.2 - Draft

### Цель
- Закрыть функциональный гэп dynamic-dialog preload: исключить ручное составление списков `SoundEvent`/id для дозагрузки.

### Планируемые изменения
- Runtime discovery registry для автоматически обнаруженных `SoundEvent`.
- Marker-based preload API:
  - `CaptureDiscoveryMarker()`
  - `PreloadDiscovered()`
  - `PreloadDiscoveredSince(marker, ...)`
- Scope-aware lifecycle discovered preload/unload (совместимо с текущим `AudioContentService`).
- Обновление demo flow для сценария: загрузка диалога -> единый вызов preload -> play без второго клика.

### Статус
- ТЗ и role-plan подготовлены.
- Реализация запланирована на релиз `0.1.2`.

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
  - обновлен путь импорта sample для версии `0.1.1`.
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
