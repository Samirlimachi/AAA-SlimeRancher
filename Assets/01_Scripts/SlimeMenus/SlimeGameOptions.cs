using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace SlimeRancherVR
{
    // Simple player options, kept in PlayerPrefs and applied on every scene: master volume, music volume,
    // controller vibration and smooth vs. snap turning with the right stick.
    public static class SlimeGameOptions
    {
        const string VolumeKey = "Opciones.Volumen", MusicKey = "Opciones.Musica", HapticsKey = "Opciones.Vibracion", SmoothTurnKey = "Opciones.GiroSuave";

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1);
            set { PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); Apply(); }
        }

        // Multiplies every music track (menu, AREA1 background, waves); read live by the music players.
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 1);
            set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public static bool Haptics
        {
            get => PlayerPrefs.GetInt(HapticsKey, 1) == 1;
            set { PlayerPrefs.SetInt(HapticsKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool SmoothTurn
        {
            get => PlayerPrefs.GetInt(SmoothTurnKey, 0) == 1;
            set { PlayerPrefs.SetInt(SmoothTurnKey, value ? 1 : 0); PlayerPrefs.Save(); Apply(); }
        }

        public static void Apply()
        {
            AudioListener.volume = Volume;
            foreach (var manager in Object.FindObjectsByType<ControllerInputActionManager>())
                manager.smoothTurnEnabled = SmoothTurn;
        }
    }
}
