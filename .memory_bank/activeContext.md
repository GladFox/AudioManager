# Active Context

## Текущее направление
Подготовка и выпуск релиза `0.1.3`: стабилизация demo UX, deterministic preload первого клика, cleanup git-шума app sample копий.

## Активные задачи
- QA_TESTER: smoke по demo flow (open/close dialogue, first-click playback, immediate unload).
- DOCS_WRITER: синхронизация релизной документации под `0.1.3`.
- STEWARD: release tag `upm/v0.1.3`.

## Последние изменения
- Demo `uGUI` увеличен и дополнен popup-окном диалога с дополнительными кнопками воспроизведения.
- Demo dialogue lifecycle переведен на `Resources.Load + Instantiate + Destroy`.
- Preload диалога переведен на `AcquireScope(scopeId, ids)` из `SoundEvent.Id` prefab-контракта.
- При закрытии диалога выполняется `ReleaseScope + UnloadUnused` для immediate cleanup.
- В git добавлен ignore для app sample копий: `AudioManager/Assets/Samples/Audio Manager`.

## Следующие шаги
1. Проверить импорт sample в чистом consumer проекте через Package Manager.
2. Зафиксировать post-release smoke checklist в docs (first-click + unload-cycle).
3. Подготовить scope следующего релиза (`0.1.4`).
