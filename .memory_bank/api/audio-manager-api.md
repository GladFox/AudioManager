# AudioManager Public API

## Playback
- `AudioHandle PlayUI(SoundEvent evt, float volumeMul = 1f, float pitchMul = 1f, bool allowOverlap = true)`
- `AudioHandle PlaySFX(SoundEvent evt, Vector3? position = null, Transform follow = null, float volumeMul = 1f, float pitchMul = 1f)`
- `AudioHandle PlayMusic(SoundEvent evt, float fadeIn = 0.5f, float crossfade = 0.5f, bool restartIfSame = false)`
- `AudioHandle PlayUI(string id)`
- `AudioHandle PlaySFX(string id, Vector3 position)`
- `AudioHandle PlayMusic(string id)`
- `AudioHandle PlayUI(AudioClip clip, float volumeMul = 1f, float pitchMul = 1f)`
- `AudioHandle PlaySFX(AudioClip clip, Vector3? position = null, Transform follow = null, float volumeMul = 1f, float pitchMul = 1f)`
- `AudioHandle PlayMusic(AudioClip clip, float fadeIn = 0.5f, float crossfade = 0.5f)`

## Mixer Control
- `void SetMasterVolume01(float value)`
- `void SetMusicVolume01(float value)`
- `void SetSfxVolume01(float value)`
- `void SetUiVolume01(float value)`
- `void MuteAll(bool mute)`
- `bool TransitionToSnapshot(string name, float transitionTime)`

## Lifecycle
- `void Stop(AudioHandle handle, float fadeOut = 0f)`
- `void StopAllSFX(float fadeOut = 0f)`
- `void StopMusic(float fadeOut = 0f)`
- `void PauseAll(bool pause)`

## Handle Control
- `bool AudioHandle.IsValid`
- `void AudioHandle.Stop(float fadeOut = 0f)`
- `void AudioHandle.SetVolume(float volume01)`
- `void AudioHandle.SetPitch(float pitch)`
- `void AudioHandle.SetFollowTarget(Transform target)`

## Debug API
- `int ActiveVoiceCount`
- `int Pool2DInUse`, `int Pool2DTotal`
- `int Pool3DInUse`, `int Pool3DTotal`
- `int GetDebugVoices(List<AudioManager.DebugVoiceInfo> buffer)`
