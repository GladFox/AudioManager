using System;
using UnityEngine;

namespace AudioManagement
{
    [CreateAssetMenu(menuName = "Audio/Sound Event", fileName = "SoundEvent")]
    public sealed class SoundEvent : ScriptableObject
    {
        [Serializable]
        public struct WeightedClip
        {
            public AudioClip Clip;
            [Min(0.001f)] public float Weight;
        }

        [Header("Identity")]
        [SerializeField] private string id = "sound.event.id";

        [Header("Content")]
        [SerializeField] private ClipSelectionMode clipSelection = ClipSelectionMode.Random;
        [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();
        [SerializeField] private WeightedClip[] weightedClips = Array.Empty<WeightedClip>();

        [Header("Routing")]
        [SerializeField] private AudioBus mixerBus = AudioBus.Sfx;

        [Header("Base Playback")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = new Vector2(1f, 1f);
        [Min(0f)]
        [SerializeField] private float randomStartOffsetMax = 0f;
        [SerializeField] private bool loop;

        [Header("Spatial")]
        [SerializeField] private SpatialMode spatialMode = SpatialMode.Auto;
        [Min(0f)]
        [SerializeField] private float minDistance = 1f;
        [Min(0.01f)]
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Control")]
        [Range(0, 256)]
        [SerializeField] private int priority = 128;
        [Min(1)]
        [SerializeField] private int maxInstances = 8;
        [Min(0f)]
        [SerializeField] private float cooldownSeconds = 0f;

        [Header("Flags")]
        [SerializeField] private bool duckSfxOnUi;
        [SerializeField] private bool bypassReverbZones;
        [SerializeField] private bool bypassEffects;

        public string Id => id;
        public ClipSelectionMode ClipSelection => clipSelection;
        public AudioClip[] Clips => clips;
        public WeightedClip[] WeightedClips => weightedClips;
        public AudioBus MixerBus => mixerBus;
        public float Volume => volume;
        public Vector2 PitchRange => pitchRange;
        public float RandomStartOffsetMax => randomStartOffsetMax;
        public bool Loop => loop;
        public SpatialMode SpatialMode => spatialMode;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public AudioRolloffMode RolloffMode => rolloffMode;
        public int Priority => priority;
        public int MaxInstances => maxInstances;
        public float CooldownSeconds => cooldownSeconds;
        public bool DuckSfxOnUi => duckSfxOnUi;
        public bool BypassReverbZones => bypassReverbZones;
        public bool BypassEffects => bypassEffects;

        private void OnValidate()
        {
            if (pitchRange.x <= 0f)
            {
                pitchRange.x = 0.01f;
            }

            if (pitchRange.y <= 0f)
            {
                pitchRange.y = 0.01f;
            }

            if (pitchRange.y < pitchRange.x)
            {
                pitchRange.y = pitchRange.x;
            }

            if (maxDistance < minDistance)
            {
                maxDistance = minDistance;
            }

            if (maxInstances < 1)
            {
                maxInstances = 1;
            }
        }
    }
}
