using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SlimeRancherVR.Editor
{
    // MAIN_MENU was built with a plain camera and a mouse-only EventSystem, so in the headset there
    // were no controller rays to press the menu buttons. This adds the same XR rig used in AREA1,
    // switches the EventSystem to XRUIInputModule (rays + mouse) and turns off the old camera.
    // Movement is disabled in the menu (turning stays). Runs by itself when MAIN_MENU is opened.
    [InitializeOnLoad]
    public static class MainMenuVRSetup
    {
        const string ScenePath = "Assets/00_Scenes/MAIN_MENU.unity";
        const string RigPrefab = "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Hands Variant.prefab";
        const float EyeHeight = 1.7f;

        static MainMenuVRSetup()
        {
            EditorApplication.delayCall += AutoSetup;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.GetActiveScene().path != ScenePath) return;
            if (!Object.FindAnyObjectByType<XROrigin>(FindObjectsInactive.Include)) Setup();
        }

        [MenuItem("Beatrix/Configurar VR en MAIN_MENU")]
        public static void Setup()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath || Application.isPlaying)
            {
                Debug.LogWarning("Abre MAIN_MENU fuera de Play para configurar el VR.");
                return;
            }
            bool wasDirty = scene.isDirty;

            // Old desktop camera: the rig brings its own tracked camera (tagged MainCamera).
            var oldCamera = Object.FindObjectsByType<Camera>().FirstOrDefault(c => !c.GetComponentInParent<XROrigin>());
            Vector3 position = new Vector3(0, 0, -3);
            float yaw = 0;
            if (oldCamera)
            {
                position = oldCamera.transform.position - Vector3.up * EyeHeight;
                yaw = oldCamera.transform.eulerAngles.y;
                oldCamera.gameObject.SetActive(false);
            }

            if (!Object.FindAnyObjectByType<XROrigin>(FindObjectsInactive.Include))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefab);
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                rig.name = "Jugador XR - Menu";
                rig.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
                var origin = rig.GetComponent<XROrigin>();
                if (origin)
                {
                    // Same tracking setup as AREA1.
                    origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
                    origin.CameraYOffset = EyeHeight;
                }
                // Menu scene: point and click only, no walking, jumping, teleporting or vignette.
                foreach (var provider in rig.GetComponentsInChildren<LocomotionProvider>(true))
                    if (!(provider is SnapTurnProvider) && !(provider is ContinuousTurnProvider)) provider.enabled = false;
                foreach (var vignette in rig.GetComponentsInChildren<TunnelingVignetteController>(true))
                    vignette.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rig);
            }

            if (!Object.FindAnyObjectByType<XRInteractionManager>())
                new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (!eventSystem) eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            foreach (var module in eventSystem.GetComponents<InputSystemUIInputModule>()) Object.DestroyImmediate(module);
            if (!eventSystem.GetComponent<XRUIInputModule>()) eventSystem.gameObject.AddComponent<XRUIInputModule>();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!wasDirty) EditorSceneManager.SaveScene(scene);
            Debug.Log("MAIN_MENU: rig XR con rayos agregado; ya se pueden pulsar los botones del menu en VR.");
        }
    }
}
