# Project Brief

## Цель проекта
Реализовать производительный и масштабируемый AudioManager для Unity, который централизует UI/SFX/Music/Ambience, управляет миксом через AudioMixer/Snapshots и обеспечивает стабильное воспроизведение без постоянных аллокаций на `Play*` за счет пуллинга `AudioSource`.

## Границы проекта
- Unity Audio stack only (`AudioMixer`, `AudioSource`, `AudioMixerSnapshot`).
- Runtime API для UI, SFX, Music.
- Data-driven подход через `ScriptableObject` (`SoundEvent`, `AudioConfig`).
- Пул 2D/3D источников с политиками при переполнении.
- Базовые editor-tools для валидации и отладки.

## Что не входит в проект
- Интеграция с FMOD/Wwise.
- Сетевой voice-chat стек.
- Полноценная DSP-система поверх Unity Audio.
- Жесткая привязка к Addressables (до отдельного решения).
