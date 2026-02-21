# Audio Manager (UPM)

`com.gladfox.audiomanager` is a Unity audio management package that provides:
- centralized playback API (`PlayUI`, `PlaySFX`, `PlayMusic`),
- `AudioMixer` buses and snapshot transitions,
- 2D/3D `AudioSource` pooling,
- Addressables-based dynamic loading with preload/progress/scope unload controls.

## Installation

### Local file dependency
Add to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gladfox.audiomanager": "file:../../upm/com.gladfox.audiomanager"
  }
}
```

### Git dependency (tag/branch)
Use Unity Package Manager git URL format, for example:

```text
https://github.com/GladFox/AudioManager.git?path=/upm/com.gladfox.audiomanager#upm/v0.1.3
```

## Quick Start
1. Install package.
2. Run `Tools/Audio/Setup/Generate Production Assets`.
3. Open your scene and ensure `AudioManager` exists (or create at runtime).
4. Play sounds by id or `SoundEvent`.

```csharp
var audio = AudioManagement.AudioManager.Instance;
audio.PlayUI("demo.ui.click");
audio.PlaySFX("demo.sfx.moving", transform);
audio.PlayMusic("demo.music.loop", 0.35f, 0.35f);
```

## Dynamic Loading API
- `PreloadByIds(...)`
- `PreloadByEvents(...)`
- `PreloadBank(...)`
- `CaptureDiscoveryMarker()`
- `PreloadDiscovered(...)`
- `PreloadDiscoveredSince(marker, ...)`
- `AcquireScope(...)` / `ReleaseScope(...)`
- `UnloadBank(...)` / `UnloadUnused()`

### Dynamic Dialog Pattern
```csharp
var audio = AudioManagement.AudioManager.Instance;
var ids = new List<string> { "dialogue.intro", "dialogue.line1", "dialogue.line2" };
var load = audio.AcquireScope("dialogue.scope", ids); // preload + hold

// ... play dialogue sounds ...

audio.ReleaseScope("dialogue.scope");
audio.UnloadUnused(); // optional immediate cleanup
```

## Samples
Import `Audio Manager Example` from Package Manager Samples.
It contains the full demo content folder (`Data`, `Demo`, `Resources`) including `AudioDemoScene`.

## Release Channel
For `0.1.x`, package releases are distributed via git tags only (`upm/vX.Y.Z`).
