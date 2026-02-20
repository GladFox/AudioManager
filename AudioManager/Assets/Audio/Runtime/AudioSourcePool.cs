using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioManagement
{
    public sealed class AudioSourcePool
    {
        public sealed class PooledSource
        {
            public AudioSource Source;
            public bool InUse;
            public double EndDspTime;
            public double StartDspTime;
            public int Priority;
            public SoundEvent CurrentEvent;
            public Transform FollowTarget;
            public int HandleId;
            public AudioBus Bus;
            public bool Looping;
        }

        private readonly List<PooledSource> sources;
        private readonly Transform root;
        private readonly bool is3DPool;
        private readonly bool debugLogs;
        private readonly Action<PooledSource> onReleased;
        private PoolSettings settings;
        private float elapsedSinceReleaseCheck;

        public AudioSourcePool(string name, Transform parent, bool is3DPool, PoolSettings settings, bool debugLogs, Action<PooledSource> onReleased)
        {
            this.is3DPool = is3DPool;
            this.debugLogs = debugLogs;
            this.onReleased = onReleased;
            this.settings = settings;
            this.settings.Normalize();

            sources = new List<PooledSource>(this.settings.MaxSize);

            var go = new GameObject(name);
            root = go.transform;
            root.SetParent(parent, false);

            Expand(this.settings.InitialSize);
        }

        public int TotalCount => sources.Count;

        public int InUseCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < sources.Count; i++)
                {
                    if (sources[i].InUse)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public IReadOnlyList<PooledSource> Sources => sources;

        public bool TryAcquire(int priority, out PooledSource result)
        {
            for (var i = 0; i < sources.Count; i++)
            {
                if (!sources[i].InUse)
                {
                    result = sources[i];
                    return true;
                }
            }

            if (sources.Count < settings.MaxSize)
            {
                var capacityBefore = sources.Count;
                var toAdd = Mathf.Min(settings.ExpandStep, settings.MaxSize - sources.Count);
                Expand(toAdd);
                if (capacityBefore < sources.Count)
                {
                    result = sources[capacityBefore];
                    return true;
                }
            }

            switch (settings.StealPolicy)
            {
                case StealPolicy.StealLowestPriority:
                    if (TryGetLowestPriorityInUse(out var lowestPrioritySource))
                    {
                        Release(lowestPrioritySource);
                        result = lowestPrioritySource;
                        return true;
                    }
                    break;
                case StealPolicy.StealOldest:
                    if (TryGetOldestInUse(out var oldestSource))
                    {
                        Release(oldestSource);
                        result = oldestSource;
                        return true;
                    }
                    break;
                case StealPolicy.SkipIfFull:
                    break;
            }

            result = null;
            return false;
        }

        public bool TryGetByHandle(int handleId, out PooledSource pooled)
        {
            for (var i = 0; i < sources.Count; i++)
            {
                var item = sources[i];
                if (item.InUse && item.HandleId == handleId)
                {
                    pooled = item;
                    return true;
                }
            }

            pooled = null;
            return false;
        }

        public void Tick(double dspTime, float deltaTime)
        {
            elapsedSinceReleaseCheck += deltaTime;
            if (elapsedSinceReleaseCheck < settings.AutoReleaseCheckInterval)
            {
                return;
            }

            elapsedSinceReleaseCheck = 0f;

            for (var i = 0; i < sources.Count; i++)
            {
                var pooled = sources[i];
                if (!pooled.InUse)
                {
                    continue;
                }

                if (pooled.FollowTarget != null)
                {
                    pooled.Source.transform.position = pooled.FollowTarget.position;
                }

                if (pooled.Looping)
                {
                    continue;
                }

                if (dspTime >= pooled.EndDspTime || !pooled.Source.isPlaying)
                {
                    Release(pooled);
                }
            }
        }

        public void Release(PooledSource pooled)
        {
            if (pooled == null || !pooled.InUse)
            {
                return;
            }

            var releasedHandleId = pooled.HandleId;
            var releasedEvent = pooled.CurrentEvent;
            var releasedBus = pooled.Bus;

            pooled.Source.Stop();
            pooled.Source.clip = null;
            pooled.Source.loop = false;
            pooled.Source.pitch = 1f;
            pooled.Source.volume = 1f;

            pooled.InUse = false;
            pooled.Priority = 128;
            pooled.CurrentEvent = null;
            pooled.FollowTarget = null;
            pooled.HandleId = -1;
            pooled.Bus = AudioBus.Sfx;
            pooled.EndDspTime = 0d;
            pooled.StartDspTime = 0d;
            pooled.Looping = false;

            // Keep handle/event metadata available for release callbacks.
            pooled.HandleId = releasedHandleId;
            pooled.CurrentEvent = releasedEvent;
            pooled.Bus = releasedBus;
            onReleased?.Invoke(pooled);

            pooled.HandleId = -1;
            pooled.CurrentEvent = null;
            pooled.Bus = AudioBus.Sfx;
        }

        private void Expand(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var go = new GameObject(is3DPool ? "AudioSource3D" : "AudioSource2D");
                var t = go.transform;
                t.SetParent(root, false);

                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = is3DPool ? 1f : 0f;

                var pooled = new PooledSource
                {
                    Source = source,
                    InUse = false,
                    EndDspTime = 0d,
                    StartDspTime = 0d,
                    Priority = 128,
                    CurrentEvent = null,
                    FollowTarget = null,
                    HandleId = -1,
                    Bus = AudioBus.Sfx,
                    Looping = false
                };

                sources.Add(pooled);
            }

            if (debugLogs)
            {
                Debug.Log($"[AudioSourcePool] Expanded {(is3DPool ? "3D" : "2D")} pool by {count}. Total={sources.Count}");
            }
        }

        private bool TryGetLowestPriorityInUse(out PooledSource pooled)
        {
            pooled = null;
            var found = false;
            var maxPriority = int.MinValue;

            for (var i = 0; i < sources.Count; i++)
            {
                var item = sources[i];
                if (!item.InUse)
                {
                    continue;
                }

                if (!found || item.Priority > maxPriority)
                {
                    found = true;
                    maxPriority = item.Priority;
                    pooled = item;
                }
            }

            return found;
        }

        private bool TryGetOldestInUse(out PooledSource pooled)
        {
            pooled = null;
            var found = false;
            var minDspTime = double.MaxValue;

            for (var i = 0; i < sources.Count; i++)
            {
                var item = sources[i];
                if (!item.InUse)
                {
                    continue;
                }

                if (!found || item.StartDspTime < minDspTime)
                {
                    found = true;
                    minDspTime = item.StartDspTime;
                    pooled = item;
                }
            }

            return found;
        }
    }
}
