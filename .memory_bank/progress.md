# Progress

## Что работает
- Библиотека выделена в UPM пакет: `upm/com.gladfox.audiomanager`.
- Runtime/editor код библиотеки перенесен в пакет:
  - `AudioManager`, `AudioSourcePool`, `AudioContentService`, `AudioLoadHandle`, `AudioHandle`;
  - `SoundEvent`, `AudioConfig`, `AudioBank`;
  - `AudioProductionSetup`, `AudioValidator`, `AudioDebuggerWindow`.
- Demo app подключает пакет как локальную `file:` dependency.
- Полный demo-контент перенесен в package sample `Samples~/AudioManager`:
  - `Data` (mixer),
  - `Demo` (scene + bootstrap + clips + sound events),
  - `Resources/Audio` (`AudioConfig.asset`).
- App-проект больше не хранит дубли demo-контента в `Assets/AudioManager`.
- Addressables dynamic loading архитектура продолжает работать после упаковки.

## В работе
- Smoke-проверка импорта sample `Audio Manager Example` через Package Manager.
- Подготовка PR с переносом полного demo-folder в `Samples~`.

## Известные проблемы
- Локальный `dotnet build` по `Assembly-CSharp.csproj` в рабочем проекте может использовать устаревшие csproj до Unity refresh.
- Batchmode по основному `projectPath` может блокироваться, если проект открыт в Unity GUI.

## Эволюция решений
- От in-project библиотеки к package-модели:
  - четкое разделение `Library (UPM)` и `Consumer App`;
  - сохранен публичный API без breaking changes;
  - release channel зафиксирован как `git tags only` для `0.1.x`;
  - demo-контент стандартизирован как пакетный sample, а не app-asset дубликат.

## Контроль изменений
last_checked_commit: 672b2080fd7215c8036b679eaed466e5f7a645b8
last_checked_date: 2026-02-21 03:33:40 +0700
