# Unity Setup Guide

## 1. Быстрый production bootstrap (рекомендуется)
- Выполнить `Tools/Audio/Setup/Generate Production Assets`.
- Будут созданы/обновлены:
  - `Assets/Audio/Data/AudioMain.mixer`
  - `Assets/Audio/Data/SoundEvents/*.asset` (demo events)
  - `Assets/Audio/Data/GeneratedClips/*.wav` (demo clips)
  - `Assets/Resources/Audio/AudioConfig.asset`
  - Build Settings с `AudioDemoScene`.

## 2. Создание/редактирование ассетов вручную
- Создать `AudioConfig` (`Create/Audio/Audio Config`) при необходимости кастомного пайплайна.
- Создать/настроить набор `SoundEvent` (`Create/Audio/Sound Event`).

## 3. AudioMixer
- Создать группы минимум: `Master`, `Music`, `SFX`, `UI`.
- Expose параметры: `MasterVolume`, `MusicVolume`, `SFXVolume`, `UIVolume`.
- Создать snapshots: `Default`, `Menu`, `Pause`, `Muffled` (минимально нужные проекту).
- Привязать группы и snapshots в `AudioConfig`.

## 4. AudioManager в сцене
- Добавить `AudioManager` на bootstrap-объект (например, `Systems/AudioManager`).
- Назначить `AudioConfig` или положить его в `Resources/Audio/AudioConfig` для auto-load.
- В `AudioConfig.SoundEvents` добавить события, которые вызываются по `id`.

## 5. UI интеграция
- На `Button`/`Selectable` добавить `UIButtonSound`.
- Назначить `SoundEvent` или `clickEventId`.

## 6. Сценовые эмиттеры
- Добавить `AudioSceneEmitter` для ambient/loop источников в сцене.

## 7. Editor-инструменты
- `Tools/Audio/Validate Sound Events` для проверки контента.
- `Tools/Audio/Debugger` для runtime-диагностики в Play Mode.
