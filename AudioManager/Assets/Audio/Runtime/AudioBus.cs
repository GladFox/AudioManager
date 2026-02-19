using System;

namespace AudioManagement
{
    public enum AudioBus
    {
        Master = 0,
        Music = 1,
        Sfx = 2,
        UI = 3,
        Ambience = 4,
        Voice = 5
    }

    public enum SpatialMode
    {
        Auto = 0,
        TwoD = 1,
        ThreeD = 2
    }

    public enum ClipSelectionMode
    {
        Random = 0,
        Sequence = 1,
        WeightedRandom = 2
    }

    public enum StealPolicy
    {
        StealLowestPriority = 0,
        StealOldest = 1,
        SkipIfFull = 2
    }

    [Serializable]
    public struct PoolSettings
    {
        public int InitialSize;
        public int MaxSize;
        public int ExpandStep;
        public StealPolicy StealPolicy;
        public float AutoReleaseCheckInterval;

        public PoolSettings(int initialSize, int maxSize, int expandStep, StealPolicy stealPolicy, float autoReleaseCheckInterval)
        {
            InitialSize = initialSize;
            MaxSize = maxSize;
            ExpandStep = expandStep;
            StealPolicy = stealPolicy;
            AutoReleaseCheckInterval = autoReleaseCheckInterval;
        }

        public void Normalize()
        {
            if (InitialSize < 0)
            {
                InitialSize = 0;
            }

            if (MaxSize < 1)
            {
                MaxSize = 1;
            }

            if (InitialSize > MaxSize)
            {
                InitialSize = MaxSize;
            }

            if (ExpandStep < 1)
            {
                ExpandStep = 1;
            }

            if (AutoReleaseCheckInterval <= 0f)
            {
                AutoReleaseCheckInterval = 0.1f;
            }
        }
    }
}
