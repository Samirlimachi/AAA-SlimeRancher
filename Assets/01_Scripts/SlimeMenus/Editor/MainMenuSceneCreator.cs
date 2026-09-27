using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using SlimeRancherVR;

namespace SlimeRancherVR.Editor
{
    public static class MainMenuSceneCreator
    {
        const string ScenePath = "Assets/00_Scenes/MAIN_MENU.unity";
        const string SkyboxPath = "Assets/Area1/Materiales/CieloArea1.mat";

        [MenuItem("Beatrix/Crear o actualizar escena MAIN_MENU")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraAnchor = new GameObject("Main Camera Anchor");
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(cameraAnchor.transform, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.localPosition = new Vector3(0, 1.6f, -3f);
            cameraObject.transform.localRotation = Quaternion.identity;
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 60;
            SetupAtmosphere();
            cameraObject.AddComponent<MainMenuCameraLook>();

            CreateEnvironment();

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var menuRoot = new GameObject("Menus VR - Slime Rancher");
            var controller = menuRoot.AddComponent<SlimeMenuController>();
            var theme = AssetDatabase.LoadAssetAtPath<SlimeMenuTheme>("Assets/01_Scripts/SlimeMenus/SlimeMenuTheme.asset");
            if (theme)
            {
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("theme").objectReferenceValue = theme;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/00_Scenes/AREA1.unity", true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Escena MAIN_MENU creada y configurada como primera escena de build. AREA1 es la escena de juego.");
        }

        [MenuItem("Beatrix/Actualizar entorno de MAIN_MENU")]
        public static void UpdateEnvironmentInOpenScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath || Application.isPlaying)
            {
                Debug.LogWarning("Abre MAIN_MENU fuera de Play para actualizar su entorno.");
                return;
            }

            SetupAtmosphere();
            var previous = GameObject.Find("Escenario del menu");
            if (previous) Object.DestroyImmediate(previous);
            CreateEnvironment();
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Entorno de MAIN_MENU actualizado sin reemplazar el resto de la escena. Revisa y guarda con Ctrl+S.");
        }

        static void SetupAtmosphere()
        {
            var camera = Camera.main;
            if (camera) camera.clearFlags = CameraClearFlags.Skybox;
            SetupSky();

            var sunObject = GameObject.Find("Menu Sun");
            if (!sunObject) sunObject = new GameObject("Menu Sun");
            var sun = sunObject.GetComponent<Light>();
            if (!sun) sun = sunObject.AddComponent<Light>();
            sunObject.transform.rotation = Quaternion.Euler(45, -30, 0);
            sun.type = LightType.Directional;
            sun.color = new Color(.88f, .94f, 1f);
            sun.intensity = 1.1f;
            RenderSettings.sun = sun;
            RenderSettings.ambientLight = new Color(.3f, .4f, .48f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.74f, .86f, .95f);
            RenderSettings.fogStartDistance = 30;
            RenderSettings.fogEndDistance = 130;
        }

        static void CreateEnvironment()
        {
            var environment = new GameObject("Escenario del menu");
            var groundMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Area1/Suelo.mat");
            var grassMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Area1/Materiales/PastoMata.mat");
            var flowerMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Area1/Materiales/PastoFlores.mat");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Suelo de pasto";
            ground.transform.SetParent(environment.transform, false);
            ground.transform.SetPositionAndRotation(new Vector3(0, -.1f, 30), Quaternion.identity);
            ground.transform.localScale = new Vector3(25, 1, 25);
            if (groundMaterial) ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            CreateFoliage(environment.transform, "Matas de pasto", "Assets/Area1/Materiales/Matasdepasto.asset", grassMaterial);
            CreateFoliage(environment.transform, "Flores", "Assets/Area1/Materiales/Flores.asset", flowerMaterial);
            CreateHills(environment.transform, groundMaterial);
            CreatePrimitive("Plataforma del menu", PrimitiveType.Cube, environment.transform, new Vector3(0, .12f, 1.4f), new Vector3(3.4f, .2f, 2.2f), new Color(.56f, .32f, .16f));
            CreatePond(environment.transform);

            var slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Area1/PinkSlimeItem.prefab");
            if (!slimePrefab) slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/SlimeGameplay/SlimeRosado.prefab");
            if (slimePrefab)
            {
                Spawn(slimePrefab, environment.transform, new Vector3(-1.9f, .45f, 2.2f), "Slime rosa izquierdo");
                Spawn(slimePrefab, environment.transform, new Vector3(2.0f, .45f, 2.8f), "Slime rosa derecho");
            }
        }

