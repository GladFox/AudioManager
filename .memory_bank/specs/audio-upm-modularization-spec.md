# AudioManager UPM Modularization Spec

## 0. Утвержденные решения (на текущий момент)
- Package name: `com.gladfox.audiomanager`.
- Стартовая версия package: `0.1.0`.
- UPM source хранится в текущем репозитории: `/upm/com.gladfox.audiomanager`.
- Демо поддерживается в двух вариантах:
  - как `Samples~` внутри пакета;
  - как standalone demo app (`AudioManager/`).
- Release channel для `0.1.x`: `git tags only` (без scoped registry на старте).

## 1. Цель
Перевести текущую аудиобиблиотеку в полноценный UPM-пакет, а существующий Unity-проект использовать как demo-приложение и референс интеграции.

Целевой результат:
- библиотека распространяется как UPM-модуль;
- demo-приложение потребляет пакет как внешнюю зависимость;
- runtime/editor API остаются стабильными для команды.

## 2. Контекст и мотивация
- Сейчас код и ассеты библиотеки смешаны с проектом приложения.
- Это усложняет переиспользование в других проектах и версионирование.
- Нужен четкий контракт: `Library (UPM)` и `Demo App (Consumer)`.

## 3. Область работ
### Входит
- выделение `AudioManagement` в UPM-пакет;
- упаковка runtime/editor кода в структуру package;
- оформление документации пакета (`README`, `CHANGELOG`, `package.json`);
- подготовка sample-контента пакета;
- перевод demo Unity-проекта на использование пакета;
- валидация установки пакета через `file:` и `git`.

### Не входит
- редизайн публичного API библиотеки;
- смена аудио-архитектуры на FMOD/Wwise;
- выпуск в Unity Asset Store;
- полная перестройка CI/CD, кроме минимально необходимого для package release.

## 4. Целевая структура репозитория
Предлагаемая структура:
- `upm/com.gladfox.audiomanager/`
- `AudioManager/` (Unity demo app)
- `.memory_bank/`
- `local/`

Содержимое UPM-пакета:
- `upm/com.gladfox.audiomanager/package.json`
- `upm/com.gladfox.audiomanager/README.md`
- `upm/com.gladfox.audiomanager/CHANGELOG.md`
- `upm/com.gladfox.audiomanager/LICENSE.md`
- `upm/com.gladfox.audiomanager/Runtime/...`
- `upm/com.gladfox.audiomanager/Editor/...`
- `upm/com.gladfox.audiomanager/Samples~/AudioDemo/...`
- `upm/com.gladfox.audiomanager/Documentation~/...` (опционально)

## 5. Пакетные требования
### 5.1 Package identity
- `name`: `com.gladfox.audiomanager`
- `displayName`: `Audio Manager`
- `version`: semver (`0.1.0` как первый package релиз)
- `unity`: минимум текущая поддерживаемая версия проекта.

### 5.2 Dependencies (package.json)
Минимум:
- `com.unity.addressables`

Опционально:
- `com.unity.inputsystem` только если runtime пакета реально зависит от неё.
  Если Input System нужна только в sample/demo, держать зависимость в sample/app, а не в package runtime.

### 5.3 Assemblies
Обязательные asmdef:
- `AudioManagement.Runtime.asmdef`
- `AudioManagement.Editor.asmdef`

Требования:
- Editor assembly зависит от Runtime assembly.
- Runtime assembly не зависит от UnityEditor.
- Явные ссылки на Addressables assembly.

## 6. Правила переноса кода и ассетов
### 6.1 Что переносим в пакет
- runtime код библиотеки (`AudioManager`, pooling, handles, content service, data SO classes);
- editor tooling библиотеки (`AudioValidator`, `AudioDebuggerWindow`, `AudioProductionSetup`).

### 6.2 Что остается в demo app
- `ProjectSettings`, `Packages/manifest.json` проекта;
- demo-specific bootstrap/scripts, если они не универсальны для пакета;
- test scenes, не являющиеся reusable sample пакета.

### 6.3 Samples
В `Samples~/AudioDemo` должны быть:
- demo scene;
- demo scripts для запуска;
- минимальные demo sound events/config/mixer;
- README c шагами импорта sample.

Важно:
- `AddressableAssetSettings` не должны требовать жесткого project-global состояния в пакете.
- если нужен bootstrap Addressables в проекте-потребителе, сделать явный setup tool + документацию.

## 7. Миграция demo приложения
Demo app должен:
- подключать пакет через `file:../../upm/com.gladfox.audiomanager` (локально);
- иметь сценарий для последующего переключения на git tag URL;
- запускаться без embedded-копии исходников библиотеки в `Assets`.

Требования:
- удалить дубли runtime/editor кода из `Assets` demo app после подключения пакета;
- сохранить рабочий demo flow (preload overlay, dialog sounds, snapshots, sound toggle).

## 8. Совместимость API
Сохранить публичные API без breaking changes:
- `PlayUI`, `PlaySFX`, `PlayMusic`
- `PreloadByIds`, `PreloadByEvents`, `PreloadBank`
- `AcquireScope`, `ReleaseScope`, `UnloadBank`, `UnloadUnused`
- volume/snapshot/pause/stop API

Если обнаружен breaking change:
- фиксировать в `CHANGELOG.md`;
- повышать major/minor по semver правилам.

## 9. Документация пакета
### 9.1 README package
Обязательно:
- назначение пакета;
- install guide (`file:` и `git`);
- quick start (создание `AudioConfig`, `SoundEvent`, preload/play);
- Addressables integration notes;
- known limitations.

### 9.2 CHANGELOG
Формат Keep a Changelog:
- Added/Changed/Fixed/Removed.

### 9.3 Migration guide
Отдельный раздел:
- как перейти с in-project версии на UPM;
- что удалить/перенести;
- как обновить references в сценах/ассетах.

## 10. Release strategy
- Утвержденная стратегия для `0.1.x`:
  - source-of-truth для релизов: git tags вида `upm/vX.Y.Z`;
  - PR в `main` с проверками compile + sample import smoke;
  - после merge — tag + release notes.
- Расширение на scoped registry допускается позже, отдельным решением.

## 11. Acceptance Criteria
1. Пакет устанавливается в чистый Unity проект через `file:` без ручного копирования скриптов.
2. Пакет устанавливается через `git` URL на tag/branch.
3. Runtime и Editor компилируются без ошибок.
4. Sample импортируется и запускается.
5. Demo app работает, используя пакет, а не локальные дубликаты кода.
6. Addressables preload/play/unload сценарии работают после упаковки.
7. Документация пакета покрывает install + quick start + migration.

## 12. Риски и узкие места
- Разрыв GUID ссылок при переносе ScriptableObject/scene assets.
- Конфликты Addressables settings между sample и проектом-потребителем.
- Неявные зависимости editor tooling на project paths.
- Потеря backward-compatibility при переносе namespaces/asmdef.

## 13. Статус утверждения
Все ключевые организационные решения по UPM-модульному этапу утверждены.
