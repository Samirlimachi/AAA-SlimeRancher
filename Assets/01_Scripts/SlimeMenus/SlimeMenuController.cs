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
        [SerializeField] SlimeMenuTheme theme;
        [SerializeField] bool showStartMenu = true;

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
        GameObject startPanel, pausePanel, controlsPanel, optionsPanel, modalDimmer;
        Text pauseTitle;
        Toggle hapticsToggle, comfortToggle;
        bool paused;
        bool menuButtonWasPressed;
        float nextMenuCheck;
        static readonly InputFeatureUsage<bool> MenuButton = new InputFeatureUsage<bool>("Menu Button");

        public bool IsPaused => paused;

        void Awake()
        {
            CreateInterface();
            bool isStartScene = SceneManager.GetActiveScene().name == startScene;
            SetStartVisible(showStartMenu && isStartScene);
            SetPauseVisible(false);
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
            bool pressed = controller.isValid && controller.TryGetFeatureValue(MenuButton, out bool menu) && menu;
            if (pressed && !menuButtonWasPressed) TogglePause();
            menuButtonWasPressed = pressed;
        }

        void LateUpdate()
        {
            if (SceneManager.GetActiveScene().name == startScene && startPanel && startPanel.activeSelf)
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
            AddButton(startPanel.transform, "COMENZAR AVENTURA", 120, StartGame, true);
            AddButton(startPanel.transform, "CONTINUAR PARTIDA", 45, ContinueGame, false);
            AddButton(startPanel.transform, "CONTROLES", -30, OpenControls, false);
            AddButton(startPanel.transform, "OPCIONES", -75, OpenOptions, false);
            AddButton(startPanel.transform, "CERRAR MENU", -120, CloseStartMenu, false);
            AddButton(startPanel.transform, "SALIR", -165, QuitGame, false);

            pausePanel = BuildPanel("Menu de pausa", "La aventura queda en pausa");
            pauseTitle = pausePanel.transform.Find("Title").GetComponent<Text>();
            AddButton(pausePanel.transform, "CONTINUAR", 120, ResumeGame, true);
            AddButton(pausePanel.transform, "GUARDAR PARTIDA", 45, SaveGame, false);
            AddButton(pausePanel.transform, "CONTROLES", -30, OpenControls, false);
            AddButton(pausePanel.transform, "OPCIONES", -75, OpenOptions, false);
            AddButton(pausePanel.transform, "VOLVER AL INICIO", -120, ReturnToStart, false);

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
            var instructions = AddText(controlsPanel.transform, "Instructions", "AGARRAR\nGrip del mando\n\nASPIRAR\nGatillo de la mano que sostiene la aspiradora\n\nLANZAR O DISPARAR AGUA\nGatillo de la otra mano\n\nMOVERSE Y GIRAR\nJoystick izquierdo y derecho\n\nCAMBIAR DEPOSITO\nB del mando derecho   |   X selecciona agua", controlsFontSize, TextAnchor.UpperCenter, 800, controlsTextScale, controlsLineSpacing);
            var instructionsRect = instructions.rectTransform;
            instructionsRect.anchorMin = new Vector2(.5f, .5f);
            instructionsRect.anchorMax = new Vector2(.5f, .5f);
            instructionsRect.pivot = new Vector2(.5f, .5f);
            instructionsRect.anchoredPosition = new Vector2(0, -40);
            instructionsRect.sizeDelta = new Vector2(560, 800);
            instructions.transform.localScale = Vector3.one * 10f;
            Destroy(instructions.GetComponent<LayoutElement>());
            AddButton(controlsPanel.transform, "CERRAR", -1, CloseModal, false);

            optionsPanel = BuildPanel("Panel de opciones", "OPCIONES DEL JUEGO", 900, false, controlsTextScale);
            hapticsToggle = AddToggle(optionsPanel.transform, "VIBRACION DE LOS MANDOS", true);
            comfortToggle = AddToggle(optionsPanel.transform, "MODO CONFORT", false);
            AddText(optionsPanel.transform, "QualityInfo", "La calidad alta mejora los materiales.\nEl modo confort limita los giros bruscos.", hintFontSize, TextAnchor.MiddleCenter, 120);
            AddButton(optionsPanel.transform, "CERRAR", -1, CloseModal, false);
            modalDimmer.transform.SetAsLastSibling();
            controlsPanel.transform.SetAsLastSibling();
            optionsPanel.transform.SetAsLastSibling();
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

        Toggle AddToggle(Transform parent, string label, bool initialValue)
        {
            var row = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Toggle), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 95;
            layout.preferredHeight = 95;
            var toggle = row.GetComponent<Toggle>();
            toggle.isOn = initialValue;
            var background = row.GetComponent<Image>();
            background.color = theme ? theme.panelSecondary : new Color(.06f, .2f, .22f);
            toggle.targetGraphic = background;
            toggle.onValueChanged.AddListener(value => ApplyOption(label, value));
            AddText(row.transform, "Label", label, buttonFontSize, TextAnchor.MiddleCenter, 95);
            return toggle;
        }

        void ApplyOption(string option, bool enabled)
        {
            if (option == "VIBRACION DE LOS MANDOS") return;
            if (option == "MODO CONFORT" && enabled) QualitySettings.vSyncCount = 1;
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

        void AddButton(Transform parent, string label, float unusedOffset, UnityEngine.Events.UnityAction action, bool primary)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = primary ? (theme ? theme.accent : new Color(1, .72f, .18f)) : (theme ? theme.panelSecondary : new Color(.06f, .2f, .22f));
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
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
        }

        void SetStartVisible(bool visible)
        {
            if (!startPanel) return;
            startPanel.SetActive(visible);
            if (visible)
            {
                Time.timeScale = 0;
                PositionInFrontOfPlayer();
            }
            else Time.timeScale = 1;
        }
        void SetPauseVisible(bool visible)
        {
            if (!pausePanel) return;
            pausePanel.SetActive(visible);
            if (visible) PositionInFrontOfPlayer();
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
            if (visible && panel)
            {
                panel.transform.SetAsLastSibling();
                PositionInFrontOfPlayer();
            }
        }

        public void PauseGame() { paused = true; Time.timeScale = 0; SetPauseVisible(true); }
        public void ResumeGame() { CloseModal(); paused = false; Time.timeScale = 1; SetPauseVisible(false); }
        public void StartGame() { CloseModal(); Time.timeScale = 1; SceneManager.LoadScene(gameplayScene); }
        public void ContinueGame() { StartGame(); }
        public void SaveGame() { if (RanchGame.Instance) RanchGame.Instance.SaveGame(); }
        public void ReturnToStart() { CloseModal(); Time.timeScale = 1; SceneManager.LoadScene(startScene); }
        public void QuitGame() { Application.Quit(); }

        public void ShowControls()
        {
            if (pauseTitle) pauseTitle.text = "CONTROLES\n\nAgarrar: Grip\nAspirar: gatillo de la mano de la herramienta\nLanzar: gatillo de la otra mano\nMenú: botón Menú izquierdo";
            if (startPanel) startPanel.transform.Find("Title").GetComponent<Text>().text = "CONTROLES\n\nGrip: agarrar\nGatillo: aspirar o lanzar\nStick izquierdo: moverse\nStick derecho: girar";
        }

        void OnDestroy() { Time.timeScale = 1; }
    }
}