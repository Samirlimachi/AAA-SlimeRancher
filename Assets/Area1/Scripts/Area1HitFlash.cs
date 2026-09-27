using UnityEngine;

namespace SlimeRancher.Area1
{
    // Short "got hit" feedback on any object: red tint on its renderers (_BaseColor) and a squash punch.
    public sealed class Area1HitFlash : MonoBehaviour
    {
        const float Duration = .35f, PunchTime = .25f;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color Hurt = new Color(1, .15f, .12f);

        Renderer[] renderers;
        Color[][] originals;
        MaterialPropertyBlock block;
        Vector3 baseScale;
        float start = -10;

        public static void Play(GameObject target)
        {
            if (!target) return;
            var flash = target.GetComponent<Area1HitFlash>();
            if (!flash) flash = target.AddComponent<Area1HitFlash>();
            flash.Restart();
        }

        void Awake()
        {
            baseScale = transform.localScale;
            block = new MaterialPropertyBlock();
            renderers = GetComponentsInChildren<Renderer>();
            originals = new Color[renderers.Length][];
            for (int r = 0; r < renderers.Length; r++)
            {
                var materials = renderers[r].sharedMaterials;
                originals[r] = new Color[materials.Length];
                for (int m = 0; m < materials.Length; m++)
                    originals[r][m] = materials[m] && materials[m].HasProperty(BaseColor) ? materials[m].GetColor(BaseColor) : Color.white;
            }
        }

        void Restart()
        {
            // A hit during the punch keeps the original size as the reference.
            if (Time.time - start > PunchTime) baseScale = transform.localScale;
            start = Time.time;
            enabled = true;
        }

        void Update()
        {
            float t = Time.time - start;
            float tint = Mathf.Clamp01(1 - t / Duration);
            ApplyTint(tint * .85f);
            transform.localScale = t < PunchTime
                ? Vector3.Scale(baseScale, new Vector3(1 + Mathf.Sin(t / PunchTime * Mathf.PI) * .18f, 1 - Mathf.Sin(t / PunchTime * Mathf.PI) * .14f, 1 + Mathf.Sin(t / PunchTime * Mathf.PI) * .18f))
                : baseScale;
            if (t >= Duration) { ApplyTint(0); enabled = false; }
        }

        void ApplyTint(float amount)
        {
            for (int r = 0; r < renderers.Length; r++)
            {
                var renderer = renderers[r];
                if (!renderer || renderer is ParticleSystemRenderer) continue;
                for (int m = 0; m < originals[r].Length; m++)
                {
                    if (amount <= 0) { renderer.SetPropertyBlock(null, m); continue; }
                    renderer.GetPropertyBlock(block, m);
                    block.SetColor(BaseColor, Color.Lerp(originals[r][m], Hurt, amount));
                    renderer.SetPropertyBlock(block, m);
                }
            }
        }

        void OnDisable()
        {
            if (renderers != null) ApplyTint(0);
            if (Time.time - start < PunchTime) transform.localScale = baseScale;
        }
    }
}
