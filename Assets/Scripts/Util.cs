// ឧបករណ៍ជំនួយ៖ ការបញ្ជា (keyboard/mouse), សម្ភារៈពណ៌, វាយនភណ្ឌខ្មែរ (ក្រមា ហូល), លេខខ្មែរ, សំឡេង និងភ្លេង
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KhmerHeroes
{
    /// <summary>ការបញ្ជាពីក្ដារចុច និងកណ្ដុរ (Input Manager ចាស់)</summary>
    public static class InputX
    {
        public static Vector2 Move()
        {
            float x = 0, y = 0;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1;
            var v = new Vector2(x, y);
            return v.sqrMagnitude > 1 ? v.normalized : v;
        }
        public static float MouseX => Input.GetAxis("Mouse X");
        public static float MouseY => Input.GetAxis("Mouse Y");
        public static bool Attack => Input.GetMouseButton(0) || Input.GetKey(KeyCode.J);
        public static bool Skill => Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.K);
        public static bool Dodge => Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.L);
        public static bool Jump => Input.GetKeyDown(KeyCode.Space);
        public static bool Interact => Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E);
        public static bool Pause => Input.GetKeyDown(KeyCode.Escape);
        public static bool Dismount => Input.GetKeyDown(KeyCode.R);
        public static bool Next => Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0);
        public static int HeroKey()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
            if (Input.GetKeyDown(KeyCode.Alpha4)) return 3;
            return -1;
        }
    }

    /// <summary>លេខខ្មែរ</summary>
    public static class Kh
    {
        static readonly char[] D = { '០', '១', '២', '៣', '៤', '៥', '៦', '៧', '៨', '៩' };
        public static string Num(int n)
        {
            var s = n.ToString();
            var c = s.ToCharArray();
            for (int i = 0; i < c.Length; i++) if (c[i] >= '0' && c[i] <= '9') c[i] = D[c[i] - '0'];
            return new string(c);
        }
    }

    /// <summary>សម្ភារៈពណ៌ (Built-in ឬ URP)</summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Shader _lit, _unlit;

        public static Shader Lit
        {
            get
            {
                if (_lit == null)
                {
                    _lit = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
                    if (_lit == null) _lit = Shader.Find("Diffuse");
                }
                return _lit;
            }
        }
        public static Shader Unlit
        {
            get
            {
                if (_unlit == null) _unlit = Shader.Find("Sprites/Default");
                return _unlit;
            }
        }

        public static Material Get(Color c, float smooth = 0.1f, Color? emission = null)
        {
            string key = ColorUtility.ToHtmlStringRGBA(c) + smooth.ToString("0.00") + (emission.HasValue ? ColorUtility.ToHtmlStringRGB(emission.Value) : "");
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Lit) { color = c };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            cache[key] = m;
            return m;
        }

        public static Material Glow(Color c, float intensity = 2f) => Get(c * 0.6f, 0.4f, c * intensity);

        public static Material Textured(string key, Texture2D tex, Vector2 tiling, float smooth = 0.1f)
        {
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Lit) { color = Color.white, mainTexture = tex };
            m.mainTextureScale = tiling;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            cache[key] = m;
            return m;
        }

        public static Material Transparent(Color c)
        {
            string key = "T" + ColorUtility.ToHtmlStringRGBA(c);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Lit) { color = c };
            if (m.HasProperty("_Mode"))
            {
                // Standard shader → Transparent
                m.SetFloat("_Mode", 3);
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.renderQueue = 3000;
            }
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.9f);
            cache[key] = m;
            return m;
        }

        public static Material Sprite(Color c)
        {
            string key = "S" + ColorUtility.ToHtmlStringRGBA(c);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Unlit) { color = c };
            cache[key] = m;
            return m;
        }
    }

    /// <summary>វាយនភណ្ឌដែលបង្កើតដោយកូដ — ក្រមា សំពត់ហូល ឆ្នូតខ្លា ដី ស្មៅ</summary>
    public static class Tex
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        static Texture2D New(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        }

        /// <summary>ក្រមា៖ ក្រឡាចត្រង្គពីរពណ៌</summary>
        public static Texture2D Krama(Color a, Color b)
        {
            string k = "krama" + a + b;
            if (cache.TryGetValue(k, out var t)) return t;
            int s = 64; t = New(s);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    bool cx = (x / 8) % 2 == 0, cy = (y / 8) % 2 == 0;
                    Color c = cx && cy ? a : (!cx && !cy ? Color.Lerp(b, Color.white, 0.15f) : Color.Lerp(a, b, 0.5f));
                    if (x % 16 == 0 || y % 16 == 0) c = Color.Lerp(c, Color.white, 0.25f);
                    t.SetPixel(x, y, c);
                }
            t.Apply(); cache[k] = t; return t;
        }

        /// <summary>សំពត់ហូល៖ ក្បាច់ពេជ្របែបចងបាច់</summary>
        public static Texture2D Hol(Color bg, Color fg, Color accent)
        {
            string k = "hol" + bg + fg + accent;
            if (cache.TryGetValue(k, out var t)) return t;
            int s = 64; t = New(s);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    int dx = Mathf.Abs((x % 16) - 8), dy = Mathf.Abs((y % 16) - 8);
                    int d = dx + dy;
                    Color c = bg;
                    if (d == 6 || d == 7) c = fg;
                    else if (d <= 2) c = accent;
                    if (y % 32 < 2) c = Color.Lerp(accent, Color.white, 0.2f);
                    float n = Mathf.PerlinNoise(x * 0.3f, y * 0.9f) * 0.12f;
                    t.SetPixel(x, y, c * (0.94f + n));
                }
            t.Apply(); cache[k] = t; return t;
        }

        public static Texture2D Stripes(Color a, Color b, int period = 10)
        {
            string k = "str" + a + b + period;
            if (cache.TryGetValue(k, out var t)) return t;
            int s = 64; t = New(s);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float w = Mathf.Sin((x + Mathf.Sin(y * 0.2f) * 4f) * Mathf.PI * 2 / period);
                    t.SetPixel(x, y, w > 0.55f ? b : a);
                }
            t.Apply(); cache[k] = t; return t;
        }

        public static Texture2D Noise(Color a, Color b, float scale = 8f, int seed = 0, int size = 128)
        {
            string k = "n" + a + b + scale + seed;
            if (cache.TryGetValue(k, out var t)) return t;
            t = New(size);
            float ox = seed * 13.1f, oy = seed * 7.7f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float n = Mathf.PerlinNoise(ox + x / (float)size * scale, oy + y / (float)size * scale) * 0.65f
                            + Mathf.PerlinNoise(ox + x / (float)size * scale * 4, oy + y / (float)size * scale * 4) * 0.35f;
                    t.SetPixel(x, y, Color.Lerp(a, b, n));
                }
            t.Apply(); cache[k] = t; return t;
        }

        /// <summary>វាលស្រែ៖ ជួរស្រូវបៃតង</summary>
        public static Texture2D Rice()
        {
            if (cache.TryGetValue("rice", out var t)) return t;
            int s = 64; t = New(s);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float row = Mathf.Abs(Mathf.Sin(x * Mathf.PI / 4f));
                    float n = Mathf.PerlinNoise(x * 0.5f, y * 0.5f);
                    var c = Color.Lerp(new Color(0.25f, 0.45f, 0.35f), new Color(0.45f, 0.75f, 0.25f), row * (0.6f + 0.4f * n));
                    t.SetPixel(x, y, c);
                }
            t.Apply(); cache["rice"] = t; return t;
        }
    }

    /// <summary>បង្កើតរាងសាមញ្ញ ជាមួយពណ៌</summary>
    public static class Shapes
    {
        public static GameObject Make(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color, bool collider = true, string name = null)
        {
            var g = GameObject.CreatePrimitive(type);
            if (name != null) g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = Mats.Get(color);
            if (!collider) Object.Destroy(g.GetComponent<Collider>());
            return g;
        }

        public static GameObject MakeM(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool collider = true)
        {
            var g = GameObject.CreatePrimitive(type);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.Destroy(g.GetComponent<Collider>());
            return g;
        }

        public static GameObject Glow(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color, float intensity = 2f)
        {
            var g = Make(type, parent, localPos, scale, color, false);
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = Mats.Glow(color, intensity);
            r.shadowCastingMode = ShadowCastingMode.Off;
            return g;
        }

        /// <summary>បំពង់កាត់ (cylinder) ពីចំណុច a ទៅ b</summary>
        public static GameObject Beam(Transform parent, Vector3 a, Vector3 b, float thick, Color color, bool collider = false)
        {
            var g = Make(PrimitiveType.Cylinder, parent, (a + b) / 2, Vector3.one, color, collider);
            var dir = b - a;
            g.transform.localScale = new Vector3(thick, dir.magnitude / 2, thick);
            g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            return g;
        }

        /// <summary>ភ្លើង (particle) — សម្រាប់ចន្លុះ និងភ្លើងជំរុំ</summary>
        public static ParticleSystem Fire(Transform parent, Vector3 localPos, float size = 1f)
        {
            var g = new GameObject("Fire");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            var ps = g.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = 0.7f;
            main.startSpeed = 1.2f * size;
            main.startSize = 0.45f * size;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.4f, 0.1f));
            var em = ps.emission; em.rateOverTime = 40;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12; sh.radius = 0.12f * size;
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(new Color(1, 0.9f, 0.4f), 0), new GradientColorKey(new Color(0.9f, 0.2f, 0.05f), 1) },
                       new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = gr;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.2f));
            var r = g.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Sprite(Color.white);
            var lg = new GameObject("Light"); lg.transform.SetParent(g.transform, false); lg.transform.localPosition = Vector3.up * 0.4f;
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1, 0.6f, 0.25f); l.range = 8 * size; l.intensity = 1.6f;
            ps.Play();
            return ps;
        }
    }

    /// <summary>សំឡេង និងភ្លេងបែបខ្មែរ (បង្កើតដោយកូដ — គ្មានឯកសារសំឡេង)</summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx I;
        AudioSource fx, music, music2;
        float musicFade = 1;
        string musicName;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        public static float MusicVolume = 0.32f, FxVolume = 0.8f;

        const int SR = 44100;

        void Awake()
        {
            I = this;
            fx = gameObject.AddComponent<AudioSource>();
            music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.volume = 0;
            music2 = gameObject.AddComponent<AudioSource>(); music2.loop = true; music2.volume = 0;
            clips["hit"] = Tone(0.12f, 160, 60, 0.9f, true);
            clips["swing"] = Tone(0.16f, 0, 0, 0.35f, true, true);
            clips["arrow"] = Tone(0.18f, 900, 300, 0.35f, true);
            clips["hurt"] = Tone(0.22f, 320, 120, 0.6f, true);
            clips["roar"] = Tone(0.7f, 110, 60, 0.8f, true);
            clips["pick"] = Chord(0.45f, new[] { 784f, 1046f }, 0.35f);
            clips["good"] = Chord(0.7f, new[] { 523f, 659f, 784f, 1046f }, 0.3f);
            clips["bad"] = Tone(0.4f, 220, 150, 0.4f, false);
            clips["gate"] = Tone(1.0f, 90, 70, 0.6f, true);
            clips["skill"] = Chord(0.5f, new[] { 392f, 587f, 784f }, 0.35f);
            clips["step"] = Tone(0.06f, 0, 0, 0.12f, true, true);
            // សំឡេងសត្វកំប្លែងៗ
            clips["meow"] = Sweep(0.55f, new[] { 520f, 900f, 700f, 450f }, 0.5f, 6f, 0.25f, 0.2f);
            clips["bark"] = Concat(Sweep(0.13f, new[] { 420f, 620f, 300f }, 0.6f, 0, 0.6f, 0.5f), Silence(0.07f), Sweep(0.13f, new[] { 440f, 640f, 320f }, 0.6f, 0, 0.6f, 0.5f));
            clips["trumpet"] = Sweep(0.9f, new[] { 260f, 520f, 620f, 480f }, 0.45f, 9f, 0.35f, 0.9f);
            clips["neigh"] = Sweep(0.8f, new[] { 900f, 1100f, 700f, 500f }, 0.4f, 18f, 0.2f, 0.4f);
            clips["moo"] = Sweep(1.0f, new[] { 140f, 160f, 120f }, 0.6f, 3f, 0.1f, 0.8f);
            clips["boing"] = Sweep(0.35f, new[] { 180f, 520f, 260f }, 0.4f, 25f, 0f, 0f);
            clips["slip"] = Sweep(0.6f, new[] { 1200f, 300f, 900f, 200f }, 0.4f, 12f, 0.1f, 0f);
            clips["count"] = Chord(0.35f, new[] { 660f }, 0.5f);
            clips["go"] = Chord(0.8f, new[] { 523f, 784f, 1046f }, 0.45f);
            clips["cheer"] = Concat(Chord(0.3f, new[] { 523f, 659f }, 0.35f), Chord(0.3f, new[] { 587f, 740f }, 0.35f), Chord(0.8f, new[] { 659f, 784f, 1046f }, 0.4f));
            clips["mus_field"] = KhmerTune(96, 0);
            clips["mus_battle"] = KhmerTune(132, 1);
            clips["mus_title"] = KhmerTune(80, 2);
            clips["mus_race"] = KhmerTune(150, 3);
        }

        void Update()
        {
            musicFade = Mathf.MoveTowards(musicFade, 1, Time.unscaledDeltaTime / 2f);
            music.volume = MusicVolume * musicFade;
            music2.volume = MusicVolume * (1 - musicFade);
            if (music2.volume <= 0.001f && music2.isPlaying) music2.Stop();
            fx.volume = FxVolume;
        }

        public void Play(string key, float vol = 1f)
        {
            if (clips.TryGetValue(key, out var c)) fx.PlayOneShot(c, vol);
        }

        public void Music(string name)
        {
            if (name == musicName) return;
            musicName = name;
            clips.TryGetValue("mus_" + name, out var c);
            music2.Stop(); music2.clip = music.clip;
            if (music2.clip != null) { music2.time = music.isPlaying ? music.time % music2.clip.length : 0; music2.Play(); }
            music.Stop(); music.clip = c; if (c) music.Play();
            musicFade = 0;
        }

        static AudioClip Tone(float dur, float f0, float f1, float vol, bool noise, bool onlyNoise = false)
        {
            int n = (int)(SR * dur);
            var d = new float[n];
            float ph = 0;
            var rnd = new System.Random(1);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = Mathf.Pow(1 - t, 2f) * Mathf.Min(1, i / 200f);
                ph += 2 * Mathf.PI * Mathf.Lerp(f0, f1, t) / SR;
                float s = onlyNoise ? 0 : Mathf.Sin(ph);
                if (noise) s = s * 0.6f + ((float)rnd.NextDouble() * 2 - 1) * (onlyNoise ? Mathf.Sin(t * Mathf.PI) : 0.4f);
                d[i] = s * env * vol;
            }
            var c = AudioClip.Create("tone", n, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        static AudioClip Chord(float dur, float[] freqs, float vol)
        {
            int n = (int)(SR * dur);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, s = 0;
                for (int k = 0; k < freqs.Length; k++)
                {
                    float st = k * 0.07f; if (t < st) continue;
                    float tt = t - st;
                    s += Mathf.Sin(2 * Mathf.PI * freqs[k] * tt) * Mathf.Exp(-tt * 3f);
                }
                d[i] = s / freqs.Length * vol * Mathf.Min(1, i / 300f);
            }
            var c = AudioClip.Create("chord", n, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        /// <summary>សំឡេងរអ៊ូ/ស្រែក៖ ប្ដូរកម្ពស់សំឡេងតាមចំណុច + រំញ័រ + harmonic</summary>
        static AudioClip Sweep(float dur, float[] pts, float vol, float vibrato, float noise, float harm)
        {
            int n = (int)(SR * dur);
            var d = new float[n];
            var rnd = new System.Random(5);
            float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float fi = t * (pts.Length - 1);
                int k = Mathf.Min(pts.Length - 2, (int)fi);
                float f = Mathf.Lerp(pts[k], pts[k + 1], Mathf.SmoothStep(0, 1, fi - k));
                f *= 1 + Mathf.Sin(i / (float)SR * vibrato * 6.283f) * 0.04f;
                ph += 6.283f * f / SR;
                float sgl = Mathf.Sin(ph) + harm * 0.5f * Mathf.Sin(ph * 2) + harm * 0.3f * Mathf.Sin(ph * 3);
                sgl += ((float)rnd.NextDouble() * 2 - 1) * noise;
                float env = Mathf.Min(1, i / (SR * 0.02f)) * Mathf.Min(1, (n - i) / (SR * 0.08f));
                d[i] = sgl * env * vol * 0.6f;
            }
            var c = AudioClip.Create("sweep", n, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        static AudioClip Silence(float dur) { int n = (int)(SR * dur); var c = AudioClip.Create("sil", n, 1, SR, false); c.SetData(new float[n], 0); return c; }

        static AudioClip Concat(params AudioClip[] parts)
        {
            int total = 0; foreach (var p in parts) total += p.samples;
            var d = new float[total]; int o = 0;
            foreach (var p in parts) { var b = new float[p.samples]; p.GetData(b, 0); b.CopyTo(d, o); o += b.Length; }
            var c = AudioClip.Create("concat", total, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        /// <summary>ភ្លេងបែបខ្មែរ៖ រនាត (pentatonic) + ស្គរ + ឈិង — រង្វិលជុំ</summary>
        static AudioClip KhmerTune(float bpm, int variant)
        {
            float beat = 60f / bpm;
            int bars = 8, steps = bars * 8; // ១ ជង្វាក់ = ៨ ចង្វាក់កន្លះ
            float stepDur = beat / 2;
            int n = (int)(SR * stepDur * steps);
            var d = new float[n];
            // សំឡេង pentatonic (C D E G A) ពីរអុកតាវ
            float[] scale = { 261.6f, 293.7f, 329.6f, 392.0f, 440.0f, 523.3f, 587.3f, 659.3f, 784.0f, 880.0f };
            var rnd = new System.Random(11 + variant);
            int pos = 4 + variant;
            for (int st = 0; st < steps; st++)
            {
                int start = (int)(st * stepDur * SR);
                // រនាត៖ ដើរជំហានតូចៗ លើក/ចុះ
                bool play = variant == 2 ? st % 2 == 0 || rnd.NextDouble() < 0.3 : rnd.NextDouble() < 0.85;
                if (play)
                {
                    int move = rnd.Next(-2, 3);
                    pos = Mathf.Clamp(pos + move, 0, scale.Length - 1);
                    if (st % 16 == 0) pos = variant == 1 ? 2 : 4;
                    float f = scale[pos];
                    int len = (int)(SR * 0.45f);
                    for (int i = 0; i < len && start + i < n; i++)
                    {
                        float t = i / (float)SR;
                        float env = Mathf.Exp(-t * 9f);
                        float s = Mathf.Sin(2 * Mathf.PI * f * t) + 0.35f * Mathf.Sin(2 * Mathf.PI * f * 3.9f * t) * Mathf.Exp(-t * 30f);
                        d[start + i] += s * env * 0.22f;
                    }
                }
                // ស្គរ (ស្គរសំភោ)៖ ចង្វាក់ ១ និង ៣ + ចង្វាក់រំលង
                bool drum = st % 4 == 0 || (variant == 1 && st % 8 == 6) || (variant == 0 && st % 16 == 11) || (variant == 3 && st % 2 == 0);
                if (drum)
                {
                    int len = (int)(SR * 0.3f);
                    float ph = 0;
                    for (int i = 0; i < len && start + i < n; i++)
                    {
                        float t = i / (float)SR;
                        ph += 2 * Mathf.PI * Mathf.Lerp(120, 55, t * 4) / SR;
                        d[start + i] += Mathf.Sin(ph) * Mathf.Exp(-t * 12f) * 0.45f;
                    }
                }
                // ឈិង៖ ចង្វាក់ ២ និង ៤
                if (st % 4 == 2)
                {
                    int len = (int)(SR * 0.25f);
                    for (int i = 0; i < len && start + i < n; i++)
                    {
                        float t = i / (float)SR;
                        float s = Mathf.Sin(2 * Mathf.PI * 2637 * t) * 0.6f + Mathf.Sin(2 * Mathf.PI * 3951 * t) * 0.4f;
                        d[start + i] += s * Mathf.Exp(-t * (st % 8 == 2 ? 6f : 25f)) * 0.07f;
                    }
                }
            }
            // បំពេញបាសស្រាលៗ (ទ្រ)
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                int bar = (int)(t / (stepDur * 8)) % 4;
                float f = new[] { 130.8f, 98.0f, 110.0f, 98.0f }[bar];
                d[i] += Mathf.Sin(2 * Mathf.PI * f * t) * 0.05f * (0.6f + 0.4f * Mathf.Sin(t * 5f));
                d[i] = Mathf.Clamp(d[i], -1, 1);
            }
            var c = AudioClip.Create("music" + variant, n, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }
    }
}
