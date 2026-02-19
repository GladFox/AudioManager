# Tech Context

## Версия Unity
- `6000.2.6f2` (из `ProjectSettings/ProjectVersion.txt`).

## Инструменты
- Unity built-in audio (`AudioSource`, `AudioMixer`, `AudioMixerSnapshot`).
- ScriptableObject для контента.
- Editor tooling (`EditorWindow`, `MenuItem`, `AssetDatabase`) для валидации/диагностики.

## Ограничения
- Без FMOD/Wwise.
- Addressables пока не включены (нужно отдельное решение для async preload/caching).
- Поддержка WebGL не зафиксирована (требуется подтверждение по autoplay/gesture ограничениям).
