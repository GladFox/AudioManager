# Progress

## Что работает
- Библиотека выделена в UPM пакет: `upm/com.gladfox.audiomanager`.
- Runtime/editor код библиотеки перенесен в пакет:
  - `AudioManager`, `AudioSourcePool`, `AudioContentService`, `AudioLoadHandle`, `AudioHandle`;
  - `SoundEvent`, `AudioConfig`, `AudioBank`;
  - `AudioProductionSetup`, `AudioValidator`, `AudioDebuggerWindow`.
- Demo app подключает пакет как локальную `file:` dependency.
- App-specific demo bootstrap сохранен в приложении и использует пакетный runtime API.
- Package sample добавлен (`Samples~/AudioDemo`):
  - scene,
  - sample bootstrap script,
  - usage README.
- Addressables dynamic loading архитектура продолжает работать после упаковки.

## В работе
- Финальный ручной PlayMode прогон в основном (не временном) Unity проекте.
- Подготовка PR и release tag `upm/v0.1.0`.

## Известные проблемы
- Локальный `dotnet build` по `Assembly-CSharp.csproj` в рабочем проекте может использовать устаревшие csproj до Unity refresh.
- Batchmode по основному `projectPath` может блокироваться, если проект открыт в Unity GUI.

## Эволюция решений
- От in-project библиотеки к package-модели:
  - четкое разделение `Library (UPM)` и `Consumer App`;
  - сохранен публичный API без breaking changes;
  - release channel зафиксирован как `git tags only` для `0.1.x`.

## Контроль изменений
last_checked_commit: 2bffd1c10cee3910d1d4885015e0a9069be7e29f
last_checked_date: 2026-02-21 03:00:18 +0700
