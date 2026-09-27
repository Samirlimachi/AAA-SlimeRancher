using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using SlimeRancherVR;
using Object = UnityEngine.Object;

namespace SlimeRancher.Area1.Editor
{
    // Builds the wave board and both shops as real scene objects (wall, screen, texts, buttons and
    // 3D models), and wires them into Area1WaveBoard / Area1FoodShop / Area1UpgradeShop.
    // Also applies the AREA1 scene setup: no tunneling vignette when moving, and a single plort
    // collector spot whose tier is upgraded from the upgrade shop.
    // Runs by itself when AREA1 is open and something is missing; the menu item rebuilds everything.
    [InitializeOnLoad]
    public static class Area1BoardInstaller
    {
        const string ScenePath = "Assets/00_Scenes/AREA1.unity";
        const string MaterialFolder = "Assets/Area1/Materiales";
        const float Pixel = .002f, ScreenCenter = 1.5f;
        static Font font;

        static Area1BoardInstaller() => EditorApplication.delayCall += AutoBuild;

        static void AutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.GetActiveScene().path != ScenePath) return;
            bool missing = Object.FindObjectsByType<Area1WaveBoard>().Any(b => !b.header)
                || Object.FindObjectsByType<Area1FoodShop>().Any(b => !b.header || b.slots == null || b.slots.Length != b.offers.Length
                    || !b.offers.Any(o => o.kind == RanchItemKind.Heart))
                || Object.FindObjectsByType<Area1UpgradeShop>().Any(b => !b.header || b.slots == null || b.slots.Length != b.upgrades.Length
                    || !b.upgrades.Any(u => u.key == Area1UpgradeShop.CollectorKey))
                || Object.FindObjectsByType<TunnelingVignetteController>().Length > 0
                || (!Object.FindAnyObjectByType<Area1CollectorTiers>() && Object.FindObjectsByType<Area1PlortCollector>().Length > 1)
                // Boards built before the models were turned to face the player.
                || Object.FindObjectsByType<Area1ModelSpin>().Any(m => Mathf.Abs(Mathf.DeltaAngle(m.transform.localEulerAngles.y, Area1ModelSpin.FacingYaw)) > 1);
            if (missing) Build();
            else if (SetupHudIcons() | ScaleCollectors() | SnapCollectorTiers() | CleanScene())
            {
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        // HUD coin/heart icons use the imported models with their textures.
        static bool SetupHudIcons()
        {
            bool changed = false;
            foreach (var hud in Object.FindObjectsByType<Area1HUD>())
            {
                if (hud.coinModel && hud.heartModel && hud.coinTexture && hud.heartTexture) continue;
                hud.coinModel = ModelAsset("Assets/04_Models/MONEDA");
                hud.coinTexture = TextureAsset("Assets/04_Models/MONEDA");
                hud.heartModel = ModelAsset("Assets/04_Models/CORAZON");
                hud.heartTexture = TextureAsset("Assets/04_Models/CORAZON");
                EditorUtility.SetDirty(hud);
                changed = true;
            }
            return changed;
        }

        // Bigger collectors (collectorScale), resting on the ground.
        static bool ScaleCollectors()
        {
            bool changed = false;
            foreach (var tiers in Object.FindObjectsByType<Area1CollectorTiers>())
            {
                if (tiers.tiers == null) continue;
                foreach (var tier in tiers.tiers)
                {
                    if (!tier || Mathf.Abs(tier.transform.localScale.x - tiers.collectorScale) < .001f) continue;
                    tier.transform.localScale = Vector3.one * tiers.collectorScale;
                    // Lift or lower so the model's bottom sits on the floor (y = 0).
                    float bottom = float.MaxValue;
                    foreach (var filter in tier.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (!filter.sharedMesh) continue;
                        var b = filter.sharedMesh.bounds;
                        for (int n = 0; n < 8; n++)
                            bottom = Mathf.Min(bottom, filter.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((n & 1) == 0 ? -1 : 1, (n & 2) == 0 ? -1 : 1, (n & 4) == 0 ? -1 : 1))).y);
                    }
                    if (bottom < float.MaxValue) tier.transform.position += Vector3.up * -bottom;
                    if (PrefabUtility.IsPartOfPrefabInstance(tier)) PrefabUtility.RecordPrefabInstancePropertyModifications(tier.transform);
                    changed = true;
                }
            }
            return changed;
        }

        // Red and black collectors follow the green one wherever it is moved in the scene.
        static bool SnapCollectorTiers()
        {
            bool changed = false;
            foreach (var tiers in Object.FindObjectsByType<Area1CollectorTiers>())
            {
                if (tiers.tiers == null || tiers.tiers.Length == 0 || !tiers.tiers[0]) continue;
                var anchor = tiers.tiers[0].transform;
                foreach (var tier in tiers.tiers)
                {
                    if (!tier || tier == tiers.tiers[0]) continue;
                    if ((tier.transform.position - anchor.position).sqrMagnitude < 1e-6f && Quaternion.Angle(tier.transform.rotation, anchor.rotation) < .01f) continue;
                    tier.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                    if (PrefabUtility.IsPartOfPrefabInstance(tier)) PrefabUtility.RecordPrefabInstancePropertyModifications(tier.transform);
                    changed = true;
                }
            }
            return changed;
        }

        // AREA1 starts calm: the boss only exists as the wave 5 prefab, and the blue slime and the
        // loose enemy slime that were placed by hand are removed (enemies now come from the waves).
        const string BossPrefabPath = "Assets/Area1/JefeEnemigo.prefab";

        static bool CleanScene()
        {
            bool changed = false;
            foreach (var boss in Object.FindObjectsByType<Area1EnemyBoss>(FindObjectsInactive.Include))
            {
                if (!boss.gameObject.scene.IsValid()) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
                if (!prefab)
                {
                    bool wasActive = boss.gameObject.activeSelf;
                    boss.gameObject.SetActive(true);
                    prefab = PrefabUtility.SaveAsPrefabAsset(boss.gameObject, BossPrefabPath);
                    boss.gameObject.SetActive(wasActive);
                }
                foreach (var board in Object.FindObjectsByType<Area1WaveBoard>())
                    if (!board.bossPrefab) { board.bossPrefab = prefab; EditorUtility.SetDirty(board); }
                Object.DestroyImmediate(boss.gameObject);
                changed = true;
            }
            foreach (var item in Object.FindObjectsByType<RanchItem>(FindObjectsInactive.Include))
            {
                if (!item || !item.data || !item.gameObject.scene.IsValid()) continue;
                if (item.data.kind != RanchItemKind.BlueSlime && item.data.kind != RanchItemKind.EnemySlime) continue;
                var root = PrefabUtility.IsPartOfPrefabInstance(item.gameObject) ? PrefabUtility.GetOutermostPrefabInstanceRoot(item.gameObject) : item.gameObject;
                Object.DestroyImmediate(root);
                changed = true;
            }
            if (changed) Debug.Log("AREA1: jefe guardado como prefab de la oleada 5; jefe, slime azul y slime enemigo quitados de la escena.");
            return changed;
        }

        static GameObject ModelAsset(string folder)
        {
            var path = AssetDatabase.FindAssets("t:Model", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
            return path == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static Texture2D TextureAsset(string folder) => AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).Select(AssetDatabase.LoadAssetAtPath<Texture2D>).FirstOrDefault();

        [MenuItem("Area1/Construir tablero y tiendas")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath || Application.isPlaying)
            {
                Debug.LogWarning("Abre AREA1 fuera de Play para construir el tablero y las tiendas.");
                return;
            }
            bool wasDirty = scene.isDirty;
            var game = Object.FindAnyObjectByType<RanchGame>();
            Directory.CreateDirectory(MaterialFolder);

            RemoveTunnelingVignette();
            CleanScene();
            EnsureHeartItem();
            SetupCollectorTiers();
            ScaleCollectors();
            SnapCollectorTiers();
            SetupHudIcons();
            var coin = TexturedMaterial("Moneda", "Assets/04_Models/MONEDA", .7f, .6f);
            Action<Transform> coinModel = parent => ObjVisual(parent, "Assets/04_Models/MONEDA", coin);

            foreach (var board in Object.FindObjectsByType<Area1WaveBoard>())
                BuildWaveBoard(board, game, coinModel);
            foreach (var shop in Object.FindObjectsByType<Area1FoodShop>())
                BuildFoodShop(shop, game, coinModel);
            foreach (var shop in Object.FindObjectsByType<Area1UpgradeShop>())
                BuildUpgradeShop(shop, coinModel);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!wasDirty) EditorSceneManager.SaveScene(scene);
            Debug.Log("AREA1: tablero de oleadas y tiendas construidos con modelos en la escena.");
        }

        // Heart item sold at the food shop: data asset + prefab (built from the carrot prefab, with the
        // heart model and Area1HealthPickup), registered in the game catalog and added as a shop offer.
        const string HeartDataPath = "Assets/Area1/HeartItem.asset", HeartPrefabPath = "Assets/Area1/HeartItem.prefab",
            CarrotPrefabPath = "Assets/Area1/CarrotItem.prefab";

        static void EnsureHeartItem()
        {
            var game = Object.FindAnyObjectByType<RanchGame>();
            if (!game) return;
            var data = AssetDatabase.LoadAssetAtPath<RanchItemData>(HeartDataPath);
            if (!data)
            {
                data = ScriptableObject.CreateInstance<RanchItemData>();
                data.kind = RanchItemKind.Heart;
                data.itemName = "Corazón";
                data.color = new Color(1, .3f, .42f);
                data.stackLimit = 10;
                data.saleValue = 0;
                data.radius = .15f;
                AssetDatabase.CreateAsset(data, HeartDataPath);
            }
            if (!data.prefab)
            {
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(HeartPrefabPath)) AssetDatabase.CopyAsset(CarrotPrefabPath, HeartPrefabPath);
                var root = PrefabUtility.LoadPrefabContents(HeartPrefabPath);
                try
                {
                    foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                    var fit = new GameObject("Modelo corazon").transform;
                    fit.SetParent(root.transform, false);
                    ObjVisual(fit, "Assets/04_Models/CORAZON", TexturedMaterial("Corazon", "Assets/04_Models/CORAZON", .45f, 0));
                    Fit(fit, .32f);
                    foreach (var renderer in fit.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    root.GetComponent<RanchItem>().data = data;
                    var sphere = root.GetComponent<SphereCollider>();
                    if (sphere) sphere.radius = .15f;
                    if (!root.GetComponent<Area1HealthPickup>()) root.AddComponent<Area1HealthPickup>();
                    PrefabUtility.SaveAsPrefabAsset(root, HeartPrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                data.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeartPrefabPath);
                EditorUtility.SetDirty(data);
            }
            if (!game.catalog.Contains(data))
            {
                game.catalog = game.catalog.Concat(new[] { data }).ToArray();
                EditorUtility.SetDirty(game);
            }
            foreach (var shop in Object.FindObjectsByType<Area1FoodShop>())
                if (!shop.offers.Any(o => o.kind == RanchItemKind.Heart))
                {
                    shop.offers = shop.offers.Concat(new[] { new Area1FoodShop.Offer { kind = RanchItemKind.Heart, price = 25 } }).ToArray();
                    EditorUtility.SetDirty(shop);
                }
            AssetDatabase.SaveAssets();
        }

        static void BuildWaveBoard(Area1WaveBoard board, RanchGame game, Action<Transform> coinModel)
        {
            var screen = Shell(board.transform, "TABLERO DE OLEADAS", new Color(.32f, .24f, .17f), coinModel, out board.header, out board.status);
            var enemy = game ? game.Data(RanchItemKind.EnemySlime) : null;
            var sceneBoss = Object.FindAnyObjectByType<Area1EnemyBoss>();
            var boss = sceneBoss ? sceneBoss.gameObject : board.bossPrefab;
            board.slots = new Area1BoardSlot[board.waves.Length];
            for (int i = 0; i < board.waves.Length; i++)
            {
                Transform source = board.waves[i].boss && boss ? boss.transform : enemy && enemy.prefab ? enemy.prefab.transform : null;
                board.slots[i] = Slot(board.transform, screen, "Nivel " + (i + 1), -400 + i * 200, 180,
                    source ? parent => CopyVisual(source, parent) : (Action<Transform>)null, coinModel);
            }
            EditorUtility.SetDirty(board);
        }

        static void BuildFoodShop(Area1FoodShop shop, RanchGame game, Action<Transform> coinModel)
        {
            var screen = Shell(shop.transform, "TIENDA DE COMIDA Y VIDA", new Color(.2f, .36f, .18f), coinModel, out shop.header, out shop.status);
            shop.slots = new Area1BoardSlot[shop.offers.Length];
            float spacing = 900f / shop.offers.Length;
            for (int i = 0; i < shop.offers.Length; i++)
            {
                var data = game ? game.Data(shop.offers[i].kind) : null;
                shop.slots[i] = Slot(shop.transform, screen, "Producto " + (i + 1), -450 + spacing * (i + .5f), spacing - 30,
                    data && data.prefab ? parent => CopyVisual(data.prefab.transform, parent) : (Action<Transform>)null, coinModel);
            }
            EditorUtility.SetDirty(shop);
        }

        static void BuildUpgradeShop(Area1UpgradeShop shop, Action<Transform> coinModel)
        {
            var screen = Shell(shop.transform, "MEJORAS DEL JUGADOR", new Color(.22f, .2f, .38f), coinModel, out shop.header, out shop.status);
            var heart = TexturedMaterial("Corazon", "Assets/04_Models/CORAZON", .45f, 0);
            var drop = TexturedMaterial("GotaAgua", "Assets/04_Models/GOTA_AGUA", .8f, 0);
            var vacuum = Object.FindAnyObjectByType<SlimeVacuum>();
            var tiers = Object.FindAnyObjectByType<Area1CollectorTiers>();
            var collectorPreview = tiers && tiers.tiers != null && tiers.tiers.Length > 0 ? tiers.tiers[Mathf.Min(1, tiers.tiers.Length - 1)] : null;
            if (!shop.upgrades.Any(u => u.key == Area1UpgradeShop.CollectorKey))
                shop.upgrades = shop.upgrades.Concat(new[] { new Area1UpgradeShop.Upgrade { title = "RECOLECTOR", key = Area1UpgradeShop.CollectorKey, prices = new[] { 250, 600 } } }).ToArray();
            shop.slots = new Area1BoardSlot[shop.upgrades.Length];
            float spacing = 900f / shop.upgrades.Length;
            for (int i = 0; i < shop.upgrades.Length; i++)
            {
                Action<Transform> model = null;
                switch (shop.upgrades[i].key)
                {
                    case Area1UpgradeShop.HealthKey: model = parent => ObjVisual(parent, "Assets/04_Models/CORAZON", heart); break;
                    case Area1UpgradeShop.DamageKey: if (vacuum) model = parent => CopyVisual(vacuum.transform, parent); break;
                    case Area1UpgradeShop.WaterKey: model = parent => ObjVisual(parent, "Assets/04_Models/GOTA_AGUA", drop); break;
                    case Area1UpgradeShop.CollectorKey: if (collectorPreview) model = parent => CopyVisual(collectorPreview.transform, parent); break;
                }
                shop.slots[i] = Slot(shop.transform, screen, shop.upgrades[i].title, -450 + spacing * (i + .5f), spacing - 30, model, coinModel);
            }
            EditorUtility.SetDirty(shop);
        }

        // The XR rig darkens the view edges while moving with the stick; AREA1 does not use it.
        static void RemoveTunnelingVignette()
        {
            foreach (var vignette in Object.FindObjectsByType<TunnelingVignetteController>())
            {
                vignette.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(vignette.gameObject);
            }
        }

        // Keeps one collector spot: all tiers share the basic collector's place and only one is active.
        static void SetupCollectorTiers()
        {
            if (Object.FindAnyObjectByType<Area1CollectorTiers>()) return;
            var collectors = Object.FindObjectsByType<Area1PlortCollector>().OrderBy(c => c.valueMultiplier).ToArray();
            if (collectors.Length < 2) return;
            var basic = collectors[0].transform;
            var root = new GameObject("Recolector de plorts - mejorable");
            root.transform.SetParent(basic.parent, false);
            root.transform.SetPositionAndRotation(basic.position, basic.rotation);
            var tiers = root.AddComponent<Area1CollectorTiers>();
            tiers.tiers = collectors.Select(c => c.gameObject).ToArray();
            foreach (var collector in collectors)
            {
                collector.transform.SetParent(root.transform, true);
                collector.transform.SetPositionAndRotation(basic.position, collector.transform.rotation);
                collector.gameObject.SetActive(collector == collectors[0]);
                if (PrefabUtility.IsPartOfPrefabInstance(collector.gameObject))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(collector.transform);
            }
            EditorUtility.SetDirty(tiers);
        }

        // Wall, dark frame, world-space screen with title, coin + header line and a status line.
        static RectTransform Shell(Transform root, string title, Color wallColor, Action<Transform> coinModel, out Text header, out Text status)
        {
            foreach (Transform child in root.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            Box(root, "Pared del tablero", new Vector3(0, 1.3f, .08f), new Vector3(2.6f, 2.6f, .15f), ColorMaterial("Tablero_Pared_" + title.Replace(" ", "_"), wallColor));
            var frame = Box(root, "Marco", new Vector3(0, ScreenCenter, -.005f), new Vector3(2.12f, 1.52f, .02f), ColorMaterial("Tablero_Marco", new Color(.07f, .1f, .14f)));
            Object.DestroyImmediate(frame.GetComponent<Collider>());

            var canvasObject = new GameObject("Pantalla", typeof(RectTransform));
            canvasObject.transform.SetParent(root, false);
            canvasObject.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvasObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 4;
            var screen = (RectTransform)canvasObject.transform;
            screen.sizeDelta = new Vector2(1000, 700);
            screen.localPosition = new Vector3(0, ScreenCenter, -.02f);
            screen.localScale = Vector3.one * Pixel;

            Label(screen, "Titulo", title, new Vector2(0, 300), new Vector2(980, 70), 50, FontStyle.Bold, TextAnchor.MiddleCenter);
            header = Label(screen, "Monedas", "Monedas: 0", new Vector2(100, 245), new Vector2(700, 40), 28, FontStyle.Normal, TextAnchor.MiddleLeft);
            status = Label(screen, "Estado", "", new Vector2(0, -265), new Vector2(960, 130), 27, FontStyle.Normal, TextAnchor.MiddleCenter);
            Model(root, "Moneda del encabezado", new Vector2(-285, 245), -.07f, .07f, coinModel);
            return screen;
        }

        // Button block (select = GRIP) with a spinning model on top, title, state and coin + price.
        static Area1BoardSlot Slot(Transform root, RectTransform screen, string name, float x, float width, Action<Transform> model, Action<Transform> coinModel)
        {
            const float y = 20, height = 330;
            var button = Box(root, name, new Vector3(x * Pixel, ScreenCenter + y * Pixel, -.03f), new Vector3(width * Pixel, height * Pixel, .04f), ColorMaterial("Tablero_Boton", Area1Board.Locked));
            button.AddComponent<XRSimpleInteractable>();
            Model(root, "Modelo " + name, new Vector2(x, y + 115), -.13f, .13f, model);
            var slot = new Area1BoardSlot
            {
                button = button.GetComponent<Renderer>(),
                title = OnButton(Label(screen, name + " - titulo", name, new Vector2(x, y + 40), new Vector2(width - 12, 64), 24, FontStyle.Bold, TextAnchor.MiddleCenter)),
                state = OnButton(Label(screen, name + " - estado", "", new Vector2(x, y - 40), new Vector2(width - 12, 90), 21, FontStyle.Normal, TextAnchor.MiddleCenter)),
                price = OnButton(Label(screen, name + " - precio", "", new Vector2(x + 18, y - 135), new Vector2(width - 60, 40), 28, FontStyle.Bold, TextAnchor.MiddleCenter)),
                coin = Model(root, "Moneda " + name, new Vector2(x - 40, y - 135), -.1f, .05f, coinModel)
            };
            return slot;
        }

        static Text OnButton(Text label)
        {
            label.rectTransform.localPosition += Vector3.back * 30; // in front of the button block
            return label;
        }

        // "Modelo" (faces the player, sways/floats) > "Ajuste" (fit size, centred) > visual meshes.
        static GameObject Model(Transform root, string name, Vector2 screenPosition, float z, float size, Action<Transform> build)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(root, false);
            holder.transform.localPosition = new Vector3(screenPosition.x * Pixel, ScreenCenter + screenPosition.y * Pixel, z);
            // Models face +Z; turn them toward the player looking at the board from -Z.
            holder.transform.localRotation = Quaternion.Euler(0, Area1ModelSpin.FacingYaw, 0);
            holder.AddComponent<Area1ModelSpin>();
            var fit = new GameObject("Ajuste").transform;
            fit.SetParent(holder.transform, false);
            build?.Invoke(fit);
            Fit(fit, size);
            return holder;
        }

        static void Fit(Transform fit, float size)
        {
            var filters = fit.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh).ToArray();
            if (filters.Length == 0) return;
            var bounds = new Bounds();
            bool first = true;
            foreach (var filter in filters)
            {
                var b = filter.sharedMesh.bounds;
                for (int n = 0; n < 8; n++)
                {
                    var p = b.center + Vector3.Scale(b.extents, new Vector3((n & 1) == 0 ? -1 : 1, (n & 2) == 0 ? -1 : 1, (n & 4) == 0 ? -1 : 1));
                    p = fit.InverseTransformPoint(filter.transform.TransformPoint(p));
                    if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                    else bounds.Encapsulate(p);
                }
            }
            float scale = size / Mathf.Max(.0001f, Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z));
            fit.localScale = Vector3.one * scale;
            fit.localPosition = -bounds.center * scale;
        }

        // Geometry only: never copies gameplay scripts, colliders or rigidbodies.
        static void CopyVisual(Transform source, Transform parent) => CopyNode(source, parent, true);

        static void CopyNode(Transform source, Transform parent, bool top)
        {
            var node = new GameObject(source.name).transform;
            node.SetParent(parent, false);
            if (!top)
            {
                node.localPosition = source.localPosition;
                node.localRotation = source.localRotation;
            }
            node.localScale = source.localScale;
            var filter = source.GetComponent<MeshFilter>();
            var renderer = source.GetComponent<MeshRenderer>();
            if (filter && renderer && renderer.enabled && filter.sharedMesh)
            {
                node.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var copy = node.gameObject.AddComponent<MeshRenderer>();
                copy.sharedMaterials = renderer.sharedMaterials;
                copy.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            foreach (Transform child in source)
                if (child.gameObject.activeSelf) CopyNode(child, node, false);
        }

        static void ObjVisual(Transform parent, string folder, Material material)
        {
            var path = AssetDatabase.FindAssets("t:Model", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
            var asset = path == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset)
            {
                Debug.LogWarning("No se encontro el modelo en " + folder);
                return;
            }
            var model = (GameObject)Object.Instantiate(asset, parent, false);
            model.name = asset.name;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, renderer.sharedMaterials.Length)).ToArray();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static GameObject Box(Transform root, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(root, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        static Text Label(RectTransform screen, string name, string value, Vector2 position, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment)
        {
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(screen, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = fontSize;
            text.text = value;
            return text;
        }

        static Material ColorMaterial(string name, Color color)
        {
            var material = LoadOrCreate(name);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material TexturedMaterial(string name, string folder, float smoothness, float metallic)
        {
            var material = LoadOrCreate(name);
            var texture = AssetDatabase.FindAssets("t:Texture2D", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>).FirstOrDefault();
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material LoadOrCreate(string name)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }
    }
}
