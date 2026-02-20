# AudioManager (Unity)

Версия продукта: `0.0.1`  
Статус ветки разработки: `0.0.2` (Addressables dynamic loading)

## Что это
`AudioManager` — централизованная аудиоподсистема для Unity-проекта: UI, SFX, Music, Ambience через единый API, `AudioMixer` и `Snapshots`.

## Для чего нужно
- убрать разрозненные вызовы `AudioSource.Play*` по проекту;
- избежать постоянных `Instantiate/Destroy` источников звука;
- дать стабильный и предсказуемый микс для команды разработки и саунд-дизайна;
- ускорить интеграцию звука в UI/геймплей и тестовые сцены.

## Ключевые возможности
- единый API: `PlayUI`, `PlaySFX`, `PlayMusic`, `Stop`, `PauseAll`, `TransitionToSnapshot`;
- data-driven события (`SoundEvent`) и конфиг (`AudioConfig`);
- пулы `AudioSource` (2D/3D), лимиты и policy при переполнении;
- управление громкостью в `0..1` с конвертацией в dB;
- A/B каналы музыки для fade/crossfade;
- динамическая загрузка клипов через Addressables (`PreloadByIds/PreloadByEvents/PreloadBank`);
- unload неиспользуемых клипов с `UnloadDelaySeconds` и scope/ref-count моделью;
- editor-инструменты: генерация production ассетов, валидация, runtime debugger;
- демо-сцена с рабочим примером.

## Состав
- Runtime:
  - `AudioManager`
  - `AudioSourcePool`
  - `AudioContentService`
  - `AudioLoadHandle`
  - `AudioHandle`
  - `UIButtonSound`
  - `AudioSceneEmitter`
- Data:
  - `SoundEvent` (SO)
  - `AudioConfig` (SO)
  - `AudioBank` (SO)
- Editor:
  - `AudioProductionSetup`
  - `AudioValidator`
  - `AudioDebuggerWindow`

## Быстрый старт
1. Открой проект Unity (`AudioManager/`).
2. Выполни `Tools/Audio/Setup/Generate Production Assets`.
3. Убедись, что в проекте создан `Assets/Resources/Audio/AudioConfig.asset`.
4. Открой `Assets/Scenes/AudioDemoScene.unity`.
5. Запусти Play Mode и проверь:
   - preload overlay с прогрессом загрузки Addressables;
   - `1/2/3`: line playback (UI/3D follow/UI);
   - `4`: music toggle;
   - `5`: pause/resume;
   - `6`: menu/gameplay snapshot;
   - `7`: sound off/on (+ reload missing dialogue sounds).

## Базовый пример использования
```csharp
var audio = AudioManager.Instance;
audio.PlayUI("demo.ui.click");
audio.PlaySFX("demo.sfx.moving", transform);
audio.PlayMusic("demo.music.loop", 0.5f, 0.5f);
audio.PreloadByIds(new [] { "demo.ui.click", "demo.sfx.moving" });
audio.TransitionToSnapshot("Menu", 0.25f);
```

## Документация
- Архитектурный source of truth: `local/README.md`
- Memory Bank: `.memory_bank/`
- Релиз-ноты: `RELEASE_NOTES.md`
