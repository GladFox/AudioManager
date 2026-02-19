# Unity Setup Guide

## 1. Создание ассетов
- Создать `AudioConfig` (`Create/Audio/Audio Config`).
- Создать набор `SoundEvent` (`Create/Audio/Sound Event`).

## 2. AudioMixer
- Создать группы минимум: `Master`, `Music`, `SFX`, `UI`.
- Expose параметры: `MasterVolume`, `MusicVolume`, `SFXVolume`, `UIVolume`.
- Создать snapshots: `Default`, `Menu`, `Pause`, `Muffled` (минимально нужные проекту).
- Привязать группы и snapshots в `AudioConfig`.

## 3. AudioManager в сцене
- Добавить `AudioManager` на bootstrap-объект (например, `Systems/AudioManager`).
- Назначить `AudioConfig`.
- В `AudioConfig.SoundEvents` добавить события, которые вызываются по `id`.

## 4. UI интеграция
- На `Button`/`Selectable` добавить `UIButtonSound`.
- Назначить `SoundEvent` или `clickEventId`.

## 5. Сценовые эмиттеры
- Добавить `AudioSceneEmitter` для ambient/loop источников в сцене.

## 6. Editor-инструменты
- `Tools/Audio/Validate Sound Events` для проверки контента.
- `Tools/Audio/Debugger` для runtime-диагностики в Play Mode.
