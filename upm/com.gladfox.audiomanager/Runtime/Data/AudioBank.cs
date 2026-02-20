using System;
using UnityEngine;

namespace AudioManagement
{
    [CreateAssetMenu(menuName = "Audio/Audio Bank", fileName = "AudioBank")]
    public sealed class AudioBank : ScriptableObject
    {
        [SerializeField] private string bankId = "audio.bank.id";
        [SerializeField] private SoundEvent[] events = Array.Empty<SoundEvent>();
        [SerializeField] private bool loadWhenSoundEnabled = true;
        [SerializeField] private bool loadWhenMusicEnabled = true;

        public string BankId => bankId;
        public SoundEvent[] Events => events;
        public bool LoadWhenSoundEnabled => loadWhenSoundEnabled;
        public bool LoadWhenMusicEnabled => loadWhenMusicEnabled;
    }
}
