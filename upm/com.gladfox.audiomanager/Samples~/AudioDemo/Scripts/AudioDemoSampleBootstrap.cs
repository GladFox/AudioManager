using System.Collections;
using System.Collections.Generic;
using AudioManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AudioManagement.Samples
{
    public sealed class AudioDemoSampleBootstrap : MonoBehaviour
    {
        private const string DemoSceneName = "AudioDemoSampleScene";
        private const string IntroSoundId = "demo.ui.click";
        private const string Line1SoundId = "demo.ui.click";
        private const string Line2SoundId = "demo.sfx.moving";
        private const string Line3SoundId = "demo.ui.click";
        private const string MusicEventId = "demo.music.loop";

        private readonly List<string> dialogueSoundIds = new List<string>(4);

        private AudioHandle musicHandle;
        private Transform movingEmitter;
        private bool soundEnabled = true;
        private bool paused;
        private bool menuSnapshot;

        private bool overlayVisible;
        private float loadingProgress;
        private Coroutine preloadRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != DemoSceneName)
            {
                return;
            }

            if (FindFirstObjectByType<AudioDemoSampleBootstrap>() != null)
            {
                return;
            }

            var go = new GameObject("Audio Demo Sample Bootstrap");
            go.AddComponent<AudioDemoSampleBootstrap>();
        }

        private void Awake()
        {
            EnsureAudioManager();

            dialogueSoundIds.Clear();
            dialogueSoundIds.Add(IntroSoundId);
            dialogueSoundIds.Add(Line1SoundId);
            dialogueSoundIds.Add(Line2SoundId);
            dialogueSoundIds.Add(Line3SoundId);

            var followGo = new GameObject("Sample SFX Follow Target");
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
            StartPreload(playIntroOnComplete: true);
        }

        private void Update()
        {
            if (movingEmitter == null)
            {
                return;
            }

            var t = Time.unscaledTime;
            movingEmitter.position = new Vector3(Mathf.Sin(t * 1.1f) * 3f, 0f, 4f + Mathf.Cos(t * 1.1f) * 3f);
        }

        private void OnGUI()
        {
            const float x = 16f;
            var y = 16f;

            GUI.Label(new Rect(x, y, 980f, 24f), "UPM Sample: run setup first, then use buttons below");
            y += 28f;

            GUI.enabled = !overlayVisible;

            if (GUI.Button(new Rect(x, y, 280f, 28f), "Play Line 1"))
            {
                PlayLine(Line1SoundId);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), "Play Line 2 (3D)"))
            {
                PlayLine(Line2SoundId);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), "Play Line 3"))
            {
                PlayLine(Line3SoundId);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), musicHandle.IsValid ? "Stop Music" : "Play Music"))
            {
                ToggleMusic();
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), paused ? "Resume" : "Pause"))
            {
                paused = !paused;
                AudioManager.Instance?.PauseAll(paused);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), menuSnapshot ? "Gameplay Snapshot" : "Menu Snapshot"))
            {
                menuSnapshot = !menuSnapshot;
                AudioManager.Instance?.TransitionToSnapshot(menuSnapshot ? "Menu" : "Gameplay", 0.25f);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 280f, 28f), soundEnabled ? "Sound OFF" : "Sound ON"))
            {
                ToggleSound();
            }

            GUI.enabled = true;

            if (overlayVisible)
            {
                DrawOverlay();
            }
        }

        private void StartPreload(bool playIntroOnComplete)
        {
            if (preloadRoutine != null)
            {
                StopCoroutine(preloadRoutine);
            }

            preloadRoutine = StartCoroutine(PreloadRoutine(playIntroOnComplete));
        }

        private IEnumerator PreloadRoutine(bool playIntroOnComplete)
        {
            var manager = AudioManager.Instance;
            if (manager == null || !soundEnabled)
            {
                yield break;
            }

            overlayVisible = true;
            loadingProgress = 0f;

            var handle = manager.PreloadByIds(dialogueSoundIds);
            while (handle != null && !handle.IsDone)
            {
                loadingProgress = Mathf.Clamp01(handle.Progress);
                yield return null;
            }

            loadingProgress = 1f;
            yield return null;

            overlayVisible = false;
            preloadRoutine = null;

            if (playIntroOnComplete)
            {
                PlayLine(IntroSoundId);
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

                overlayVisible = false;
                loadingProgress = 0f;
                return;
            }

            StartPreload(playIntroOnComplete: false);
        }

        private void PlayLine(string id)
        {
            var manager = AudioManager.Instance;
            if (manager == null || !soundEnabled)
            {
                return;
            }

            if (id == Line2SoundId && movingEmitter != null)
            {
                manager.PlaySFX(id, follow: movingEmitter);
                return;
            }

            manager.PlayUI(id);
        }

        private void DrawOverlay()
        {
            var rect = new Rect(0f, 0f, Screen.width, Screen.height);
            var oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var percent = Mathf.RoundToInt(loadingProgress * 100f);
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            GUI.Label(rect, $"Loading Audio...\n{percent}%", style);
            GUI.color = oldColor;
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
