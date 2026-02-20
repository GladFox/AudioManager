using UnityEngine;

namespace AudioManagement
{
    public readonly struct AudioHandle
    {
        private readonly AudioManager manager;
        private readonly int id;

        internal AudioHandle(AudioManager manager, int id)
        {
            this.manager = manager;
            this.id = id;
        }

        public int Id => id;
        public bool IsValid => manager != null && manager.IsHandleValid(id);

        public void Stop(float fadeOut = 0f)
        {
            if (manager != null)
            {
                manager.Stop(this, fadeOut);
            }
        }

        public void SetVolume(float volume01)
        {
            if (manager != null)
            {
                manager.SetHandleVolume(this, volume01);
            }
        }

        public void SetPitch(float pitch)
        {
            if (manager != null)
            {
                manager.SetHandlePitch(this, pitch);
            }
        }

        public void SetFollowTarget(Transform target)
        {
            if (manager != null)
            {
                manager.SetHandleFollowTarget(this, target);
            }
        }

        public static AudioHandle Invalid => new AudioHandle(null, -1);
        public override string ToString() => $"AudioHandle(id={id}, valid={IsValid})";
    }
}
