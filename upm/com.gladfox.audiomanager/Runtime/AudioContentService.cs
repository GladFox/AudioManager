using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AudioManagement
{
    public sealed class AudioContentService
    {
        public readonly struct WeightedLoadedClip
        {
            public readonly AudioClip Clip;
            public readonly float Weight;

            public WeightedLoadedClip(AudioClip clip, float weight)
            {
                Clip = clip;
                Weight = weight;
            }
        }

        private enum ClipLoadState
        {
            Unloaded = 0,
            Loading = 1,
            Loaded = 2,
            Failed = 3
        }

        private sealed class ClipRecord
        {
            public string Guid;
            public AssetReferenceT<AudioClip> Reference;
            public AsyncOperationHandle<AudioClip> Handle;
            public AudioClip Clip;
            public ClipLoadState State;
            public int RefCount;
            public int InUseCount;
            public float ReleaseCandidateSince;
        }

        private readonly Dictionary<string, ClipRecord> recordsByGuid = new Dictionary<string, ClipRecord>(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> scopeGuids = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private readonly Dictionary<AudioClip, string> clipGuidByClip = new Dictionary<AudioClip, string>();
        private readonly HashSet<string> preloadDedup = new HashSet<string>(StringComparer.Ordinal);

        private bool enableLogs;

        public AudioContentService(bool enableLogs)
        {
            this.enableLogs = enableLogs;
        }

        public int LoadedClipCount
        {
            get
            {
                var count = 0;
                foreach (var kv in recordsByGuid)
                {
                    if (kv.Value.State == ClipLoadState.Loaded && kv.Value.Clip != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int LoadingClipCount
        {
            get
            {
                var count = 0;
                foreach (var kv in recordsByGuid)
                {
                    if (kv.Value.State == ClipLoadState.Loading)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int FailedClipCount
        {
            get
            {
                var count = 0;
                foreach (var kv in recordsByGuid)
                {
                    if (kv.Value.State == ClipLoadState.Failed)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int ScopeCount => scopeGuids.Count;

        public void SetLogsEnabled(bool enabled)
        {
            enableLogs = enabled;
        }

        public AudioLoadHandle PreloadEvents(IReadOnlyList<SoundEvent> events, bool acquireRef = false, string scopeId = null)
        {
            if (events == null || events.Count == 0)
            {
                return AudioLoadHandle.Completed();
            }

            var rawHandles = new List<AsyncOperationHandle>(32);
            preloadDedup.Clear();

            HashSet<string> scopeSet = null;
            if (!string.IsNullOrWhiteSpace(scopeId))
            {
                if (!scopeGuids.TryGetValue(scopeId, out scopeSet))
                {
                    scopeSet = new HashSet<string>(StringComparer.Ordinal);
                    scopeGuids.Add(scopeId, scopeSet);
                }
            }

            for (var i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                if (evt == null)
                {
                    continue;
                }

                CollectEventReferences(evt, preloadDedup, rawHandles, acquireRef, scopeSet);
            }

            if (rawHandles.Count == 0)
            {
                return AudioLoadHandle.Completed();
            }

            var group = Addressables.ResourceManager.CreateGenericGroupOperation(rawHandles);
            return AudioLoadHandle.FromOperations(rawHandles, group);
        }

        public AudioLoadHandle AcquireScope(string scopeId, IReadOnlyList<SoundEvent> events)
        {
            if (string.IsNullOrWhiteSpace(scopeId))
            {
                return AudioLoadHandle.Failed("scopeId is required.");
            }

            if (scopeGuids.ContainsKey(scopeId))
            {
                ReleaseScope(scopeId);
            }

            return PreloadEvents(events, acquireRef: true, scopeId: scopeId);
        }

        public void ReleaseScope(string scopeId)
        {
            if (string.IsNullOrWhiteSpace(scopeId))
            {
                return;
            }

            if (!scopeGuids.TryGetValue(scopeId, out var guids))
            {
                return;
            }

            foreach (var guid in guids)
            {
                if (!recordsByGuid.TryGetValue(guid, out var record))
                {
                    continue;
                }

                record.RefCount = Mathf.Max(0, record.RefCount - 1);
                if (record.RefCount == 0 && record.ReleaseCandidateSince < 0f)
                {
                    record.ReleaseCandidateSince = -1f;
                }
            }

            scopeGuids.Remove(scopeId);
        }

        public void ReleaseAllScopes()
        {
            var keys = new List<string>(scopeGuids.Keys);
            for (var i = 0; i < keys.Count; i++)
            {
                ReleaseScope(keys[i]);
            }
        }

        public void RequestUnloadEvents(IReadOnlyList<SoundEvent> events, bool immediate = false)
        {
            if (events == null || events.Count == 0)
            {
                return;
            }

            preloadDedup.Clear();
            for (var i = 0; i < events.Count; i++)
            {
                CollectEventGuids(events[i], preloadDedup);
            }

            foreach (var guid in preloadDedup)
            {
                if (!recordsByGuid.TryGetValue(guid, out var record))
                {
                    continue;
                }

                if (record.RefCount > 0 || record.InUseCount > 0)
                {
                    continue;
                }

                if (immediate)
                {
                    ReleaseRecord(record);
                }
                else if (record.ReleaseCandidateSince < 0f)
                {
                    record.ReleaseCandidateSince = 0f;
                }
            }
        }

        public void UnloadUnusedNow()
        {
            foreach (var kv in recordsByGuid)
            {
                var record = kv.Value;
                if (record.RefCount == 0 && record.InUseCount == 0)
                {
                    ReleaseRecord(record);
                }
            }
        }

        public bool IsEventReady(SoundEvent evt)
        {
            if (evt == null)
            {
                return false;
            }

            if (evt.ClipSelection == ClipSelectionMode.WeightedRandom && evt.WeightedClipReferences != null && evt.WeightedClipReferences.Length > 0)
            {
                var hasAny = false;
                for (var i = 0; i < evt.WeightedClipReferences.Length; i++)
                {
                    var reference = evt.WeightedClipReferences[i].Reference;
                    if (!TryGetGuid(reference, out var guid))
                    {
                        continue;
                    }

                    hasAny = true;
                    if (!recordsByGuid.TryGetValue(guid, out var record) || record.State != ClipLoadState.Loaded || record.Clip == null)
                    {
                        return false;
                    }
                }

                return hasAny;
            }

            if (evt.ClipReferences == null || evt.ClipReferences.Length == 0)
            {
                return false;
            }

            var hasStandard = false;
            for (var i = 0; i < evt.ClipReferences.Length; i++)
            {
                var reference = evt.ClipReferences[i];
                if (!TryGetGuid(reference, out var guid))
                {
                    continue;
                }

                hasStandard = true;
                if (!recordsByGuid.TryGetValue(guid, out var record) || record.State != ClipLoadState.Loaded || record.Clip == null)
                {
                    return false;
                }
            }

            return hasStandard;
        }

        public bool TryCollectLoadedClips(SoundEvent evt, List<AudioClip> clips, List<WeightedLoadedClip> weighted)
        {
            if (clips == null || weighted == null || evt == null)
            {
                return false;
            }

            clips.Clear();
            weighted.Clear();

            if (evt.ClipReferences != null)
            {
                for (var i = 0; i < evt.ClipReferences.Length; i++)
                {
                    if (!TryGetGuid(evt.ClipReferences[i], out var guid))
                    {
                        continue;
                    }

                    if (recordsByGuid.TryGetValue(guid, out var record) && record.State == ClipLoadState.Loaded && record.Clip != null)
                    {
                        clips.Add(record.Clip);
                    }
                }
            }

            if (evt.WeightedClipReferences != null)
            {
                for (var i = 0; i < evt.WeightedClipReferences.Length; i++)
                {
                    var item = evt.WeightedClipReferences[i];
                    if (!TryGetGuid(item.Reference, out var guid))
                    {
                        continue;
                    }

                    if (item.Weight <= 0f)
                    {
                        continue;
                    }

                    if (recordsByGuid.TryGetValue(guid, out var record) && record.State == ClipLoadState.Loaded && record.Clip != null)
                    {
                        weighted.Add(new WeightedLoadedClip(record.Clip, item.Weight));
                    }
                }
            }

            return clips.Count > 0 || weighted.Count > 0;
        }

        public void RegisterClipInUse(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (!clipGuidByClip.TryGetValue(clip, out var guid))
            {
                return;
            }

            if (!recordsByGuid.TryGetValue(guid, out var record))
            {
                return;
            }

            record.InUseCount++;
        }

        public void UnregisterClipInUse(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (!clipGuidByClip.TryGetValue(clip, out var guid))
            {
                return;
            }

            if (!recordsByGuid.TryGetValue(guid, out var record))
            {
                return;
            }

            record.InUseCount = Mathf.Max(0, record.InUseCount - 1);
            if (record.RefCount == 0 && record.InUseCount == 0 && record.ReleaseCandidateSince < 0f)
            {
                record.ReleaseCandidateSince = -1f;
            }
        }

        public void Tick(float realtime, float unloadDelaySeconds)
        {
            if (unloadDelaySeconds < 0f)
            {
                unloadDelaySeconds = 0f;
            }

            foreach (var kv in recordsByGuid)
            {
                var record = kv.Value;
                if (record.RefCount > 0 || record.InUseCount > 0)
                {
                    record.ReleaseCandidateSince = -1f;
                    continue;
                }

                if (record.State != ClipLoadState.Loaded || record.Clip == null)
                {
                    continue;
                }

                if (record.ReleaseCandidateSince < 0f)
                {
                    record.ReleaseCandidateSince = realtime;
                    continue;
                }

                if (realtime - record.ReleaseCandidateSince < unloadDelaySeconds)
                {
                    continue;
                }

                ReleaseRecord(record);
            }
        }

        public int FillLoadedClips(List<AudioClip> buffer)
        {
            if (buffer == null)
            {
                return 0;
            }

            buffer.Clear();
            foreach (var kv in recordsByGuid)
            {
                var record = kv.Value;
                if (record.State == ClipLoadState.Loaded && record.Clip != null)
                {
                    buffer.Add(record.Clip);
                }
            }

            return buffer.Count;
        }

        public void ForceUnloadAll()
        {
            foreach (var kv in recordsByGuid)
            {
                ReleaseRecord(kv.Value);
            }

            scopeGuids.Clear();
            clipGuidByClip.Clear();
        }

        private void CollectEventReferences(SoundEvent evt, HashSet<string> dedup, List<AsyncOperationHandle> rawHandles, bool acquireRef, HashSet<string> scopeSet)
        {
            if (evt.ClipReferences != null)
            {
                for (var i = 0; i < evt.ClipReferences.Length; i++)
                {
                    var reference = evt.ClipReferences[i];
                    if (!TryGetGuid(reference, out var guid))
                    {
                        continue;
                    }

                    RegisterReference(guid, reference, dedup, rawHandles, acquireRef, scopeSet);
                }
            }

            if (evt.WeightedClipReferences != null)
            {
                for (var i = 0; i < evt.WeightedClipReferences.Length; i++)
                {
                    var reference = evt.WeightedClipReferences[i].Reference;
                    if (!TryGetGuid(reference, out var guid))
                    {
                        continue;
                    }

                    RegisterReference(guid, reference, dedup, rawHandles, acquireRef, scopeSet);
                }
            }
        }

        private static void CollectEventGuids(SoundEvent evt, HashSet<string> guids)
        {
            if (evt == null || guids == null)
            {
                return;
            }

            if (evt.ClipReferences != null)
            {
                for (var i = 0; i < evt.ClipReferences.Length; i++)
                {
                    if (TryGetGuid(evt.ClipReferences[i], out var guid))
                    {
                        guids.Add(guid);
                    }
                }
            }

            if (evt.WeightedClipReferences != null)
            {
                for (var i = 0; i < evt.WeightedClipReferences.Length; i++)
                {
                    if (TryGetGuid(evt.WeightedClipReferences[i].Reference, out var guid))
                    {
                        guids.Add(guid);
                    }
                }
            }
        }

        private void RegisterReference(string guid, AssetReferenceT<AudioClip> reference, HashSet<string> dedup, List<AsyncOperationHandle> rawHandles, bool acquireRef, HashSet<string> scopeSet)
        {
            if (!recordsByGuid.TryGetValue(guid, out var record))
            {
                record = new ClipRecord
                {
                    Guid = guid,
                    Reference = reference,
                    State = ClipLoadState.Unloaded,
                    RefCount = 0,
                    InUseCount = 0,
                    ReleaseCandidateSince = -1f
                };
                recordsByGuid.Add(guid, record);
            }
            else if (record.Reference == null)
            {
                record.Reference = reference;
            }

            var isFirstInBatch = dedup.Add(guid);
            var addedToScope = scopeSet != null && scopeSet.Add(guid);

            if (acquireRef)
            {
                var shouldAcquireRef = scopeSet != null ? addedToScope : isFirstInBatch;
                if (shouldAcquireRef)
                {
                    record.RefCount++;
                    record.ReleaseCandidateSince = -1f;
                }
            }

            if (!isFirstInBatch)
            {
                return;
            }

            if (record.State == ClipLoadState.Loaded && record.Clip != null)
            {
                return;
            }

            var handle = EnsureLoadStarted(record);
            if (handle.IsValid())
            {
                rawHandles.Add(handle);
            }
        }

        private AsyncOperationHandle EnsureLoadStarted(ClipRecord record)
        {
            if (record.State == ClipLoadState.Loading && record.Handle.IsValid())
            {
                return record.Handle;
            }

            if (record.State == ClipLoadState.Loaded && record.Clip != null)
            {
                return default;
            }

            if (record.Reference == null)
            {
                record.State = ClipLoadState.Failed;
                return default;
            }

            if (record.State == ClipLoadState.Failed && record.Handle.IsValid())
            {
                try
                {
                    Addressables.Release(record.Handle);
                }
                catch (Exception ex)
                {
                    LogWarn($"Release failed while retrying load for {record.Guid}: {ex.Message}");
                }
            }

            record.State = ClipLoadState.Loading;
            record.ReleaseCandidateSince = -1f;
            record.Handle = record.Reference.LoadAssetAsync();
            record.Handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
                {
                    record.Clip = op.Result;
                    record.State = ClipLoadState.Loaded;
                    clipGuidByClip[op.Result] = record.Guid;
                    LogInfo($"Loaded clip {record.Guid}");
                    return;
                }

                record.State = ClipLoadState.Failed;
                LogWarn($"Failed to load clip {record.Guid}");
            };

            return record.Handle;
        }

        private void ReleaseRecord(ClipRecord record)
        {
            if (record == null || record.Reference == null)
            {
                return;
            }

            if (record.InUseCount > 0)
            {
                return;
            }

            try
            {
                if (record.Handle.IsValid() && record.State == ClipLoadState.Loaded && record.Clip != null)
                {
                    clipGuidByClip.Remove(record.Clip);
                    record.Reference.ReleaseAsset();
                }
                else if (record.Handle.IsValid() && record.State != ClipLoadState.Unloaded)
                {
                    Addressables.Release(record.Handle);
                }
            }
            catch (Exception ex)
            {
                LogWarn($"Release failed for {record.Guid}: {ex.Message}");
            }

            record.Clip = null;
            record.Handle = default;
            record.State = ClipLoadState.Unloaded;
            record.ReleaseCandidateSince = -1f;
            LogInfo($"Released clip {record.Guid}");
        }

        private static bool TryGetGuid(AssetReference reference, out string guid)
        {
            guid = null;
            if (reference == null)
            {
                return false;
            }

            guid = reference.AssetGUID;
            return !string.IsNullOrWhiteSpace(guid);
        }

        private void LogInfo(string message)
        {
            if (enableLogs)
            {
                Debug.Log($"[AudioContentService] {message}");
            }
        }

        private void LogWarn(string message)
        {
            if (enableLogs)
            {
                Debug.LogWarning($"[AudioContentService] {message}");
            }
        }
    }
}
