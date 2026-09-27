using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

namespace SlimeRancherVR
{
    public sealed class SlimeMenuController : MonoBehaviour
    {
        [Header("Scene flow")]
        [SerializeField] string gameplayScene = "AREA1";
        [SerializeField] string startScene = "MAIN_MENU";
        [Tooltip("Archivo de guardado de la escena de juego (RanchGame.saveName).")]
        [SerializeField] string saveFileName = "AREA1_rancho_v1.json";
        [SerializeField] SlimeMenuTheme theme;
        [SerializeField] bool showStartMenu = true;

        [Tooltip("Rig XR que se crea al jugar si la escena no tiene uno (MAIN_MENU se hizo con una camara normal).")]
        [SerializeField] GameObject vrRigPrefab;
        [SerializeField] float rigEyeHeight = 1.7f;

        [Header("VR layout")]
        [SerializeField] Transform menuAnchor;
        [SerializeField] float menuDistance = 1.8f;
        [SerializeField] float menuHeight = 1.45f;
        [SerializeField, Min(0.1f)] float menuWorldWidth = 4.3f;
        [SerializeField, Min(1)] int titleFontSize = 64;
        [SerializeField, Min(1)] int subtitleFontSize = 28;
        [SerializeField, Min(1)] int hintFontSize = 22;
        [SerializeField, Min(1)] int primaryButtonFontSize = 32;
        [SerializeField, Min(1)] int buttonFontSize = 28;
        [SerializeField, Min(0.5f)] float textSizeMultiplier = 2.5f;
        [SerializeField, Min(0.1f)] float titleTextScale = 10f;
        [SerializeField, Min(0.1f)] float bodyTextScale = 10f;
        [SerializeField, Min(0.1f)] float buttonTextScale = 10f;
        [SerializeField, Min(1)] int controlsFontSize = 20;
        [SerializeField, Min(0.1f)] float controlsTextScale = 1.2f;
        [SerializeField, Min(0.1f)] float controlsLineSpacing = 1.35f;

        Canvas canvas;
        GameObject startPanel, pausePanel, controlsPanel, optionsPanel, confirmPanel, modalDimmer;
        Text pauseTitle, pauseHint, continueLabel, volumeLabel, hapticsLabel, turnLabel;
        Button continueButton;
        bool paused;
        bool menuButtonWasPressed;
        float startShownAt;
        float nextMenuCheck;
        static readonly InputFeatureUsage<bool> MenuButton = new InputFeatureUsage<bool>("Menu Button");

        public bool IsPaused => paused;

        void Awake()
        {
            EnsureVRRig();
            EnsureXRUIEventSystem();
            CreateInterface();
            bool isStartScene = SceneManager.GetActiveScene().name == startScene;
            SetStartVisible(showStartMenu && isStartScene);
            SetPauseVisible(false);
        }

        void Start() => SlimeGameOptions.Apply();

        // Without an XR rig there are no controller rays to press the buttons. Replace the plain
        // camera with the rig (same spot and facing); only turning stays enabled in the menu.
        void EnsureVRRig()
        {
            if (!vrRigPrefab || FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>()) return;
            var oldCamera = Camera.main;
            var position = oldCamera ? oldCamera.transform.position - Vector3.up * rigEyeHeight : Vector3.zero;
            float yaw = oldCamera ? oldCamera.transform.eulerAngles.y : 0;
            if (oldCamera) oldCamera.gameObject.SetActive(false);
            var rig = Instantiate(vrRigPrefab, position, Quaternion.Euler(0, yaw, 0));
            rig.name = "Jugador XR - Menu";
            var origin = rig.GetComponent<Unity.XR.CoreUtils.XROrigin>();
            if (origin)
            {
                origin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Device;
                origin.CameraYOffset = rigEyeHeight;
            }
            foreach (var provider in rig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>(true))
                if (!(provider is UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider) &&
                    !(provider is UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider))
                    provider.enabled = false;
            foreach (var vignette in rig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController>(true))
                vignette.gameObject.SetActive(false);
        }

        // Menu buttons are pressed with the controller rays (and the mouse on desktop): that needs an
        // EventSystem driven by XRUIInputModule. AREA1 had none and MAIN_MENU had a mouse-only module.
        static void EnsureXRUIEventSystem()
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (!eventSystem) eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            if (eventSystem.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>()) return;
            foreach (var module in eventSystem.GetComponents<BaseInputModule>()) DestroyImmediate(module);
            eventSystem.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
        }

