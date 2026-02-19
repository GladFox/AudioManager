# AudioManager Acceptance Test Strategy

## Functional Checks
- Проверка `PlayUI/PlaySFX/PlayMusic` по `SoundEvent` и по `id`.
- Проверка `Cooldown` и `MaxInstances` на `SoundEvent`.
- Проверка 2D/3D routing, follow-target, release loop/non-loop.
- Проверка `Set*Volume01` и `MuteAll` через exposed mixer params.
- Проверка snapshot-переходов и политики приоритета.

## Performance Checks
- Профилирование flood scenario (`PlayUI` burst 200/сек).
- Контроль отсутствия `Instantiate/Destroy` в steady-state.
- Контроль отсутствия регулярных GC allocations в `Play*` после прогрева пулов.

## Regression Checks
- Потеря/возврат фокуса приложения.
- `Time.timeScale = 0` и поведение UI-звуков.
- Сохранение/восстановление пользовательских громкостей между запусками.
