using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AudioManagement
{
    public sealed class AudioDemoSceneBootstrap : MonoBehaviour
    {
        private const string DemoSceneName = "AudioDemoScene";
        private const string UiEventId = "demo.ui.click";
        private const string SfxEventId = "demo.sfx.moving";
        private const string MusicEventId = "demo.music.loop";

        private AudioHandle musicHandle;
        private Transform movingEmitter;
        private float motionTime;
        private bool paused;
        private bool menuSnapshot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != DemoSceneName)
            {
                return;
            }

            if (FindFirstObjectByType<AudioDemoSceneBootstrap>() != null)
            {
                return;
            }

            var bootstrap = new GameObject("Audio Demo Bootstrap");
            bootstrap.AddComponent<AudioDemoSceneBootstrap>();
        }

        private void Awake()
        {
            EnsureAudioManager();

            var followGo = new GameObject("SFX Follow Target");
            movingEmitter = followGo.transform;
            movingEmitter.position = new Vector3(0f, 0f, 4f);
        }

        private void Start()
        {
            var manager = AudioManager.Instance;
            if (manager == null)
            {
                return;
            }

            musicHandle = manager.PlayMusic(MusicEventId);
            manager.PlayUI(UiEventId);
            manager.TransitionToSnapshot("Gameplay", 0.1f);
        }

        private void Update()
        {
            var manager = AudioManager.Instance;
            if (manager == null)
            {
                return;
            }

            motionTime += Time.unscaledDeltaTime;
            if (movingEmitter != null)
            {
                var x = Mathf.Sin(motionTime * 1.2f) * 3f;
                var z = 4f + Mathf.Cos(motionTime * 1.2f) * 3f;
                movingEmitter.position = new Vector3(x, 0f, z);
            }

            if (WasPressedThisFrame(Key.Digit1, Key.Numpad1))
            {
                manager.PlayUI(UiEventId);
            }

            if (WasPressedThisFrame(Key.Digit2, Key.Numpad2))
            {
                manager.PlaySFX(SfxEventId, movingEmitter.position);
                var handle = manager.PlaySFX(SfxEventId, follow: movingEmitter);
                if (handle.IsValid)
                {
                    handle.SetFollowTarget(movingEmitter);
                }
            }

            if (WasPressedThisFrame(Key.Digit3, Key.Numpad3))
            {
                if (musicHandle.IsValid)
                {
                    manager.Stop(musicHandle, 0.35f);
                    musicHandle = AudioHandle.Invalid;
                }
                else
                {
                    musicHandle = manager.PlayMusic(MusicEventId, 0.35f, 0.35f);
                }
            }

            if (WasPressedThisFrame(Key.Digit4, Key.Numpad4))
            {
                paused = !paused;
                manager.PauseAll(paused);
            }

            if (WasPressedThisFrame(Key.Digit5, Key.Numpad5))
            {
                menuSnapshot = !menuSnapshot;
                manager.TransitionToSnapshot(menuSnapshot ? "Menu" : "Gameplay", 0.25f);
            }

            if (WasPressedThisFrame(Key.Digit6, Key.Numpad6))
            {
                manager.TransitionToSnapshot("Muffled", 0.25f);
            }

            if (WasPressedThisFrame(Key.Digit7, Key.Numpad7))
            {
                manager.TransitionToSnapshot("Default", 0.25f);
                menuSnapshot = false;
            }
        }

        private void OnGUI()
        {
            const float x = 16f;
            var y = 16f;

            GUI.Label(new Rect(x, y, 800f, 24f), "Audio Demo (Input System): 1=UI, 2=3D SFX follow, 3=Music toggle, 4=Pause, 5=Menu snapshot, 6=Muffled, 7=Default");
            y += 28f;

            if (GUI.Button(new Rect(x, y, 220f, 28f), "Play UI (1)"))
            {
                AudioManager.Instance?.PlayUI(UiEventId);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 220f, 28f), "Play 3D SFX Follow (2)"))
            {
                var manager = AudioManager.Instance;
                if (manager != null)
                {
                    var handle = manager.PlaySFX(SfxEventId, follow: movingEmitter);
                    if (handle.IsValid)
                    {
                        handle.SetFollowTarget(movingEmitter);
                    }
                }
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 220f, 28f), "Toggle Music (3)"))
            {
                var manager = AudioManager.Instance;
                if (manager == null)
                {
                    return;
                }

                if (musicHandle.IsValid)
                {
                    manager.Stop(musicHandle, 0.35f);
                    musicHandle = AudioHandle.Invalid;
                }
                else
                {
                    musicHandle = manager.PlayMusic(MusicEventId, 0.35f, 0.35f);
                }
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 220f, 28f), paused ? "Resume (4)" : "Pause (4)"))
            {
                paused = !paused;
                AudioManager.Instance?.PauseAll(paused);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 220f, 28f), menuSnapshot ? "Gameplay Snapshot (5)" : "Menu Snapshot (5)"))
            {
                menuSnapshot = !menuSnapshot;
                AudioManager.Instance?.TransitionToSnapshot(menuSnapshot ? "Menu" : "Gameplay", 0.25f);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 220f, 28f), "Muffled Snapshot (6)"))
            {
                AudioManager.Instance?.TransitionToSnapshot("Muffled", 0.25f);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 220f, 28f), "Default Snapshot (7)"))
            {
                menuSnapshot = false;
                AudioManager.Instance?.TransitionToSnapshot("Default", 0.25f);
            }
        }

        private static bool WasPressedThisFrame(Key mainKey, Key altKey)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return keyboard[mainKey].wasPressedThisFrame || keyboard[altKey].wasPressedThisFrame;
        }

        private static void EnsureAudioManager()
        {
            if (AudioManager.Instance != null)
            {
                return;
            }

            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }
    }
}
