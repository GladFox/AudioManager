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
- Подготовка релиза `0.1.2` (dynamic `SoundEvent` discovery preload).
- Спецификация и role-plan на реализацию автосбора событий без ручных списков.

## Известные проблемы
- Локальный `dotnet build` по `Assembly-CSharp.csproj` в рабочем проекте может использовать устаревшие csproj до Unity refresh.
- Batchmode по основному `projectPath` может блокироваться, если проект открыт в Unity GUI.

## Эволюция решений
- От in-project библиотеки к package-модели:
  - четкое разделение `Library (UPM)` и `Consumer App`;
  - сохранен публичный API без breaking changes;
  - release channel зафиксирован как `git tags only` для `0.1.x`;
  - demo-контент стандартизирован как пакетный sample, а не app-asset дубликат.
  - релиз `0.1.1` зафиксировал UPM-first delivery и единый installation URL в документации.
  - в `0.1.2` целевой шаг: автоматический discovery preload для динамических диалогов вместо ручных preload-list.

## Контроль изменений
last_checked_commit: 0e140c1290679c6f89df29add6e86f2ae425996b
last_checked_date: 2026-02-22 03:35:48 +0700