        void Update()
        {
            if (SceneManager.GetActiveScene().name == startScene && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                StartGame();
                return;
            }
            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.mKey.wasPressedThisFrame))
                TogglePause();

            if (Time.unscaledTime < nextMenuCheck) return;
            nextMenuCheck = Time.unscaledTime + .05f;
            var controller = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            // "MenuButton" is the standard XR usage name; the spaced one is kept for older device layouts.
            bool pressed = controller.isValid &&
                ((controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out bool menu) && menu) ||
                 (controller.TryGetFeatureValue(MenuButton, out bool legacyMenu) && legacyMenu));
            if (pressed && !menuButtonWasPressed) TogglePause();
            menuButtonWasPressed = pressed;
        }

        void LateUpdate()
        {
            // Settle in front of the headset once tracking starts, then stay still so the rays can aim at it.
            if (SceneManager.GetActiveScene().name == startScene && startPanel && startPanel.activeSelf && Time.unscaledTime < startShownAt + 1.5f)
                PositionInFrontOfPlayer();
        }

        void CreateInterface()
        {
            var root = new GameObject("Slime Menus", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 500;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.referencePixelsPerUnit = 100;
            scaler.dynamicPixelsPerUnit = 100;
            scaler.scaleFactor = 1;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 1300);

            startPanel = BuildPanel("Menu de inicio", "EXPLORA. ATRAPA. CREA TU RANCHO.");
            AddButton(startPanel.transform, "NUEVA PARTIDA", 120, NewGame, true);
            continueLabel = AddButton(startPanel.transform, "CONTINUAR PARTIDA", 45, ContinueGame, false);
            continueButton = continueLabel.GetComponentInParent<Button>();
            AddButton(startPanel.transform, "CONTROLES", -30, OpenControls, false);
            AddButton(startPanel.transform, "OPCIONES", -75, OpenOptions, false);
            AddButton(startPanel.transform, "SALIR DEL JUEGO", -165, QuitGame, false);

            pausePanel = BuildPanel("Menu de pausa", "La aventura queda en pausa", 1450);
            pauseTitle = pausePanel.transform.Find("Title").GetComponent<Text>();
            pauseHint = pausePanel.transform.Find("Hint").GetComponent<Text>();
            AddButton(pausePanel.transform, "CONTINUAR", 120, ResumeGame, true);
            AddButton(pausePanel.transform, "GUARDAR PARTIDA", 45, SaveGame, false);
            AddButton(pausePanel.transform, "CONTROLES", -30, OpenControls, false);
            AddButton(pausePanel.transform, "OPCIONES", -75, OpenOptions, false);
            AddButton(pausePanel.transform, "VOLVER AL INICIO", -120, ReturnToStart, false);
            AddButton(pausePanel.transform, "SALIR DEL JUEGO", -165, QuitGame, false);

            modalDimmer = new GameObject("Oscurecer menus", typeof(RectTransform), typeof(Image));
            modalDimmer.transform.SetParent(canvas.transform, false);
            var dimmerRect = modalDimmer.GetComponent<RectTransform>();
            dimmerRect.anchorMin = Vector2.zero;
            dimmerRect.anchorMax = Vector2.one;
            dimmerRect.offsetMin = Vector2.zero;
            dimmerRect.offsetMax = Vector2.zero;
            modalDimmer.GetComponent<Image>().color = new Color(0, 0, 0, .72f);

            controlsPanel = BuildPanel("Panel de controles", "CONTROLES VR", 1100, false, controlsTextScale);
            var controlsLayout = controlsPanel.GetComponent<VerticalLayoutGroup>();
            controlsLayout.padding.top = 150;
            var instructions = AddText(controlsPanel.transform, "Instructions", "AGARRAR / SOLTAR ASPIRADORA\nGrip (una vez para agarrar, otra para soltar)\n\nASPIRAR\nGatillo de la mano que sostiene la aspiradora\n\nLANZAR O DISPARAR AGUA\nGatillo de la otra mano\n\nMOVERSE / CORRER / GIRAR\nJoystick izquierdo (hundirlo para correr) / derecho\n\nCAMBIAR RANURA\nA siguiente   |   B anterior (la 5 es el agua)\n\nTIENDAS Y TABLERO\nApunta al boton y presiona Grip\n\nMENU\nBoton de tres rayas del mando izquierdo", controlsFontSize, TextAnchor.UpperCenter, 800, controlsTextScale, controlsLineSpacing);
            var instructionsRect = instructions.rectTransform;
            instructionsRect.anchorMin = new Vector2(.5f, .5f);
            instructionsRect.anchorMax = new Vector2(.5f, .5f);
            instructionsRect.pivot = new Vector2(.5f, .5f);
            instructionsRect.anchoredPosition = new Vector2(0, -40);
            instructionsRect.sizeDelta = new Vector2(560, 800);
            instructions.transform.localScale = Vector3.one * 10f;
            Destroy(instructions.GetComponent<LayoutElement>());
            AddButton(controlsPanel.transform, "CERRAR", -1, CloseModal, false);

            optionsPanel = BuildPanel("Panel de opciones", "OPCIONES DEL JUEGO", 1250, false, controlsTextScale);
            volumeLabel = AddButton(optionsPanel.transform, "", -1, CycleVolume, false);
            hapticsLabel = AddButton(optionsPanel.transform, "", -1, ToggleHaptics, false);
            turnLabel = AddButton(optionsPanel.transform, "", -1, ToggleTurn, false);
            AddText(optionsPanel.transform, "OptionsInfo", "Pulsa cada opcion para cambiarla.\nSe guardan automaticamente.", hintFontSize, TextAnchor.MiddleCenter, 120);
            AddButton(optionsPanel.transform, "CERRAR", -1, CloseModal, false);
            RefreshOptions();

            confirmPanel = BuildPanel("Panel de confirmacion", "¿EMPEZAR DE CERO?", 900, false, controlsTextScale);
            AddText(confirmPanel.transform, "ConfirmInfo", "Se borrara tu partida guardada,\ntus mejoras y tus oleadas completadas.", hintFontSize, TextAnchor.MiddleCenter, 160);
            AddButton(confirmPanel.transform, "SI, NUEVA PARTIDA", -1, ConfirmNewGame, true);
            AddButton(confirmPanel.transform, "NO, VOLVER", -1, CloseModal, false);
            modalDimmer.transform.SetAsLastSibling();
            controlsPanel.transform.SetAsLastSibling();
            optionsPanel.transform.SetAsLastSibling();
            confirmPanel.transform.SetAsLastSibling();
            SetModalVisible(false, null);
        }

        GameObject BuildPanel(string objectName, string hint, float height = 1200, bool includeGameHeader = true, float compactTitleScale = -1f)
        {
            var panel = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(720, height);
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = theme ? theme.panel : new Color(.035f, .13f, .16f, .98f);
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(80, 80, 72, 48);
            layout.spacing = 32;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            AddText(panel.transform, "Title", includeGameHeader ? theme ? theme.gameTitle : "SLIME RANCHER" : hint, includeGameHeader ? titleFontSize : controlsFontSize, TextAnchor.MiddleCenter, includeGameHeader ? 180 : 120, compactTitleScale);
            if (includeGameHeader)
            {
                AddText(panel.transform, "Subtitle", theme ? theme.subtitle : "La aventura de Beatrix LeBeau", subtitleFontSize, TextAnchor.MiddleCenter, 90);
                AddText(panel.transform, "Hint", hint, hintFontSize, TextAnchor.MiddleCenter, 90);
            }
            return panel;
        }

        Text AddText(Transform parent, string objectName, string value, int size, TextAnchor anchor, float height, float scaleOverride = -1f, float lineSpacing = 1f)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = value;
            text.font = theme && theme.font ? theme.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var fontMultiplier = objectName == "Instructions" ? 1f : scaleOverride > 0 ? Mathf.Max(scaleOverride, 2.5f) : Mathf.Max(textSizeMultiplier, 12f);
            var calculatedFontSize = Mathf.RoundToInt(size * fontMultiplier);
            if (objectName == "Instructions") calculatedFontSize = Mathf.Max(calculatedFontSize, 20);
            text.fontSize = Mathf.Max(1, calculatedFontSize);
            text.alignment = anchor;
            text.lineSpacing = lineSpacing;
            text.color = theme ? theme.text : Color.white;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var layout = go.AddComponent<LayoutElement>();
            layout.minWidth = 1;
            layout.flexibleWidth = 1;
            layout.preferredHeight = height;
            text.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, height);
            var textScale = objectName == "Instructions" ? Mathf.Max(controlsTextScale, 10f) : scaleOverride > 0 ? 1f : objectName == "Title" ? titleTextScale : objectName == "Label" ? buttonTextScale : bodyTextScale;
            go.transform.localScale = Vector3.one * textScale;
            return text;
        }

        Text AddButton(Transform parent, string label, float unusedOffset, UnityEngine.Events.UnityAction action, bool primary)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = primary ? (theme ? theme.accent : new Color(1, .72f, .18f)) : (theme ? theme.panelSecondary : new Color(.06f, .2f, .22f));
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => SlimeRancher.Area1.Area1Audio.Play2D(b => b.clickMenu));
            button.onClick.AddListener(action);
            var colors = button.colors;
            colors.highlightedColor = theme ? theme.accentSecondary : new Color(.12f, .78f, .72f);
            colors.pressedColor = theme ? theme.accent : new Color(1, .72f, .18f);
            button.colors = colors;
            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 150;
            layout.preferredHeight = 150;
            var text = AddText(go.transform, "Label", label, primary ? primaryButtonFontSize : buttonFontSize, TextAnchor.MiddleCenter, 150);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18, 0);
            textRect.offsetMax = new Vector2(-18, 0);
            text.color = primary ? Color.black : (theme ? theme.text : Color.white);
            return text;
        }

        void SetStartVisible(bool visible)
        {
            if (!startPanel) return;
            startPanel.SetActive(visible);
            if (visible)
            {
                RefreshContinue();
                startShownAt = Time.unscaledTime;
                Time.timeScale = 0;
                PositionInFrontOfPlayer();
            }
            else Time.timeScale = 1;
        }
        void SetPauseVisible(bool visible)
        {
            if (!pausePanel) return;
            pausePanel.SetActive(visible);
            if (!visible) return;
            if (pauseHint) pauseHint.text = "La aventura queda en pausa";
            PositionInFrontOfPlayer();
        }

        void PositionInFrontOfPlayer()
        {
            var camera = menuAnchor ? menuAnchor : Camera.main ? Camera.main.transform : null;
            if (!camera) return;
            canvas.transform.position = camera.position + Vector3.ProjectOnPlane(camera.forward, Vector3.up).normalized * menuDistance + Vector3.up * menuHeight;
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - camera.position, Vector3.up);
            var parentScale = transform.lossyScale;
            var inheritedScale = Mathf.Max(Mathf.Abs(parentScale.x), Mathf.Abs(parentScale.y), Mathf.Abs(parentScale.z));
            var widthScale = menuWorldWidth / canvas.GetComponent<RectTransform>().rect.width;
            canvas.transform.localScale = Vector3.one * (widthScale / Mathf.Max(inheritedScale, .0001f));
        }

        public void TogglePause()
        {
            if (SceneManager.GetActiveScene().name == startScene)
            {
                SetStartVisible(!startPanel.activeSelf);
                return;
            }
            if (paused) ResumeGame(); else PauseGame();
        }

        public void CloseStartMenu()
        {
            CloseModal();
            if (SceneManager.GetActiveScene().name == startScene)
                SetStartVisible(false);
        }

        public void OpenControls() => SetModalVisible(true, controlsPanel);
        public void OpenOptions() => SetModalVisible(true, optionsPanel);

        public void CloseModal() => SetModalVisible(false, null);

        void SetModalVisible(bool visible, GameObject panel)
        {
            if (modalDimmer) modalDimmer.SetActive(visible);
            if (controlsPanel) controlsPanel.SetActive(visible && panel == controlsPanel);
            if (optionsPanel) optionsPanel.SetActive(visible && panel == optionsPanel);
            if (confirmPanel) confirmPanel.SetActive(visible && panel == confirmPanel);
            if (visible && panel)
            {
                panel.transform.SetAsLastSibling();
                PositionInFrontOfPlayer();
            }
        }

        public void PauseGame() { paused = true; Time.timeScale = 0; SetPauseVisible(true); }
        public void ResumeGame() { CloseModal(); paused = false; Time.timeScale = 1; SetPauseVisible(false); }
        public void StartGame() { CloseModal(); Time.timeScale = 1; SceneManager.LoadScene(gameplayScene); }

        // New game: if there is anything to lose, ask first.
        public void NewGame()
        {
            if (HasProgress()) SetModalVisible(true, confirmPanel);
            else ConfirmNewGame();
        }

        public void ConfirmNewGame()
        {
            RanchGame.DeleteSave(saveFileName);
            foreach (var key in ProgressKeys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            RanchGame.LoadRequested = false;
            StartGame();
        }

        // Continue: open the game scene and load the saved file there.
        public void ContinueGame()
        {
            if (!RanchGame.SaveExists(saveFileName)) { RefreshContinue(); return; }
            RanchGame.LoadRequested = true;
            StartGame();
        }

        public void SaveGame()
        {
            bool saved = RanchGame.Instance && RanchGame.Instance.SaveGame();
            if (pauseHint) pauseHint.text = saved ? "Partida guardada correctamente" : "No se pudo guardar la partida";
        }

        public void ReturnToStart() { CloseModal(); Time.timeScale = 1; SceneManager.LoadScene(startScene); }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // Permanent AREA1 progress kept outside the save file (waves and upgrades).
        static readonly string[] ProgressKeys =
        {
            SlimeRancher.Area1.Area1WaveBoard.CompletedKey, SlimeRancher.Area1.Area1WaveBoard.PurchasedKey,
            SlimeRancher.Area1.Area1UpgradeShop.HealthKey, SlimeRancher.Area1.Area1UpgradeShop.DamageKey,
            SlimeRancher.Area1.Area1UpgradeShop.WaterKey, SlimeRancher.Area1.Area1UpgradeShop.CollectorKey
        };

        bool HasProgress()
        {
            if (RanchGame.SaveExists(saveFileName)) return true;
            foreach (var key in ProgressKeys) if (PlayerPrefs.HasKey(key)) return true;
            return false;
        }

        void RefreshContinue()
        {
            if (!continueButton) return;
            bool exists = RanchGame.SaveExists(saveFileName);
            continueButton.interactable = exists;
            continueLabel.text = exists ? "CONTINUAR PARTIDA" : "SIN PARTIDA GUARDADA";
        }

        // ---- Options (each button cycles its value) ----
        static readonly float[] VolumeSteps = { 1, .75f, .5f, .25f, 0 };

        void CycleVolume()
        {
            float current = SlimeGameOptions.Volume;
            int index = 0;
            for (int i = 0; i < VolumeSteps.Length; i++) if (Mathf.Abs(VolumeSteps[i] - current) < .01f) index = i;
            SlimeGameOptions.Volume = VolumeSteps[(index + 1) % VolumeSteps.Length];
            RefreshOptions();
        }

        void ToggleHaptics() { SlimeGameOptions.Haptics = !SlimeGameOptions.Haptics; RefreshOptions(); }
        void ToggleTurn() { SlimeGameOptions.SmoothTurn = !SlimeGameOptions.SmoothTurn; RefreshOptions(); }

        void RefreshOptions()
        {
            if (volumeLabel) volumeLabel.text = "VOLUMEN: " + Mathf.RoundToInt(SlimeGameOptions.Volume * 100) + "%";
            if (hapticsLabel) hapticsLabel.text = "VIBRACION: " + (SlimeGameOptions.Haptics ? "SI" : "NO");
            if (turnLabel) turnLabel.text = "GIRO: " + (SlimeGameOptions.SmoothTurn ? "SUAVE" : "POR PASOS");
        }

        public void ShowControls()
        {
            if (pauseTitle) pauseTitle.text = "CONTROLES\n\nAgarrar: Grip\nAspirar: gatillo de la mano de la herramienta\nLanzar: gatillo de la otra mano\nMenú: botón Menú izquierdo";
            if (startPanel) startPanel.transform.Find("Title").GetComponent<Text>().text = "CONTROLES\n\nGrip: agarrar\nGatillo: aspirar o lanzar\nStick izquierdo: moverse\nStick derecho: girar";
        }

        void OnDestroy() { Time.timeScale = 1; }
    }
}