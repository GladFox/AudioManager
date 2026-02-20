#if UNITY_EDITOR
using System.Collections.Generic;
using AudioManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace AudioManagementEditor
{
    public sealed class AudioDebuggerWindow : EditorWindow
    {
        private readonly List<AudioManager.DebugVoiceInfo> voices = new List<AudioManager.DebugVoiceInfo>(64);
        private readonly List<AudioClip> allDebugClips = new List<AudioClip>(256);
        private readonly List<AudioClip> activeDebugClips = new List<AudioClip>(128);
        private Vector2 scroll;

        [MenuItem("Tools/Audio/Debugger")]
        private static void Open()
        {
            GetWindow<AudioDebuggerWindow>("Audio Debugger");
        }

        private void OnEnable()
        {
            EditorApplication.update += TickRepaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= TickRepaint;
        }

        private void TickRepaint()
        {
            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Runtime Audio Diagnostics", EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to inspect AudioManager runtime state.", MessageType.Info);
                return;
            }

            var manager = AudioManager.Instance;
            if (manager == null)
            {
                EditorGUILayout.HelpBox("AudioManager instance not found in scene.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pools", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("2D In Use", manager.Pool2DInUse.ToString());
            EditorGUILayout.LabelField("2D Total", manager.Pool2DTotal.ToString());
            EditorGUILayout.LabelField("3D In Use", manager.Pool3DInUse.ToString());
            EditorGUILayout.LabelField("3D Total", manager.Pool3DTotal.ToString());
            EditorGUILayout.LabelField("Active Voices", manager.ActiveVoiceCount.ToString());
            EditorGUILayout.LabelField("Sound Enabled", manager.SoundEnabled.ToString());
            EditorGUILayout.LabelField("Music Enabled", manager.MusicEnabled.ToString());

            manager.GetDebugAudioClips(allDebugClips, includeCatalog: true, includeActiveVoices: true);
            manager.GetDebugAudioClips(activeDebugClips, includeCatalog: false, includeActiveVoices: true);
            var totalAudioBytes = CalculateRuntimeClipMemory(allDebugClips);
            var activeAudioBytes = CalculateRuntimeClipMemory(activeDebugClips);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio Memory", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Loaded Clips (unique)", allDebugClips.Count.ToString());
            EditorGUILayout.LabelField("Playing Clips (unique)", activeDebugClips.Count.ToString());
            EditorGUILayout.LabelField("Loaded Clips Memory", EditorUtility.FormatBytes(totalAudioBytes));
            EditorGUILayout.LabelField("Playing Clips Memory", EditorUtility.FormatBytes(activeAudioBytes));
            EditorGUILayout.LabelField("Addressables Loaded", manager.LoadedAddressableClipCount.ToString());
            EditorGUILayout.LabelField("Addressables Loading", manager.LoadingAddressableClipCount.ToString());
            EditorGUILayout.LabelField("Addressables Failed", manager.FailedAddressableClipCount.ToString());
            EditorGUILayout.LabelField("Audio Scopes", manager.ActiveAudioScopeCount.ToString());

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pause SFX/Music"))
                {
                    manager.PauseAll(true);
                }

                if (GUILayout.Button("Resume SFX/Music"))
                {
                    manager.PauseAll(false);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sound OFF"))
                {
                    manager.SetSoundEnabled(false);
                }

                if (GUILayout.Button("Sound ON"))
                {
                    manager.SetSoundEnabled(true);
                }
            }

            manager.GetDebugVoices(voices);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Now Playing", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (voices.Count == 0)
            {
                EditorGUILayout.LabelField("No active voices.");
            }
            else
            {
                for (var i = 0; i < voices.Count; i++)
                {
                    var voice = voices[i];
                    EditorGUILayout.LabelField($"#{voice.HandleId} | {voice.Bus} | {(voice.IsMusic ? "Music" : "Pool")} | Playing={voice.IsPlaying} | Event={voice.EventId}");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static long CalculateRuntimeClipMemory(List<AudioClip> clips)
        {
            if (clips == null)
            {
                return 0L;
            }

            long total = 0L;
            for (var i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                if (clip != null)
                {
                    total += Profiler.GetRuntimeMemorySizeLong(clip);
                }
            }

            return total;
        }
    }
}
#endif
