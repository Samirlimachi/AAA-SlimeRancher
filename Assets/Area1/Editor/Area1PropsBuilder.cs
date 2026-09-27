using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlimeRancher.Area1.Editor
{
    // Turns the two grey placeholder boxes of AREA1 into proper props, keeping their colliders:
    //  - "Mesa de la aspiradora": wooden table (top, legs, rails) with a painted plank texture.
    //  - "Plataforma para probar salto": cobblestone block with a grass cap.
    // The original box renderer is hidden; the new pieces are visual children without colliders.
    // Runs by itself once when AREA1 is open; the menu item rebuilds.
    [InitializeOnLoad]
    public static class Area1PropsBuilder
    {
        const string ScenePath = "Assets/00_Scenes/AREA1.unity";
        const string Folder = "Assets/Area1/Materiales";
        const string TableName = "Mesa de la aspiradora", PlatformName = "Plataforma para probar salto";
        const string VisualName = "Modelo decorado";

        static Area1PropsBuilder() => EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().path != ScenePath) return;
            var table = GameObject.Find(TableName);
            var platform = GameObject.Find(PlatformName);
            if ((table && !table.transform.Find(VisualName)) || (platform && !platform.transform.Find(VisualName))) Build();
        };

        [MenuItem("Area1/Decorar mesa y plataforma")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath || Application.isPlaying) { Debug.LogWarning("Abre AREA1 fuera de Play."); return; }
            Directory.CreateDirectory(Folder);
            var wood = Material("MaderaMesa", WoodTexture("MaderaTablas"), new Vector2(1, 1), .25f);
            var darkWood = Material("MaderaMesaOscura", WoodTexture("MaderaTablas"), new Vector2(1, 1), .2f, new Color(.62f, .5f, .42f));
            var stone = Material("PiedraPlataforma", StoneTexture("PiedraAdoquin"), new Vector2(1.5f, .4f), .15f);
            var grassSource = AssetDatabase.LoadAssetAtPath<Material>("Assets/Area1/Suelo.mat");
            var grass = Material("PastoPlataforma", grassSource ? grassSource.GetTexture("_BaseMap") as Texture2D : null, new Vector2(.8f, .8f), .1f);
            if (grassSource && grassSource.GetTexture("_BumpMap")) { grass.SetTexture("_BumpMap", grassSource.GetTexture("_BumpMap")); grass.EnableKeyword("_NORMALMAP"); }

            var table = GameObject.Find(TableName);
            if (table) BuildTable(table.transform, wood, darkWood);
            var platform = GameObject.Find(PlatformName);
            if (platform) BuildPlatform(platform.transform, stone, grass);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("AREA1: mesa de madera y plataforma de piedra con pasto listas. Guarda la escena con Ctrl+S.");
        }

        // The box is 1 x 1 x 1 in local space (scaled by the object); parts are placed in that space
        // and counter-scaled, so they keep real proportions whatever the box size.
        static Transform Visual(Transform box)
        {
            var old = box.Find(VisualName);
            if (old) Object.DestroyImmediate(old.gameObject);
            var renderer = box.GetComponent<MeshRenderer>();
            if (renderer) renderer.enabled = false; // keep the collider, hide the grey box
            var visual = new GameObject(VisualName).transform;
            visual.SetParent(box, false);
            return visual;
        }

        static void Part(Transform visual, Transform box, string name, Vector3 centerMeters, Vector3 sizeMeters, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(visual, false);
            var s = box.lossyScale;
            part.transform.localPosition = new Vector3(centerMeters.x / s.x, centerMeters.y / s.y, centerMeters.z / s.z);
            part.transform.localScale = new Vector3(sizeMeters.x / s.x, sizeMeters.y / s.y, sizeMeters.z / s.z);
            part.GetComponent<Renderer>().sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(part, StaticEditorFlags.BatchingStatic);
        }

        static void BuildTable(Transform box, Material wood, Material darkWood)
        {
            var visual = Visual(box);
            var size = box.lossyScale; // meters (the box is centred on its position)
            float w = size.x, h = size.y, d = size.z, top = .09f, leg = .1f;
            float legHeight = h - top;
            // Table top slightly larger than the box, with a darker edge band.
            Part(visual, box, "Tablero", new Vector3(0, h / 2 - top / 2, 0), new Vector3(w + .08f, top, d + .08f), wood);
            Part(visual, box, "Borde del tablero", new Vector3(0, h / 2 - top - .03f, 0), new Vector3(w - .06f, .06f, d - .06f), darkWood);
            // Four legs.
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Part(visual, box, "Pata", new Vector3(x * (w / 2 - leg), -top / 2, z * (d / 2 - leg)), new Vector3(leg, legHeight, leg), darkWood);
            // Rails between the legs near the floor.
            Part(visual, box, "Travesaño", new Vector3(0, -h / 2 + .18f, d / 2 - leg), new Vector3(w - leg * 2, .06f, .05f), darkWood);
            Part(visual, box, "Travesaño", new Vector3(0, -h / 2 + .18f, -(d / 2 - leg)), new Vector3(w - leg * 2, .06f, .05f), darkWood);
            Part(visual, box, "Travesaño", new Vector3(w / 2 - leg, -h / 2 + .18f, 0), new Vector3(.05f, .06f, d - leg * 2), darkWood);
            Part(visual, box, "Travesaño", new Vector3(-(w / 2 - leg), -h / 2 + .18f, 0), new Vector3(.05f, .06f, d - leg * 2), darkWood);
        }

        static void BuildPlatform(Transform box, Material stone, Material grass)
        {
            var visual = Visual(box);
            var size = box.lossyScale;
            float w = size.x, h = size.y, d = size.z;
            // Stone body, a grass cap that overhangs a little, and a slightly wider base.
            Part(visual, box, "Bloque de piedra", new Vector3(0, -.04f, 0), new Vector3(w, h - .08f, d), stone);
            Part(visual, box, "Base de piedra", new Vector3(0, -h / 2 + .06f, 0), new Vector3(w + .12f, .12f, d + .12f), stone);
            Part(visual, box, "Pasto de arriba", new Vector3(0, h / 2 - .05f, 0), new Vector3(w + .1f, .1f, d + .1f), grass);
        }

        static Material Material(string name, Texture2D texture, Vector2 tiling, float smoothness, Color? tint = null)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetColor("_BaseColor", tint ?? Color.white);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Warm cartoon planks: grain streaks, dark gaps between boards, a few knots.
        static Texture2D WoodTexture(string name)
        {
            string path = Folder + "/" + name + ".png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing) return existing;
            const int size = 512, planks = 6;
            var random = new System.Random(21);
            var tints = new float[planks];
            for (int i = 0; i < planks; i++) tints[i] = .85f + (float)random.NextDouble() * .3f;
            var baseColor = new Color(.62f, .4f, .22f);
            var pixels = new Color[size * size];
            var knots = new Vector3[10];
            for (int k = 0; k < knots.Length; k++) knots[k] = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), 4 + (float)random.NextDouble() * 5);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size, v = (float)y / size;
                    int plank = Mathf.Min(planks - 1, (int)(v * planks));
                    float inPlank = v * planks - plank;
                    float grain = Mathf.Sin((v * 90 + Noise(u * 6, v * 30, 6, 30, 7 + plank) * 6) * Mathf.PI) * .5f + .5f;
                    var c = baseColor * tints[plank] * (.88f + grain * .18f);
                    foreach (var knot in knots)
                    {
                        float dx = Mathf.Abs(u - knot.x); dx = Mathf.Min(dx, 1 - dx);
                        float dy = Mathf.Abs(v - knot.y); dy = Mathf.Min(dy, 1 - dy);
                        float r = Mathf.Sqrt(dx * dx * 4 + dy * dy) * size / knot.z;
                        if (r < 2) c *= .7f + .3f * Mathf.Clamp01(r - 1) + .1f * Mathf.Sin(r * 9);
                    }
                    if (inPlank < .03f || inPlank > .97f) c *= .45f; // gap between planks
                    c.a = 1;
                    pixels[y * size + x] = c;
                }
            return Save(path, pixels, size);
        }

        // Cartoon cobblestones: rounded cells in blue-grey tones with dark mortar (tileable Voronoi).
        static Texture2D StoneTexture(string name)
        {
            string path = Folder + "/" + name + ".png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing) return existing;
            const int size = 512, cells = 26;
            var random = new System.Random(5);
            var points = new Vector2[cells];
            var shades = new Color[cells];
            for (int i = 0; i < cells; i++)
            {
                points[i] = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
                float g = .42f + (float)random.NextDouble() * .2f;
                shades[i] = new Color(g, g * 1.02f, g * 1.1f);
            }
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((float)x / size, (float)y / size);
                    float first = 9, second = 9; int nearest = 0;
                    for (int i = 0; i < cells; i++)
                    {
                        var delta = p - points[i];
                        delta.x -= Mathf.Round(delta.x); delta.y -= Mathf.Round(delta.y); // wrap: seamless
                        float dist = delta.magnitude;
                        if (dist < first) { second = first; first = dist; nearest = i; }
                        else if (dist < second) second = dist;
                    }
                    float edge = Mathf.Clamp01((second - first) * 22);
                    float speckle = .92f + Noise(p.x * 40, p.y * 40, 40, 40, 3) * .16f;
                    var c = Color.Lerp(new Color(.18f, .17f, .19f), shades[nearest] * speckle * (.85f + .15f * edge), Mathf.SmoothStep(0, 1, edge));
                    c.a = 1;
                    pixels[y * size + x] = c;
                }
            return Save(path, pixels, size);
        }

        static Texture2D Save(string path, Color[] pixels, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Tileable value noise in [0,1] (periods in lattice cells).
        static float Noise(float x, float y, int periodX, int periodY, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float Hash(int hx, int hy)
            {
                hx = ((hx % periodX) + periodX) % periodX; hy = ((hy % periodY) + periodY) % periodY;
                unchecked { uint h = (uint)(hx * 374761393 + hy * 668265263 + seed * 144269504); h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16; return (h & 0xFFFFFF) / (float)0xFFFFFF; }
            }
            return Mathf.Lerp(Mathf.Lerp(Hash(x0, y0), Hash(x0 + 1, y0), fx), Mathf.Lerp(Hash(x0, y0 + 1), Hash(x0 + 1, y0 + 1), fx), fy);
        }
    }
}
