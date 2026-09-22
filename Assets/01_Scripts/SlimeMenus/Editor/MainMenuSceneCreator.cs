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
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.18f, .42f, .5f, 1);
            camera.fieldOfView = 60;
            cameraObject.AddComponent<MainMenuCameraLook>();

            var sunObject = new GameObject("Menu Sun", typeof(Light));
            sunObject.transform.rotation = Quaternion.Euler(45, -30, 0);
            sunObject.GetComponent<Light>().type = LightType.Directional;
            sunObject.GetComponent<Light>().intensity = 1.2f;
            RenderSettings.ambientLight = new Color(.35f, .42f, .45f);

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
            Debug.Log("Escena MAIN_MENU creada y configurada como primera escena de build. AREA1 es la escena de juego.");
        }

        static void CreateEnvironment()
        {
            var environment = new GameObject("Escenario del menu");
            CreatePrimitive("Suelo", PrimitiveType.Plane, environment.transform, new Vector3(0, 0, 3), new Vector3(2, 1, 2), new Color(.28f, .47f, .2f));
            CreatePrimitive("Plataforma del menu", PrimitiveType.Cube, environment.transform, new Vector3(0, .12f, 1.4f), new Vector3(3.4f, .2f, 2.2f), new Color(.56f, .32f, .16f));
            CreatePrimitive("Roca izquierda", PrimitiveType.Sphere, environment.transform, new Vector3(-3, .65f, 3.5f), new Vector3(1.3f, .8f, 1), new Color(.34f, .38f, .36f));
            CreatePrimitive("Roca derecha", PrimitiveType.Sphere, environment.transform, new Vector3(3, .65f, 4.5f), new Vector3(1.1f, .7f, .9f), new Color(.3f, .36f, .38f));
            CreatePrimitive("Estanque", PrimitiveType.Cylinder, environment.transform, new Vector3(-3.5f, .03f, 1.5f), new Vector3(1.7f, .03f, 1.7f), new Color(.05f, .45f, .7f));

            var slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Area1/PinkSlimeItem.prefab");
            if (!slimePrefab) slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/SlimeGameplay/SlimeRosado.prefab");
            if (slimePrefab)
            {
                Spawn(slimePrefab, environment.transform, new Vector3(-1.9f, .45f, 2.2f), "Slime rosa izquierdo");
                Spawn(slimePrefab, environment.transform, new Vector3(2.0f, .45f, 2.8f), "Slime rosa derecho");
            }
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