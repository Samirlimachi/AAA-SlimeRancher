using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SlimeRancher.Area1.Editor
{
    // Creates Assets/Area1/Resources/SonidosJuego.asset and fills each slot with the matching file from
    // Assets/Sonidos (by name). Sounds heard in 3D are imported as mono so the headset can place them.
    // Runs by itself once; the menu item re-assigns (only empty slots are filled, your choices stay).
    [InitializeOnLoad]
    public static class Area1SoundBankSetup
    {
        const string BankPath = "Assets/Area1/Resources/SonidosJuego.asset";
        const string SoundFolder = "Assets/Sonidos";

        // slot, file name (without extension), heard in 3D
        static readonly (string slot, string file, bool spatial)[] Map =
        {
            ("aspiradoraSuccionando", "Aspiradora_succionando", true),
            ("aspiradoraBota", "Aspiradora_vota_objeto", true),
            ("aguaChoque", "Agua_choca_objeto", true),
            ("slimeSalpicadura", "Slime_salpicadura", true),
            ("ataqueSlime", "AtaqueSlimeMalo", true),
            ("slimeComeVegetal", "Slime_come_Vegetal", true),
            ("slimeSueltaPlort", "Slime_suelta_Plort", true),
            ("plortCaeSuelo", "Plort_cae_suelo", true),
            ("plortEntraRecolector", "Plort_entra_extractor", true),
            ("spawnPollo", "SpaneoPollo", true),
            ("jugadorCamina", "Jugador_camina", false),
            ("jugadorSalto", "Salto_Jugador", false),
            ("jugadorAterrizaje", "Jugador_salto_aterrizage", false),
            ("jugadorRecibeDanio", "Recibe_daño_Jugador", false),
            ("clickMenu", "Click_boto_menu", false),
            ("ganoOleada", "Gano_Oleada", false),
        };

        static Area1SoundBankSetup() => EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !AssetDatabase.LoadAssetAtPath<Area1SoundBank>(BankPath)) Setup();
        };

        [MenuItem("Area1/Asignar sonidos del juego")]
        public static void Setup()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BankPath));
            var bank = AssetDatabase.LoadAssetAtPath<Area1SoundBank>(BankPath);
            if (!bank)
            {
                bank = ScriptableObject.CreateInstance<Area1SoundBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }
            var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { SoundFolder }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var serialized = new SerializedObject(bank);
            int assigned = 0;
            foreach (var (slot, file, spatial) in Map)
            {
                var path = clips.FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p).Normalize() == file.Normalize());
                if (path == null) { Debug.LogWarning("Sonido no encontrado para '" + slot + "': " + file); continue; }
                if (spatial && AssetImporter.GetAtPath(path) is AudioImporter importer && !importer.forceToMono)
                {
                    importer.forceToMono = true;
                    importer.SaveAndReimport();
                }
                var property = serialized.FindProperty(slot).FindPropertyRelative("clip");
                if (property.objectReferenceValue) continue; // keep what the user chose
                property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                assigned++;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            Debug.Log("Sonidos del juego: " + assigned + " asignados en " + BankPath + ". Cambia cualquiera desde ese archivo.");
        }
    }
}
