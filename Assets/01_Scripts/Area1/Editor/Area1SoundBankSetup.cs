using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SlimeRancher.Area1.Editor
{
    // Creates Assets/03_SO/Resources/SonidosJuego.asset and fills each slot with the matching file from
    // Assets/05_Sonidos (by name). Sounds heard in 3D are imported as mono so the headset can place them.
    // Runs by itself once; the menu item re-assigns (only empty slots are filled, your choices stay).
    [InitializeOnLoad]
    public static class Area1SoundBankSetup
    {
        const string BankPath = "Assets/03_SO/Resources/SonidosJuego.asset";
        const string SoundFolder = "Assets/05_Sonidos";

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
            ("cacareoPollo1", "chicken-noise-1", true),
            ("cacareoPollo2", "chicken-noise-2", true),
            ("cacareoPollo3", "chicken-noise-3", true),
            ("ambienteArea1", "area1-bg-1", false),
            ("musicaFondo", "MUSICA _FONDO", false),
            ("musicaOleada", "MUSICA OLEADA", false),
            ("musicaMenu", "MUSICA_MENU", false),
            ("compra", "SONIDO DE COMPRAS", true),
            ("jugadorCamina", "Jugador_camina", false),
            ("jugadorSalto", "Salto_Jugador", false),
            ("jugadorAterrizaje", "Jugador_salto_aterrizage", false),
            ("jugadorRecibeDanio", "Recibe_daño_Jugador", false),
            ("clickMenu", "Click_boto_menu", false),
            ("ganoOleada", "Gano_Oleada", false),
        };

        // Long music files: streamed from disk and compressed, instead of fully loaded in memory.
        static readonly string[] Music = { "musicaFondo", "musicaOleada", "musicaMenu" };
        // Slots added after the first setup: filled automatically when their file appears.
        static readonly string[] AutoFill = { "musicaFondo", "musicaOleada", "musicaMenu", "compra" };

        static Area1SoundBankSetup() => EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var bank = AssetDatabase.LoadAssetAtPath<Area1SoundBank>(BankPath);
            if (!bank || HasEmptySlotWithFile(bank)) Setup();
        };

        // A music slot is still empty although its file is in Assets/05_Sonidos (other slots may be emptied on purpose).
        static bool HasEmptySlotWithFile(Area1SoundBank bank)
        {
            var names = AssetDatabase.FindAssets("t:AudioClip", new[] { SoundFolder })
                .Select(g => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)).Normalize()).ToArray();
            var serialized = new SerializedObject(bank);
            return Map.Any(m => AutoFill.Contains(m.slot) && names.Contains(m.file.Normalize()) &&
                serialized.FindProperty(m.slot)?.FindPropertyRelative("clip").objectReferenceValue == null);
        }

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
                if (Music.Contains(slot) && AssetImporter.GetAtPath(path) is AudioImporter musicImporter &&
                    musicImporter.defaultSampleSettings.loadType != AudioClipLoadType.Streaming)
                {
                    var settings = musicImporter.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.Streaming;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = .7f;
                    musicImporter.defaultSampleSettings = settings;
                    musicImporter.SaveAndReimport();
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
