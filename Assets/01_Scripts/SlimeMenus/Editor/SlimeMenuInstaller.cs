using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SlimeRancherVR;

namespace SlimeRancherVR.Editor
{
    public static class SlimeMenuInstaller
    {
        [MenuItem("Beatrix/Instalar menus VR en escena actual")]
        public static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/00_Scenes/MAIN_MENU.unity" && scene.path != "Assets/00_Scenes/AREA1.unity")
                throw new System.Exception("Abre MAIN_MENU o AREA1 antes de instalar los menus.");
            var existing = Object.FindObjectsByType<SlimeMenuController>();
            foreach (var existingController in existing)
                Object.DestroyImmediate(existingController.gameObject);
            const string themePath = "Assets/01_Scripts/SlimeMenus/SlimeMenuTheme.asset";
            var theme = AssetDatabase.LoadAssetAtPath<SlimeMenuTheme>(themePath);
            if (!theme)
            {
                theme = ScriptableObject.CreateInstance<SlimeMenuTheme>();
                AssetDatabase.CreateAsset(theme, themePath);
                AssetDatabase.SaveAssets();
            }
            var root = new GameObject("Menus VR - Slime Rancher");
            var controller = root.AddComponent<SlimeMenuController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("theme").objectReferenceValue = theme;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Menus VR instalados en " + scene.name + ". Personaliza SlimeMenuController y SlimeMenuTheme desde el Inspector.");
        }
    }
}