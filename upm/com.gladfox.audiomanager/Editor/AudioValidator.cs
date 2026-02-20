#if UNITY_EDITOR
using System.Collections.Generic;
using AudioManagement;
using UnityEditor;
using UnityEngine;

namespace AudioManagementEditor
{
    public static class AudioValidator
    {
        [MenuItem("Tools/Audio/Validate Sound Events")]
        public static void ValidateSoundEvents()
        {
            var config = LoadPrimaryConfig();
            var guids = AssetDatabase.FindAssets("t:SoundEvent");
            var idMap = new Dictionary<string, SoundEvent>();
            var requiredSnapshots = new[] { "Default", "Gameplay", "Menu", "Pause", "Muffled" };

            var errors = 0;
            var warnings = 0;

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var evt = AssetDatabase.LoadAssetAtPath<SoundEvent>(path);
                if (evt == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(evt.Id))
                {
                    errors++;
                    Debug.LogError($"[AudioValidator] SoundEvent without id: {path}", evt);
                }
                else if (idMap.TryGetValue(evt.Id, out var duplicate))
                {
                    errors++;
                    Debug.LogError($"[AudioValidator] Duplicate SoundEvent id '{evt.Id}'.\nFirst: {AssetDatabase.GetAssetPath(duplicate)}\nSecond: {path}", evt);
                }
                else
                {
                    idMap.Add(evt.Id, evt);
                }

                if (!HasAnyClip(evt))
                {
                    errors++;
                    Debug.LogError($"[AudioValidator] SoundEvent '{evt.name}' has no clips configured.", evt);
                }

                if (config != null && !config.TryGetMixerGroup(evt.MixerBus, out _))
                {
                    warnings++;
                    Debug.LogWarning($"[AudioValidator] SoundEvent '{evt.name}' uses bus '{evt.MixerBus}' without AudioMixerGroup binding in AudioConfig.", evt);
                }
            }

            if (config == null)
            {
                warnings++;
                Debug.LogWarning("[AudioValidator] AudioConfig not found. Bus routing checks were skipped.");
            }
            else
            {
                if (config.Mixer == null)
                {
                    errors++;
                    Debug.LogError("[AudioValidator] AudioConfig has no AudioMixer assigned.", config);
                }

                for (var i = 0; i < requiredSnapshots.Length; i++)
                {
                    if (!config.TryGetSnapshot(requiredSnapshots[i], out _, out _))
                    {
                        warnings++;
                        Debug.LogWarning($"[AudioValidator] AudioConfig is missing snapshot '{requiredSnapshots[i]}'.", config);
                    }
                }

                if (config.SoundEvents == null || config.SoundEvents.Length == 0)
                {
                    warnings++;
                    Debug.LogWarning("[AudioValidator] AudioConfig has empty SoundEvents catalog.", config);
                }
            }

            if (errors == 0 && warnings == 0)
            {
                Debug.Log("[AudioValidator] Validation passed with no issues.");
                return;
            }

            Debug.Log($"[AudioValidator] Validation complete. Errors={errors}, Warnings={warnings}, Checked={guids.Length}");
        }

        private static AudioConfig LoadPrimaryConfig()
        {
            var configGuids = AssetDatabase.FindAssets("t:AudioConfig");
            if (configGuids.Length == 0)
            {
                return null;
            }

            var configPath = AssetDatabase.GUIDToAssetPath(configGuids[0]);
            return AssetDatabase.LoadAssetAtPath<AudioConfig>(configPath);
        }

        private static bool HasAnyClip(SoundEvent evt)
        {
            var clips = evt.ClipReferences;
            if (clips != null)
            {
                for (var i = 0; i < clips.Length; i++)
                {
                    var reference = clips[i];
                    if (reference != null && !string.IsNullOrWhiteSpace(reference.AssetGUID))
                    {
                        return true;
                    }
                }
            }

            var weighted = evt.WeightedClipReferences;
            if (weighted != null)
            {
                for (var i = 0; i < weighted.Length; i++)
                {
                    var reference = weighted[i].Reference;
                    if (reference != null && !string.IsNullOrWhiteSpace(reference.AssetGUID) && weighted[i].Weight > 0f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
#endif
