using UnityEngine;

namespace SlimeRancherVR
{
    [CreateAssetMenu(fileName = "SlimeMenuTheme", menuName = "Slime Rancher/Menu Theme")]
    public sealed class SlimeMenuTheme : ScriptableObject
    {
        public string gameTitle = "SLIME RANCHER";
        public string subtitle = "La aventura de Beatrix LeBeau";
        public Color background = new Color(0.025f, 0.075f, 0.1f, 0.96f);
        public Color panel = new Color(0.035f, 0.1f, 0.15f, 0.98f);
        public Color panelSecondary = new Color(0.1f, 0.31f, 0.2f, 0.98f);
        public Color accent = new Color(1f, 0.77f, 0.2f, 1f);
        public Color accentSecondary = new Color(0.31f, 0.82f, 0.29f, 1f);
        public Color text = new Color(0.98f, 0.99f, 0.9f, 1f);
        public Color mutedText = new Color(0.71f, 0.88f, 0.64f, 1f);
        public Font font;
        public Sprite logo;
        public Sprite backgroundImage;
    }
}