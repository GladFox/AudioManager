using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AudioManagement
{
    public sealed class AudioDemoSceneBootstrap : MonoBehaviour
    {
        private const string DemoSceneName = "AudioDemoScene";
        private const string DialogueScopeId = "demo.dialogue";
        private const string DialoguePrefabResourcePath = "Audio/DemoDialoguePrefab";

        private const float PrefabLoadWeight = 0.35f;

        private AudioHandle musicHandle;
        private Transform movingEmitter;
        private float motionTime;
        private bool paused;
        private bool menuSnapshot;
        private bool soundEnabled = true;

        private bool dialogueScopeActive;
        private bool dialogueLoading;
        private Coroutine preloadRoutine;

        private GameObject dialoguePrefabAsset;
        private GameObject dialogueInstance;
        private AudioDemoDialoguePrefab dialogueData;

        private bool isLoadingOverlayVisible;
        private float loadingProgress01;
        private string loadingMessage = "Загрузка аудио...";

        private Font uiFont;
        private Text stateLabel;
        private Text musicButtonLabel;
        private Text pauseButtonLabel;
        private Text snapshotButtonLabel;
        private Text soundButtonLabel;
        private Text dialogueButtonLabel;
        private GameObject loadingOverlayRoot;
        private Text loadingOverlayText;

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
            EnsureEventSystem();
            ResolveUiFont();
            BuildUi();

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

            OpenDialogue(playIntroOnComplete: true);
            RefreshUiState();
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
                PlayDialogueLine(1);
            }

            if (WasPressedThisFrame(Key.Digit2, Key.Numpad2))
            {
                PlayDialogueLine(2);
            }

            if (WasPressedThisFrame(Key.Digit3, Key.Numpad3))
            {
                PlayDialogueLine(3);
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

            if (WasPressedThisFrame(Key.Digit8, Key.Numpad8))
            {
                ToggleDialogueScope();
            }

            RefreshUiState();
        }

        private void OnDestroy()
        {
            if (preloadRoutine != null)
            {
                StopCoroutine(preloadRoutine);
                preloadRoutine = null;
            }

            if (dialogueScopeActive)
            {
                AudioManager.Instance?.ReleaseScope(DialogueScopeId);
                dialogueScopeActive = false;
            }

            if (dialogueInstance != null)
            {
                Destroy(dialogueInstance);
                dialogueInstance = null;
            }

            dialoguePrefabAsset = null;
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
                CloseDialogue();
                return;
            }

            OpenDialogue(playIntroOnComplete: false);
        }

        private void ToggleMusic()
        {
            var manager = AudioManager.Instance;
            if (manager == null || !soundEnabled || dialogueData == null || dialogueData.Music == null)
            {
                return;
            }

            if (musicHandle.IsValid)
            {
                manager.Stop(musicHandle, 0.35f);
                musicHandle = AudioHandle.Invalid;
                return;
            }

            musicHandle = manager.PlayMusic(dialogueData.Music, 0.35f, 0.35f);
        }

        private void PlayIntro()
        {
            var manager = AudioManager.Instance;
            if (manager == null || !soundEnabled || dialogueData == null || dialogueData.Intro == null)
            {
                return;
            }

            manager.PlayUI(dialogueData.Intro);
        }

        private void PlayDialogueLine(int lineIndex)
        {
            var manager = AudioManager.Instance;
            if (manager == null || !soundEnabled || dialogueData == null)
            {
                return;
            }

            var evt = dialogueData.GetLineEvent(lineIndex);
            if (evt == null)
            {
                return;
            }

            if (lineIndex == 2 && dialogueData.Line2UsesFollowTarget && movingEmitter != null)
            {
                var handle = manager.PlaySFX(evt, follow: movingEmitter);
                if (handle.IsValid)
                {
                    handle.SetFollowTarget(movingEmitter);
                }

                return;
            }

            manager.PlayUI(evt);
        }

        private void ToggleDialogueScope()
        {
            if (dialogueScopeActive || dialogueLoading)
            {
                CloseDialogue();
                return;
            }

            OpenDialogue(playIntroOnComplete: false);
        }

        private void OpenDialogue(bool playIntroOnComplete)
        {
            if (!soundEnabled)
            {
                return;
            }

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

            dialogueLoading = true;
            SetLoadingOverlay(true, "Загрузка prefab диалога...", 0f);

            var marker = manager.CaptureDiscoveryMarker();

            if (dialoguePrefabAsset == null)
            {
                var prefabLoad = Resources.LoadAsync<GameObject>(DialoguePrefabResourcePath);
                while (!prefabLoad.isDone)
                {
                    SetLoadingOverlay(true, "Загрузка prefab диалога...", Mathf.Clamp01(prefabLoad.progress) * PrefabLoadWeight);
                    yield return null;
                }

                dialoguePrefabAsset = prefabLoad.asset as GameObject;
                if (dialoguePrefabAsset == null)
                {
                    Debug.LogError($"[AudioDemo] Dialogue prefab not found by path '{DialoguePrefabResourcePath}'.");
                    SetLoadingOverlay(false, string.Empty, 0f);
                    dialogueLoading = false;
                    preloadRoutine = null;
                    yield break;
                }
            }

            if (dialogueInstance != null)
            {
                Destroy(dialogueInstance);
            }

            dialogueInstance = Instantiate(dialoguePrefabAsset);
            dialogueInstance.name = "Demo Dialogue (Runtime)";
            dialogueData = dialogueInstance.GetComponent<AudioDemoDialoguePrefab>();
            if (dialogueData == null)
            {
                Debug.LogError("[AudioDemo] Loaded dialogue prefab does not contain AudioDemoDialoguePrefab component.");
                Destroy(dialogueInstance);
                dialogueInstance = null;
                SetLoadingOverlay(false, string.Empty, 0f);
                dialogueLoading = false;
                preloadRoutine = null;
                yield break;
            }

            var handle = manager.PreloadDiscoveredSince(marker, acquireScope: true, scopeId: DialogueScopeId);
            while (handle != null && !handle.IsDone)
            {
                var weighted = PrefabLoadWeight + Mathf.Clamp01(handle.Progress) * (1f - PrefabLoadWeight);
                SetLoadingOverlay(true, "Загрузка звуков диалога...", weighted);
                yield return null;
            }

            SetLoadingOverlay(true, "Звуки диалога готовы", 1f);
            yield return null;
            SetLoadingOverlay(false, string.Empty, 0f);

            dialogueScopeActive = true;
            dialogueLoading = false;
            preloadRoutine = null;

            if (playIntroOnComplete)
            {
                PlayIntro();
            }
        }

        private void CloseDialogue()
        {
            if (preloadRoutine != null)
            {
                StopCoroutine(preloadRoutine);
                preloadRoutine = null;
            }

            dialogueLoading = false;
            SetLoadingOverlay(false, string.Empty, 0f);

            var manager = AudioManager.Instance;
            if (manager != null)
            {
                StopDialogueEvents(manager);
                if (dialogueScopeActive)
                {
                    manager.ReleaseScope(DialogueScopeId);
                }
            }

            dialogueScopeActive = false;

            if (dialogueInstance != null)
            {
                Destroy(dialogueInstance);
                dialogueInstance = null;
            }

            dialogueData = null;

            // Keep loaded prefab cached. UnloadAsset is not valid for GameObject prefabs.
        }

        private void StopDialogueEvents(AudioManager manager)
        {
            if (musicHandle.IsValid)
            {
                manager.Stop(musicHandle, 0.15f);
                musicHandle = AudioHandle.Invalid;
            }

            StopEvent(manager, dialogueData?.Intro);
            StopEvent(manager, dialogueData?.Line1);
            StopEvent(manager, dialogueData?.Line2);
            StopEvent(manager, dialogueData?.Line3);
        }

        private static void StopEvent(AudioManager manager, SoundEvent evt)
        {
            if (evt == null || string.IsNullOrEmpty(evt.Id))
            {
                return;
            }

            manager.StopByEventId(evt.Id, 0.05f);
        }

        private void SetLoadingOverlay(bool visible, string message, float progress01)
        {
            isLoadingOverlayVisible = visible;
            loadingMessage = message;
            loadingProgress01 = Mathf.Clamp01(progress01);

            if (loadingOverlayRoot != null)
            {
                loadingOverlayRoot.SetActive(visible);
            }

            if (loadingOverlayText != null)
            {
                var percent = Mathf.RoundToInt(loadingProgress01 * 100f);
                loadingOverlayText.text = visible ? $"{loadingMessage}\n{percent}%" : string.Empty;
            }
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("Audio Demo UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = CreateRectTransform(
                "Panel",
                canvas.transform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(16f, -16f),
                new Vector2(430f, 430f));

            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.1f, 0.78f);

            CreateText(
                "Title",
                panel,
                "Addressables Demo (uGUI)\n1/2/3 lines, 4 music, 5 pause, 6 snapshot, 7 sound, 8 dialog",
                new Vector2(12f, -10f),
                new Vector2(406f, 56f),
                14,
                TextAnchor.UpperLeft,
                new Color(0.9f, 0.95f, 1f, 1f));

            stateLabel = CreateText(
                "State",
                panel,
                string.Empty,
                new Vector2(12f, -68f),
                new Vector2(406f, 56f),
                13,
                TextAnchor.UpperLeft,
                new Color(0.85f, 0.9f, 0.95f, 1f));

            var y = -132f;
            CreateButton(panel, "Play Line 1", y, () => PlayDialogueLine(1), out _);
            y -= 36f;
            CreateButton(panel, "Play Line 2 (3D)", y, () => PlayDialogueLine(2), out _);
            y -= 36f;
            CreateButton(panel, "Play Line 3", y, () => PlayDialogueLine(3), out _);
            y -= 36f;
            CreateButton(panel, "Replay Intro", y, PlayIntro, out _);
            y -= 36f;
            CreateButton(panel, "Play Music (4)", y, ToggleMusic, out musicButtonLabel);
            y -= 36f;
            CreateButton(panel, "Pause (5)", y, TogglePause, out pauseButtonLabel);
            y -= 36f;
            CreateButton(panel, "Menu Snapshot (6)", y, ToggleSnapshot, out snapshotButtonLabel);
            y -= 36f;
            CreateButton(panel, "Sound OFF (7)", y, ToggleSound, out soundButtonLabel);
            y -= 36f;
            CreateButton(panel, "Close Dialogue (8)", y, ToggleDialogueScope, out dialogueButtonLabel);

            BuildLoadingOverlay(canvas.transform);
        }

        private void TogglePause()
        {
            paused = !paused;
            AudioManager.Instance?.PauseAll(paused);
        }

        private void ToggleSnapshot()
        {
            menuSnapshot = !menuSnapshot;
            AudioManager.Instance?.TransitionToSnapshot(menuSnapshot ? "Menu" : "Gameplay", 0.25f);
        }

        private void BuildLoadingOverlay(Transform parent)
        {
            loadingOverlayRoot = new GameObject("Loading Overlay", typeof(RectTransform), typeof(Image));
            loadingOverlayRoot.transform.SetParent(parent, false);

            var rootRect = loadingOverlayRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var rootImage = loadingOverlayRoot.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.78f);

            var text = CreateText(
                "Loading Text",
                rootRect,
                string.Empty,
                new Vector2(0f, 0f),
                new Vector2(700f, 160f),
                28,
                TextAnchor.MiddleCenter,
                Color.white);

            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;

            loadingOverlayText = text;
            loadingOverlayRoot.SetActive(false);
        }

        private Button CreateButton(Transform parent, string text, float y, UnityEngine.Events.UnityAction onClick, out Text label)
        {
            var buttonRect = CreateRectTransform(
                text + " Button",
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(12f, y),
                new Vector2(406f, 30f));

            var image = buttonRect.gameObject.AddComponent<Image>();
            image.color = new Color(0.17f, 0.2f, 0.25f, 0.95f);

            var button = buttonRect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.86f, 0.95f, 1f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            label = CreateText(
                "Label",
                buttonRect,
                text,
                new Vector2(0f, 0f),
                new Vector2(406f, 30f),
                14,
                TextAnchor.MiddleCenter,
                Color.white);

            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;

            return button;
        }

        private Text CreateText(
            string name,
            Transform parent,
            string content,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            var rect = CreateRectTransform(
                name,
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                anchoredPosition,
                size);

            var text = rect.gameObject.AddComponent<Text>();
            text.font = uiFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRectTransform(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private void RefreshUiState()
        {
            if (stateLabel != null)
            {
                var dialogueStatus = dialogueScopeActive ? "active" : "inactive";
                var prefabStatus = dialogueData != null ? "loaded" : "not loaded";
                stateLabel.text = $"Dialogue prefab: {prefabStatus}\nScope: {dialogueStatus}, Sound: {(soundEnabled ? "on" : "off")}";
            }

            if (musicButtonLabel != null)
            {
                musicButtonLabel.text = musicHandle.IsValid ? "Stop Music (4)" : "Play Music (4)";
            }

            if (pauseButtonLabel != null)
            {
                pauseButtonLabel.text = paused ? "Resume (5)" : "Pause (5)";
            }

            if (snapshotButtonLabel != null)
            {
                snapshotButtonLabel.text = menuSnapshot ? "Gameplay Snapshot (6)" : "Menu Snapshot (6)";
            }

            if (soundButtonLabel != null)
            {
                soundButtonLabel.text = soundEnabled ? "Sound OFF (7)" : "Sound ON (7)";
            }

            if (dialogueButtonLabel != null)
            {
                dialogueButtonLabel.text = (dialogueScopeActive || dialogueLoading) ? "Close Dialogue (8)" : "Open Dialogue (8)";
            }

            if (loadingOverlayRoot != null && loadingOverlayRoot.activeSelf != isLoadingOverlayVisible)
            {
                loadingOverlayRoot.SetActive(isLoadingOverlayVisible);
            }

            if (loadingOverlayText != null && isLoadingOverlayVisible)
            {
                var percent = Mathf.RoundToInt(loadingProgress01 * 100f);
                loadingOverlayText.text = $"{loadingMessage}\n{percent}%";
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

        private void ResolveUiFont()
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemGo.GetComponent<EventSystem>().sendNavigationEvents = true;
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
