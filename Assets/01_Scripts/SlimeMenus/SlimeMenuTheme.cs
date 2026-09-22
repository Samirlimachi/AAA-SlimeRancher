using UnityEngine;

namespace SlimeRancherVR
{
    [CreateAssetMenu(fileName = "SlimeMenuTheme", menuName = "Slime Rancher/Menu Theme")]
    public sealed class SlimeMenuTheme : ScriptableObject
    {
        public string gameTitle = "SLIME RANCHER";
        public string subtitle = "La aventura de Beatrix LeBeau";
        public Color background = new Color(0.025f, 0.07f, 0.09f, 0.96f);
        public Color panel = new Color(0.035f, 0.13f, 0.16f, 0.98f);
        public Color panelSecondary = new Color(0.06f, 0.2f, 0.22f, 0.98f);
        public Color accent = new Color(1f, 0.72f, 0.18f, 1f);
        public Color accentSecondary = new Color(0.12f, 0.78f, 0.72f, 1f);
        public Color text = Color.white;
        public Color mutedText = new Color(0.72f, 0.86f, 0.84f, 1f);
        public Font font;
        public Sprite logo;
        public Sprite backgroundImage;
    }
}