using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AudioManagement
{
    public sealed class AudioDemoSceneBootstrap : MonoBehaviour
    {
        private const string DemoSceneName = "AudioDemoScene";

        private AudioHandle musicHandle;
        private Transform movingEmitter;
        private AudioClip uiClip;
        private AudioClip sfxClip;
        private AudioClip musicClip;
        private float motionTime;
        private bool paused;

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
            uiClip = CreateTone("UI_Click", 1320f, 0.08f, 0.18f);
            sfxClip = CreateTone("SFX_Blip", 440f, 0.28f, 0.22f);
            musicClip = CreateMusicLoop("Music_Loop", 6f, 0.08f);

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

            musicHandle = manager.PlayMusic(musicClip, 0.4f, 0.4f);
            manager.PlayUI(uiClip, 0.8f, 1f);
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
                manager.PlayUI(uiClip);
            }

            if (WasPressedThisFrame(Key.Digit2, Key.Numpad2))
            {
                manager.PlaySFX(sfxClip, follow: movingEmitter);
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
                    musicHandle = manager.PlayMusic(musicClip, 0.35f, 0.35f);
                }
            }

            if (WasPressedThisFrame(Key.Digit4, Key.Numpad4))
            {
                paused = !paused;
                manager.PauseAll(paused);
            }
        }

        private void OnGUI()
        {
            const float x = 16f;
            var y = 16f;

            GUI.Label(new Rect(x, y, 520f, 24f), "Audio Demo Scene (keys: 1=UI, 2=SFX follow, 3=Music toggle, 4=Pause/Resume)");
            y += 28f;

            if (GUI.Button(new Rect(x, y, 180f, 28f), "Play UI (1)"))
            {
                AudioManager.Instance?.PlayUI(uiClip);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 180f, 28f), "Play 3D SFX (2)"))
            {
                AudioManager.Instance?.PlaySFX(sfxClip, follow: movingEmitter);
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 180f, 28f), "Toggle Music (3)"))
            {
                if (musicHandle.IsValid)
                {
                    AudioManager.Instance?.Stop(musicHandle, 0.35f);
                    musicHandle = AudioHandle.Invalid;
                }
                else
                {
                    musicHandle = AudioManager.Instance != null
                        ? AudioManager.Instance.PlayMusic(musicClip, 0.35f, 0.35f)
                        : AudioHandle.Invalid;
                }
            }

            y += 34f;
            if (GUI.Button(new Rect(x, y, 180f, 28f), paused ? "Resume (4)" : "Pause (4)"))
            {
                paused = !paused;
                AudioManager.Instance?.PauseAll(paused);
            }
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

        private static AudioClip CreateTone(string name, float frequency, float durationSeconds, float amplitude)
        {
            var sampleRate = 44100;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds * sampleRate));
            var data = new float[sampleCount];
            var attackSamples = Mathf.Max(1, Mathf.CeilToInt(sampleRate * 0.005f));
            var releaseSamples = Mathf.Max(1, Mathf.CeilToInt(sampleRate * 0.01f));

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = 1f;
                if (i < attackSamples)
                {
                    envelope = i / (float)attackSamples;
                }
                else if (i > sampleCount - releaseSamples)
                {
                    envelope = (sampleCount - i) / (float)releaseSamples;
                }

                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * Mathf.Clamp01(envelope);
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
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

        private static AudioClip CreateMusicLoop(string name, float durationSeconds, float amplitude)
        {
            var sampleRate = 44100;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds * sampleRate));
            var data = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var toneA = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.65f;
                var toneB = Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.35f;
                var toneC = Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.25f;
                data[i] = (toneA + toneB + toneC) * amplitude;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
