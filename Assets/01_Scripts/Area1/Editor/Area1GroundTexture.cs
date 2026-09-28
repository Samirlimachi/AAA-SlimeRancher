using System.IO;
using UnityEditor;
using UnityEngine;

namespace SlimeRancher.Area1.Editor
{
    // Paints a seamless, cartoon grass texture in the Slime Rancher style (bright saturated greens,
    // soft sunny patches, short blades and tiny flowers) plus a matching normal map, and puts them
    // on the AREA1 ground material. Runs by itself once while Suelo.mat has no texture.
    [InitializeOnLoad]
    public static class Area1GroundTexture
    {
        const string MaterialPath = "Assets/06_Materiales/Area1/Suelo.mat";
        const string ColorPath = "Assets/06_Materiales/Generados/SueloPasto.png";
        const string NormalPath = "Assets/06_Materiales/Generados/SueloPasto_Normal.png";
        const int Size = 1024;
        const float TilesAcrossGround = 10; // the 40 m floor repeats the texture every 4 m

        static Area1GroundTexture() => EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material && !material.GetTexture("_BaseMap")) Generate();
        };

        [MenuItem("Area1/Generar suelo de pasto")]
        public static void Generate()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material) { Debug.LogWarning("No se encontro " + MaterialPath); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(ColorPath));

            var random = new System.Random(7);
            var color = new Color[Size * Size];
            var height = new float[Size * Size];
            var deep = new Color(.2f, .47f, .15f);
            var grass = new Color(.39f, .7f, .22f);
            var sunny = new Color(.6f, .83f, .3f);
            var straw = new Color(.74f, .8f, .34f);

            // Base: large soft patches + medium mottling, all tileable.
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float u = (float)x / Size, v = (float)y / Size;
                    float patches = Fbm(u, v, 3, 4, 11);
                    float mottling = Fbm(u, v, 24, 3, 37);
                    float dry = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.62f, .78f, Fbm(u, v, 5, 3, 91)));
                    var c = patches < .5f ? Color.Lerp(deep, grass, patches * 2) : Color.Lerp(grass, sunny, (patches - .5f) * 2);
                    c = Color.Lerp(c, straw, dry * .45f);
                    c *= .9f + mottling * .2f;
                    color[y * Size + x] = c;
                    height[y * Size + x] = patches * .35f + mottling * .25f;
                }

            // Grass blades: short strokes, darker at the root and lighter at the tip.
            for (int i = 0; i < 14000; i++)
            {
                float x0 = (float)random.NextDouble() * Size, y0 = (float)random.NextDouble() * Size;
                float angle = Mathf.PI / 2 + ((float)random.NextDouble() - .5f) * 1.2f;
                float length = 5 + (float)random.NextDouble() * 10;
                float shade = (float)random.NextDouble();
                var root = Color.Lerp(deep, grass, shade * .6f);
                var tip = Color.Lerp(grass, sunny, .4f + shade * .6f);
                for (float t = 0; t <= 1; t += 1 / length)
                {
                    int px = Wrap(Mathf.RoundToInt(x0 + Mathf.Cos(angle) * length * t));
                    int py = Wrap(Mathf.RoundToInt(y0 + Mathf.Sin(angle) * length * t));
                    int index = py * Size + px;
                    color[index] = Color.Lerp(color[index], Color.Lerp(root, tip, t), .55f);
                    height[index] += .12f * (1 - t * .5f);
                }
            }

            // Little flowers: coloured petals around a yellow centre, a few in each tile.
            var petals = new[] { Color.white, new Color(1, .86f, .3f), new Color(1, .55f, .75f), new Color(.65f, .8f, 1) };
            for (int i = 0; i < 140; i++)
            {
                int cx = random.Next(Size), cy = random.Next(Size);
                var petal = petals[random.Next(petals.Length)];
                float radius = 2.5f + (float)random.NextDouble() * 2;
                for (int dy = -6; dy <= 6; dy++)
                    for (int dx = -6; dx <= 6; dx++)
                    {
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d > radius + 1) continue;
                        int index = Wrap(cy + dy) * Size + Wrap(cx + dx);
                        float alpha = Mathf.Clamp01(radius + 1 - d);
                        var c = d < radius * .4f ? new Color(1, .8f, .2f) : petal;
                        color[index] = Color.Lerp(color[index], c, alpha);
                        height[index] += .25f * alpha;
                    }
            }

            var colorTexture = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            colorTexture.SetPixels(color);
            colorTexture.Apply();
            File.WriteAllBytes(ColorPath, colorTexture.EncodeToPNG());

            // Normal map from the painted height (wrapping at the borders so it stays seamless).
            var normal = new Color[Size * Size];
            const float strength = 3.5f;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float dx = height[y * Size + Wrap(x + 1)] - height[y * Size + Wrap(x - 1)];
                    float dy = height[Wrap(y + 1) * Size + x] - height[Wrap(y - 1) * Size + x];
                    var n = new Vector3(-dx * strength, -dy * strength, 1).normalized;
                    normal[y * Size + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f);
                }
            var normalTexture = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            normalTexture.SetPixels(normal);
            normalTexture.Apply();
            File.WriteAllBytes(NormalPath, normalTexture.EncodeToPNG());
            Object.DestroyImmediate(colorTexture);
            Object.DestroyImmediate(normalTexture);

            AssetDatabase.ImportAsset(ColorPath);
            AssetDatabase.ImportAsset(NormalPath);
            Configure(ColorPath, false);
            Configure(NormalPath, true);

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(ColorPath);
            var bump = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
            material.SetTexture("_BaseMap", albedo);
            material.SetTexture("_MainTex", albedo);
            material.SetTextureScale("_BaseMap", Vector2.one * TilesAcrossGround);
            material.SetTextureScale("_MainTex", Vector2.one * TilesAcrossGround);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", bump);
            material.SetFloat("_BumpScale", .7f);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Smoothness", .12f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Debug.Log("AREA1: suelo de pasto generado (" + ColorPath + ") y aplicado a Suelo.mat.");
        }

        static void Configure(string path, bool isNormal)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = Size;
            importer.SaveAndReimport();
        }

        static int Wrap(int value) => ((value % Size) + Size) % Size;

        // Tileable fractal value noise in [0,1]: lattice points wrap every `period` cells.
        static float Fbm(float u, float v, int period, int octaves, int seed)
        {
            float sum = 0, amplitude = .5f, total = 0;
            for (int o = 0; o < octaves; o++)
            {
                sum += Noise(u * period, v * period, period, seed + o * 17) * amplitude;
                total += amplitude;
                amplitude *= .5f;
                period *= 2;
            }
            return sum / total;
        }

        static float Noise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            float a = Hash(x0, y0, period, seed), b = Hash(x0 + 1, y0, period, seed);
            float c = Hash(x0, y0 + 1, period, seed), d = Hash(x0 + 1, y0 + 1, period, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Hash(int x, int y, int period, int seed)
        {
            x = ((x % period) + period) % period;
            y = ((y % period) + period) % period;
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 144269504);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }
    }
}
