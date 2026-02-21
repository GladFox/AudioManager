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
- Discovery preload для динамических `SoundEvent` реализован в runtime API (`PreloadDiscovered*`).
- Demo bootstrap использует крупный `uGUI` и popup-окно диалога поверх основного UI.
- Demo dialogue runtime flow переведен на `Resources.Load + Instantiate + Destroy`.
- Для prefab-контракта диалога реализован deterministic preload по `AcquireScope(scopeId, ids)`.
- На закрытии диалога выполняется immediate cleanup: `ReleaseScope + UnloadUnused`.
- Локальные app-копии sample исключены из git (`AudioManager/Assets/Samples/Audio Manager`).

## В работе
- Финальный post-release smoke под `0.1.3`.

## Известные проблемы
- Локальный `dotnet build` по `Assembly-CSharp.csproj` в рабочем проекте может использовать устаревшие csproj до Unity refresh.
- Batchmode по основному `projectPath` может блокироваться, если проект открыт в Unity GUI.
- Для полного подтверждения релиза требуется ручной PlayMode прогон в открытом Unity Editor.

## Эволюция решений
- От in-project библиотеки к package-модели:
  - четкое разделение `Library (UPM)` и `Consumer App`;
  - сохранен публичный API без breaking changes;
  - release channel зафиксирован как `git tags only` для `0.1.x`;
  - demo-контент стандартизирован как пакетный sample, а не app-asset дубликат.
  - релиз `0.1.1` зафиксировал UPM-first delivery и единый installation URL в документации.
  - в `0.1.2` реализован автоматический discovery preload для динамических диалогов вместо ручных preload-list.
  - в `0.1.3` demo-поток переведен на явный prefab lifecycle и deterministic scope preload для first-click ready playback.
  - в `0.1.3` app sample копии исключены из git; source-of-truth demo ассетов остается в UPM `Samples~`.

## Контроль изменений
last_checked_commit: 1c6abf5b9441a8cfcd1f3e271a52655dffec7277
last_checked_date: 2026-02-21
