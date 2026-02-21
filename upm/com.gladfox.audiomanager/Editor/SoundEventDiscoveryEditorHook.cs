#if UNITY_EDITOR
using AudioManagement;
using UnityEditor;

namespace AudioManagementEditor
{
    [InitializeOnLoad]
    public static class SoundEventDiscoveryEditorHook
    {
        static SoundEventDiscoveryEditorHook()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SoundEventDiscoveryRegistry.Reset();
            }
        }
    }
}
#endif
