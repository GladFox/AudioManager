# AudioManager (UPM + Demo App)

Стартовая UPM-версия: `0.1.2`  
Release channel `0.1.x`: `git tags only`

## Что это
Репозиторий разделен на:
- UPM библиотеку: `/upm/com.gladfox.audiomanager`
- Unity demo приложение: `/AudioManager`

Библиотека предоставляет централизованную аудиоподсистему Unity: UI, SFX, Music, Ambience через единый API, `AudioMixer` и `Snapshots`.

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
- динамическая загрузка клипов через Addressables (`PreloadByIds/PreloadByEvents/PreloadBank/PreloadDiscovered`);
- unload неиспользуемых клипов с `UnloadDelaySeconds` и scope/ref-count моделью;
- discovery preload для динамически появляющихся `SoundEvent` без ручных preload-list;
- editor-инструменты: генерация production ассетов, валидация, runtime debugger;
- демо-сцена с рабочим примером.

## Структура
- UPM пакет:
  - `/upm/com.gladfox.audiomanager/Runtime`
  - `/upm/com.gladfox.audiomanager/Editor`
  - `/upm/com.gladfox.audiomanager/Samples~`
- Demo app:
  - `/AudioManager/Assets`
  - `/AudioManager/Packages`
  - `/AudioManager/ProjectSettings`

## UPM
- Package path: `/upm/com.gladfox.audiomanager`
- Git dependency URL:
  - `https://github.com/GladFox/AudioManager.git?path=/upm/com.gladfox.audiomanager#upm/v0.1.2`

## Быстрый старт (demo app)
1. Открой проект Unity (`/AudioManager`).
2. Убедись, что в `Packages/manifest.json` подключен:
   - `com.gladfox.audiomanager: file:../../upm/com.gladfox.audiomanager`
3. Импортируй sample `Audio Manager Example` из Package Manager.
4. Открой `Assets/Samples/Audio Manager/<package-version>/Audio Manager Example/Demo/AudioDemoScene.unity`.
5. Запусти Play Mode и проверь:
   - preload overlay с прогрессом загрузки Addressables;
   - dynamic dialog prefab (`Resources/Audio/DemoDialoguePrefab`) со ссылками на `SoundEvent`;
   - `1/2/3`: line playback (UI/3D follow/UI);
   - `4`: music toggle;
   - `5`: pause/resume;
   - `6`: menu/gameplay snapshot;
   - `7`: sound off/on (+ reload missing dialogue sounds);
   - `8`: dialogue open/close with `PreloadDiscoveredSince` + `ReleaseScope`.

## Базовый пример использования
```csharp
var audio = AudioManager.Instance;
audio.PlayUI("demo.ui.click");
audio.PlaySFX("demo.sfx.moving", transform);
audio.PlayMusic("demo.music.loop", 0.5f, 0.5f);
audio.PreloadDiscovered(acquireScope: true, scopeId: "dialogue.scope");
audio.TransitionToSnapshot("Menu", 0.25f);
```

## Документация
- Архитектурный source of truth: `local/README.md`
- Memory Bank: `.memory_bank/`
- Релиз-ноты: `RELEASE_NOTES.md`
- UPM package README: `upm/com.gladfox.audiomanager/README.md`
