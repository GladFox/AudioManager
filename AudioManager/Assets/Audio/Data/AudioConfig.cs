using System;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioManagement
{
    [CreateAssetMenu(menuName = "Audio/Audio Config", fileName = "AudioConfig")]
    public sealed class AudioConfig : ScriptableObject
    {
        [Serializable]
        public struct MixerGroupBinding
        {
            public AudioBus Bus;
            public AudioMixerGroup Group;
        }

        [Serializable]
        public struct SnapshotBinding
        {
            public string Name;
            public AudioMixerSnapshot Snapshot;
            public int Priority;
        }

        [Serializable]
        public struct ExposedVolumeParams
        {
            public string Master;
            public string Music;
            public string Sfx;
            public string UI;
            public string Ambience;
            public string Voice;
        }

        [Serializable]
        public struct DefaultVolumes01
        {
            [Range(0f, 1f)] public float Master;
            [Range(0f, 1f)] public float Music;
            [Range(0f, 1f)] public float Sfx;
            [Range(0f, 1f)] public float UI;
            [Range(0f, 1f)] public float Ambience;
            [Range(0f, 1f)] public float Voice;
        }

        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private MixerGroupBinding[] mixerGroups = Array.Empty<MixerGroupBinding>();
        [SerializeField] private SnapshotBinding[] snapshots = Array.Empty<SnapshotBinding>();

        [Header("Exposed Params")]
        [SerializeField] private ExposedVolumeParams exposedVolumeParams = new ExposedVolumeParams
        {
            Master = "MasterVolume",
            Music = "MusicVolume",
            Sfx = "SFXVolume",
            UI = "UIVolume",
            Ambience = "AmbienceVolume",
            Voice = "VoiceVolume"
        };

        [Header("Default Volumes (0..1)")]
        [SerializeField] private DefaultVolumes01 defaultVolumes = new DefaultVolumes01
        {
            Master = 1f,
            Music = 1f,
            Sfx = 1f,
            UI = 1f,
            Ambience = 1f,
            Voice = 1f
        };

        [Header("Pool Settings")]
        [SerializeField] private PoolSettings pool2D = new PoolSettings(16, 64, 4, StealPolicy.StealLowestPriority, 0.1f);
        [SerializeField] private PoolSettings pool3D = new PoolSettings(16, 64, 4, StealPolicy.StealLowestPriority, 0.1f);

        [Header("Catalog")]
        [SerializeField] private SoundEvent[] soundEvents = Array.Empty<SoundEvent>();

        [Header("Runtime")]
        [SerializeField] private bool enableDebugLogs;
        [SerializeField] private bool pauseSfxAndMusicOnFocusLost = true;
        [SerializeField] private bool pauseSfxAndMusicOnApplicationPause = true;
        [SerializeField] private bool uiAlwaysUnscaled = true;
        [SerializeField] private float minDb = -80f;
        [SerializeField] private float maxDb = 0f;

        public AudioMixer Mixer => mixer;
        public MixerGroupBinding[] MixerGroups => mixerGroups;
        public SnapshotBinding[] Snapshots => snapshots;
        public ExposedVolumeParams ExposedParams => exposedVolumeParams;
        public DefaultVolumes01 DefaultVolumes => defaultVolumes;
        public PoolSettings Pool2D => pool2D;
        public PoolSettings Pool3D => pool3D;
        public SoundEvent[] SoundEvents => soundEvents;
        public bool EnableDebugLogs => enableDebugLogs;
        public bool PauseSfxAndMusicOnFocusLost => pauseSfxAndMusicOnFocusLost;
        public bool PauseSfxAndMusicOnApplicationPause => pauseSfxAndMusicOnApplicationPause;
        public bool UIAlwaysUnscaled => uiAlwaysUnscaled;
        public float MinDb => minDb;
        public float MaxDb => maxDb;

        public static AudioConfig CreateRuntimeDefaults()
        {
            var instance = CreateInstance<AudioConfig>();
            instance.hideFlags = HideFlags.DontSave;

            instance.mixer = null;
            instance.mixerGroups = Array.Empty<MixerGroupBinding>();
            instance.snapshots = Array.Empty<SnapshotBinding>();
            instance.soundEvents = Array.Empty<SoundEvent>();

            instance.exposedVolumeParams = new ExposedVolumeParams
            {
                Master = "MasterVolume",
                Music = "MusicVolume",
                Sfx = "SFXVolume",
                UI = "UIVolume",
                Ambience = "AmbienceVolume",
                Voice = "VoiceVolume"
            };

            instance.defaultVolumes = new DefaultVolumes01
            {
                Master = 1f,
                Music = 1f,
                Sfx = 1f,
                UI = 1f,
                Ambience = 1f,
                Voice = 1f
            };

            instance.pool2D = new PoolSettings(16, 64, 4, StealPolicy.StealLowestPriority, 0.1f);
            instance.pool3D = new PoolSettings(16, 64, 4, StealPolicy.StealLowestPriority, 0.1f);
            instance.pool2D.Normalize();
            instance.pool3D.Normalize();

            instance.enableDebugLogs = false;
            instance.pauseSfxAndMusicOnFocusLost = true;
            instance.pauseSfxAndMusicOnApplicationPause = true;
            instance.uiAlwaysUnscaled = true;
            instance.minDb = -80f;
            instance.maxDb = 0f;

            return instance;
        }

        public bool TryGetMixerGroup(AudioBus bus, out AudioMixerGroup group)
        {
            for (var i = 0; i < mixerGroups.Length; i++)
            {
                if (mixerGroups[i].Bus == bus && mixerGroups[i].Group != null)
                {
                    group = mixerGroups[i].Group;
                    return true;
                }
            }

            group = null;
            return false;
        }

        public bool TryGetSnapshot(string name, out AudioMixerSnapshot snapshot, out int priority)
        {
            for (var i = 0; i < snapshots.Length; i++)
            {
                var binding = snapshots[i];
                if (string.Equals(binding.Name, name, StringComparison.OrdinalIgnoreCase) && binding.Snapshot != null)
                {
                    snapshot = binding.Snapshot;
                    priority = binding.Priority;
                    return true;
                }
            }

            snapshot = null;
            priority = 0;
            return false;
        }

        public bool TryGetVolumeParam(AudioBus bus, out string param)
        {
            param = bus switch
            {
                AudioBus.Master => exposedVolumeParams.Master,
                AudioBus.Music => exposedVolumeParams.Music,
                AudioBus.Sfx => exposedVolumeParams.Sfx,
                AudioBus.UI => exposedVolumeParams.UI,
                AudioBus.Ambience => exposedVolumeParams.Ambience,
                AudioBus.Voice => exposedVolumeParams.Voice,
                _ => null
            };

            return !string.IsNullOrWhiteSpace(param);
        }

        public float GetDefaultVolume01(AudioBus bus)
        {
            return bus switch
            {
                AudioBus.Master => defaultVolumes.Master,
                AudioBus.Music => defaultVolumes.Music,
                AudioBus.Sfx => defaultVolumes.Sfx,
                AudioBus.UI => defaultVolumes.UI,
                AudioBus.Ambience => defaultVolumes.Ambience,
                AudioBus.Voice => defaultVolumes.Voice,
                _ => 1f
            };
        }

        private void OnValidate()
        {
            pool2D.Normalize();
            pool3D.Normalize();

            if (minDb > maxDb)
            {
                var tmp = minDb;
                minDb = maxDb;
                maxDb = tmp;
            }
        }
    }
}
