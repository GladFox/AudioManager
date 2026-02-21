using System.Collections;
using System.Collections.Generic;
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

        private AudioHandle musicHandle;
        private Transform movingEmitter;
        private float motionTime;
        private bool paused;
        private bool menuSnapshot;
        private bool soundEnabled = true;

        private bool dialogueScopeActive;
        private bool dialogueScopeHeld;
        private bool dialogueLoading;
        private Coroutine preloadRoutine;
        private readonly List<string> dialogueScopeIdScratch = new List<string>(8);

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
        private Transform uiCanvasTransform;

        private GameObject dialogueWindowRoot;
        private Text dialogueWindowInfoLabel;
        private Text dialogueWindowMusicLabel;

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
                TogglePause();
            }

            if (WasPressedThisFrame(Key.Digit6, Key.Numpad6))
            {
                ToggleSnapshot();
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

            var manager = AudioManager.Instance;
            if (manager != null && dialogueScopeHeld)
            {
                manager.ReleaseScope(DialogueScopeId);
                dialogueScopeHeld = false;
            }

            if (dialogueInstance != null)
            {
                Destroy(dialogueInstance);
                dialogueInstance = null;
            }

            DestroyDialogueWindow();
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

            var prefab = Resources.Load<GameObject>(DialoguePrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[AudioDemo] Dialogue prefab not found by path '{DialoguePrefabResourcePath}'.");
                dialogueLoading = false;
                SetLoadingOverlay(false, string.Empty, 0f);
                preloadRoutine = null;
                yield break;
            }

            SetLoadingOverlay(true, "Создание диалога...", 0.25f);
            yield return null;

            if (dialogueInstance != null)
            {
                Destroy(dialogueInstance);
                dialogueInstance = null;
            }

            dialogueInstance = Instantiate(prefab);
            dialogueInstance.name = "Demo Dialogue (Runtime)";
            dialogueData = dialogueInstance.GetComponent<AudioDemoDialoguePrefab>();
            if (dialogueData == null)
            {
                Debug.LogError("[AudioDemo] Loaded dialogue prefab does not contain AudioDemoDialoguePrefab component.");
                Destroy(dialogueInstance);
                dialogueInstance = null;
                dialogueLoading = false;
                SetLoadingOverlay(false, string.Empty, 0f);
                preloadRoutine = null;
                yield break;
            }

            EnsureDialogueWindow();
            SetLoadingOverlay(true, "Подгрузка звуков диалога...", 0.35f);

            var handle = AcquireDialogueScope(manager);
            while (handle != null && !handle.IsDone)
            {
                var weighted = 0.35f + Mathf.Clamp01(handle.Progress) * 0.65f;
                SetLoadingOverlay(true, "Подгрузка звуков диалога...", weighted);
                yield return null;
            }

            SetLoadingOverlay(true, "Диалог готов", 1f);
            yield return null;
            SetLoadingOverlay(false, string.Empty, 0f);

            dialogueScopeActive = true;
            dialogueLoading = false;
            preloadRoutine = null;
            RefreshDialogueWindowState();

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

                if (dialogueScopeHeld)
                {
                    manager.ReleaseScope(DialogueScopeId);
                    dialogueScopeHeld = false;
                    manager.UnloadUnused();
                }
            }

            dialogueScopeActive = false;

            if (dialogueInstance != null)
            {
                Destroy(dialogueInstance);
                dialogueInstance = null;
            }

            dialogueData = null;
            DestroyDialogueWindow();
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

        private AudioLoadHandle AcquireDialogueScope(AudioManager manager)
        {
            dialogueScopeIdScratch.Clear();
            AddEventId(dialogueScopeIdScratch, dialogueData?.Intro);
            AddEventId(dialogueScopeIdScratch, dialogueData?.Line1);
            AddEventId(dialogueScopeIdScratch, dialogueData?.Line2);
            AddEventId(dialogueScopeIdScratch, dialogueData?.Line3);
            AddEventId(dialogueScopeIdScratch, dialogueData?.Music);

            dialogueScopeHeld = dialogueScopeIdScratch.Count > 0;
            if (!dialogueScopeHeld)
            {
                return AudioLoadHandle.Completed();
            }

            return manager.AcquireScope(DialogueScopeId, dialogueScopeIdScratch);
        }

        private static void AddEventId(List<string> list, SoundEvent evt)
        {
            if (list == null || evt == null || string.IsNullOrWhiteSpace(evt.Id) || list.Contains(evt.Id))
            {
                return;
            }

            list.Add(evt.Id);
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
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            uiCanvasTransform = canvas.transform;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = CreateRectTransform(
                "Main Panel",
                canvas.transform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -20f),
                new Vector2(760f, 760f));

            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.1f, 0.88f);

            CreateText(
                "Title",
                panel,
                "Addressables Demo (uGUI)\nБольшой UI + динамический popup-диалог",
                new Vector2(20f, -20f),
                new Vector2(720f, 100f),
                28,
                TextAnchor.UpperLeft,
                new Color(0.92f, 0.97f, 1f, 1f));

            CreateText(
                "Hint",
                panel,
                "Горячие клавиши: 1/2/3 линии, 4 музыка, 5 пауза, 6 snapshot, 7 звук, 8 диалог",
                new Vector2(20f, -128f),
                new Vector2(720f, 56f),
                20,
                TextAnchor.UpperLeft,
                new Color(0.82f, 0.89f, 0.95f, 1f));

            stateLabel = CreateText(
                "State",
                panel,
                string.Empty,
                new Vector2(20f, -188f),
                new Vector2(720f, 92f),
                20,
                TextAnchor.UpperLeft,
                new Color(0.86f, 0.92f, 0.96f, 1f));

            var y = -300f;
            CreateButton(panel, "Open / Close Dialogue (8)", y, new Vector2(720f, 58f), 22, ToggleDialogueScope, out dialogueButtonLabel);
            y -= 68f;
            CreateButton(panel, "Sound OFF (7)", y, new Vector2(720f, 58f), 22, ToggleSound, out soundButtonLabel);
            y -= 68f;
            CreateButton(panel, "Play Music (4)", y, new Vector2(720f, 58f), 22, ToggleMusic, out musicButtonLabel);
            y -= 68f;
            CreateButton(panel, "Pause (5)", y, new Vector2(720f, 58f), 22, TogglePause, out pauseButtonLabel);
            y -= 68f;
            CreateButton(panel, "Menu Snapshot (6)", y, new Vector2(720f, 58f), 22, ToggleSnapshot, out snapshotButtonLabel);

            BuildLoadingOverlay(canvas.transform);
        }

        private void EnsureDialogueWindow()
        {
            if (dialogueWindowRoot != null)
            {
                dialogueWindowRoot.SetActive(true);
                return;
            }

            dialogueWindowRoot = new GameObject("Dialogue Popup", typeof(RectTransform), typeof(Image));
            var parent = uiCanvasTransform != null ? uiCanvasTransform : transform;
            dialogueWindowRoot.transform.SetParent(parent, false);

            var rootRect = dialogueWindowRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(900f, 620f);
            rootRect.anchoredPosition = new Vector2(280f, -30f);

            var rootImage = dialogueWindowRoot.GetComponent<Image>();
            rootImage.color = new Color(0.06f, 0.11f, 0.16f, 0.94f);

            CreateText(
                "Popup Title",
                rootRect,
                "Dynamic Dialogue Loaded",
                new Vector2(20f, -20f),
                new Vector2(860f, 56f),
                30,
                TextAnchor.UpperLeft,
                new Color(0.91f, 0.98f, 1f, 1f));

            dialogueWindowInfoLabel = CreateText(
                "Popup Info",
                rootRect,
                string.Empty,
                new Vector2(20f, -84f),
                new Vector2(860f, 116f),
                20,
                TextAnchor.UpperLeft,
                new Color(0.82f, 0.92f, 0.98f, 1f));

            var y = -220f;
            CreateButton(rootRect, "Play Intro", y, new Vector2(860f, 56f), 22, PlayIntro, out _);
            y -= 66f;
            CreateButton(rootRect, "Play Line 1", y, new Vector2(860f, 56f), 22, () => PlayDialogueLine(1), out _);
            y -= 66f;
            CreateButton(rootRect, "Play Line 2 (3D Follow)", y, new Vector2(860f, 56f), 22, () => PlayDialogueLine(2), out _);
            y -= 66f;
            CreateButton(rootRect, "Play Line 3", y, new Vector2(860f, 56f), 22, () => PlayDialogueLine(3), out _);
            y -= 66f;
            CreateButton(rootRect, "Play Music", y, new Vector2(860f, 56f), 22, ToggleMusic, out dialogueWindowMusicLabel);
            y -= 66f;
            CreateButton(rootRect, "Unload Dialogue (Destroy)", y, new Vector2(860f, 56f), 22, CloseDialogue, out _);

            RefreshDialogueWindowState();
        }

        private void DestroyDialogueWindow()
        {
            if (dialogueWindowRoot == null)
            {
                return;
            }

            Destroy(dialogueWindowRoot);
            dialogueWindowRoot = null;
            dialogueWindowInfoLabel = null;
            dialogueWindowMusicLabel = null;
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
            rootImage.color = new Color(0f, 0f, 0f, 0.8f);

            var text = CreateText(
                "Loading Text",
                rootRect,
                string.Empty,
                new Vector2(0f, 0f),
                new Vector2(960f, 220f),
                44,
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

        private Button CreateButton(
            Transform parent,
            string text,
            float y,
            Vector2 size,
            int fontSize,
            UnityEngine.Events.UnityAction onClick,
            out Text label)
        {
            var buttonRect = CreateRectTransform(
                text + " Button",
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, y),
                size);

            var image = buttonRect.gameObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.24f, 0.32f, 0.98f);

            var button = buttonRect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.98f, 1f, 1f);
            colors.pressedColor = new Color(0.76f, 0.88f, 0.96f, 1f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            label = CreateText(
                "Label",
                buttonRect,
                text,
                Vector2.zero,
                size,
                fontSize,
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
                var prefabStatus = dialogueData != null ? "instantiated" : "not created";
                stateLabel.text =
                    $"Dialogue object: {prefabStatus}\n" +
                    $"Scope: {dialogueStatus}, Loading: {(dialogueLoading ? "yes" : "no")}, Sound: {(soundEnabled ? "on" : "off")}";
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
                dialogueButtonLabel.text = (dialogueScopeActive || dialogueLoading)
                    ? "Close Dialogue (8)"
                    : "Open Dialogue (8)";
            }

            RefreshDialogueWindowState();

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

        private void RefreshDialogueWindowState()
        {
            if (dialogueWindowRoot != null)
            {
                dialogueWindowRoot.SetActive(dialogueScopeActive && dialogueData != null);
            }

            if (dialogueWindowInfoLabel != null)
            {
                var introId = dialogueData?.Intro != null ? dialogueData.Intro.Id : "-";
                var line1Id = dialogueData?.Line1 != null ? dialogueData.Line1.Id : "-";
                var line2Id = dialogueData?.Line2 != null ? dialogueData.Line2.Id : "-";
                var line3Id = dialogueData?.Line3 != null ? dialogueData.Line3.Id : "-";
                var musicId = dialogueData?.Music != null ? dialogueData.Music.Id : "-";

                dialogueWindowInfoLabel.text =
                    $"Runtime prefab instantiated. SoundEvent IDs:\n" +
                    $"intro={introId}, line1={line1Id}, line2={line2Id}, line3={line3Id}, music={musicId}";
            }

            if (dialogueWindowMusicLabel != null)
            {
                dialogueWindowMusicLabel.text = musicHandle.IsValid ? "Stop Music" : "Play Music";
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
