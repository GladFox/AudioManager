using System.Collections.Generic;
using UnityEngine;

namespace AudioManagement
{
    public static class SoundEventDiscoveryRegistry
    {
        private static readonly HashSet<SoundEvent> discoveredEvents = new HashSet<SoundEvent>();
        private static readonly Dictionary<SoundEvent, int> discoveredRevisionByEvent = new Dictionary<SoundEvent, int>();
        private static readonly List<SoundEvent> removeScratch = new List<SoundEvent>(32);
        private static bool hydratedFromLoadedObjects;
        private static int currentRevision;

        public static int Count
        {
            get
            {
                PruneNulls();
                return discoveredEvents.Count;
            }
        }

        public static int CurrentRevision => currentRevision;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            Reset();
        }

        public static void Reset()
        {
            discoveredEvents.Clear();
            discoveredRevisionByEvent.Clear();
            hydratedFromLoadedObjects = false;
            currentRevision = 0;
        }

        public static int CaptureMarker()
        {
            return currentRevision;
        }

        public static void Register(SoundEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (!discoveredEvents.Add(evt))
            {
                return;
            }

            currentRevision++;
            discoveredRevisionByEvent[evt] = currentRevision;
        }

        public static void Unregister(SoundEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (!discoveredEvents.Remove(evt))
            {
                return;
            }

            discoveredRevisionByEvent.Remove(evt);
            currentRevision++;
        }

        public static int FillAll(List<SoundEvent> buffer)
        {
            return FillSince(-1, buffer);
        }

        public static int FillSince(int marker, List<SoundEvent> buffer)
        {
            if (buffer == null)
            {
                return 0;
            }

            buffer.Clear();
            EnsureHydratedFromLoadedObjects();
            PruneNulls();

            foreach (var evt in discoveredEvents)
            {
                if (evt == null)
                {
                    continue;
                }

                if (marker >= 0 && discoveredRevisionByEvent.TryGetValue(evt, out var revision) && revision <= marker)
                {
                    continue;
                }

                buffer.Add(evt);
            }

            return buffer.Count;
        }

        private static void EnsureHydratedFromLoadedObjects()
        {
            if (hydratedFromLoadedObjects && discoveredEvents.Count > 0)
            {
                return;
            }

            hydratedFromLoadedObjects = true;
            var loadedEvents = Resources.FindObjectsOfTypeAll<SoundEvent>();
            for (var i = 0; i < loadedEvents.Length; i++)
            {
                Register(loadedEvents[i]);
            }
        }

        private static void PruneNulls()
        {
            if (discoveredEvents.Count == 0)
            {
                return;
            }

            removeScratch.Clear();
            foreach (var evt in discoveredEvents)
            {
                if (evt == null)
                {
                    removeScratch.Add(evt);
                }
            }

            if (removeScratch.Count == 0)
            {
                return;
            }

            for (var i = 0; i < removeScratch.Count; i++)
            {
                var evt = removeScratch[i];
                discoveredEvents.Remove(evt);
                discoveredRevisionByEvent.Remove(evt);
            }

            currentRevision++;
            removeScratch.Clear();
        }
    }
}
