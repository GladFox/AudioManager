using UnityEngine;

namespace AudioManagement
{
    public sealed class AudioSceneEmitter : MonoBehaviour
    {
        [SerializeField] private SoundEvent soundEvent;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool followSelf = true;

        private AudioHandle handle;

        private void OnEnable()
        {
            if (!playOnEnable)
            {
                return;
            }

            Play();
        }

        private void OnDisable()
        {
            if (handle.IsValid)
            {
                handle.Stop(0.1f);
            }
        }

        [ContextMenu("Play")]
        public void Play()
        {
            var manager = AudioManager.Instance;
            if (manager == null || soundEvent == null)
            {
                return;
            }

            handle = manager.PlaySFX(soundEvent, transform.position, followSelf ? transform : null);
        }

        [ContextMenu("Stop")]
        public void Stop()
        {
            if (handle.IsValid)
            {
                handle.Stop(0.2f);
            }
        }
    }
}
