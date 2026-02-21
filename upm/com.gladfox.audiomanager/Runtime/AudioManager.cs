using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioManagement
{
    [DefaultExecutionOrder(-1000)]
    public sealed class AudioManager : MonoBehaviour
    {
        private const string PrefMaster = "audio.master.01";
        private const string PrefMusic = "audio.music.01";
        private const string PrefSfx = "audio.sfx.01";
        private const string PrefUi = "audio.ui.01";
        private const string PrefAmbience = "audio.ambience.01";
        private const string PrefVoice = "audio.voice.01";

        private struct EventRuntimeState
        {
            public int ActiveInstances;
            public int SequenceIndex;
            public float LastPlayRealtime;
        }

        private struct ActiveVoice
        {
            public int HandleId;
            public AudioSource Source;
            public AudioClip Clip;
            public AudioSourcePool.PooledSource PooledSource;
            public SoundEvent Event;
            public AudioBus Bus;
            public bool IsMusic;
        }

        public readonly struct DebugVoiceInfo
        {
            public readonly int HandleId;
            public readonly string EventId;
            public readonly AudioBus Bus;
            public readonly bool IsMusic;
            public readonly bool IsPlaying;

            public DebugVoiceInfo(int handleId, string eventId, AudioBus bus, bool isMusic, bool isPlaying)
            {
                HandleId = handleId;
                EventId = eventId;
                Bus = bus;
                IsMusic = isMusic;
                IsPlaying = isPlaying;
            }
        }

        private struct FadeJob
        {
            public int HandleId;
            public AudioSource Source;
            public float StartVolume;
            public float TargetVolume;
            public float Duration;
            public float Elapsed;
            public bool StopOnComplete;
        }

        [Serializable]
        private struct MusicChannel
        {
            public AudioSource Source;
            public int HandleId;
            public SoundEvent CurrentEvent;
        }

        public static AudioManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private AudioConfig config;
        [SerializeField] private bool dontDestroyOnLoad = true;

        private readonly Dictionary<string, SoundEvent> eventById = new Dictionary<string, SoundEvent>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<SoundEvent, EventRuntimeState> eventState = new Dictionary<SoundEvent, EventRuntimeState>();
        private readonly Dictionary<int, ActiveVoice> activeVoices = new Dictionary<int, ActiveVoice>(128);
        private readonly List<FadeJob> fadeJobs = new List<FadeJob>(16);
        private readonly HashSet<AudioClip> debugClipSet = new HashSet<AudioClip>();
        private readonly List<AudioClip> resolvedClips = new List<AudioClip>(16);
        private readonly List<AudioContentService.WeightedLoadedClip> resolvedWeightedClips = new List<AudioContentService.WeightedLoadedClip>(16);
        private readonly List<SoundEvent> preloadEventScratch = new List<SoundEvent>(32);
        private readonly List<SoundEvent> discoveredEventScratch = new List<SoundEvent>(64);
        private readonly List<string> preloadIdScratch = new List<string>(32);

        private AudioSourcePool pool2D;
        private AudioSourcePool pool3D;
        private MusicChannel musicA;
        private MusicChannel musicB;
        private AudioContentService contentService;

        private int nextHandleId = 1;
        private int activeSnapshotPriority;
        private int activeSnapshotFrame = -1;
        private string activeSnapshotName = string.Empty;
        private bool isPaused;
        private bool userPauseRequested;
        private bool focusPauseRequested;
        private bool appPauseRequested;
        private bool soundEnabled = true;
        private bool musicEnabled = true;
        private bool restoreMusicPending;
        private SoundEvent restoreMusicEvent;
        private AudioClip restoreMusicClip;
        private uint rngState = 2463534242u;
        private float lastMasterVolumeBeforeMute = 1f;
        private int lastDiscoveredPreloadCount;

        public AudioConfig Config => config;

        public int ActiveVoiceCount => activeVoices.Count;
        public int Pool2DInUse => pool2D != null ? pool2D.InUseCount : 0;
        public int Pool3DInUse => pool3D != null ? pool3D.InUseCount : 0;
        public int Pool2DTotal => pool2D != null ? pool2D.TotalCount : 0;
        public int Pool3DTotal => pool3D != null ? pool3D.TotalCount : 0;
        public int LoadedAddressableClipCount => contentService != null ? contentService.LoadedClipCount : 0;
        public int LoadingAddressableClipCount => contentService != null ? contentService.LoadingClipCount : 0;
        public int FailedAddressableClipCount => contentService != null ? contentService.FailedClipCount : 0;
        public int ActiveAudioScopeCount => contentService != null ? contentService.ScopeCount : 0;
        public int DiscoveredEventCount => SoundEventDiscoveryRegistry.Count;
        public int DiscoveryRevision => SoundEventDiscoveryRegistry.CurrentRevision;
        public int LastDiscoveredPreloadCount => lastDiscoveredPreloadCount;
        public bool SoundEnabled => soundEnabled;
        public bool MusicEnabled => musicEnabled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (config == null)
            {
                config = Resources.Load<AudioConfig>("Audio/AudioConfig");
                if (config == null)
                {
                    config = AudioConfig.CreateRuntimeDefaults();
                    Debug.LogWarning("[AudioManager] AudioConfig is missing. Runtime defaults were applied.");
                }
            }

            contentService = new AudioContentService(config != null && config.EnableAddressablesLogs);
            InitializeEventCatalog();
            InitializePools();
            InitializeMusicChannels();
            LoadAndApplyVolumes();
            PreloadAutoBanksForCurrentSettings();
        }

        private void Update()
        {
            var dspTime = AudioSettings.dspTime;
            var deltaTime = Time.unscaledDeltaTime;
            var realtime = Time.unscaledTime;

            pool2D?.Tick(dspTime, deltaTime);
            pool3D?.Tick(dspTime, deltaTime);
            contentService?.Tick(realtime, config != null ? config.UnloadDelaySeconds : 15f);

            UpdateFadeJobs(deltaTime);
            TryRestoreMusicAfterEnable();
            CleanupFinishedMusic();
        }

        public AudioHandle PlayUI(SoundEvent evt, float volumeMul = 1f, float pitchMul = 1f, bool allowOverlap = true)
        {
            return PlayEvent(evt, volumeMul, pitchMul, allowOverlap, null, null, true);
        }

        public AudioHandle PlaySFX(SoundEvent evt, Vector3? position = null, Transform follow = null, float volumeMul = 1f, float pitchMul = 1f)
        {
            return PlayEvent(evt, volumeMul, pitchMul, true, position, follow, false);
        }

        public AudioHandle PlayUI(AudioClip clip, float volumeMul = 1f, float pitchMul = 1f)
        {
            return PlayClipDirect(clip, AudioBus.UI, false, null, null, volumeMul, pitchMul);
        }

        public AudioHandle PlaySFX(AudioClip clip, Vector3? position = null, Transform follow = null, float volumeMul = 1f, float pitchMul = 1f)
        {
            var use3D = position.HasValue || follow != null;
            return PlayClipDirect(clip, AudioBus.Sfx, use3D, position, follow, volumeMul, pitchMul);
        }

        public AudioHandle PlayMusic(SoundEvent evt, float fadeIn = 0.5f, float crossfade = 0.5f, bool restartIfSame = false)
        {
            if (!musicEnabled)
            {
                return AudioHandle.Invalid;
            }

            if (!ValidateConfigAndEvent(evt, "PlayMusic"))
            {
                return AudioHandle.Invalid;
            }

            if (!EnsureEventContentReady(evt, preloadIfMissing: true))
            {
                return AudioHandle.Invalid;
            }

            if (!CanPlayByEventRules(evt))
            {
                return AudioHandle.Invalid;
            }

            if (!TryPickClip(evt, out var clip, out _))
            {
                LogWarn($"PlayMusic failed for event '{evt.Id}' because no clip is configured.");
                return AudioHandle.Invalid;
            }

            var active = GetActiveMusicChannel();
            if (!restartIfSame && active.Source != null && active.CurrentEvent == evt && active.Source.isPlaying)
            {
                return new AudioHandle(this, active.HandleId);
            }

            var incoming = active.Source == musicA.Source ? musicB : musicA;
            if (incoming.Source == null)
            {
                LogWarn("PlayMusic failed because music channels are not initialized.");
                return AudioHandle.Invalid;
            }

            if (incoming.HandleId >= 0)
            {
                UnregisterVoice(incoming.HandleId, stopSource: true);
            }

            incoming.Source.Stop();
            incoming.Source.clip = clip;
            incoming.Source.loop = evt.Loop;
            incoming.Source.priority = Mathf.Clamp(evt.Priority, 0, 256);
            incoming.Source.pitch = PickPitch(evt);
            incoming.Source.volume = 0f;
            incoming.Source.spatialBlend = 0f;
            incoming.Source.time = PickStartOffset(evt, clip);
            if (config.TryGetMixerGroup(AudioBus.Music, out var musicGroup))
            {
                incoming.Source.outputAudioMixerGroup = musicGroup;
            }
            incoming.Source.Play();
            if (isPaused)
            {
                incoming.Source.Pause();
            }

            var incomingHandle = nextHandleId++;
            incoming.HandleId = incomingHandle;
            incoming.CurrentEvent = evt;
            SetMusicChannel(incoming);

            RegisterVoice(new ActiveVoice
            {
                HandleId = incomingHandle,
                Source = incoming.Source,
                Clip = incoming.Source.clip,
                PooledSource = null,
                Event = evt,
                Bus = AudioBus.Music,
                IsMusic = true
            });
            contentService?.RegisterClipInUse(incoming.Source.clip);

            var targetVol = Mathf.Clamp01(evt.Volume);
            EnqueueFade(incomingHandle, incoming.Source, 0f, targetVol, Mathf.Max(0f, fadeIn), false);

            if (active.Source != null && active.Source.isPlaying)
            {
                EnqueueFade(active.HandleId, active.Source, active.Source.volume, 0f, Mathf.Max(0f, crossfade), true);
            }

            var runtime = GetOrCreateEventState(evt);
            runtime.ActiveInstances++;
            runtime.LastPlayRealtime = GetRealtimeNow(evt);
            eventState[evt] = runtime;

            return new AudioHandle(this, incomingHandle);
        }

        public AudioHandle PlayMusic(AudioClip clip, float fadeIn = 0.5f, float crossfade = 0.5f)
        {
            if (!musicEnabled)
            {
                return AudioHandle.Invalid;
            }

            if (config == null)
            {
                LogWarn("PlayMusic(AudioClip) ignored because AudioConfig is not assigned.");
                return AudioHandle.Invalid;
            }

            if (clip == null)
            {
                LogWarn("PlayMusic(AudioClip) ignored because clip is null.");
                return AudioHandle.Invalid;
            }

            var active = GetActiveMusicChannel();
            var incoming = active.Source == musicA.Source ? musicB : musicA;
            if (incoming.Source == null)
            {
                LogWarn("PlayMusic(AudioClip) failed because music channels are not initialized.");
                return AudioHandle.Invalid;
            }

            if (incoming.HandleId >= 0)
            {
                UnregisterVoice(incoming.HandleId, stopSource: true);
            }

            incoming.Source.Stop();
            incoming.Source.clip = clip;
            incoming.Source.loop = true;
            incoming.Source.priority = 64;
            incoming.Source.pitch = 1f;
            incoming.Source.volume = 0f;
            incoming.Source.spatialBlend = 0f;
            incoming.Source.time = 0f;
            if (config.TryGetMixerGroup(AudioBus.Music, out var musicGroup))
            {
                incoming.Source.outputAudioMixerGroup = musicGroup;
            }
            incoming.Source.Play();
            if (isPaused)
            {
                incoming.Source.Pause();
            }

            var incomingHandle = nextHandleId++;
            incoming.HandleId = incomingHandle;
            incoming.CurrentEvent = null;
            SetMusicChannel(incoming);

            RegisterVoice(new ActiveVoice
            {
                HandleId = incomingHandle,
                Source = incoming.Source,
                Clip = incoming.Source.clip,
                PooledSource = null,
                Event = null,
                Bus = AudioBus.Music,
                IsMusic = true
            });
            contentService?.RegisterClipInUse(incoming.Source.clip);

            EnqueueFade(incomingHandle, incoming.Source, 0f, 1f, Mathf.Max(0f, fadeIn), false);
            if (active.Source != null && active.Source.isPlaying)
            {
                EnqueueFade(active.HandleId, active.Source, active.Source.volume, 0f, Mathf.Max(0f, crossfade), true);
            }

            return new AudioHandle(this, incomingHandle);
        }

        public AudioHandle PlayUI(string id)
        {
            return TryGetEventById(id, out var evt) ? PlayUI(evt) : AudioHandle.Invalid;
        }

        public AudioHandle PlaySFX(string id, Vector3 position)
        {
            return TryGetEventById(id, out var evt) ? PlaySFX(evt, position) : AudioHandle.Invalid;
        }

        public AudioHandle PlaySFX(string id, Transform follow)
        {
            return TryGetEventById(id, out var evt) ? PlaySFX(evt, follow: follow) : AudioHandle.Invalid;
        }

        public AudioHandle PlayMusic(string id)
        {
            return TryGetEventById(id, out var evt) ? PlayMusic(evt) : AudioHandle.Invalid;
        }

        public AudioHandle PlayMusic(string id, float fadeIn, float crossfade, bool restartIfSame = false)
        {
            return TryGetEventById(id, out var evt) ? PlayMusic(evt, fadeIn, crossfade, restartIfSame) : AudioHandle.Invalid;
        }

        public AudioLoadHandle PreloadBank(string bankId)
        {
            if (config == null || config.Banks == null || string.IsNullOrWhiteSpace(bankId))
            {
                return AudioLoadHandle.Completed();
            }

            for (var i = 0; i < config.Banks.Length; i++)
            {
                var bank = config.Banks[i];
                if (bank == null || !string.Equals(bank.BankId, bankId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!soundEnabled && !musicEnabled)
                {
                    return AudioLoadHandle.Completed();
                }

                return PreloadByEvents(bank.Events);
            }

            return AudioLoadHandle.Completed();
        }

        public AudioLoadHandle PreloadByEvents(IReadOnlyList<SoundEvent> events)
        {
            if (contentService == null || events == null || events.Count == 0)
            {
                return AudioLoadHandle.Completed();
            }

            preloadEventScratch.Clear();
            for (var i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                if (evt != null && CanLoadEventForCurrentSettings(evt))
                {
                    preloadEventScratch.Add(evt);
                }
            }

            if (preloadEventScratch.Count == 0)
            {
                return AudioLoadHandle.Completed();
            }

            return contentService.PreloadEvents(preloadEventScratch);
        }

        public AudioLoadHandle PreloadByIds(IReadOnlyList<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return AudioLoadHandle.Completed();
            }

            preloadEventScratch.Clear();
            preloadIdScratch.Clear();
            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (string.IsNullOrWhiteSpace(id) || preloadIdScratch.Contains(id))
                {
                    continue;
                }

                preloadIdScratch.Add(id);
                if (TryGetEventById(id, out var evt) && CanLoadEventForCurrentSettings(evt))
                {
                    preloadEventScratch.Add(evt);
                }
            }

            if (preloadEventScratch.Count == 0)
            {
                return AudioLoadHandle.Completed();
            }

            return contentService.PreloadEvents(preloadEventScratch);
        }

        public int CaptureDiscoveryMarker()
        {
            return SoundEventDiscoveryRegistry.CaptureMarker();
        }

        public AudioLoadHandle PreloadDiscovered(bool acquireScope = false, string scopeId = null)
        {
            lastDiscoveredPreloadCount = SoundEventDiscoveryRegistry.FillAll(discoveredEventScratch);
            return PreloadDiscoveredInternal(acquireScope, scopeId);
        }

        public AudioLoadHandle PreloadDiscoveredSince(int marker, bool acquireScope = false, string scopeId = null)
        {
            lastDiscoveredPreloadCount = SoundEventDiscoveryRegistry.FillSince(marker, discoveredEventScratch);
            return PreloadDiscoveredInternal(acquireScope, scopeId);
        }

        public AudioLoadHandle AcquireScope(string scopeId, IReadOnlyList<string> ids)
        {
            if (contentService == null || string.IsNullOrWhiteSpace(scopeId) || ids == null || ids.Count == 0)
            {
                return AudioLoadHandle.Completed();
            }

            preloadEventScratch.Clear();
            preloadIdScratch.Clear();
            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (string.IsNullOrWhiteSpace(id) || preloadIdScratch.Contains(id))
                {
                    continue;
                }

                preloadIdScratch.Add(id);
                if (TryGetEventById(id, out var evt) && CanLoadEventForCurrentSettings(evt))
                {
                    preloadEventScratch.Add(evt);
                }
            }

            if (preloadEventScratch.Count == 0)
            {
                return AudioLoadHandle.Completed();
            }

            return contentService.AcquireScope(scopeId, preloadEventScratch);
        }

        private AudioLoadHandle PreloadDiscoveredInternal(bool acquireScope, string scopeId)
        {
            if (contentService == null)
            {
                return AudioLoadHandle.Completed();
            }

            if (!soundEnabled && !musicEnabled)
            {
                return AudioLoadHandle.Completed();
            }

            if (acquireScope && string.IsNullOrWhiteSpace(scopeId))
            {
                return AudioLoadHandle.Failed("scopeId is required when acquireScope=true.");
            }

            preloadEventScratch.Clear();
            for (var i = 0; i < discoveredEventScratch.Count; i++)
            {
                var evt = discoveredEventScratch[i];
                if (evt == null || !CanLoadEventForCurrentSettings(evt) || preloadEventScratch.Contains(evt))
                {
                    continue;
                }

                preloadEventScratch.Add(evt);
            }

            lastDiscoveredPreloadCount = preloadEventScratch.Count;
            if (preloadEventScratch.Count == 0)
            {
                return AudioLoadHandle.Completed();
            }

            if (acquireScope)
            {
                return contentService.AcquireScope(scopeId, preloadEventScratch);
            }

            return contentService.PreloadEvents(preloadEventScratch);
        }

        public void ReleaseScope(string scopeId)
        {
            contentService?.ReleaseScope(scopeId);
        }

        public void UnloadUnused()
        {
            if (contentService == null)
            {
                return;
            }

            contentService.ReleaseAllScopes();
            contentService.UnloadUnusedNow();
        }

        public void UnloadBank(string bankId)
        {
            if (contentService == null || config == null || config.Banks == null || string.IsNullOrWhiteSpace(bankId))
            {
                return;
            }

            for (var i = 0; i < config.Banks.Length; i++)
            {
                var bank = config.Banks[i];
                if (bank == null || !string.Equals(bank.BankId, bankId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                contentService.RequestUnloadEvents(bank.Events, immediate: false);
                return;
            }
        }

        public float GetLoadProgress(AudioLoadHandle handle)
        {
            return handle == null ? 1f : handle.Progress;
        }

        public void SetSoundEnabled(bool enabled)
        {
            if (soundEnabled == enabled && musicEnabled == enabled)
            {
                return;
            }

            soundEnabled = enabled;
            musicEnabled = enabled;

            if (!enabled)
            {
                CaptureMusicForRestore();
                StopAllSFX(0.05f);
                StopMusic(0.2f);
                contentService?.ReleaseAllScopes();
                return;
            }

            contentService?.SetLogsEnabled(config != null && config.EnableAddressablesLogs);
            PreloadAutoBanksForCurrentSettings();
            TryRestoreMusicAfterEnable();
        }

        public void SetMasterVolume01(float value)
        {
            SetMixerVolume(AudioBus.Master, value, PrefMaster, true);
        }

        public void SetMusicVolume01(float value)
        {
            SetMixerVolume(AudioBus.Music, value, PrefMusic, true);
        }

        public void SetSfxVolume01(float value)
        {
            SetMixerVolume(AudioBus.Sfx, value, PrefSfx, true);
        }

        public void SetUiVolume01(float value)
        {
            SetMixerVolume(AudioBus.UI, value, PrefUi, true);
        }

        public void MuteAll(bool mute)
        {
            if (config == null)
            {
                return;
            }

            if (mute)
            {
                lastMasterVolumeBeforeMute = GetSavedOrDefaultVolume(PrefMaster, config.GetDefaultVolume01(AudioBus.Master));
                SetMixerVolume(AudioBus.Master, 0f, PrefMaster, false);
                return;
            }

            SetMixerVolume(AudioBus.Master, lastMasterVolumeBeforeMute, PrefMaster, false);
        }

        public bool TransitionToSnapshot(string name, float transitionTime)
        {
            if (config == null || config.Mixer == null)
            {
                LogWarn("TransitionToSnapshot ignored because AudioConfig/Mixer is missing.");
                return false;
            }

            if (!config.TryGetSnapshot(name, out var snapshot, out var priority))
            {
                LogWarn($"TransitionToSnapshot('{name}') failed: snapshot not found.");
                return false;
            }

            // Policy: within the same frame, highest priority wins; across frames, last request wins.
            if (activeSnapshotFrame == Time.frameCount && !string.IsNullOrEmpty(activeSnapshotName) && priority < activeSnapshotPriority)
            {
                LogInfo($"TransitionToSnapshot('{name}') skipped by priority policy. Active='{activeSnapshotName}'({activeSnapshotPriority}), requested={priority}.");
                return false;
            }

            snapshot.TransitionTo(Mathf.Max(0f, transitionTime));
            activeSnapshotName = name;
            activeSnapshotPriority = priority;
            activeSnapshotFrame = Time.frameCount;
            return true;
        }

        public void Stop(AudioHandle handle, float fadeOut = 0f)
        {
            if (!TryGetActiveVoice(handle.Id, out var voice))
            {
                return;
            }

            if (fadeOut <= 0f)
            {
                CompleteStop(voice.HandleId);
                return;
            }

            EnqueueFade(voice.HandleId, voice.Source, voice.Source.volume, 0f, fadeOut, true);
        }

        public void StopAllSFX(float fadeOut = 0f)
        {
            var ids = CollectHandlesByBus(AudioBus.Sfx, AudioBus.Ambience, AudioBus.Voice);
            for (var i = 0; i < ids.Count; i++)
            {
                Stop(new AudioHandle(this, ids[i]), fadeOut);
            }
        }

        public int StopByEventId(string id, float fadeOut = 0f)
        {
            if (!TryGetEventById(id, out var evt))
            {
                return 0;
            }

            return StopByEvent(evt, fadeOut);
        }

        public int StopByEvent(SoundEvent evt, float fadeOut = 0f)
        {
            if (evt == null)
            {
                return 0;
            }

            var ids = new List<int>(activeVoices.Count);
            foreach (var kv in activeVoices)
            {
                if (kv.Value.Event == evt)
                {
                    ids.Add(kv.Key);
                }
            }

            for (var i = 0; i < ids.Count; i++)
            {
                Stop(new AudioHandle(this, ids[i]), fadeOut);
            }

            return ids.Count;
        }

        public void StopMusic(float fadeOut = 0f)
        {
            var ids = CollectHandlesByBus(AudioBus.Music);
            for (var i = 0; i < ids.Count; i++)
            {
                Stop(new AudioHandle(this, ids[i]), fadeOut);
            }
        }

        public void PauseAll(bool pause)
        {
            userPauseRequested = pause;
            ApplyPauseState();
        }

        private void ApplyPauseState()
        {
            var targetPause = userPauseRequested || focusPauseRequested || appPauseRequested;
            if (targetPause == isPaused)
            {
                return;
            }

            isPaused = targetPause;

            foreach (var kv in activeVoices)
            {
                var voice = kv.Value;
                if (voice.Bus == AudioBus.UI)
                {
                    continue;
                }

                if (targetPause)
                {
                    voice.Source.Pause();
                }
                else
                {
                    voice.Source.UnPause();
                }
            }
        }

        public bool IsHandleValid(int handleId)
        {
            return activeVoices.ContainsKey(handleId);
        }

        public void SetHandleVolume(AudioHandle handle, float volume01)
        {
            if (TryGetActiveVoice(handle.Id, out var voice))
            {
                voice.Source.volume = Mathf.Clamp01(volume01);
            }
        }

        public void SetHandlePitch(AudioHandle handle, float pitch)
        {
            if (TryGetActiveVoice(handle.Id, out var voice))
            {
                voice.Source.pitch = Mathf.Max(0.01f, pitch);
            }
        }

        public void SetHandleFollowTarget(AudioHandle handle, Transform target)
        {
            if (!TryGetActiveVoice(handle.Id, out var voice) || voice.PooledSource == null)
            {
                return;
            }

            voice.PooledSource.FollowTarget = target;
        }

        public int GetDebugVoices(List<DebugVoiceInfo> buffer)
        {
            if (buffer == null)
            {
                return 0;
            }

            buffer.Clear();
            foreach (var kv in activeVoices)
            {
                var voice = kv.Value;
                var evtId = voice.Event != null ? voice.Event.Id : "<direct>";
                var isPlaying = voice.Source != null && voice.Source.isPlaying;
                buffer.Add(new DebugVoiceInfo(voice.HandleId, evtId, voice.Bus, voice.IsMusic, isPlaying));
            }

            return buffer.Count;
        }

        public int GetDebugAudioClips(List<AudioClip> buffer, bool includeCatalog = true, bool includeActiveVoices = true)
        {
            if (buffer == null)
            {
                return 0;
            }

            buffer.Clear();
            debugClipSet.Clear();

            if (includeCatalog && contentService != null)
            {
                resolvedClips.Clear();
                contentService.FillLoadedClips(resolvedClips);
                for (var i = 0; i < resolvedClips.Count; i++)
                {
                    var clip = resolvedClips[i];
                    if (clip != null)
                    {
                        debugClipSet.Add(clip);
                    }
                }
            }

            if (includeActiveVoices)
            {
                foreach (var kv in activeVoices)
                {
                    var clip = kv.Value.Source != null ? kv.Value.Source.clip : null;
                    if (clip != null)
                    {
                        debugClipSet.Add(clip);
                    }
                }
            }

            foreach (var clip in debugClipSet)
            {
                buffer.Add(clip);
            }

            return buffer.Count;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (config == null || !config.PauseSfxAndMusicOnFocusLost)
            {
                focusPauseRequested = false;
                ApplyPauseState();
                return;
            }

            focusPauseRequested = !hasFocus;
            ApplyPauseState();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (config == null || !config.PauseSfxAndMusicOnApplicationPause)
            {
                appPauseRequested = false;
                ApplyPauseState();
                return;
            }

            appPauseRequested = pauseStatus;
            ApplyPauseState();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            contentService?.ForceUnloadAll();
        }

        private AudioHandle PlayEvent(SoundEvent evt, float volumeMul, float pitchMul, bool allowOverlap, Vector3? position, Transform follow, bool force2D)
        {
            if (!soundEnabled)
            {
                return AudioHandle.Invalid;
            }

            if (!ValidateConfigAndEvent(evt, "PlayEvent"))
            {
                return AudioHandle.Invalid;
            }

            if (!EnsureEventContentReady(evt, preloadIfMissing: true))
            {
                return AudioHandle.Invalid;
            }

            if (!allowOverlap && GetEventActiveInstances(evt) > 0)
            {
                return AudioHandle.Invalid;
            }

            if (!CanPlayByEventRules(evt))
            {
                return AudioHandle.Invalid;
            }

            if (!TryPickClip(evt, out var clip, out var sequenceIndex))
            {
                LogWarn($"PlayEvent failed for '{evt.Id}' because clip could not be selected.");
                return AudioHandle.Invalid;
            }

            var use3D = ResolveIs3D(evt, force2D, position.HasValue, follow != null);
            var pool = use3D ? pool3D : pool2D;
            if (pool == null)
            {
                LogWarn("PlayEvent failed because source pool is not initialized.");
                return AudioHandle.Invalid;
            }

            if (!pool.TryAcquire(evt.Priority, out var pooled))
            {
                LogWarn($"PlayEvent skipped for '{evt.Id}' because pool is full and policy skipped allocation.");
                return AudioHandle.Invalid;
            }

            var source = pooled.Source;
            var bus = evt.MixerBus;
            if (config.TryGetMixerGroup(bus, out var group))
            {
                source.outputAudioMixerGroup = group;
            }

            source.clip = clip;
            source.loop = evt.Loop;
            source.priority = Mathf.Clamp(evt.Priority, 0, 256);
            source.volume = Mathf.Clamp01(evt.Volume * Mathf.Clamp01(volumeMul));
            source.pitch = Mathf.Max(0.01f, PickPitch(evt) * Mathf.Max(0.01f, pitchMul));
            source.time = PickStartOffset(evt, clip);
            source.spatialBlend = use3D ? 1f : 0f;
            source.rolloffMode = evt.RolloffMode;
            source.minDistance = evt.MinDistance;
            source.maxDistance = evt.MaxDistance;
            source.bypassReverbZones = evt.BypassReverbZones;
            source.bypassEffects = evt.BypassEffects;

            if (use3D)
            {
                if (follow != null)
                {
                    source.transform.position = follow.position;
                }
                else if (position.HasValue)
                {
                    source.transform.position = position.Value;
                }
            }
            else
            {
                source.transform.localPosition = Vector3.zero;
            }

            var handleId = nextHandleId++;
            pooled.InUse = true;
            pooled.Priority = source.priority;
            pooled.CurrentEvent = evt;
            pooled.FollowTarget = follow;
            pooled.HandleId = handleId;
            pooled.Bus = bus;
            pooled.StartDspTime = AudioSettings.dspTime;
            pooled.Looping = evt.Loop;

            var duration = ComputeDurationSeconds(source.clip, source.time, source.pitch);
            pooled.EndDspTime = evt.Loop ? double.PositiveInfinity : (pooled.StartDspTime + duration);

            source.Play();
            if (isPaused && bus != AudioBus.UI)
            {
                source.Pause();
            }

            RegisterVoice(new ActiveVoice
            {
                HandleId = handleId,
                Source = source,
                Clip = source.clip,
                PooledSource = pooled,
                Event = evt,
                Bus = bus,
                IsMusic = false
            });
            contentService?.RegisterClipInUse(source.clip);

            var runtime = GetOrCreateEventState(evt);
            runtime.ActiveInstances++;
            runtime.SequenceIndex = sequenceIndex;
            runtime.LastPlayRealtime = GetRealtimeNow(evt);
            eventState[evt] = runtime;

            return new AudioHandle(this, handleId);
        }

        private AudioHandle PlayClipDirect(AudioClip clip, AudioBus bus, bool use3D, Vector3? position, Transform follow, float volumeMul, float pitchMul)
        {
            if ((bus == AudioBus.Music && !musicEnabled) || (bus != AudioBus.Music && !soundEnabled))
            {
                return AudioHandle.Invalid;
            }

            if (config == null)
            {
                LogWarn("PlayClipDirect ignored because AudioConfig is not assigned.");
                return AudioHandle.Invalid;
            }

            if (clip == null)
            {
                LogWarn("PlayClipDirect ignored because clip is null.");
                return AudioHandle.Invalid;
            }

            var pool = use3D ? pool3D : pool2D;
            if (pool == null)
            {
                LogWarn("PlayClipDirect failed because source pool is not initialized.");
                return AudioHandle.Invalid;
            }

            if (!pool.TryAcquire(128, out var pooled))
            {
                LogWarn("PlayClipDirect skipped because pool is full and policy skipped allocation.");
                return AudioHandle.Invalid;
            }

            var source = pooled.Source;
            if (config.TryGetMixerGroup(bus, out var group))
            {
                source.outputAudioMixerGroup = group;
            }

            source.clip = clip;
            source.loop = false;
            source.priority = 128;
            source.volume = Mathf.Clamp01(volumeMul);
            source.pitch = Mathf.Max(0.01f, pitchMul);
            source.time = 0f;
            source.spatialBlend = use3D ? 1f : 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1f;
            source.maxDistance = 20f;
            source.bypassReverbZones = false;
            source.bypassEffects = false;

            if (use3D)
            {
                if (follow != null)
                {
                    source.transform.position = follow.position;
                }
                else if (position.HasValue)
                {
                    source.transform.position = position.Value;
                }
            }
            else
            {
                source.transform.localPosition = Vector3.zero;
            }

            var handleId = nextHandleId++;
            pooled.InUse = true;
            pooled.Priority = source.priority;
            pooled.CurrentEvent = null;
            pooled.FollowTarget = follow;
            pooled.HandleId = handleId;
            pooled.Bus = bus;
            pooled.StartDspTime = AudioSettings.dspTime;
            pooled.Looping = false;
            pooled.EndDspTime = pooled.StartDspTime + ComputeDurationSeconds(source.clip, 0f, source.pitch);

            source.Play();
            if (isPaused && bus != AudioBus.UI)
            {
                source.Pause();
            }

            RegisterVoice(new ActiveVoice
            {
                HandleId = handleId,
                Source = source,
                Clip = source.clip,
                PooledSource = pooled,
                Event = null,
                Bus = bus,
                IsMusic = false
            });
            contentService?.RegisterClipInUse(source.clip);

            return new AudioHandle(this, handleId);
        }

        private void InitializeEventCatalog()
        {
            eventById.Clear();
            eventState.Clear();

            if (config.SoundEvents == null)
            {
                return;
            }

            for (var i = 0; i < config.SoundEvents.Length; i++)
            {
                var evt = config.SoundEvents[i];
                if (evt == null || string.IsNullOrWhiteSpace(evt.Id))
                {
                    continue;
                }

                if (!eventById.TryAdd(evt.Id, evt))
                {
                    LogWarn($"Duplicate SoundEvent id '{evt.Id}' in AudioConfig catalog. Keeping first entry.");
                }

                if (!eventState.ContainsKey(evt))
                {
                    eventState.Add(evt, new EventRuntimeState());
                }
            }
        }

        private void PreloadAutoBanksForCurrentSettings()
        {
            if (config == null || config.Banks == null || contentService == null)
            {
                return;
            }

            preloadEventScratch.Clear();
            for (var i = 0; i < config.Banks.Length; i++)
            {
                var bank = config.Banks[i];
                if (bank == null || bank.Events == null || bank.Events.Length == 0)
                {
                    continue;
                }

                var canLoadBank = (soundEnabled && bank.LoadWhenSoundEnabled) || (musicEnabled && bank.LoadWhenMusicEnabled);
                if (!canLoadBank)
                {
                    continue;
                }

                for (var j = 0; j < bank.Events.Length; j++)
                {
                    var evt = bank.Events[j];
                    if (evt == null || !CanLoadEventForCurrentSettings(evt) || preloadEventScratch.Contains(evt))
                    {
                        continue;
                    }

                    preloadEventScratch.Add(evt);
                }
            }

            if (preloadEventScratch.Count > 0)
            {
                contentService.PreloadEvents(preloadEventScratch);
            }
        }

        private void CaptureMusicForRestore()
        {
            restoreMusicPending = false;
            restoreMusicEvent = null;
            restoreMusicClip = null;

            var active = GetActiveMusicChannel();
            if (active.Source == null || active.Source.clip == null)
            {
                if (musicA.HandleId >= 0 && musicA.Source != null && musicA.Source.clip != null)
                {
                    active = musicA;
                }
                else if (musicB.HandleId >= 0 && musicB.Source != null && musicB.Source.clip != null)
                {
                    active = musicB;
                }
                else
                {
                    return;
                }
            }

            restoreMusicEvent = active.CurrentEvent;
            restoreMusicClip = active.Source.clip;
            restoreMusicPending = true;
        }

        private void TryRestoreMusicAfterEnable()
        {
            if (!restoreMusicPending || !musicEnabled)
            {
                return;
            }

            AudioHandle handle;
            if (restoreMusicEvent != null)
            {
                handle = PlayMusic(restoreMusicEvent, fadeIn: 0.35f, crossfade: 0.2f, restartIfSame: true);
            }
            else if (restoreMusicClip != null)
            {
                handle = PlayMusic(restoreMusicClip, fadeIn: 0.35f, crossfade: 0.2f);
            }
            else
            {
                restoreMusicPending = false;
                return;
            }

            if (!handle.IsValid)
            {
                return;
            }

            restoreMusicPending = false;
            restoreMusicEvent = null;
            restoreMusicClip = null;
        }

        private void InitializePools()
        {
            var poolRoot = new GameObject("AudioPools").transform;
            poolRoot.SetParent(transform, false);

            pool2D = new AudioSourcePool("Pool2D", poolRoot, false, config.Pool2D, config.EnableDebugLogs, OnPooledSourceReleased);
            pool3D = new AudioSourcePool("Pool3D", poolRoot, true, config.Pool3D, config.EnableDebugLogs, OnPooledSourceReleased);
        }

        private void InitializeMusicChannels()
        {
            var root = new GameObject("MusicChannels").transform;
            root.SetParent(transform, false);

            musicA = new MusicChannel
            {
                Source = CreateMusicSource("Music_A", root),
                HandleId = -1,
                CurrentEvent = null
            };

            musicB = new MusicChannel
            {
                Source = CreateMusicSource("Music_B", root),
                HandleId = -1,
                CurrentEvent = null
            };
        }

        private AudioSource CreateMusicSource(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.priority = 64;

            if (config.TryGetMixerGroup(AudioBus.Music, out var group))
            {
                source.outputAudioMixerGroup = group;
            }

            return source;
        }

        private void LoadAndApplyVolumes()
        {
            SetMixerVolume(AudioBus.Master, GetSavedOrDefaultVolume(PrefMaster, config.GetDefaultVolume01(AudioBus.Master)), PrefMaster, false);
            SetMixerVolume(AudioBus.Music, GetSavedOrDefaultVolume(PrefMusic, config.GetDefaultVolume01(AudioBus.Music)), PrefMusic, false);
            SetMixerVolume(AudioBus.Sfx, GetSavedOrDefaultVolume(PrefSfx, config.GetDefaultVolume01(AudioBus.Sfx)), PrefSfx, false);
            SetMixerVolume(AudioBus.UI, GetSavedOrDefaultVolume(PrefUi, config.GetDefaultVolume01(AudioBus.UI)), PrefUi, false);
            SetMixerVolume(AudioBus.Ambience, GetSavedOrDefaultVolume(PrefAmbience, config.GetDefaultVolume01(AudioBus.Ambience)), PrefAmbience, false);
            SetMixerVolume(AudioBus.Voice, GetSavedOrDefaultVolume(PrefVoice, config.GetDefaultVolume01(AudioBus.Voice)), PrefVoice, false);
        }

        private void SetMixerVolume(AudioBus bus, float value01, string prefKey, bool save)
        {
            if (config == null || config.Mixer == null)
            {
                return;
            }

            value01 = Mathf.Clamp01(value01);
            if (!config.TryGetVolumeParam(bus, out var param))
            {
                return;
            }

            var db = ToDb(value01, config.MinDb, config.MaxDb);
            config.Mixer.SetFloat(param, db);
            if (save)
            {
                PlayerPrefs.SetFloat(prefKey, value01);
                PlayerPrefs.Save();
            }
        }

        private static float ToDb(float value01, float minDb, float maxDb)
        {
            if (value01 <= 0.0001f)
            {
                return minDb;
            }

            var db = 20f * Mathf.Log10(value01);
            return Mathf.Clamp(db, minDb, maxDb);
        }

        private float GetSavedOrDefaultVolume(string key, float fallback)
        {
            return PlayerPrefs.GetFloat(key, Mathf.Clamp01(fallback));
        }

        private bool ValidateConfigAndEvent(SoundEvent evt, string caller)
        {
            if (config == null)
            {
                LogWarn($"{caller} ignored because AudioConfig is not assigned.");
                return false;
            }

            if (evt == null)
            {
                LogWarn($"{caller} ignored because SoundEvent is null.");
                return false;
            }

            return true;
        }

        private bool EnsureEventContentReady(SoundEvent evt, bool preloadIfMissing)
        {
            if (contentService == null || evt == null)
            {
                return false;
            }

            if (!CanLoadEventForCurrentSettings(evt))
            {
                return false;
            }

            if (contentService.IsEventReady(evt))
            {
                return true;
            }

            if (preloadIfMissing)
            {
                preloadEventScratch.Clear();
                preloadEventScratch.Add(evt);
                contentService.PreloadEvents(preloadEventScratch);
            }

            if (config != null && config.OnDemandPlayPolicy == OnDemandPlayPolicy.QueueAndPlay)
            {
                LogInfo($"Event '{evt.Id}' is loading. QueueAndPlay policy is not enabled in this build path; playback skipped for now.");
            }

            return false;
        }

        private bool CanLoadEventForCurrentSettings(SoundEvent evt)
        {
            if (evt == null)
            {
                return false;
            }

            return evt.MixerBus == AudioBus.Music ? musicEnabled : soundEnabled;
        }

        private bool ResolveIs3D(SoundEvent evt, bool force2D, bool hasPosition, bool hasFollow)
        {
            if (force2D)
            {
                return false;
            }

            return evt.SpatialMode switch
            {
                SpatialMode.TwoD => false,
                SpatialMode.ThreeD => true,
                _ => hasPosition || hasFollow
            };
        }

        private bool CanPlayByEventRules(SoundEvent evt)
        {
            var runtime = GetOrCreateEventState(evt);
            if (runtime.ActiveInstances >= evt.MaxInstances)
            {
                return false;
            }

            var now = GetRealtimeNow(evt);
            if (evt.CooldownSeconds > 0f && now - runtime.LastPlayRealtime < evt.CooldownSeconds)
            {
                return false;
            }

            return true;
        }

        private float GetRealtimeNow(SoundEvent evt)
        {
            if (config.UIAlwaysUnscaled && evt.MixerBus == AudioBus.UI)
            {
                return Time.unscaledTime;
            }

            return Time.realtimeSinceStartup;
        }

        private EventRuntimeState GetOrCreateEventState(SoundEvent evt)
        {
            if (eventState.TryGetValue(evt, out var runtime))
            {
                return runtime;
            }

            runtime = new EventRuntimeState();
            eventState.Add(evt, runtime);
            return runtime;
        }

        private int GetEventActiveInstances(SoundEvent evt)
        {
            return eventState.TryGetValue(evt, out var runtime) ? runtime.ActiveInstances : 0;
        }

        private bool TryPickClip(SoundEvent evt, out AudioClip clip, out int nextSequenceIndex)
        {
            clip = null;
            nextSequenceIndex = 0;

            var runtime = GetOrCreateEventState(evt);
            nextSequenceIndex = runtime.SequenceIndex;

            if (contentService == null)
            {
                return false;
            }

            if (!contentService.TryCollectLoadedClips(evt, resolvedClips, resolvedWeightedClips))
            {
                return false;
            }

            if (evt.ClipSelection == ClipSelectionMode.WeightedRandom && resolvedWeightedClips.Count > 0)
            {
                var totalWeight = 0f;
                for (var i = 0; i < resolvedWeightedClips.Count; i++)
                {
                    var weight = resolvedWeightedClips[i].Weight;
                    if (resolvedWeightedClips[i].Clip != null && weight > 0f)
                    {
                        totalWeight += weight;
                    }
                }

                if (totalWeight > 0f)
                {
                    var pick = NextRandom01() * totalWeight;
                    var cumulative = 0f;
                    for (var i = 0; i < resolvedWeightedClips.Count; i++)
                    {
                        var weighted = resolvedWeightedClips[i];
                        if (weighted.Clip == null || weighted.Weight <= 0f)
                        {
                            continue;
                        }

                        cumulative += weighted.Weight;
                        if (pick <= cumulative)
                        {
                            clip = weighted.Clip;
                            return true;
                        }
                    }
                }
            }

            if (resolvedClips.Count == 0)
            {
                return false;
            }

            if (evt.ClipSelection == ClipSelectionMode.Sequence)
            {
                var index = runtime.SequenceIndex;
                for (var attempts = 0; attempts < resolvedClips.Count; attempts++)
                {
                    var clipCandidate = resolvedClips[index % resolvedClips.Count];
                    index = (index + 1) % resolvedClips.Count;
                    if (clipCandidate == null)
                    {
                        continue;
                    }

                    clip = clipCandidate;
                    nextSequenceIndex = index;
                    return true;
                }

                return false;
            }

            var selectedIndex = Mathf.FloorToInt(NextRandom01() * resolvedClips.Count);
            selectedIndex = Mathf.Clamp(selectedIndex, 0, resolvedClips.Count - 1);

            if (resolvedClips[selectedIndex] != null)
            {
                clip = resolvedClips[selectedIndex];
                return true;
            }

            for (var i = 0; i < resolvedClips.Count; i++)
            {
                if (resolvedClips[i] != null)
                {
                    clip = resolvedClips[i];
                    return true;
                }
            }

            return false;
        }

        private float PickPitch(SoundEvent evt)
        {
            var range = evt.PitchRange;
            if (Mathf.Approximately(range.x, range.y))
            {
                return range.x;
            }

            return Mathf.Lerp(range.x, range.y, NextRandom01());
        }

        private float PickStartOffset(SoundEvent evt, AudioClip clip)
        {
            if (clip == null || evt.RandomStartOffsetMax <= 0f)
            {
                return 0f;
            }

            var maxOffset = Mathf.Min(evt.RandomStartOffsetMax, Mathf.Max(0f, clip.length - 0.01f));
            return maxOffset <= 0f ? 0f : maxOffset * NextRandom01();
        }

        private static double ComputeDurationSeconds(AudioClip clip, float startOffset, float pitch)
        {
            if (clip == null)
            {
                return 0d;
            }

            var remaining = Mathf.Max(0f, clip.length - startOffset);
            var normalizedPitch = Mathf.Max(0.01f, Mathf.Abs(pitch));
            return remaining / normalizedPitch;
        }

        private float NextRandom01()
        {
            rngState ^= rngState << 13;
            rngState ^= rngState >> 17;
            rngState ^= rngState << 5;

            return (rngState & 0x00FFFFFFu) / 16777215f;
        }

        private bool TryGetEventById(string id, out SoundEvent evt)
        {
            evt = null;
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            if (eventById.TryGetValue(id, out evt))
            {
                return true;
            }

            LogWarn($"SoundEvent id '{id}' not found in AudioConfig catalog.");
            return false;
        }

        private void RegisterVoice(ActiveVoice voice)
        {
            activeVoices[voice.HandleId] = voice;
        }

        private bool TryGetActiveVoice(int handleId, out ActiveVoice voice)
        {
            return activeVoices.TryGetValue(handleId, out voice);
        }

        private List<int> CollectHandlesByBus(params AudioBus[] buses)
        {
            var result = new List<int>(activeVoices.Count);
            foreach (var kv in activeVoices)
            {
                var bus = kv.Value.Bus;
                for (var i = 0; i < buses.Length; i++)
                {
                    if (bus == buses[i])
                    {
                        result.Add(kv.Key);
                        break;
                    }
                }
            }

            return result;
        }

        private void CompleteStop(int handleId)
        {
            if (!TryGetActiveVoice(handleId, out var voice))
            {
                return;
            }

            if (voice.IsMusic)
            {
                voice.Source.Stop();
                UnregisterVoice(handleId, stopSource: false);
                return;
            }

            if (voice.PooledSource != null)
            {
                voice.PooledSource.Source.Stop();
                if (pool2D != null && pool2D.TryGetByHandle(handleId, out var pooled2D))
                {
                    pool2D.Release(pooled2D);
                }
                else if (pool3D != null && pool3D.TryGetByHandle(handleId, out var pooled3D))
                {
                    pool3D.Release(pooled3D);
                }
                else
                {
                    UnregisterVoice(handleId, stopSource: false);
                }
            }
            else
            {
                UnregisterVoice(handleId, stopSource: false);
            }
        }

        private void OnPooledSourceReleased(AudioSourcePool.PooledSource pooled)
        {
            if (pooled == null)
            {
                return;
            }

            var handleId = pooled.HandleId;
            if (handleId >= 0)
            {
                UnregisterVoice(handleId, stopSource: false);
            }
        }

        private void UnregisterVoice(int handleId, bool stopSource)
        {
            if (!activeVoices.TryGetValue(handleId, out var voice))
            {
                return;
            }

            if (stopSource && voice.Source != null)
            {
                voice.Source.Stop();
            }

            contentService?.UnregisterClipInUse(voice.Clip);
            activeVoices.Remove(handleId);
            RemoveFadeJobs(handleId);

            if (voice.Event != null && eventState.TryGetValue(voice.Event, out var runtime))
            {
                runtime.ActiveInstances = Mathf.Max(0, runtime.ActiveInstances - 1);
                eventState[voice.Event] = runtime;
            }

            if (voice.IsMusic)
            {
                if (musicA.HandleId == handleId)
                {
                    musicA.HandleId = -1;
                    musicA.CurrentEvent = null;
                }

                if (musicB.HandleId == handleId)
                {
                    musicB.HandleId = -1;
                    musicB.CurrentEvent = null;
                }
            }
        }

        private void EnqueueFade(int handleId, AudioSource source, float startVolume, float targetVolume, float duration, bool stopOnComplete)
        {
            if (source == null)
            {
                return;
            }

            if (duration <= 0f)
            {
                source.volume = targetVolume;
                if (stopOnComplete)
                {
                    CompleteStop(handleId);
                }

                return;
            }

            for (var i = 0; i < fadeJobs.Count; i++)
            {
                if (fadeJobs[i].HandleId == handleId)
                {
                    var replace = fadeJobs[i];
                    replace.Source = source;
                    replace.StartVolume = startVolume;
                    replace.TargetVolume = targetVolume;
                    replace.Duration = duration;
                    replace.Elapsed = 0f;
                    replace.StopOnComplete = stopOnComplete;
                    fadeJobs[i] = replace;
                    return;
                }
            }

            fadeJobs.Add(new FadeJob
            {
                HandleId = handleId,
                Source = source,
                StartVolume = startVolume,
                TargetVolume = targetVolume,
                Duration = duration,
                Elapsed = 0f,
                StopOnComplete = stopOnComplete
            });
        }

        private void UpdateFadeJobs(float deltaTime)
        {
            if (fadeJobs.Count == 0)
            {
                return;
            }

            for (var i = fadeJobs.Count - 1; i >= 0; i--)
            {
                var job = fadeJobs[i];
                if (job.Source == null)
                {
                    fadeJobs.RemoveAt(i);
                    continue;
                }

                job.Elapsed += deltaTime;
                var t = job.Duration <= 0f ? 1f : Mathf.Clamp01(job.Elapsed / job.Duration);
                job.Source.volume = Mathf.Lerp(job.StartVolume, job.TargetVolume, t);

                if (t >= 1f)
                {
                    if (job.StopOnComplete)
                    {
                        // CompleteStop unregisters voice and removes all fade jobs for that handle.
                        CompleteStop(job.HandleId);
                        continue;
                    }

                    fadeJobs.RemoveAt(i);
                    continue;
                }

                fadeJobs[i] = job;
            }
        }

        private void CleanupFinishedMusic()
        {
            if (isPaused)
            {
                return;
            }

            if (musicA.HandleId >= 0 && !musicA.Source.isPlaying)
            {
                UnregisterVoice(musicA.HandleId, stopSource: false);
            }

            if (musicB.HandleId >= 0 && !musicB.Source.isPlaying)
            {
                UnregisterVoice(musicB.HandleId, stopSource: false);
            }
        }

        private void RemoveFadeJobs(int handleId)
        {
            for (var i = fadeJobs.Count - 1; i >= 0; i--)
            {
                if (fadeJobs[i].HandleId == handleId)
                {
                    fadeJobs.RemoveAt(i);
                }
            }
        }

        private MusicChannel GetActiveMusicChannel()
        {
            var aPlaying = musicA.Source != null && musicA.Source.isPlaying;
            var bPlaying = musicB.Source != null && musicB.Source.isPlaying;

            if (aPlaying && bPlaying)
            {
                return musicA.Source.volume >= musicB.Source.volume ? musicA : musicB;
            }

            if (aPlaying)
            {
                return musicA;
            }

            return bPlaying ? musicB : default;
        }

        private void SetMusicChannel(MusicChannel channel)
        {
            if (musicA.Source == channel.Source)
            {
                musicA = channel;
            }
            else if (musicB.Source == channel.Source)
            {
                musicB = channel;
            }
        }

        private void LogInfo(string message)
        {
            if (config != null && config.EnableDebugLogs)
            {
                Debug.Log($"[AudioManager] {message}");
            }
        }

        private void LogWarn(string message)
        {
            if (config != null && config.EnableDebugLogs)
            {
                Debug.LogWarning($"[AudioManager] {message}");
            }
        }
    }
}
