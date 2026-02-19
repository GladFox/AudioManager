#if UNITY_EDITOR
using System.Collections.Generic;
using AudioManagement;
using UnityEditor;
using UnityEngine;

namespace AudioManagementEditor
{
    public sealed class AudioDebuggerWindow : EditorWindow
    {
        private readonly List<AudioManager.DebugVoiceInfo> voices = new List<AudioManager.DebugVoiceInfo>(64);
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
    }
}
#endif
