# AudioManager Spec (Consolidated)

## Scope
- Централизованное воспроизведение UI/SFX/Music/Ambience.
- Работа через `AudioMixer` + `AudioMixerSnapshot`.
- Пуллинг `AudioSource` (2D и 3D пулы, лимиты, steal policy).
- API уровня gameplay/UI + управление жизненным циклом.
- Автоматизируемый production bootstrap ассетов (`AudioProductionSetup`).

## Required Runtime Components
- `AudioManager`
- `AudioConfig` (SO)
- `SoundEvent` (SO)
- `AudioHandle`
- `AudioSourcePool`
- `UIButtonSound`

## Optional/Recommended Components
- `AudioDebuggerWindow`
- `AudioValidator`
- `AudioSceneEmitter`

## Acceptance Scenarios
1. UI click без создания новых объектов/источников.
2. Flood PlayUI 200/сек без заметных GC spikes и с анти-спам логикой.
3. 3D follow корректно следует за target и освобождается по окончании.
4. Music crossfade заданной длительности без щелчков.
5. Snapshot transition корректно меняет/возвращает микс.
6. Pause не ломает UI-клики, SFX/Music pause/muffle работают.
7. Громкости Master/Music/SFX/UI сохраняются и восстанавливаются.
