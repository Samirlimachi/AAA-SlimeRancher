using UnityEditor;
using UnityEditor.SceneManagement;

namespace SlimeRancherVR.Editor
{
    // Pressing Play in the Editor always starts at MAIN_MENU (like the built game), whatever scene is
    // open. Toggle with Beatrix > Iniciar Play desde MAIN_MENU.
    [InitializeOnLoad]
    public static class PlayFromMainMenu
    {
        const string ScenePath = "Assets/00_Scenes/MAIN_MENU.unity";
        const string MenuPath = "Beatrix/Iniciar Play desde MAIN_MENU";
        const string PrefKey = "SlimeRancher.PlayFromMainMenu";

        static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        static PlayFromMainMenu() => EditorApplication.delayCall += Apply;

        static void Apply()
        {
            EditorSceneManager.playModeStartScene = Enabled ? AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) : null;
            Menu.SetChecked(MenuPath, Enabled);
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }
    }
}
