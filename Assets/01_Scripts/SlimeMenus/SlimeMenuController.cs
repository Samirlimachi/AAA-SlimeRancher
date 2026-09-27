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
        Text volumeLabel, musicLabel;
        Text pauseTitle;
        Toggle hapticsToggle, comfortToggle;
        bool paused;
        bool menuButtonWasPressed;
        float startShownAt;
        float nextMenuCheck;
        static Sprite roundedButtonSprite;
        static readonly InputFeatureUsage<bool> MenuButton = new InputFeatureUsage<bool>("Menu Button");

        public bool IsPaused => paused;

        void Awake()
        {
            PrepareStartScene();
            CreateInterface();
            bool isStartScene = SceneManager.GetActiveScene().name == startScene;
            SetStartVisible(showStartMenu && isStartScene);
            SetPauseVisible(false);
        }

        void Start()
        {
            SlimeGameOptions.Apply();
            StartMenuMusic();
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

        // Main menu music (SonidosJuego > "Musica Menu"): loops and fades in, only in the start scene.
        AudioSource menuMusic;
        float menuMusicVolume;

        void StartMenuMusic()
        {
            if (SceneManager.GetActiveScene().name != startScene) return;
            var music = SlimeRancher.Area1.Area1Audio.Pick(b => b.musicaMenu);
            if (music == null) return;
            menuMusic = gameObject.AddComponent<AudioSource>();
            menuMusic.clip = music.clip;
            menuMusic.loop = true;
            menuMusic.spatialBlend = 0;
            menuMusic.priority = 0;
            menuMusic.volume = 0;
            menuMusicVolume = music.volume;
            if (music.clip.loadState == AudioDataLoadState.Unloaded) music.clip.LoadAudioData();
            menuMusic.Play();
        }

        void LateUpdate()
        {
            // Fade the menu music in (unscaled: the start menu pauses time).
            if (menuMusic)
                menuMusic.volume = Mathf.MoveTowards(menuMusic.volume, menuMusicVolume * SlimeGameOptions.MusicVolume, Time.unscaledDeltaTime * Mathf.Max(menuMusicVolume, .1f) / 2);
            // Settle in front of the player once tracking starts, then stay still so you can look around
            // and aim at the buttons.
            if (SceneManager.GetActiveScene().name == startScene && startPanel && startPanel.activeSelf && Time.unscaledTime < startShownAt + 1.5f)
                PositionInFrontOfPlayer();
        }

        // MAIN_MENU decor was built without colliders: chickens and slimes fell through the floor forever
        // (the old menu froze time, which hid it). Give the ground and the pond bed collision, and hide the
        // controller help labels ("UI Press", "Grab"...) like in AREA1.
        void PrepareStartScene()
        {
            if (SceneManager.GetActiveScene().name != startScene) return;
            foreach (var name in new[] { "Suelo de pasto", "Lecho de piedra" })
            {
                var ground = GameObject.Find(name);
                if (ground && !ground.GetComponent<Collider>() && ground.GetComponent<MeshFilter>())
                    ground.AddComponent<MeshCollider>();
            }
            foreach (var callouts in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (callouts.name.StartsWith("Affordance Callouts")) callouts.gameObject.SetActive(false);
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
            AddButton(startPanel.transform, "CONTINUAR PARTIDA", 45, ContinueGame, false);
            AddButton(startPanel.transform, "CONTROLES", -30, OpenControls, false);
            AddButton(startPanel.transform, "OPCIONES", -75, OpenOptions, false);
            AddButton(startPanel.transform, "SALIR", -165, QuitGame, false);

            pausePanel = BuildPanel("Menu de pausa", "La aventura queda en pausa");
            pauseTitle = pausePanel.transform.Find("Title").GetComponent<Text>();
            AddButton(pausePanel.transform, "CONTINUAR", 120, ResumeGame, true);
            AddButton(pausePanel.transform, "GUARDAR PARTIDA", 45, SaveGame, false);
            AddButton(pausePanel.transform, "CONTROLES", -75, OpenControls, false);
            AddButton(pausePanel.transform, "OPCIONES", -120, OpenOptions, false);
            AddButton(pausePanel.transform, "VOLVER AL MENU", -165, ReturnToStart, false);

            modalDimmer = new GameObject("Oscurecer menus", typeof(RectTransform), typeof(Image));
            modalDimmer.transform.SetParent(canvas.transform, false);
            var dimmerRect = modalDimmer.GetComponent<RectTransform>();
            dimmerRect.anchorMin = Vector2.zero;
            dimmerRect.anchorMax = Vector2.one;
            dimmerRect.offsetMin = Vector2.zero;
            dimmerRect.offsetMax = Vector2.zero;
            modalDimmer.GetComponent<Image>().color = new Color(0, 0, 0, .72f);

            controlsPanel = BuildPanel("Panel de controles", "CONTROLES VR", 1400, false, controlsTextScale);
            var controlsLayout = controlsPanel.GetComponent<VerticalLayoutGroup>();
            controlsLayout.padding.top = 60;
            var instructions = AddText(controlsPanel.transform, "Instructions", "<color=#FFC94A><b>AGARRAR / SOLTAR ASPIRADORA</b></color>\nGrip: una vez agarra, otra vez suelta\n<color=#FFC94A><b>AGARRAR OBJETOS</b></color>\nMantén el Grip\n<color=#FFC94A><b>ASPIRAR</b></color>\nGatillo de la mano con la aspiradora\n<color=#FFC94A><b>LANZAR OBJETO</b></color>\nGatillo de la otra mano\n<color=#FFC94A><b>AGUA</b></color>\nAspira apuntando al estanque  |  ranura 5 + gatillo dispara\n<color=#FFC94A><b>CAMBIAR RANURA</b></color>\nA siguiente  |  B anterior\n<color=#FFC94A><b>MOVERSE / CORRER</b></color>\nJoystick izquierdo  |  húndelo para correr\n<color=#FFC94A><b>GIRAR / TELETRANSPORTE</b></color>\nJoystick derecho a los lados  |  hacia adelante\n<color=#FFC94A><b>SALTAR</b></color>\nA (mando derecho)\n<color=#FFC94A><b>TIENDAS Y TABLERO</b></color>\nApunta al botón y presiona Grip\n<color=#FFC94A><b>CURARTE</b></color>\nPasa por encima de un corazón\n<color=#FFC94A><b>MENÚ</b></color>\nBotón de tres rayas del mando izquierdo", controlsFontSize, TextAnchor.UpperCenter, 800, controlsTextScale, controlsLineSpacing);
            var instructionsRect = instructions.rectTransform;
            instructionsRect.anchorMin = new Vector2(.5f, .5f);
            instructionsRect.anchorMax = new Vector2(.5f, .5f);
            instructionsRect.pivot = new Vector2(.5f, .5f);
            // The list fits in a fixed box between the title and CERRAR (shrinks the font if needed).
            // Menu texts are drawn small and scaled up; at x9 the list is half the old size and the box
            // (in the text's own units) covers the panel between the title and CERRAR.
            const float listScale = 9f;
            instructionsRect.anchoredPosition = new Vector2(0, -10);
            instructionsRect.sizeDelta = new Vector2(667, 1030);
            instructions.transform.localScale = Vector3.one * listScale;
            instructions.fontSize = 20;
            instructions.lineSpacing = 1.05f;
            instructions.horizontalOverflow = HorizontalWrapMode.Wrap;
            instructions.verticalOverflow = VerticalWrapMode.Truncate;
            instructions.resizeTextForBestFit = true;
            instructions.resizeTextMinSize = 8;
            instructions.resizeTextMaxSize = 20;
            Destroy(instructions.GetComponent<LayoutElement>());
            AddButton(controlsPanel.transform, "CERRAR", -1, CloseModal, false);
            // CERRAR sits at the bottom of the panel, out of the list's way.
            var closeButton = (RectTransform)controlsPanel.transform.Find("CERRAR");
            closeButton.GetComponent<LayoutElement>().ignoreLayout = true;
            closeButton.anchorMin = closeButton.anchorMax = new Vector2(.5f, 0);
            closeButton.pivot = new Vector2(.5f, .5f);
            closeButton.anchoredPosition = new Vector2(0, 95);
            closeButton.sizeDelta = new Vector2(520, 120);

            optionsPanel = BuildPanel("Panel de opciones", "OPCIONES DEL JUEGO", 1250, false, controlsTextScale);
            hapticsToggle = AddToggle(optionsPanel.transform, "VIBRACION DE LOS MANDOS", SlimeGameOptions.Haptics);
            comfortToggle = AddToggle(optionsPanel.transform, "MODO CONFORT", SlimeGameOptions.SmoothTurn);
            AddButton(optionsPanel.transform, "VOLUMEN GENERAL", -1, CycleVolume, false);
            volumeLabel = optionsPanel.transform.Find("VOLUMEN GENERAL/Label").GetComponent<Text>();
            AddButton(optionsPanel.transform, "MUSICA", -1, CycleMusic, false);
            musicLabel = optionsPanel.transform.Find("MUSICA/Label").GetComponent<Text>();
            RefreshVolumeLabels();
            AddText(optionsPanel.transform, "QualityInfo", "El modo confort activa el giro suave para evitar giros bruscos.\nPulsa el volumen para bajarlo; despues de 0% vuelve a 100%.", hintFontSize, TextAnchor.MiddleCenter, 120);
            AddButton(optionsPanel.transform, "CERRAR", -1, CloseModal, false);
            confirmPanel = BuildPanel("Panel de confirmacion", "¿EMPEZAR UNA NUEVA PARTIDA?", 900, false, controlsTextScale);
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
            var title = AddText(panel.transform, "Title", includeGameHeader ? theme ? theme.gameTitle : "SLIME RANCHER" : hint, includeGameHeader ? titleFontSize : controlsFontSize, TextAnchor.MiddleCenter, includeGameHeader ? 180 : 120, compactTitleScale);
            if (includeGameHeader && theme && theme.logo)
            {
                title.gameObject.SetActive(false);
                AddLogo(panel.transform);
            }
            if (includeGameHeader)
            {
                AddText(panel.transform, "Subtitle", theme ? theme.subtitle : "", subtitleFontSize, TextAnchor.MiddleCenter, 90);
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
            var menuAudio = row.AddComponent<SlimeMenuButtonAudio>();
            toggle.onValueChanged.AddListener(_ => menuAudio.PlayClick());
            var background = row.GetComponent<Image>();
            background.color = theme ? theme.panelSecondary : new Color(.06f, .2f, .22f);
            background.sprite = RoundedButtonSprite();
            background.type = Image.Type.Sliced;
            toggle.targetGraphic = background;
            var indicatorObject = new GameObject("Estado", typeof(RectTransform), typeof(Image));
            indicatorObject.transform.SetParent(row.transform, false);
            var indicator = indicatorObject.GetComponent<Image>();
            indicator.color = new Color(.25f, .85f, .66f);
            indicator.raycastTarget = false;
            indicator.enabled = initialValue;
            var indicatorRect = indicator.rectTransform;
            indicatorRect.anchorMin = new Vector2(0, .5f);
            indicatorRect.anchorMax = new Vector2(0, .5f);
            indicatorRect.pivot = new Vector2(0, .5f);
            indicatorRect.anchoredPosition = new Vector2(24, 0);
            indicatorRect.sizeDelta = new Vector2(28, 28);
            toggle.graphic = indicator;
            toggle.onValueChanged.AddListener(value => ApplyOption(label, value));
            AddText(row.transform, "Label", label, buttonFontSize, TextAnchor.MiddleCenter, 95);
            return toggle;
        }

        void ApplyOption(string option, bool enabled)
        {
            if (option == "VIBRACION DE LOS MANDOS") SlimeGameOptions.Haptics = enabled;
            else if (option == "MODO CONFORT") SlimeGameOptions.SmoothTurn = enabled;
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
            image.color = primary ? (theme ? theme.accent : new Color(1, .77f, .2f)) : (theme ? theme.panelSecondary : new Color(.1f, .31f, .2f));
            image.sprite = RoundedButtonSprite();
            image.type = Image.Type.Sliced;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var menuAudio = go.AddComponent<SlimeMenuButtonAudio>();
            button.onClick.AddListener(menuAudio.PlayClick);
            button.onClick.AddListener(action);
            var colors = button.colors;
            colors.highlightedColor = theme ? theme.accentSecondary : new Color(.31f, .82f, .29f);
            colors.pressedColor = theme ? theme.accent : new Color(1, .77f, .2f);
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

        void AddLogo(Transform parent)
        {
            var go = new GameObject("Logo Slime Rancher", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = theme.logo;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = 180;
            layout.preferredHeight = 180;
            layout.flexibleWidth = 1;
        }

        static Sprite RoundedButtonSprite()
        {
            if (roundedButtonSprite) return roundedButtonSprite;

            const int size = 64;
            const float radius = 17;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Menu Rounded Button" };
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float center = (size - 1) * .5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x - center) - (center - radius);
                    float qy = Mathf.Abs(y - center) - (center - radius);
                    float outside = Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0));
                    float distance = outside + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
                    float alpha = 1 - Mathf.SmoothStep(-1, 1, distance);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply();
            roundedButtonSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            roundedButtonSprite.name = "Menu Rounded Button";
            return roundedButtonSprite;
        }

        void SetStartVisible(bool visible)
        {
            if (!startPanel) return;
            if (visible) SlimeMenuButtonAudio.PlayOpen();
            startPanel.SetActive(visible);
            if (visible) RefreshContinue();
            // The start scene has no gameplay to pause, and a paused clock freezes the XR simulator's head
            // (mouse look) on PC. Keep time running here.
            Time.timeScale = 1;
            if (visible)
            {
                startShownAt = Time.unscaledTime;
                PositionInFrontOfPlayer();
            }
        }
        void SetPauseVisible(bool visible)
        {
            if (!pausePanel) return;
            if (visible) SlimeMenuButtonAudio.PlayOpen();
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
            if (visible) SlimeMenuButtonAudio.PlayOpen();
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
        public void ContinueGame()
        {
            if (!RanchGame.SaveExists(saveFileName)) { RefreshContinue(); return; }
            RanchGame.LoadRequested = true;
            StartGame();
        }

        // New game: ask first if there is anything to lose, then start clean.
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
            var button = startPanel ? startPanel.transform.Find("CONTINUAR PARTIDA") : null;
            if (!button) return;
            bool exists = RanchGame.SaveExists(saveFileName);
            button.GetComponent<Button>().interactable = exists;
            var label = button.Find("Label");
            if (label) label.GetComponent<Text>().text = exists ? "CONTINUAR PARTIDA" : "SIN PARTIDA GUARDADA";
        }

        // ---- Volume options ----
        static readonly float[] VolumeSteps = { 1, .75f, .5f, .25f, 0 };

        static float NextStep(float current)
        {
            int index = 0;
            for (int i = 0; i < VolumeSteps.Length; i++) if (Mathf.Abs(VolumeSteps[i] - current) < .01f) index = i;
            return VolumeSteps[(index + 1) % VolumeSteps.Length];
        }

        void CycleVolume() { SlimeGameOptions.Volume = NextStep(SlimeGameOptions.Volume); RefreshVolumeLabels(); }
        void CycleMusic() { SlimeGameOptions.MusicVolume = NextStep(SlimeGameOptions.MusicVolume); RefreshVolumeLabels(); }

        void RefreshVolumeLabels()
        {
            if (volumeLabel) volumeLabel.text = "VOLUMEN GENERAL: " + Mathf.RoundToInt(SlimeGameOptions.Volume * 100) + "%";
            if (musicLabel) musicLabel.text = "MUSICA: " + Mathf.RoundToInt(SlimeGameOptions.MusicVolume * 100) + "%";
        }
        public void SaveGame() { if (RanchGame.Instance) RanchGame.Instance.SaveGame(); }
        public void LoadGame() { if (RanchGame.Instance) RanchGame.Instance.LoadGame(); }
        public void ReturnToStart() { SaveGame(); CloseModal(); Time.timeScale = 1; SceneManager.LoadScene(startScene); }
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ShowControls()
        {
            if (pauseTitle) pauseTitle.text = "CONTROLES\n\nAgarrar: Grip\nAspirar: gatillo de la mano de la herramienta\nLanzar: gatillo de la otra mano\nMenú: botón Menú izquierdo";
            if (startPanel) startPanel.transform.Find("Title").GetComponent<Text>().text = "CONTROLES\n\nGrip: agarrar\nGatillo: aspirar o lanzar\nStick izquierdo: moverse\nStick derecho: girar";
        }

        void OnDestroy() { Time.timeScale = 1; }
    }
}