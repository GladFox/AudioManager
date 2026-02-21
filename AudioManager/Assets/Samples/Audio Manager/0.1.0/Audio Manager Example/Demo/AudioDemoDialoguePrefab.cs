using UnityEngine;

namespace AudioManagement
{
    public sealed class AudioDemoDialoguePrefab : MonoBehaviour
    {
        [Header("Dialogue Events")]
        [SerializeField] private SoundEvent intro;
        [SerializeField] private SoundEvent line1;
        [SerializeField] private SoundEvent line2;
        [SerializeField] private SoundEvent line3;

        [Header("Music")]
        [SerializeField] private SoundEvent music;

        [Header("Playback Flags")]
        [SerializeField] private bool line2UsesFollowTarget = true;

        public SoundEvent Intro => intro;
        public SoundEvent Line1 => line1;
        public SoundEvent Line2 => line2;
        public SoundEvent Line3 => line3;
        public SoundEvent Music => music;
        public bool Line2UsesFollowTarget => line2UsesFollowTarget;

        public SoundEvent GetLineEvent(int lineIndex)
        {
            return lineIndex switch
            {
                1 => line1,
                2 => line2,
                3 => line3,
                _ => null
            };
        }
    }
}
