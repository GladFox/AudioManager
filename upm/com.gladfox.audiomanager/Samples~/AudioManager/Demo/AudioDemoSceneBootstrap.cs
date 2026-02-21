using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AudioManagement
{
    public sealed class AudioDemoSceneBootstrap : MonoBehaviour
    {
        private const string DemoSceneName = "AudioDemoScene";
        private const string IntroSoundId = "demo.ui.click";
        private const string Line1SoundId = "demo.ui.click";
        private const string Line2SoundId = "demo.sfx.moving";
        private const string Line3SoundId = "demo.ui.click";
        private const string MusicEventId = "demo.music.loop";

        private readonly List<string> dialogueSoundIds = new List<string>(5);

        private AudioHandle musicHandle;
        private Transform movingEmitter;
        private float motionTime;
        private bool paused;
        private bool menuSnapshot;
        private bool soundEnabled = true;

        private bool isLoadingOverlayVisible;
        private float loadingProgress01;
        private string loadingMessage = "Загрузка аудио...";
        private Coroutine preloadRoutine;

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

            dialogueSoundIds.Clear();
            dialogueSoundIds.Add(IntroSoundId);
            dialogueSoundIds.Add(Line1SoundId);
            dialogueSoundIds.Add(Line2SoundId);
            dialogueSoundIds.Add(Line3SoundId);
            dialogueSoundIds.Add(MusicEventId);

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

            manager.SetSoundEnabled(soundEnabled);
            manager.TransitionToSnapshot("Gameplay", 0.1f);

            StartDialoguePreload(playIntroOnComplete: true);
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
                PlayDialogueLine(Line1SoundId);
            }

            if (WasPressedThisFrame(Key.Digit2, Key.Numpad2))
            {
                PlayDialogueLine(Line2SoundId);
            }

            if (WasPressedThisFrame(Key.Digit3, Key.Numpad3))
            {
                PlayDialogueLine(Line3SoundId);
            }

            if (WasPressedThisFrame(Key.Digit4, Key.Numpad4))
            {
                ToggleMusic();
            }

            if (WasPressedThisFrame(Key.Digit5, Key.Numpad5))
            {
                paused = !paused;
                manager.PauseAll(paused);
            }

            if (WasPressedThisFrame(Key.Digit6, Key.Numpad6))
            {
                menuSnapshot = !menuSnapshot;
                manager.TransitionToSnapshot(menuSnapshot ? "Menu" : "Gameplay", 0.25f);
            }

            if (WasPressedThisFrame(Key.Digit7, Key.Numpad7))
            {
                ToggleSound();
            }
        }

        private void OnGUI()
        {
            const float x = 16f;
            var y = 16f;

            GUI.Label(new Rect(x, y, 980f, 24f), "Addressables Demo: 1/2/3=dialogue lines, 4=music toggle, 5=pause, 6=menu snapshot, 7=sound on/off");
            y += 28f;

            GUI.enabled = !isLoadingOverlayVisible;

            if (GUI.Button(new Rect(x, y, 280f, 28f), "Play Line 1"))
            {
                PlayDialogueLine(Line1SoundId);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), "Play Line 2 (3D)"))
            {
                PlayDialogueLine(Line2SoundId);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), "Play Line 3"))
            {
                PlayDialogueLine(Line3SoundId);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), "Replay Intro"))
            {
                PlayDialogueLine(IntroSoundId);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), musicHandle.IsValid ? "Stop Music (4)" : "Play Music (4)"))
            {
                ToggleMusic();
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), paused ? "Resume (5)" : "Pause (5)"))
            {
                paused = !paused;
                AudioManager.Instance?.PauseAll(paused);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), menuSnapshot ? "Gameplay Snapshot (6)" : "Menu Snapshot (6)"))
            {
                menuSnapshot = !menuSnapshot;
                AudioManager.Instance?.TransitionToSnapshot(menuSnapshot ? "Menu" : "Gameplay", 0.25f);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), soundEnabled ? "Sound OFF (7)" : "Sound ON (7)"))
            {
                ToggleSound();
            }

            GUI.enabled = true;

            if (isLoadingOverlayVisible)
            {
                DrawLoadingOverlay();
            }
        }

        private void StartDialoguePreload(bool playIntroOnComplete)
        {
            if (preloadRoutine != null)
            {
                StopCoroutine(preloadRoutine);
            }

            preloadRoutine = StartCoroutine(PreloadDialogueRoutine(playIntroOnComplete));
        }

        private IEnumerator PreloadDialogueRoutine(bool playIntroOnComplete)
        {
            var manager = AudioManager.Instance;
            if (manager == null || !soundEnabled)
            {
                yield break;
            }

            isLoadingOverlayVisible = true;
            loadingProgress01 = 0f;
            loadingMessage = "Загрузка звуков диалога...";

            var handle = manager.PreloadByIds(dialogueSoundIds);
            while (handle != null && !handle.IsDone)
            {
                loadingProgress01 = Mathf.Clamp01(handle.Progress);
                yield return null;
            }

            loadingProgress01 = 1f;
            yield return null;

            isLoadingOverlayVisible = false;
            preloadRoutine = null;

            if (playIntroOnComplete)
            {
                PlayDialogueLine(IntroSoundId);
            }
        }

        private void ToggleSound()
        {
            soundEnabled = !soundEnabled;
            var manager = AudioManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.SetSoundEnabled(soundEnabled);
            if (!soundEnabled)
            {
                if (preloadRoutine != null)
                {
                    StopCoroutine(preloadRoutine);
                    preloadRoutine = null;
                }

                isLoadingOverlayVisible = false;
                loadingProgress01 = 0f;
                return;
            }

            if (soundEnabled)
            {
                StartDialoguePreload(playIntroOnComplete: false);
            }
        }

        private void ToggleMusic()
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

        private void PlayDialogueLine(string soundId)
        {
            var manager = AudioManager.Instance;
            if (manager == null || !soundEnabled)
            {
                return;
            }

            if (soundId == Line2SoundId && movingEmitter != null)
            {
                var handle = manager.PlaySFX(soundId, follow: movingEmitter);
                if (handle.IsValid)
                {
                    handle.SetFollowTarget(movingEmitter);
                }

                return;
            }

            manager.PlayUI(soundId);
        }

        private void DrawLoadingOverlay()
        {
            var rect = new Rect(0f, 0f, Screen.width, Screen.height);
            var oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var percent = Mathf.RoundToInt(loadingProgress01 * 100f);
            var text = $"{loadingMessage}\n{percent}%";
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(rect, text, style);
            GUI.color = oldColor;
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
