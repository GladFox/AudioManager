# Migration From In-Project Scripts To UPM Package

1. Remove duplicated AudioManager runtime/editor scripts from `Assets/Audio`.
2. Install `com.gladfox.audiomanager` package.
3. Keep project assets (`AudioConfig`, `SoundEvent`, mixer, clips, scenes).
4. Run `Tools/Audio/Validate Sound Events` and fix warnings if needed.
5. Run Play Mode smoke test (UI/SFX/Music/Snapshots/Addressables preload).