        static void SetupSky()
        {
            var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            var shader = Shader.Find("Skybox/Procedural");
            if (!shader) { Debug.LogWarning("No se encontro el shader Skybox/Procedural."); return; }
            if (!sky)
            {
                sky = new Material(shader) { name = "Cielo AREA1" };
                AssetDatabase.CreateAsset(sky, SkyboxPath);
            }
            sky.shader = shader;
            sky.SetFloat("_SunDisk", 0);
            sky.SetFloat("_SunSize", .025f);
            sky.SetFloat("_AtmosphereThickness", 1f);
            sky.SetColor("_SkyTint", new Color(.38f, .66f, .88f));
            sky.SetColor("_GroundColor", new Color(.34f, .47f, .34f));
            sky.SetFloat("_Exposure", .85f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
        }

        static void CreateHills(Transform parent, Material material)
        {
            var random = new System.Random(2026);
            for (int i = 0; i < 15; i++)
            {
                float x = (i - 7) * 17 + (float)random.NextDouble() * 7 - 3.5f;
                float distance = 66 + (float)random.NextDouble() * 24;
                float width = 22 + (float)random.NextDouble() * 18;
                float height = 8 + (float)random.NextDouble() * 11;
                var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.name = "Colina";
                hill.transform.SetParent(parent, false);
                hill.transform.SetPositionAndRotation(new Vector3(x, -.1f, distance), Quaternion.identity);
                hill.transform.localScale = new Vector3(width, height, width * (.8f + (float)random.NextDouble() * .4f));
                if (material) hill.GetComponent<Renderer>().sharedMaterial = material;
                Object.DestroyImmediate(hill.GetComponent<Collider>());
            }
        }

        static void CreatePond(Transform parent)
        {
            var root = new GameObject("Estanque de AREA1").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(-3.5f, 0, 1.5f);

            var bedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Area1/PondStone.mat");
            var waterMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Area1/PondWater.mat");
            var rockAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/04_Models/PIEDRA 1/Piedra1.obj");
            var bed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bed.name = "Lecho de piedra";
            bed.transform.SetParent(root, false);
            bed.transform.localPosition = new Vector3(0, -.14f, 0);
            bed.transform.localScale = new Vector3(2.45f, .12f, 2.45f);
            if (bedMaterial) bed.GetComponent<Renderer>().sharedMaterial = bedMaterial;
            Object.DestroyImmediate(bed.GetComponent<Collider>());

            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Agua";
            water.transform.SetParent(root, false);
            water.transform.localPosition = new Vector3(0, -.07f, 0);
            water.transform.localScale = new Vector3(1.8f, .018f, 1.8f);
            if (waterMaterial) water.GetComponent<Renderer>().sharedMaterial = waterMaterial;
            Object.DestroyImmediate(water.GetComponent<Collider>());

            if (!rockAsset) return;
            var stoneMaterial = bedMaterial ? bedMaterial : AssetDatabase.LoadAssetAtPath<Material>("Assets/Area1/Materiales/Piedra.mat");
            var random = new System.Random(13);
            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI * 2 / 10 + (float)random.NextDouble() * .2f;
                float size = .65f + (float)random.NextDouble() * .55f;
                var holder = new GameObject("Piedra del estanque").transform;
                holder.SetParent(root, false);
                var stone = (GameObject)PrefabUtility.InstantiatePrefab(rockAsset);
                stone.name = rockAsset.name;
                stone.transform.SetParent(holder, false);
                foreach (var renderer in stone.GetComponentsInChildren<Renderer>(true))
                    if (stoneMaterial) renderer.sharedMaterial = stoneMaterial;
                var bounds = new Bounds();
                bool hasBounds = false;
                foreach (var renderer in stone.GetComponentsInChildren<Renderer>(true))
                {
                    if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (!hasBounds) { Object.DestroyImmediate(holder.gameObject); continue; }
                float scale = size / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                stone.transform.localScale = Vector3.one * scale;
                var center = holder.InverseTransformPoint(bounds.center);
                var bottom = holder.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
                stone.transform.localPosition = new Vector3(-center.x, -bottom.y, -center.z) * scale;
                holder.SetPositionAndRotation(
                    new Vector3(Mathf.Cos(angle) * 2.05f, -.1f - size * .1f, Mathf.Sin(angle) * 2.05f),
                    Quaternion.Euler((float)random.NextDouble() * 12 - 6, (float)random.NextDouble() * 360, (float)random.NextDouble() * 12 - 6));
            }
        }

        static void CreateFoliage(Transform parent, string name, string meshPath, Material material)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (!mesh || !material) return;
            var foliage = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            foliage.transform.SetParent(parent, false);
            foliage.GetComponent<MeshFilter>().sharedMesh = mesh;
            foliage.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void Spawn(GameObject prefab, Transform parent, Vector3 position, string name)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            var body = instance.GetComponent<Rigidbody>();
            if (body) body.isKinematic = true;
            var grab = instance.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab) grab.enabled = false;
        }

        static void CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var objectRoot = GameObject.CreatePrimitive(type);
            objectRoot.name = name;
            objectRoot.transform.SetParent(parent, false);
            objectRoot.transform.SetPositionAndRotation(position, Quaternion.identity);
            objectRoot.transform.localScale = scale;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            objectRoot.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}