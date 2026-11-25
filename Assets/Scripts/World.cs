// ពិភពលោក៖ ដីភ្នំ វាលស្រែ ព្រៃឫស្សី ទន្លេ ផ្ទះឈើខ្ពស់ ដើមត្នោត ជំរុំចោរ — បង្កើតដោយកូដទាំងអស់
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KhmerHeroes
{
    public static class World
    {
        public const float WaterY = 4.2f;
        public const float RiverZ = 475f, RiverHalf = 13f;
        public const float MinX = -80, MinZ = -20, SizeX = 160, SizeZ = 740, SizeY = 60;
        // ព្រំដែនតំបន់ (z)
        public static readonly float[] ZoneStart = { -20, 128, 268, 395, 560 };
        public static Terrain terrain;
        static Transform props;
        static System.Random rnd = new System.Random(7);

        static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        public static float Height(Vector3 p)
        {
            if (terrain == null) return 0;
            return terrain.SampleHeight(p) + terrain.transform.position.y;
        }
        public static Vector3 Ground(Vector3 p) => new Vector3(p.x, Height(p), p.z);
        public static bool Blocked(Vector3 p) => Physics.CheckSphere(p + Vector3.up * 1.0f, 0.45f, ~(1 << 2), QueryTriggerInteraction.Ignore);
        public static int ZoneOf(float z) { int zn = 0; for (int i = 0; i < ZoneStart.Length; i++) if (z >= ZoneStart[i]) zn = i; return zn; }

        static float SmoothStep(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }

        /// <summary>កម្ពស់ដី (ម៉ែត្រ) តាមទីតាំង</summary>
        public static float H(float x, float z)
        {
            float h = 6f + (Mathf.PerlinNoise(x * 0.03f + 10, z * 0.03f) - 0.5f) * 1.4f;
            bool mountain = z > 285 && z < 395;
            float W = mountain ? 24 : 38;
            if (z > 270 && z <= 285) W = Mathf.Lerp(38, 24, (z - 270) / 15f);
            if (z >= 395 && z < 410) W = Mathf.Lerp(24, 38, (z - 395) / 15f);
            float edge = Mathf.Abs(x) - W;
            if (edge > 0) h += SmoothStep(0, 12, edge) * 26f + Mathf.PerlinNoise(x * 0.08f, z * 0.08f) * 6f * SmoothStep(0, 6, edge);
            if (z < 0) h += SmoothStep(0, 18, -z) * 26f;
            if (z > 700) h += SmoothStep(700, 718, z) * 26f;
            // ភ្នំ៖ ឡើងពី z 280 ដល់ 320 ខ្ពង់រាប ដល់ 392 ហើយចុះ
            float plateau = SmoothStep(280, 320, z) * (1 - SmoothStep(392, 412, z)) * 11f;
            h += plateau;
            if (mountain) h += (Mathf.PerlinNoise(x * 0.12f, z * 0.12f) - 0.5f) * 1.2f * SmoothStep(4, 20, Mathf.Abs(x));
            // ទន្លេ
            float d = Mathf.Abs(z - RiverZ);
            if (d < RiverHalf) h = Mathf.Lerp(1f, h, SmoothStep(RiverHalf - 3.2f, RiverHalf, d));
            return h;
        }

        public static void Build()
        {
            props = new GameObject("Props").transform;
            BuildTerrain();
            BuildWater();
            Lighting();
            Village();
            Fields();
            Bamboo();
            Mountain();
            River();
            Camp();
            Forests();
            StaticBatchingUtility.Combine(props.gameObject);
        }

        // ---------------- ដី ----------------
        static void BuildTerrain()
        {
            var td = new TerrainData();
            td.heightmapResolution = 513;
            td.size = new Vector3(SizeX, SizeY, SizeZ);
            int res = td.heightmapResolution;
            var hs = new float[res, res];
            for (int j = 0; j < res; j++)
                for (int i = 0; i < res; i++)
                {
                    float x = MinX + i / (float)(res - 1) * SizeX;
                    float z = MinZ + j / (float)(res - 1) * SizeZ;
                    hs[j, i] = Mathf.Clamp01(H(x, z) / SizeY);
                }
            td.SetHeights(0, 0, hs);

            var layers = new[]
            {
                Layer(Tex.Noise(new Color(0.30f, 0.45f, 0.16f), new Color(0.46f, 0.58f, 0.22f), 6, 1), 6),   // ស្មៅ
                Layer(Tex.Noise(new Color(0.58f, 0.40f, 0.26f), new Color(0.70f, 0.52f, 0.34f), 9, 2), 4),   // ដីក្រហម (ផ្លូវ)
                Layer(Tex.Noise(new Color(0.40f, 0.39f, 0.36f), new Color(0.58f, 0.56f, 0.52f), 12, 3), 8),  // ថ្ម
                Layer(Tex.Rice(), 3),                                                                          // ស្រែ
                Layer(Tex.Noise(new Color(0.72f, 0.64f, 0.46f), new Color(0.82f, 0.74f, 0.55f), 10, 4), 5),  // ខ្សាច់
            };
            td.terrainLayers = layers;
            td.alphamapResolution = 512;
            int ar = td.alphamapResolution;
            var al = new float[ar, ar, layers.Length];
            for (int j = 0; j < ar; j++)
                for (int i = 0; i < ar; i++)
                {
                    float x = MinX + i / (float)(ar - 1) * SizeX;
                    float z = MinZ + j / (float)(ar - 1) * SizeZ;
                    float steep = td.GetSteepness(i / (float)(ar - 1), j / (float)(ar - 1));
                    float h = H(x, z);
                    float[] w = new float[layers.Length];
                    w[0] = 1;
                    float path = 1 - SmoothStep(2.2f, 3.6f, Mathf.Abs(x - PathX(z)));
                    w[1] = path * 1.5f;
                    if (z > 135 && z < 205 && Mathf.Abs(x) > 8 && Mathf.Abs(x) < 36 && Mathf.Abs(x) % 10 > 0.8f && z % 14 > 1f) w[3] = 3;
                    float riverD = Mathf.Abs(z - RiverZ);
                    if (riverD < RiverHalf + 6) w[4] = 2.5f * (1 - SmoothStep(RiverHalf + 2, RiverHalf + 6, riverD));
                    if (z > 560 && Mathf.Abs(x) < 34) { w[1] += 1.2f; w[0] *= 0.6f; }
                    if (steep > 32 || h > 20) { w[2] = 4; w[0] = 0.2f; }
                    float sum = 0; foreach (var v in w) sum += v;
                    for (int k = 0; k < w.Length; k++) al[j, i, k] = w[k] / sum;
                }
            td.SetAlphamaps(0, 0, al);

            // ស្មៅ (detail)
            td.SetDetailResolution(512, 16);
            var grassTex = GrassTexture();
            td.detailPrototypes = new[]
            {
                new DetailPrototype { prototypeTexture = grassTex, renderMode = DetailRenderMode.GrassBillboard, healthyColor = new Color(0.55f, 0.7f, 0.3f), dryColor = new Color(0.7f, 0.65f, 0.35f),
                                      minWidth = 0.5f, maxWidth = 0.9f, minHeight = 0.35f, maxHeight = 0.8f, noiseSpread = 0.3f, usePrototypeMesh = false }
            };
            int dr = 512; var dens = new int[dr, dr];
            for (int j = 0; j < dr; j++)
                for (int i = 0; i < dr; i++)
                {
                    float x = MinX + i / (float)(dr - 1) * SizeX, z = MinZ + j / (float)(dr - 1) * SizeZ;
                    float a0 = al[Mathf.Min(ar - 1, j * ar / dr), Mathf.Min(ar - 1, i * ar / dr), 0];
                    if (a0 > 0.6f && Mathf.Abs(x - PathX(z)) > 4) dens[j, i] = Mathf.PerlinNoise(x * 0.15f, z * 0.15f) > 0.35f ? 4 : 1;
                }
            td.SetDetailLayer(0, 0, 0, dens);

            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Terrain";
            go.transform.position = new Vector3(MinX, 0, MinZ);
            terrain = go.GetComponent<Terrain>();
            terrain.detailObjectDistance = 70;
            terrain.detailObjectDensity = 0.8f;
            terrain.heightmapPixelError = 4;
            terrain.basemapDistance = 400;
            terrain.drawInstanced = false;
            terrain.Flush();
        }

        public static float PathX(float z) => Mathf.Sin(z / 37f) * 3f * (z > 285 && z < 395 ? 0.4f : 1f) * (Mathf.Abs(z - RiverZ) < RiverHalf + 10 ? 0 : 1) * (z > 555 ? 0 : 1);

        static TerrainLayer Layer(Texture2D src, float tile)
        {
            // Terrain Standard យក alpha នៃរូបភាពជា "ភាពរលោង" — ដាក់ alpha ទាប ដើម្បីកុំឲ្យដីភ្លឺចាំងពេក
            var t = new Texture2D(src.width, src.height, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = src.GetPixels();
            for (int i = 0; i < px.Length; i++) px[i].a = 0.04f;
            t.SetPixels(px); t.Apply();
            return new TerrainLayer { diffuseTexture = t, tileSize = new Vector2(tile, tile), smoothness = 0f, metallic = 0f };
        }

        static Texture2D GrassTexture()
        {
            int w = 32, h = 32;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) t.SetPixel(x, y, clear);
            var r = new System.Random(3);
            for (int b = 0; b < 7; b++)
            {
                float x0 = 3 + r.Next(w - 6), lean = (float)(r.NextDouble() - 0.5) * 8f; int top = h - 2 - r.Next(10);
                for (int y = 0; y < top; y++)
                {
                    float f = y / (float)top;
                    int x = Mathf.RoundToInt(x0 + lean * f * f);
                    int wd = f < 0.7f ? 1 : 0;
                    for (int dx = -wd; dx <= wd; dx++)
                        if (x + dx >= 0 && x + dx < w) t.SetPixel(x + dx, y, Color.Lerp(new Color(0.35f, 0.55f, 0.2f), new Color(0.7f, 0.85f, 0.4f), f));
                }
            }
            t.Apply();
            return t;
        }

        static void BuildWater()
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Plane);
            w.name = "River";
            Object.Destroy(w.GetComponent<Collider>());
            w.transform.position = new Vector3(0, WaterY, RiverZ);
            w.transform.localScale = new Vector3(SizeX / 10f, 1, (RiverHalf * 2 + 6) / 10f);
            var r = w.GetComponent<Renderer>();
            r.sharedMaterial = Mats.Transparent(new Color(0.18f, 0.42f, 0.45f, 0.82f));
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        public static void Lighting()
        {
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.93f, 0.8f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sunGo.transform.rotation = Quaternion.Euler(48, -35, 0);
            RenderSettings.sun = sun;
            var sky = Shader.Find("Skybox/Procedural");
            if (sky != null)
            {
                var m = new Material(sky);
                m.SetFloat("_SunSize", 0.04f);
                m.SetFloat("_AtmosphereThickness", 1.05f);
                m.SetColor("_SkyTint", new Color(0.55f, 0.65f, 0.8f));
                m.SetColor("_GroundColor", new Color(0.4f, 0.38f, 0.32f));
                m.SetFloat("_Exposure", 1.25f);
                RenderSettings.skybox = m;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.7f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.55f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.27f, 0.22f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.75f, 0.8f, 0.82f);
            RenderSettings.fogStartDistance = 70;
            RenderSettings.fogEndDistance = 260;
            QualitySettings.shadowDistance = 90;
        }

        // ---------------- វត្ថុតុបតែង ----------------
        static Vector3 G(float x, float z) => new Vector3(x, H(x, z), z);

        static GameObject Group(string name, Vector3 pos, float yaw = 0)
        {
            var g = new GameObject(name); g.transform.SetParent(props, false); g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return g;
        }

        /// <summary>ដើមត្នោត — ដើមឈើជាតិខ្មែរ</summary>
        public static void SugarPalm(float x, float z)
        {
            var g = Group("SugarPalm", G(x, z), R(0, 360));
            float h = R(9, 14);
            var trunkC = new Color(0.33f, 0.28f, 0.22f);
            var t = Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, h / 2, 0), new Vector3(0.45f, h / 2, 0.45f), trunkC);
            t.GetComponent<CapsuleCollider>();
            var crown = new GameObject("Crown").transform; crown.SetParent(g.transform, false); crown.localPosition = new Vector3(0, h, 0);
            Shapes.Make(PrimitiveType.Sphere, crown, Vector3.zero, Vector3.one * 1.1f, new Color(0.25f, 0.3f, 0.15f), false);
            int n = 14;
            for (int i = 0; i < n; i++)
            {
                float yaw = i * 360f / n + R(-8, 8), pitch = R(-25, 40);
                var leaf = new GameObject("Leaf").transform; leaf.SetParent(crown, false); leaf.localRotation = Quaternion.Euler(pitch, yaw, 0);
                // ស្លឹកត្នោតរាងកង្ហារ
                var fan = Shapes.Make(PrimitiveType.Sphere, leaf, new Vector3(0, 0.3f, 1.9f), new Vector3(2.4f, 0.08f, 1.8f), Color.Lerp(new Color(0.28f, 0.45f, 0.18f), new Color(0.45f, 0.55f, 0.22f), R(0, 1)), false);
                fan.transform.localRotation = Quaternion.Euler(-15, 0, 0);
                Shapes.Make(PrimitiveType.Cube, leaf, new Vector3(0, 0, 0.9f), new Vector3(0.08f, 0.08f, 1.8f), new Color(0.35f, 0.38f, 0.2f), false);
            }
            // ផ្លែត្នោត
            for (int i = 0; i < 5; i++)
                Shapes.Make(PrimitiveType.Sphere, crown, new Vector3(R(-0.6f, 0.6f), -0.7f, R(-0.6f, 0.6f)), Vector3.one * 0.32f, new Color(0.15f, 0.1f, 0.08f), false);
        }

        public static void Tree(float x, float z, float scale = 1f)
        {
            var g = Group("Tree", G(x, z) + Vector3.down * 0.2f, R(0, 360));
            g.transform.localScale = Vector3.one * scale * R(0.8f, 1.25f);
            Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, 2.2f, 0), new Vector3(0.6f, 2.2f, 0.6f), new Color(0.32f, 0.23f, 0.15f));
            var leaf = Color.Lerp(new Color(0.17f, 0.36f, 0.12f), new Color(0.3f, 0.5f, 0.18f), R(0, 1));
            Shapes.Make(PrimitiveType.Sphere, g.transform, new Vector3(0, 5.2f, 0), new Vector3(4.6f, 3.6f, 4.6f), leaf, false);
            Shapes.Make(PrimitiveType.Sphere, g.transform, new Vector3(1.3f, 4.4f, 0.4f), new Vector3(3f, 2.4f, 3f), leaf * 0.9f, false);
            Shapes.Make(PrimitiveType.Sphere, g.transform, new Vector3(-1.1f, 4.6f, -0.6f), new Vector3(3.2f, 2.6f, 3.2f), leaf * 1.08f, false);
            Shapes.Make(PrimitiveType.Sphere, g.transform, new Vector3(0, 6.6f, 0.3f), new Vector3(2.8f, 2.2f, 2.8f), leaf * 1.12f, false);
        }

        public static void BambooClump(float x, float z)
        {
            var g = Group("Bamboo", G(x, z), R(0, 360));
            int n = rnd.Next(5, 10);
            for (int i = 0; i < n; i++)
            {
                float h = R(6, 10);
                var stem = new GameObject("Stem").transform; stem.SetParent(g.transform, false);
                stem.localPosition = new Vector3(R(-0.8f, 0.8f), 0, R(-0.8f, 0.8f));
                stem.localRotation = Quaternion.Euler(R(-12, 12), 0, R(-12, 12));
                var c = Color.Lerp(new Color(0.45f, 0.6f, 0.2f), new Color(0.6f, 0.65f, 0.25f), R(0, 1));
                Shapes.Make(PrimitiveType.Cylinder, stem, new Vector3(0, h / 2, 0), new Vector3(0.14f, h / 2, 0.14f), c, i == 0);
                for (float y = 1.2f; y < h; y += 1.2f) Shapes.Make(PrimitiveType.Cylinder, stem, new Vector3(0, y, 0), new Vector3(0.17f, 0.03f, 0.17f), c * 0.8f, false);
                for (int k = 0; k < 4; k++)
                {
                    var l = Shapes.Make(PrimitiveType.Sphere, stem, new Vector3(R(-0.8f, 0.8f), h - R(0, 2.5f), R(-0.8f, 0.8f)), new Vector3(1.4f, 0.35f, 0.7f), new Color(0.35f, 0.55f, 0.2f), false);
                    l.transform.localRotation = Quaternion.Euler(R(-20, 20), R(0, 360), 0);
                }
            }
        }

        public static void Rock(float x, float z, float s)
        {
            var g = Group("Rock", G(x, z));
            var m = Mats.Textured("rock", Tex.Noise(new Color(0.42f, 0.4f, 0.37f), new Color(0.58f, 0.56f, 0.5f), 12, 6), Vector2.one);
            var r = Shapes.MakeM(PrimitiveType.Sphere, g.transform, new Vector3(0, s * 0.25f, 0), new Vector3(s * R(0.8f, 1.3f), s * R(0.5f, 0.8f), s * R(0.8f, 1.3f)), m);
            r.transform.localRotation = Quaternion.Euler(R(0, 30), R(0, 360), R(0, 30));
        }

        /// <summary>ផ្ទះឈើខ្ពស់បែបខ្មែរ មានដំបូលស្រួច និងចុងដំបូលងើប</summary>
        public static void StiltHouse(float x, float z, float yaw, float scale = 1f)
        {
            var g = Group("House", G(x, z), yaw);
            g.transform.localScale = Vector3.one * scale;
            var wood = new Color(0.45f, 0.3f, 0.18f); var dark = new Color(0.3f, 0.2f, 0.12f);
            var roofC = new Color(0.62f, 0.25f, 0.15f);
            float W = 6, L = 9, floorY = 2.6f, wallH = 2.6f;
            for (int ix = -1; ix <= 1; ix++)
                for (int iz = -1; iz <= 1; iz++)
                    Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(ix * W / 2.2f, floorY / 2 - 0.3f, iz * L / 2.2f), new Vector3(0.3f, floorY / 2 + 0.3f, 0.3f), dark);
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, floorY, 0), new Vector3(W + 0.6f, 0.25f, L + 0.6f), wood);
            // ជញ្ជាំងបន្ទះក្ដារ
            var plank = Mats.Textured("plank", Tex.Stripes(wood, wood * 0.8f, 8), new Vector2(4, 1));
            Shapes.MakeM(PrimitiveType.Cube, g.transform, new Vector3(0, floorY + wallH / 2, L / 2), new Vector3(W, wallH, 0.15f), plank);
            Shapes.MakeM(PrimitiveType.Cube, g.transform, new Vector3(0, floorY + wallH / 2, -L / 2), new Vector3(W, wallH, 0.15f), plank);
            Shapes.MakeM(PrimitiveType.Cube, g.transform, new Vector3(W / 2, floorY + wallH / 2, 0), new Vector3(0.15f, wallH, L), plank);
            Shapes.MakeM(PrimitiveType.Cube, g.transform, new Vector3(-W / 2, floorY + wallH / 2, 0), new Vector3(0.15f, wallH, L), plank);
            // បង្អួច
            for (int i = -1; i <= 1; i += 2)
            {
                Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(-W / 2 - 0.05f, floorY + 1.5f, i * 2.2f), new Vector3(0.1f, 0.9f, 1f), new Color(0.12f, 0.08f, 0.05f), false);
                Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(W / 2 + 0.05f, floorY + 1.5f, i * 2.2f), new Vector3(0.1f, 0.9f, 1f), new Color(0.12f, 0.08f, 0.05f), false);
            }
            // ដំបូល
            float roofY = floorY + wallH;
            var r1 = Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(-W / 4 - 0.15f, roofY + 1.3f, 0), new Vector3(W / 2 + 1.7f, 0.18f, L + 1.6f), roofC, false);
            r1.transform.localRotation = Quaternion.Euler(0, 0, 40);
            var r2 = Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(W / 4 + 0.15f, roofY + 1.3f, 0), new Vector3(W / 2 + 1.7f, 0.18f, L + 1.6f), roofC, false);
            r2.transform.localRotation = Quaternion.Euler(0, 0, -40);
            // ចុងដំបូលងើបឡើង
            foreach (var sz in new[] { -1, 1 })
            {
                var gable = Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, roofY + 1.1f, sz * (L / 2)), new Vector3(W * 0.7f, 2f, 0.12f), wood * 0.9f, false);
                gable.transform.localRotation = Quaternion.Euler(0, 0, 45);
                gable.transform.localScale = new Vector3(2.8f, 2.8f, 0.12f);
                var horn = Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, roofY + 3.5f, sz * (L / 2 + 0.8f)), new Vector3(0.18f, 1.1f, 0.18f), new Color(0.88f, 0.7f, 0.3f), false);
                horn.transform.localRotation = Quaternion.Euler(sz * 30, 0, 0);
            }
            // ជណ្ដើរ
            var ladder = new GameObject("Ladder").transform; ladder.SetParent(g.transform, false); ladder.localPosition = new Vector3(1.5f, 0, L / 2 + 1.2f); ladder.localRotation = Quaternion.Euler(-35, 0, 0);
            Shapes.Make(PrimitiveType.Cube, ladder, new Vector3(-0.5f, 1.6f, 0), new Vector3(0.12f, 3.4f, 0.12f), dark, false);
            Shapes.Make(PrimitiveType.Cube, ladder, new Vector3(0.5f, 1.6f, 0), new Vector3(0.12f, 3.4f, 0.12f), dark, false);
            for (int i = 0; i < 6; i++) Shapes.Make(PrimitiveType.Cube, ladder, new Vector3(0, 0.3f + i * 0.5f, 0), new Vector3(1f, 0.08f, 0.12f), wood, false);
            // ពាងទឹក
            Shapes.Make(PrimitiveType.Sphere, g.transform, new Vector3(-2.2f, 0.55f, L / 2 + 1.3f), new Vector3(1f, 1.1f, 1f), new Color(0.35f, 0.22f, 0.15f));
            // ក្រមាហាលលើខ្សែ
            var kr = Mats.Textured("kramaR", Tex.Krama(new Color(0.75f, 0.12f, 0.12f), Color.white), new Vector2(2, 2));
            Shapes.MakeM(PrimitiveType.Cube, g.transform, new Vector3(W / 2 + 0.1f, floorY + 0.4f, R(-2, 2)), new Vector3(0.05f, 0.9f, 1.2f), kr, false);
        }

        static void OxCart(float x, float z, float yaw)
        {
            var g = Group("OxCart", G(x, z), yaw);
            var wood = new Color(0.45f, 0.3f, 0.16f);
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, 1.2f, 0), new Vector3(1.6f, 0.15f, 3f), wood);
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, 1.6f, 0), new Vector3(1.6f, 0.7f, 2.6f), new Color(0.75f, 0.6f, 0.35f), false);
            foreach (var sx in new[] { -1, 1 })
            {
                var w = Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(sx * 1f, 0.9f, -0.3f), new Vector3(1.8f, 0.06f, 1.8f), wood * 0.7f, false);
                w.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, 1f, 2.6f), new Vector3(0.15f, 0.15f, 2.6f), wood, false);
        }

        static void HayStack(float x, float z)
        {
            var g = Group("Hay", G(x, z));
            Shapes.Make(PrimitiveType.Sphere, g.transform, new Vector3(0, 1f, 0), new Vector3(2.6f, 2.6f, 2.6f), new Color(0.85f, 0.72f, 0.38f));
            Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, 2.4f, 0), new Vector3(0.1f, 0.6f, 0.1f), new Color(0.4f, 0.3f, 0.2f), false);
        }

        static void Fence(Vector3 a, Vector3 b)
        {
            var wood = new Color(0.42f, 0.3f, 0.18f);
            float len = Vector3.Distance(a, b);
            int n = Mathf.Max(2, (int)(len / 2));
            for (int i = 0; i <= n; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)n);
                var g = Group("Fence", G(p.x, p.z));
                Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, 0.6f, 0), new Vector3(0.15f, 1.2f, 0.15f), wood, false);
                if (i < n)
                {
                    var q = Vector3.Lerp(a, b, (i + 1) / (float)n);
                    var mid = (p + q) / 2; var d = q - p; d.y = 0;
                    var rail = Group("Rail", G(mid.x, mid.z), Quaternion.LookRotation(d).eulerAngles.y);
                    Shapes.Make(PrimitiveType.Cube, rail.transform, new Vector3(0, 0.8f, 0), new Vector3(0.08f, 0.08f, d.magnitude), wood, false);
                    Shapes.Make(PrimitiveType.Cube, rail.transform, new Vector3(0, 0.4f, 0), new Vector3(0.08f, 0.08f, d.magnitude), wood, false);
                }
            }
        }

        // ---------------- តំបន់ ----------------
        static void Village()
        {
            float[,] houses = { { -20, 22, 90 }, { 22, 30, -90 }, { -24, 52, 80 }, { 21, 62, -95 }, { -19, 86, 95 }, { 24, 96, -85 }, { -30, 112, 70 } };
            for (int i = 0; i < houses.GetLength(0); i++) StiltHouse(houses[i, 0], houses[i, 1], houses[i, 2]);
            OxCart(-10, 44, 20); OxCart(-10, 100, -160);
            HayStack(30, 45); HayStack(-31, 70); HayStack(33, 78);
            Fence(new Vector3(-12, 0, 14), new Vector3(-12, 0, 40)); Fence(new Vector3(-13, 0, 72), new Vector3(-13, 0, 104));
            // ក្រោលសត្វជំនិះ
            Fence(new Vector3(-30, 0, 0.5f), new Vector3(-10, 0, 0.5f)); Fence(new Vector3(10, 0, 0.5f), new Vector3(30, 0, 0.5f));
            HayStack(-32, 6); HayStack(32, 6);
            for (int i = 0; i < 26; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * R(8, 36), z = R(0, 122);
                if (Near(x, z, houses, 7)) continue;
                if (x > 5 && x < 19 && z < 118) continue; // ផ្លូវប្រណាំង
                if (z < 10) continue;                      // ក្រោលសត្វ
                SugarPalm(x, z);
            }
            // អណ្ដូងទឹក
            var well = Group("Well", G(-6, 64));
            Shapes.Make(PrimitiveType.Cylinder, well.transform, new Vector3(0, 0.5f, 0), new Vector3(2f, 0.5f, 2f), new Color(0.55f, 0.52f, 0.48f));
            Shapes.Make(PrimitiveType.Cylinder, well.transform, new Vector3(0, 0.98f, 0), new Vector3(1.6f, 0.02f, 1.6f), new Color(0.15f, 0.3f, 0.35f), false);
        }

        static bool Near(float x, float z, float[,] list, float d)
        {
            for (int i = 0; i < list.GetLength(0); i++) if (Mathf.Abs(list[i, 0] - x) < d && Mathf.Abs(list[i, 1] - z) < d) return true;
            return false;
        }

        static void Fields()
        {
            // ភ្លឺស្រែ
            for (float z = 135; z < 205; z += 14)
                foreach (var sx in new[] { -1, 1 })
                {
                    var g = Group("Dike", G(sx * 22, z));
                    Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, 0.15f, 0), new Vector3(28, 0.35f, 0.7f), new Color(0.5f, 0.4f, 0.28f), false);
                }
            for (int i = 0; i < 18; i++) SugarPalm((i % 2 == 0 ? -1 : 1) * R(10, 36), R(135, 205));
            // ខ្ទមស្រែ
            var hut = Group("Hut", G(-26, 170), 60);
            foreach (var p in new[] { new Vector3(-1.2f, 0, -1.2f), new Vector3(1.2f, 0, -1.2f), new Vector3(-1.2f, 0, 1.2f), new Vector3(1.2f, 0, 1.2f) })
                Shapes.Make(PrimitiveType.Cylinder, hut.transform, p + Vector3.up, new Vector3(0.15f, 1f, 0.15f), new Color(0.4f, 0.3f, 0.2f));
            Shapes.Make(PrimitiveType.Cube, hut.transform, new Vector3(0, 1.2f, 0), new Vector3(2.8f, 0.15f, 2.8f), new Color(0.5f, 0.38f, 0.22f), false);
            var hr = Shapes.Make(PrimitiveType.Cube, hut.transform, new Vector3(0, 2.6f, 0), new Vector3(2.6f, 2.6f, 3.2f), new Color(0.78f, 0.66f, 0.38f), false);
            hr.transform.localRotation = Quaternion.Euler(0, 0, 45);
        }

        static void Bamboo()
        {
            for (int i = 0; i < 70; i++)
            {
                float z = R(205, 262), x = (i % 2 == 0 ? -1 : 1) * R(6, 40);
                if (z > 228 && Mathf.Abs(x) < 18) continue; // កន្លែងល្បែងថ្មរុញ
                BambooClump(x, z);
            }
        }

        static void Mountain()
        {
            for (int i = 0; i < 40; i++)
            {
                float z = R(272, 410), x = (i % 2 == 0 ? -1 : 1) * R(8, 28);
                if (z > 362 && z < 395 && Mathf.Abs(x) < 14) continue;
                if (i % 3 == 0) Rock(x, z, R(1.5f, 3.5f)); else Tree(x, z, 0.9f);
            }
        }

        static void River()
        {
            for (int i = 0; i < 16; i++)
            {
                float z = i < 8 ? R(420, 455) : R(495, 550), x = (i % 2 == 0 ? -1 : 1) * R(10, 36);
                if (i % 2 == 0) SugarPalm(x, z); else BambooClump(x, z);
            }
            // ទូកអុំ
            foreach (var p in new[] { new Vector3(-14, 0, 470), new Vector3(20, 0, 479) })
            {
                var b = Group("Boat", new Vector3(p.x, WaterY - 0.1f, p.z), 80);
                Shapes.Make(PrimitiveType.Capsule, b.transform, Vector3.zero, new Vector3(0.9f, 2.5f, 0.6f), new Color(0.4f, 0.26f, 0.14f), false).transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            // ខ្ទមយាយស
            StiltHouse(-26, 432, 100, 0.8f);
        }

        static void Camp()
        {
            var wood = new Color(0.32f, 0.22f, 0.13f);
            // របងជំរុំ ចំហៀងទាំងពីរ
            for (float z = 575; z < 700; z += 0.9f)
                foreach (var sx in new[] { -1, 1 })
                {
                    float x = sx * 30;
                    var g = Group("Palisade", G(x, z));
                    float h = R(3.6f, 4.8f);
                    Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, h / 2 - 0.4f, 0), new Vector3(0.55f, h / 2, 0.55f), wood * R(0.8f, 1.1f));
                }
            // តង់
            float[,] tents = { { -18, 600, 30 }, { 18, 605, -30 }, { -20, 650, 60 }, { 20, 655, -60 }, { -12, 690, 0 }, { 12, 690, 0 } };
            for (int i = 0; i < tents.GetLength(0); i++)
            {
                var g = Group("Tent", G(tents[i, 0], tents[i, 1]), tents[i, 2]);
                var cloth = i % 2 == 0 ? new Color(0.35f, 0.3f, 0.22f) : new Color(0.45f, 0.15f, 0.12f);
                var a = Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(-1.1f, 1.2f, 0), new Vector3(0.1f, 3f, 4f), cloth);
                a.transform.localRotation = Quaternion.Euler(0, 0, -38);
                var b = Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(1.1f, 1.2f, 0), new Vector3(0.1f, 3f, 4f), cloth);
                b.transform.localRotation = Quaternion.Euler(0, 0, 38);
            }
            // ប្រអប់ និងទង់ក្រហមរបស់ចោរ
            for (int i = 0; i < 10; i++)
            {
                var g = Group("Crate", G(R(-25, 25), R(585, 695)), R(0, 90));
                if (Mathf.Abs(g.transform.position.x) < 6) { Object.Destroy(g); continue; }
                Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, 0.5f, 0), Vector3.one, new Color(0.5f, 0.36f, 0.2f));
            }
            var flag = Group("Flag", G(0, 698));
            Shapes.Make(PrimitiveType.Cylinder, flag.transform, new Vector3(0, 4, 0), new Vector3(0.2f, 4, 0.2f), wood);
            Shapes.Make(PrimitiveType.Cube, flag.transform, new Vector3(1.2f, 7.2f, 0), new Vector3(2.4f, 1.4f, 0.05f), new Color(0.7f, 0.08f, 0.08f), false);
            // វេទិកាដាក់កញ្ជើ
            var plat = Group("Platform", G(0, 680));
            Shapes.Make(PrimitiveType.Cube, plat.transform, new Vector3(0, 0.25f, 0), new Vector3(6, 0.5f, 6), new Color(0.4f, 0.28f, 0.16f));
        }

        static void Forests()
        {
            // ព្រៃលើជម្រាលភ្នំទាំងពីរខាង
            for (int i = 0; i < 170; i++)
            {
                float z = R(-10, 715);
                if (Mathf.Abs(z - RiverZ) < RiverHalf + 2) continue;
                float W = z > 285 && z < 395 ? 24 : 38;
                float x = (i % 2 == 0 ? -1 : 1) * R(W + 3, W + 30);
                if (Mathf.Abs(x) > 78) continue;
                if (z > 205 && z < 262 && i % 3 == 0) BambooClump(x, z); else Tree(x, z, R(1f, 1.4f));
            }
        }
    }
}
