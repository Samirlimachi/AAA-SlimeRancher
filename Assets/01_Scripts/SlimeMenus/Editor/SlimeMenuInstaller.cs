using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SlimeRancherVR;

namespace SlimeRancherVR.Editor
{
    [InitializeOnLoad]
    public static class SlimeMenuInstaller
    {
        const string ThemePath = "Assets/01_Scripts/SlimeMenus/SlimeMenuTheme.asset";
        const string LogoPath = "Assets/Menus/Slime_Rancher_logo.png";

        static SlimeMenuInstaller() => EditorApplication.delayCall += ConfigureLogo;

        [MenuItem("Beatrix/Conectar logo del menu")]
        static void ConfigureLogo()
        {
            if (AssetImporter.GetAtPath(LogoPath) is TextureImporter importer &&
                (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || !importer.alphaIsTransparency))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            var theme = AssetDatabase.LoadAssetAtPath<SlimeMenuTheme>(ThemePath);
            if (!logo)
            {
                Debug.LogError("No se pudo importar el logo como Sprite unico: " + LogoPath);
                return;
            }
            if (!theme)
            {
                Debug.LogError("No se encontro el tema del menu: " + ThemePath);
                return;
            }
            if (theme.logo == logo)
            {
                Debug.Log("Logo Slime Rancher ya conectado al tema.");
                return;
            }
            theme.logo = logo;
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("Logo Slime Rancher conectado al tema del menu.");
        }

        [MenuItem("Beatrix/Instalar menus VR en escena actual")]
        public static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/00_Scenes/MAIN_MENU.unity" && scene.path != "Assets/00_Scenes/AREA1.unity")
                throw new System.Exception("Abre MAIN_MENU o AREA1 antes de instalar los menus.");
            var existing = Object.FindObjectsByType<SlimeMenuController>();
            foreach (var existingController in existing)
                Object.DestroyImmediate(existingController.gameObject);
            var theme = AssetDatabase.LoadAssetAtPath<SlimeMenuTheme>(ThemePath);
            if (!theme)
            {
                theme = ScriptableObject.CreateInstance<SlimeMenuTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
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