// កោះឧបសគ្គ៖ ផ្លូវឧបសគ្គកំប្លែងៗ សម្រាប់លេងម្នាក់ឯង លេងសាមគ្គី ឬប្រកួត (រហូតដល់ ៤ នាក់តាមអ៊ីនធឺណិត)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Netcode;

namespace KhmerHeroes
{
    public enum CourseMode { Solo = 0, Coop = 1, Versus = 2 }

    /// <summary>ឧបសគ្គដែលអាចបុកអ្នកលេង</summary>
    public abstract class Obstacle : MonoBehaviour
    {
        public static readonly List<Obstacle> All = new List<Obstacle>();
        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);
        /// <summary>បើបុក ត្រឡប់ true និងទិសច្រាន</summary>
        public abstract bool HitTest(Vector3 feet, out Vector3 knock);
        public static float Now => CourseManager.Now;
    }

    /// <summary>វេទិកាដែលផ្លាស់ទី — អ្នកលេងឈរលើវា ត្រូវតាមវា</summary>
    public abstract class Mover : MonoBehaviour
    {
        public abstract Vector3 Velocity { get; }
    }

    /// <summary>ក្បូនឫស្សីអណ្ដែត (ឡើងចុះ) ឬរំកិលឆ្វេងស្ដាំ</summary>
    public class Raft : Mover
    {
        public Vector3 basePos; public float bob = 0.35f, slide, speed = 1.5f, phase;
        public override Vector3 Velocity => new Vector3(slide * speed * Mathf.Cos(Obstacle.Now * speed + phase), bob * speed * Mathf.Cos(Obstacle.Now * speed * 1.3f + phase) * 1.3f, 0);
        void Update()
        {
            float t = Obstacle.Now;
            transform.position = basePos + new Vector3(slide * Mathf.Sin(t * speed + phase), bob * Mathf.Sin(t * speed * 1.3f + phase), 0);
        }
    }

    /// <summary>ដុំឈើយោល (pendulum)</summary>
    public class Pendulum : Obstacle
    {
        public Vector3 pivot; public float rope = 6f, amp = 55f, speed = 1.6f, phase; Transform log, ropeT;
        public static Pendulum Build(Transform parent, Vector3 pivot, float phase)
        {
            var g = new GameObject("Pendulum"); g.transform.SetParent(parent, false);
            var p = g.AddComponent<Pendulum>(); p.pivot = pivot; p.phase = phase;
            p.ropeT = Shapes.Make(PrimitiveType.Cylinder, g.transform, Vector3.zero, Vector3.one, new Color(0.55f, 0.45f, 0.3f), false).transform;
            p.log = new GameObject("Log").transform; p.log.SetParent(g.transform, false);
            var lg = Shapes.Make(PrimitiveType.Cylinder, p.log, Vector3.zero, new Vector3(1.3f, 1.4f, 1.3f), new Color(0.42f, 0.28f, 0.15f), false);
            lg.transform.localRotation = Quaternion.Euler(90, 0, 0);
            for (int i = -1; i <= 1; i += 2) Shapes.Make(PrimitiveType.Cylinder, p.log, new Vector3(0, 0, i * 1.0f), new Vector3(1.36f, 0.08f, 1.36f), new Color(0.75f, 0.12f, 0.12f), false).transform.localRotation = Quaternion.Euler(90, 0, 0);
            // ក្បាលឈើមានមុខញញឹម (កំប្លែង)
            Shapes.Make(PrimitiveType.Sphere, p.log, new Vector3(-0.3f, 0.25f, 1.42f), Vector3.one * 0.25f, Color.white, false);
            Shapes.Make(PrimitiveType.Sphere, p.log, new Vector3(0.3f, 0.25f, 1.42f), Vector3.one * 0.25f, Color.white, false);
            Shapes.Make(PrimitiveType.Sphere, p.log, new Vector3(-0.3f, 0.25f, 1.52f), Vector3.one * 0.12f, Color.black, false);
            Shapes.Make(PrimitiveType.Sphere, p.log, new Vector3(0.3f, 0.25f, 1.52f), Vector3.one * 0.12f, Color.black, false);
            Shapes.Make(PrimitiveType.Cube, p.log, new Vector3(0, -0.2f, 1.42f), new Vector3(0.5f, 0.08f, 0.05f), Color.black, false);
            return p;
        }
        float Angle(float t) => amp * Mathf.Sin(t * speed + phase);
        Vector3 LogPos(float t) { float a = Angle(t) * Mathf.Deg2Rad; return pivot + new Vector3(Mathf.Sin(a) * rope, -Mathf.Cos(a) * rope, 0); }
        void Update()
        {
            var p = LogPos(Now);
            log.position = p;
            log.rotation = Quaternion.Euler(0, 0, Angle(Now));
            var mid = (pivot + p) / 2; var d = p - pivot;
            ropeT.position = mid; ropeT.localScale = new Vector3(0.12f, d.magnitude / 2, 0.12f);
            ropeT.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
        }
        public override bool HitTest(Vector3 feet, out Vector3 knock)
        {
            knock = Vector3.zero;
            var p = LogPos(Now);
            var c = feet + Vector3.up * 0.9f;
            var dz = Mathf.Abs(c.z - p.z);
            var dxy = new Vector2(c.x - p.x, c.y - p.y).magnitude;
            if (dz > 1.6f || dxy > 1.5f) return false;
            float vx = Mathf.Cos(Now * speed + phase) >= 0 ? 1 : -1;
            knock = new Vector3(vx * 15f, 7f, 0);
            return true;
        }
    }

    /// <summary>ដងបង្វិលលើវេទិកាមូល — ត្រូវលោតរំលង</summary>
    public class Sweeper : Obstacle
    {
        public Vector3 center; public float len = 8.6f, height = 0.6f, speed = 75f; Transform bar;
        public static Sweeper Build(Transform parent, Vector3 center)
        {
            var g = new GameObject("Sweeper"); g.transform.SetParent(parent, false); g.transform.position = center;
            var s = g.AddComponent<Sweeper>(); s.center = center;
            Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, 1.1f, 0), new Vector3(1.2f, 1.1f, 1.2f), new Color(0.93f, 0.74f, 0.3f));
            s.bar = new GameObject("Bar").transform; s.bar.SetParent(g.transform, false); s.bar.localPosition = new Vector3(0, s.height, 0);
            var b = Shapes.Make(PrimitiveType.Cube, s.bar, Vector3.zero, new Vector3(s.len * 2, 0.5f, 0.5f), new Color(0.75f, 0.12f, 0.12f), false);
            for (int i = -4; i <= 4; i++) Shapes.Make(PrimitiveType.Cube, s.bar, new Vector3(i * 2f, 0, 0), new Vector3(0.6f, 0.55f, 0.55f), Color.white, false);
            return s;
        }
        float Ang => Now * speed;
        void Update() { bar.localRotation = Quaternion.Euler(0, Ang, 0); }
        public override bool HitTest(Vector3 feet, out Vector3 knock)
        {
            knock = Vector3.zero;
            var rel = feet - center;
            if (rel.y > height + 0.35f || rel.y < -0.5f) return false; // លោតរួចហើយ
            var dir = Quaternion.Euler(0, Ang, 0) * Vector3.right;
            float along = Vector3.Dot(new Vector3(rel.x, 0, rel.z), dir);
            if (Mathf.Abs(along) > len) return false;
            var perpV = new Vector3(rel.x, 0, rel.z) - dir * along;
            if (perpV.magnitude > 0.7f) return false;
            var tang = Vector3.Cross(Vector3.up, dir) * Mathf.Sign(along);
            knock = tang * 13f + Vector3.up * 6f;
            return true;
        }
    }

    /// <summary>ក្ដារស្រុត — ធ្លាក់ពេលមានគេជាន់</summary>
    public class FallTile : MonoBehaviour
    {
        Vector3 home; float stepAt = -1, fallT = -1; Renderer r;
        public static FallTile Build(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            var g = Shapes.Make(PrimitiveType.Cube, parent, pos, size, c);
            g.name = "FallTile";
            var t = g.AddComponent<FallTile>(); t.home = pos; t.r = g.GetComponent<Renderer>();
            return t;
        }
        void Update()
        {
            var cm = CourseManager.I; if (cm == null) return;
            float now = Time.time;
            if (fallT < 0 && stepAt < 0)
                foreach (var rn in cm.runners)
                    if (rn != null && Mathf.Abs(rn.transform.position.x - home.x) < 1.9f && Mathf.Abs(rn.transform.position.z - home.z) < 1.9f && Mathf.Abs(rn.transform.position.y - (home.y + 0.2f)) < 0.6f)
                    { stepAt = now; break; }
            if (stepAt >= 0 && fallT < 0)
            {
                // ញ័រមុនធ្លាក់
                transform.position = home + new Vector3(Mathf.Sin(now * 60) * 0.06f, 0, 0);
                if (now - stepAt > 0.7f) { fallT = 0; Sfx.I?.Play("slip", 0.3f); }
            }
            if (fallT >= 0)
            {
                fallT += Time.deltaTime;
                transform.position = home + Vector3.down * (fallT * fallT * 6f);
                if (fallT > 3.2f) { fallT = -1; stepAt = -1; transform.position = home; }
            }
        }
    }

    /// <summary>ស្គរលោត (trampoline)</summary>
    public class BounceDrum : MonoBehaviour
    {
        public float power = 21f; Transform skin; float squash;
        public static BounceDrum Build(Transform parent, Vector3 pos)
        {
            var g = new GameObject("Drum"); g.transform.SetParent(parent, false); g.transform.position = pos;
            var d = g.AddComponent<BounceDrum>();
            Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, -1.5f, 0), new Vector3(0.6f, 1.5f, 0.6f), new Color(0.35f, 0.22f, 0.12f));
            var body = Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, 0, 0), new Vector3(3.2f, 0.5f, 3.2f), new Color(0.7f, 0.12f, 0.12f), false);
            var bc = body.AddComponent<BoxCollider>(); bc.size = new Vector3(0.95f, 2f, 0.95f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36 * Mathf.Deg2Rad;
                Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(Mathf.Cos(a) * 1.6f, 0, Mathf.Sin(a) * 1.6f), new Vector3(0.12f, 1f, 0.12f), new Color(0.93f, 0.74f, 0.3f), false);
            }
            d.skin = Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, 0.52f, 0), new Vector3(3.0f, 0.03f, 3.0f), new Color(0.95f, 0.9f, 0.75f), false).transform;
            return d;
        }
        public void Pop() { squash = 1; Sfx.I?.Play("boing", 0.9f); }
        void Update()
        {
            squash = Mathf.MoveTowards(squash, 0, Time.deltaTime * 3);
            skin.localScale = new Vector3(3.0f + squash * 0.4f, 0.03f, 3.0f + squash * 0.4f);
            skin.localPosition = new Vector3(0, 0.52f - squash * 0.25f, 0);
        }
    }

    /// <summary>ផ្លែដូងរមៀលចុះពីលើជម្រាល</summary>
    public class CoconutRoller : Obstacle
    {
        public Vector3 top, bottom; public float every = 1.5f, speed = 9f;
        readonly List<Transform> pool = new List<Transform>();
        static readonly float[] Lanes = { -2f, 0.5f, 2.2f, -0.8f, 1.4f, -2.3f, 0f };
        public static CoconutRoller Build(Transform parent, Vector3 top, Vector3 bottom)
        {
            var g = new GameObject("Coconuts"); g.transform.SetParent(parent, false);
            var c = g.AddComponent<CoconutRoller>(); c.top = top; c.bottom = bottom;
            for (int i = 0; i < 6; i++)
            {
                var s = Shapes.Make(PrimitiveType.Sphere, g.transform, Vector3.zero, Vector3.one * 1.3f, new Color(0.4f, 0.28f, 0.15f), false);
                Shapes.Make(PrimitiveType.Sphere, s.transform, new Vector3(0.2f, 0.3f, 0.35f), Vector3.one * 0.15f, Color.black, false);
                Shapes.Make(PrimitiveType.Sphere, s.transform, new Vector3(-0.2f, 0.3f, 0.35f), Vector3.one * 0.15f, Color.black, false);
                c.pool.Add(s.transform);
            }
            return c;
        }
        float Len => Vector3.Distance(top, bottom);
        bool Pos(int k, float t, out Vector3 p)
        {
            float s = (t - k * every) * speed; p = Vector3.zero;
            if (s < 0 || s > Len + 2) return false;
            var dir = (bottom - top).normalized;
            p = top + dir * s + Vector3.right * Lanes[((k % Lanes.Length) + Lanes.Length) % Lanes.Length] + Vector3.up * 0.65f;
            return true;
        }
        void Update()
        {
            float t = Now; int k0 = Mathf.FloorToInt(t / every);
            for (int i = 0; i < pool.Count; i++)
            {
                int k = k0 - i;
                bool on = Pos(k, t, out var p);
                pool[i].gameObject.SetActive(on);
                if (on) { pool[i].position = p; pool[i].rotation = Quaternion.Euler(-t * speed * 50, 0, 0); }
            }
        }
        public override bool HitTest(Vector3 feet, out Vector3 knock)
        {
            knock = Vector3.zero;
            float t = Now; int k0 = Mathf.FloorToInt(t / every);
            for (int i = 0; i < pool.Count; i++)
            {
                if (!Pos(k0 - i, t, out var p)) continue;
                if (Vector3.Distance(p, feet + Vector3.up * 0.7f) < 1.3f) { knock = (bottom - top).normalized * 13f + Vector3.up * 7f; return true; }
            }
            return false;
        }
    }

    /// <summary>ដុំថ្មរមៀលចុះពីភ្នំ (ដំណើររឿង)</summary>
    public class StoryBoulders : Obstacle
    {
        public float zTop = 322, zBottom = 280, every = 3.2f, speed = 9f;
        readonly List<Transform> pool = new List<Transform>();
        static readonly float[] Lanes = { -4f, 1.5f, 4.5f, -1.5f, 3f, -5f };
        public static StoryBoulders Build()
        {
            var g = new GameObject("Boulders");
            var b = g.AddComponent<StoryBoulders>();
            var m = Mats.Textured("rock", Tex.Noise(new Color(0.42f, 0.4f, 0.37f), new Color(0.58f, 0.56f, 0.5f), 12, 6), Vector2.one);
            for (int i = 0; i < 6; i++) b.pool.Add(Shapes.MakeM(PrimitiveType.Sphere, g.transform, Vector3.zero, Vector3.one * 2f, m, false).transform);
            return b;
        }
        bool Pos(int k, float t, out Vector3 p)
        {
            float s = (t - k * every) * speed; p = Vector3.zero;
            if (s < 0 || zTop - s < zBottom) return false;
            float z = zTop - s, x = World.PathX(z) + Lanes[((k % Lanes.Length) + Lanes.Length) % Lanes.Length];
            p = new Vector3(x, World.H(x, z) + 1f, z);
            return true;
        }
        void Update()
        {
            float t = Time.time; int k0 = Mathf.FloorToInt(t / every);
            for (int i = 0; i < pool.Count; i++)
            {
                bool on = Pos(k0 - i, t, out var p);
                pool[i].gameObject.SetActive(on);
                if (on) { pool[i].position = p; pool[i].rotation = Quaternion.Euler(t * 300, 0, 0); }
            }
            var pl = Player.I;
            if (pl != null && pl.Alive && Game.I.Playing && HitTest(pl.transform.position, out var knock))
            {
                pl.Damage(12, pl.transform.position - knock.normalized, 6f);
                UI.I.FloatText(pl.transform.position + Vector3.up * 2.5f, "ថ្មរមៀល!", new Color(0.8f, 0.8f, 0.75f));
            }
        }
        public override bool HitTest(Vector3 feet, out Vector3 knock)
        {
            knock = Vector3.zero;
            float t = Time.time; int k0 = Mathf.FloorToInt(t / every);
            for (int i = 0; i < pool.Count; i++)
            {
                if (!Pos(k0 - i, t, out var p)) continue;
                if (Vector3.Distance(p, feet + Vector3.up * 0.8f) < 1.7f) { knock = Vector3.back * 10f + Vector3.up * 4f; return true; }
            }
            return false;
        }
    }

    /// <summary>ដុំឈើយោលនៅព្រៃឫស្សី (ដំណើររឿង)</summary>
    public class StoryLogs : MonoBehaviour
    {
        readonly List<Pendulum> logs = new List<Pendulum>();
        float cd;
        public static StoryLogs Build(float[] zs)
        {
            var g = new GameObject("StoryLogs");
            var s = g.AddComponent<StoryLogs>();
            var wood = new Color(0.36f, 0.24f, 0.13f);
            for (int i = 0; i < zs.Length; i++)
            {
                float z = zs[i], x = World.PathX(z), gy = World.H(x, z);
                var pivot = new Vector3(x, gy + 8.5f, z);
                // ស៊ុមឈើ
                foreach (var sx in new[] { -1, 1 })
                    Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(x + sx * 7f, gy + 4.4f, z), new Vector3(0.45f, 4.4f, 0.45f), wood);
                Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(x, gy + 8.7f, z), new Vector3(14.6f, 0.4f, 0.5f), wood, false);
                var p = Pendulum.Build(g.transform, pivot, i * 1.7f);
                p.rope = 6.5f; p.amp = 50; p.speed = 1.3f;
                s.logs.Add(p);
            }
            return s;
        }
        void Update()
        {
            var pl = Player.I;
            cd -= Time.deltaTime;
            if (pl == null || !pl.Alive || Game.I == null || !Game.I.Playing || cd > 0) return;
            foreach (var l in logs)
                if (l.HitTest(pl.transform.position, out var knock))
                {
                    pl.Damage(10, pl.transform.position - new Vector3(knock.x, 0, knock.z).normalized, 8f);
                    UI.I.FloatText(pl.transform.position + Vector3.up * 2.5f, "បុកដុំឈើ!", new Color(1f, 0.7f, 0.4f));
                    cd = 0.8f; break;
                }
        }
    }

    /// <summary>កោះឧបសគ្គ៖ បង្កើតពិភព និងគ្រប់គ្រងការលេង</summary>
    public class CourseManager : MonoBehaviour
    {
        public static CourseManager I;
        public CourseMode mode = CourseMode.Solo;
        public bool online;
        public double startAt = -1;      // ពេលចាប់ផ្ដើម (ServerTime)
        public int round;
        public readonly List<CourseRunner> runners = new List<CourseRunner>();
        public Vector3[] checkpoints;
        public float finishZ = 252f, finishY = 15f;
        GameObject barrier, gateL, gateR;
        Renderer plateA, plateB;
        float plateLatch = -1, gateT; bool gateOpen;
        bool countdownShown; int lastCount = -1; float resultShownRound = -1; float firstFinish = -1;
        public bool Started => startAt >= 0 && Now >= startAt;

        public static float Now
        {
            get
            {
                var nm = NetworkManager.Singleton;
                if (nm != null && nm.IsListening) return (float)nm.ServerTime.Time;
                return Time.time;
            }
        }

        public CourseRunner Local { get { foreach (var r in runners) if (r != null && r.isLocal) return r; return null; } }

        public static CourseManager Build(CourseMode mode, bool online)
        {
            var g = new GameObject("Course");
            var c = g.AddComponent<CourseManager>();
            I = c; c.mode = mode; c.online = online;
            c.BuildWorld();
            return c;
        }

        // ---------- ពិភព ----------
        Transform root;
        GameObject Box(Vector3 center, Vector3 size, Color c, string name = "Box")
        {
            var g = Shapes.Make(PrimitiveType.Cube, root, center, size, c); g.name = name; return g;
        }
        GameObject Plat(Vector3 center, Vector3 size, bool krama = false)
        {
            var g = Box(center, size, new Color(0.5f, 0.36f, 0.2f), "Platform");
            var top = Shapes.MakeM(PrimitiveType.Cube, g.transform, new Vector3(0, 0.5f, 0), new Vector3(1.002f, 0.04f / size.y, 1.002f),
                krama ? Mats.Textured("kramaPlat", Tex.Krama(new Color(0.75f, 0.12f, 0.12f), Color.white), new Vector2(size.x / 2, size.z / 2))
                      : Mats.Textured("grassPlat", Tex.Noise(new Color(0.35f, 0.55f, 0.2f), new Color(0.5f, 0.68f, 0.28f), 6, 2), new Vector2(size.x / 4, size.z / 4)), false);
            // គែមមាស
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, 0.5f, 0), new Vector3(1.01f, 0.02f / size.y, 1.01f), new Color(0.93f, 0.74f, 0.3f), false);
            return g;
        }

        void Flag(Vector3 at, Color c)
        {
            var g = new GameObject("Flag"); g.transform.SetParent(root, false); g.transform.position = at;
            Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, 2.2f, 0), new Vector3(0.15f, 2.2f, 0.15f), new Color(0.4f, 0.3f, 0.2f), false);
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0.75f, 3.9f, 0), new Vector3(1.5f, 0.9f, 0.05f), c, false);
            Shapes.Glow(PrimitiveType.Sphere, g.transform, new Vector3(0, 4.6f, 0), Vector3.one * 0.35f, new Color(1f, 0.85f, 0.4f), 2f);
        }

        void BuildWorld()
        {
            root = new GameObject("CourseWorld").transform;
            World.Lighting();
            RenderSettings.fogStartDistance = 120; RenderSettings.fogEndDistance = 420;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.88f);
            // សមុទ្រ
            var sea = GameObject.CreatePrimitive(PrimitiveType.Plane); Destroy(sea.GetComponent<Collider>());
            sea.transform.SetParent(root); sea.transform.position = new Vector3(0, 0, 130); sea.transform.localScale = new Vector3(60, 1, 60);
            var sr = sea.GetComponent<Renderer>(); sr.sharedMaterial = Mats.Get(new Color(0.15f, 0.45f, 0.55f), 0.85f); sr.shadowCastingMode = ShadowCastingMode.Off;
            // កោះតុបតែងឆ្ងាយៗ មានដើមត្នោត
            var rnd = new System.Random(4);
            for (int i = 0; i < 14; i++)
            {
                float side = i % 2 == 0 ? -1 : 1, x = side * (40 + (float)rnd.NextDouble() * 60), z = -20 + (float)rnd.NextDouble() * 300;
                var isl = Shapes.Make(PrimitiveType.Sphere, root, new Vector3(x, -3, z), new Vector3(24, 8, 20), new Color(0.82f, 0.74f, 0.5f), false);
                Shapes.Make(PrimitiveType.Sphere, root, new Vector3(x, -1.2f, z), new Vector3(18, 6, 15), new Color(0.35f, 0.55f, 0.22f), false);
                PalmAt(new Vector3(x - 3, 1.5f, z), 9); PalmAt(new Vector3(x + 4, 1.5f, z + 2), 11);
            }

            checkpoints = new[] { new Vector3(0, 2, 4), new Vector3(0, 2, 56), new Vector3(0, 2, 96), new Vector3(0, 2, 153), new Vector3(0, 2, 189), new Vector3(0, 10, 218) };

            // ០. កោះចាប់ផ្ដើម
            Plat(new Vector3(0, 1, 6), new Vector3(22, 2, 16), true);
            for (int i = 0; i < 4; i++) Shapes.Glow(PrimitiveType.Cylinder, root, new Vector3(-6 + i * 4, 2.03f, 2), new Vector3(2.2f, 0.02f, 2.2f), new Color(0.5f, 0.8f, 1f), 1.2f);
            barrier = Box(new Vector3(0, 4, 14.4f), new Vector3(22, 6, 0.4f), Color.white, "Barrier");
            barrier.GetComponent<Renderer>().sharedMaterial = Mats.Transparent(new Color(0.5f, 0.8f, 1f, 0.35f));
            Flag(new Vector3(-10, 2, 13), new Color(0.75f, 0.12f, 0.12f)); Flag(new Vector3(10, 2, 13), new Color(0.15f, 0.35f, 0.7f));

            // ១. ក្បូនឫស្សីអណ្ដែត
            for (int i = 0; i < 6; i++) RaftAt(new Vector3(i % 2 == 0 ? -2.2f : 2.2f, 1.75f, 18 + i * 6), new Vector3(4, 0.5f, 4), 0.35f, 0f, i * 1.1f);
            Plat(new Vector3(0, 1, 56), new Vector3(12, 2, 8)); Flag(new Vector3(5, 2, 58), new Color(0.93f, 0.74f, 0.3f));

            // ២. ស្ពានតូច + ដុំឈើយោល
            Box(new Vector3(0, 1.75f, 75), new Vector3(4, 0.5f, 30), new Color(0.55f, 0.38f, 0.2f), "Bridge");
            for (int i = 0; i < 3; i++)
            {
                float z = 66 + i * 9;
                foreach (var sx in new[] { -1, 1 }) Shapes.Make(PrimitiveType.Cylinder, root, new Vector3(sx * 7, 5, z), new Vector3(0.5f, 5, 0.5f), new Color(0.4f, 0.27f, 0.14f));
                Shapes.Make(PrimitiveType.Cube, root, new Vector3(0, 10, z), new Vector3(14.5f, 0.5f, 0.5f), new Color(0.4f, 0.27f, 0.14f), false);
                Pendulum.Build(root, new Vector3(0, 10, z), i * 2.1f);
            }
            Plat(new Vector3(0, 1, 96), new Vector3(12, 2, 12)); Flag(new Vector3(5, 2, 100), new Color(0.93f, 0.74f, 0.3f));

            // ៣. វេទិកាមូល + ដងបង្វិល
            Box(new Vector3(0, 1.75f, 103.6f), new Vector3(4, 0.5f, 3.4f), new Color(0.55f, 0.38f, 0.2f), "Link");
            var disk = Shapes.Make(PrimitiveType.Cylinder, root, new Vector3(0, 1, 114), new Vector3(18, 1, 18), new Color(0.5f, 0.36f, 0.2f));
            Destroy(disk.GetComponent<Collider>());
            var mc = disk.AddComponent<MeshCollider>(); mc.sharedMesh = disk.GetComponent<MeshFilter>().sharedMesh; mc.convex = true;
            Shapes.MakeM(PrimitiveType.Cylinder, disk.transform, new Vector3(0, 1.0f, 0), new Vector3(1, 0.02f, 1), Mats.Textured("kramaDisk", Tex.Krama(new Color(0.15f, 0.35f, 0.7f), Color.white), new Vector2(6, 6)), false);
            Sweeper.Build(root, new Vector3(0, 2, 114));

            // ៤. ក្បូនរំកិលឆ្វេងស្ដាំ
            for (int i = 0; i < 3; i++) RaftAt(new Vector3(0, 1.75f, 128 + i * 8), new Vector3(5, 0.5f, 6), 0.1f, 6f, i * 2f, 0.9f);
            Plat(new Vector3(0, 1, 155), new Vector3(20, 2, 12));
            // បន្ទះសាមគ្គី និងទ្វារ
            plateA = Shapes.Glow(PrimitiveType.Cylinder, root, new Vector3(-6, 2.05f, 155), new Vector3(2.6f, 0.05f, 2.6f), new Color(0.3f, 0.7f, 1f), 1.2f).GetComponent<Renderer>();
            plateB = Shapes.Glow(PrimitiveType.Cylinder, root, new Vector3(6, 2.05f, 155), new Vector3(2.6f, 0.05f, 2.6f), new Color(0.3f, 0.7f, 1f), 1.2f).GetComponent<Renderer>();
            gateL = Box(new Vector3(-5, 4, 160.6f), new Vector3(10, 4, 0.5f), new Color(0.45f, 0.3f, 0.16f), "GateL");
            gateR = Box(new Vector3(5, 4, 160.6f), new Vector3(10, 4, 0.5f), new Color(0.45f, 0.3f, 0.16f), "GateR");
            foreach (var g in new[] { gateL, gateR }) Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, 0.3f, -0.6f), new Vector3(0.9f, 0.1f, 0.2f), new Color(0.93f, 0.74f, 0.3f), false);
            Flag(new Vector3(9, 2, 151), new Color(0.93f, 0.74f, 0.3f));

            // ៥. ក្ដារស្រុត
            for (int row = 0; row < 5; row++)
                for (int col = -1; col <= 1; col++)
                    FallTile.Build(root, new Vector3(col * 4f, 1.8f, 165 + row * 4), new Vector3(3.6f, 0.4f, 3.6f), (row + col) % 2 == 0 ? new Color(0.75f, 0.12f, 0.12f) : new Color(0.93f, 0.85f, 0.65f));
            Plat(new Vector3(0, 1, 189), new Vector3(12, 2, 10)); Flag(new Vector3(5, 2, 192), new Color(0.93f, 0.74f, 0.3f));

            // ៦. ស្គរលោត ទៅវេទិកាខ្ពស់
            BounceDrum.Build(root, new Vector3(0, 2.3f, 199.5f));
            BounceDrum.Build(root, new Vector3(0, 2.6f, 207.5f));
            Plat(new Vector3(0, 9, 218), new Vector3(12, 2, 12)); Flag(new Vector3(5, 10, 222), new Color(0.93f, 0.74f, 0.3f));
            foreach (var sx in new[] { -1, 1 }) Shapes.Make(PrimitiveType.Cylinder, root, new Vector3(sx * 5, 4.5f, 218), new Vector3(1.2f, 4.5f, 1.2f), new Color(0.5f, 0.36f, 0.2f));

            // ៧. ជម្រាលផ្លែដូង
            var ramp = Box(new Vector3(0, 12.9f, 238), new Vector3(7, 0.5f, 28.8f), new Color(0.55f, 0.38f, 0.2f), "Ramp");
            ramp.transform.rotation = Quaternion.Euler(-Mathf.Atan2(6, 28) * Mathf.Rad2Deg, 0, 0);
            foreach (var sx in new[] { -1, 1 })
            {
                var rail = Box(new Vector3(sx * 3.7f, 13.6f, 238), new Vector3(0.3f, 1.2f, 28.8f), new Color(0.93f, 0.74f, 0.3f), "Rail");
                rail.transform.rotation = ramp.transform.rotation;
            }
            CoconutRoller.Build(root, new Vector3(0, 16.2f, 251), new Vector3(0, 10.2f, 224));

            // ៨. ទីបញ្ចប់
            Plat(new Vector3(0, 15, 258), new Vector3(14, 2, 12), true);
            var arch = new GameObject("FinishArch"); arch.transform.SetParent(root, false);
            foreach (var sx in new[] { -1, 1 }) Shapes.Make(PrimitiveType.Cylinder, arch.transform, new Vector3(sx * 6, 19, 253), new Vector3(0.4f, 3, 0.4f), new Color(0.93f, 0.74f, 0.3f));
            Shapes.MakeM(PrimitiveType.Cube, arch.transform, new Vector3(0, 22.2f, 253), new Vector3(12.6f, 1.2f, 0.2f), Mats.Textured("finish", Tex.Krama(Color.black, Color.white), new Vector2(6, 1)), false);
            // ពានមាស
            var cup = new GameObject("Trophy"); cup.transform.SetParent(root, false); cup.transform.position = new Vector3(0, 16, 260);
            Shapes.Glow(PrimitiveType.Cylinder, cup.transform, new Vector3(0, 0.5f, 0), new Vector3(1.2f, 0.5f, 1.2f), new Color(1f, 0.8f, 0.3f), 1.5f);
            Shapes.Glow(PrimitiveType.Sphere, cup.transform, new Vector3(0, 1.8f, 0), new Vector3(1.6f, 1.4f, 1.6f), new Color(1f, 0.8f, 0.3f), 1.8f);
            cup.AddComponent<Spin>();
            Flag(new Vector3(-5, 16, 262), new Color(0.75f, 0.12f, 0.12f)); Flag(new Vector3(5, 16, 262), new Color(0.15f, 0.35f, 0.7f));
        }

        void PalmAt(Vector3 p, float h)
        {
            Shapes.Make(PrimitiveType.Cylinder, root, p + Vector3.up * h / 2, new Vector3(0.4f, h / 2, 0.4f), new Color(0.35f, 0.28f, 0.2f), false);
            for (int i = 0; i < 8; i++)
            {
                var l = Shapes.Make(PrimitiveType.Sphere, root, p + Vector3.up * h + Quaternion.Euler(0, i * 45, 0) * Vector3.forward * 1.5f, new Vector3(1f, 0.12f, 3.2f), new Color(0.3f, 0.5f, 0.2f), false);
                l.transform.rotation = Quaternion.Euler(20, i * 45, 0);
            }
        }

        void RaftAt(Vector3 p, Vector3 size, float bob, float slide, float phase, float speed = 1.5f)
        {
            var g = Shapes.Make(PrimitiveType.Cube, root, p, size, new Color(0.6f, 0.62f, 0.3f));
            g.name = "Raft";
            for (float x = -size.x / 2 + 0.25f; x < size.x / 2; x += 0.5f)
                Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(x / size.x, 0.55f, 0), new Vector3(0.45f / size.x, 0.5f, 0.45f / size.y), new Color(0.55f, 0.6f, 0.25f), false).transform.localRotation = Quaternion.Euler(90, 0, 0);
            var r = g.AddComponent<Raft>(); r.basePos = p; r.bob = bob; r.slide = slide; r.phase = phase; r.speed = speed;
        }

        // ---------- ការលេង ----------
        public Vector3 SpawnPoint(int idx) => new Vector3(-6 + (idx % 4) * 4, 2.2f, 2);

        public Vector3 RespawnFor(CourseRunner r)
        {
            int cp = r.checkpoint;
            if (mode == CourseMode.Coop) foreach (var o in runners) if (o != null) cp = Mathf.Max(cp, o.checkpoint);
            var p = checkpoints[Mathf.Clamp(cp, 0, checkpoints.Length - 1)];
            return p + new Vector3(((r.slot % 4) - 1.5f) * 1.6f, 0.3f, 0);
        }

        public int CheckpointAt(Vector3 pos)
        {
            int best = -1;
            for (int i = 1; i < checkpoints.Length; i++)
            {
                var c = checkpoints[i];
                if (Mathf.Abs(pos.z - c.z) < 5f && Mathf.Abs(pos.y - c.y) < 2f && Mathf.Abs(pos.x - c.x) < 8f) best = i;
            }
            return best;
        }

        public bool AtFinish(Vector3 pos) => pos.z > finishZ + 1 && pos.y > finishY;

        /// <summary>ម្ចាស់បន្ទប់ (ឬអ្នកលេងម្នាក់ឯង) ចាប់ផ្ដើម/ចាប់ផ្ដើមម្ដងទៀត</summary>
        public void HostStart()
        {
            startAt = Now + 4f; round++;
            OnNewRound();
            Net.PushHostState();
        }

        public bool IsHost => !online || (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer);

        void Update()
        {
            float now = Now;
            // ទ្វារសាមគ្គី
            int onA = 0, onB = 0;
            foreach (var r in runners)
            {
                if (r == null) continue;
                var p = r.transform.position;
                if (Mathf.Abs(p.y - 2.2f) < 1f)
                {
                    if (Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(-6, 0, 155)) < 1.6f) onA++;
                    if (Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(6, 0, 155)) < 1.6f) onB++;
                }
            }
            bool wantOpen;
            if (mode == CourseMode.Versus) wantOpen = Mathf.Repeat(now, 7f) < 4f;           // បើក ៤ វិ បិទ ៣ វិ
            else if (mode == CourseMode.Solo || runners.Count <= 1)
            {
                if (onA > 0 || onB > 0) plateLatch = now + 6f;                              // ម្នាក់ឯង៖ បន្ទះនៅ ៦ វិនាទី
                wantOpen = now < plateLatch;
            }
            else
            {
                // សាមគ្គី៖ ត្រូវមានអ្នកឈរលើបន្ទះទាំងពីរព្រមគ្នា ទើបទ្វារបើក (ហើយនៅបើករហូត)
                if (onA > 0 && onB > 0 && !gateLatchedCoop) { gateLatchedCoop = true; UI.I.Toast("សាមគ្គីគឺជាកម្លាំង! ទ្វារបើកហើយ", 3f); }
                wantOpen = false;
            }
            if (gateLatchedCoop) wantOpen = true;
            plateA.sharedMaterial = Mats.Glow(onA > 0 ? new Color(0.3f, 1f, 0.4f) : new Color(0.3f, 0.7f, 1f), onA > 0 ? 2.5f : 1.2f);
            plateB.sharedMaterial = Mats.Glow(onB > 0 ? new Color(0.3f, 1f, 0.4f) : new Color(0.3f, 0.7f, 1f), onB > 0 ? 2.5f : 1.2f);
            if (wantOpen != gateOpen) { gateOpen = wantOpen; Sfx.I?.Play(gateOpen ? "gate" : "bad", 0.4f); }
            gateT = Mathf.MoveTowards(gateT, gateOpen ? 1 : 0, Time.deltaTime * 1.5f);
            gateL.transform.position = new Vector3(-5 - gateT * 10f, 4, 160.6f);
            gateR.transform.position = new Vector3(5 + gateT * 10f, 4, 160.6f);

            // របាំងចាប់ផ្ដើម
            bool running = Started;
            barrier.SetActive(!running);

            // រាប់ថយក្រោយ
            if (startAt >= 0 && now < startAt)
            {
                int c = Mathf.CeilToInt((float)(startAt - now));
                if (c != lastCount && c <= 3 && c >= 1) { UI.I.Big(Kh.Num(c), 0.9f); Sfx.I.Play("count"); }
                lastCount = c;
                if (!countdownShown) { countdownShown = true; var l = Local; if (l) l.ResetForRound(); }
            }
            else if (running && lastCount != 0) { lastCount = 0; UI.I.Big("ទៅ!", 1.2f, new Color(0.5f, 1f, 0.5f)); Sfx.I.Play("go"); }

            // ពេលទាំងអស់គ្នាដល់ទី → លទ្ធផល
            if (running && resultShownRound != round && runners.Count > 0)
            {
                bool all = true; int finished = 0;
                foreach (var r in runners) if (r != null) { if (r.finishTime < 0) all = false; else finished++; }
                if (finished > 0 && firstFinish < 0) firstFinish = now;
                bool versusTimeout = mode == CourseMode.Versus && firstFinish >= 0 && now - firstFinish > 45f;
                if (all || versusTimeout) { resultShownRound = round; ShowResults(); }
            }
            Hud(now, running);
        }
        bool gateLatchedCoop;

        void LateUpdate()
        {
            // ស្លាកឈ្មោះលើក្បាលមិត្តភក្ដិ
            foreach (var r in runners)
                if (r != null) UI.I.Tag(r, r.displayName, r.transform.position + Vector3.up * 2.35f, !r.isLocal);
        }

        void OnDestroy() { if (UI.I != null) UI.I.ClearTags(); if (I == this) I = null; }

        public void OnNewRound()
        {
            countdownShown = false; lastCount = -1; firstFinish = -1; gateLatchedCoop = false; plateLatch = -1;
            UI.I.CloseModal();
        }

        void Hud(float now, bool running)
        {
            var local = Local;
            string modeName = mode == CourseMode.Solo ? "លេងម្នាក់ឯង" : mode == CourseMode.Coop ? "លេងសាមគ្គី" : "ប្រកួតគ្នា";
            string obj;
            if (startAt < 0) obj = IsHost ? "• ចុច Enter ដើម្បីចាប់ផ្ដើម" + (online ? "\n• រង់ចាំមិត្តភក្ដិចូលសិន ក៏បាន" : "") : "• រង់ចាំម្ចាស់បន្ទប់ចាប់ផ្ដើម...";
            else if (mode == CourseMode.Coop) obj = "• ជួយគ្នាឲ្យគ្រប់គ្នាដល់ពានមាស\n• បន្ទះពណ៌ខៀវពីរ ត្រូវមានមនុស្សឈរព្រមគ្នា ទើបទ្វារបើក";
            else if (mode == CourseMode.Versus) obj = "• ប្រណាំងទៅពានមាសឲ្យដល់មុនគេ\n• ចុច J/កណ្ដុរឆ្វេង ដើម្បីរុញគូប្រកួត!";
            else obj = "• ឆ្លងកាត់ឧបសគ្គ ទៅដល់ពានមាស\n• ជាន់បន្ទះពណ៌ខៀវ ដើម្បីបើកទ្វារ";
            var sb = new System.Text.StringBuilder(obj);
            sb.Append("\n\nអ្នកលេង (").Append(Kh.Num(runners.Count)).Append("/៤)៖");
            foreach (var r in runners)
            {
                if (r == null) continue;
                sb.Append("\n").Append(r.isLocal ? "▸ " : "• ").Append(r.displayName);
                if (r.finishTime >= 0) sb.Append("  ✓ ").Append(Fmt(r.finishTime));
                else if (running) sb.Append("  · ចំណុច ").Append(Kh.Num(r.checkpoint)).Append("/៥");
            }
            UI.I.Objective("កោះឧបសគ្គ · " + modeName, sb.ToString());
            if (running && local != null)
            {
                float t = local.finishTime >= 0 ? local.finishTime : now - (float)startAt;
                string rank = "";
                if (mode == CourseMode.Versus && runners.Count > 1)
                {
                    int rk = 1; foreach (var r in runners) if (r != null && r != local && Score(r) > Score(local)) rk++;
                    rank = "   ·   ទី" + Kh.Num(rk) + "/" + Kh.Num(runners.Count);
                }
                UI.I.RaceHud("ពេល " + Fmt(t) + rank + "   ·   ធ្លាក់ទឹក " + Kh.Num(local.falls) + " ដង");
            }
            else UI.I.RaceHud(startAt >= 0 ? "ត្រៀមខ្លួន..." : online && Net.JoinCode != null ? "លេខកូដបន្ទប់៖ " + Net.JoinCode : "");
        }

        static float Score(CourseRunner r) => r.finishTime >= 0 ? 10000 - r.finishTime : r.transform.position.z + r.transform.position.y;

        public static string Fmt(float t)
        {
            int s = Mathf.Max(0, (int)t);
            return Kh.Num(s / 60) + ":" + Kh.Num(s % 60 / 10) + Kh.Num(s % 10);
        }

        void ShowResults()
        {
            var list = new List<CourseRunner>();
            foreach (var r in runners) if (r != null) list.Add(r);
            list.Sort((a, b) => Score(b).CompareTo(Score(a)));
            var rows = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                rows.Add(Kh.Num(i + 1) + ".  " + r.displayName + "   " + (r.finishTime >= 0 ? Fmt(r.finishTime) : "មិនទាន់ដល់") + "   (ធ្លាក់ទឹក " + Kh.Num(r.falls) + ")");
            }
            string note;
            if (mode == CourseMode.Coop)
            {
                float team = 0; foreach (var r in list) team = Mathf.Max(team, r.finishTime);
                note = "ក្រុមរបស់អ្នកបានដល់ពានមាសទាំងអស់គ្នា! ពេលក្រុម " + Fmt(team) + " — សាមគ្គីគឺជាកម្លាំង!";
            }
            else if (mode == CourseMode.Versus) note = list.Count > 0 ? list[0].displayName + " ឈ្នះលេខ ១! អបអរសាទរ!" : "";
            else note = "អស្ចារ្យ! អ្នកឆ្លងកាត់កោះឧបសគ្គ ក្នុងរយៈពេល " + (list.Count > 0 ? Fmt(list[0].finishTime) : "");
            Sfx.I.Play("cheer");
            UI.I.CourseResults(rows.ToArray(), note, IsHost, () => HostStart(), () => Game.I.LeaveCourse());
        }
    }

    public class Spin : MonoBehaviour { void Update() { transform.Rotate(0, 60 * Time.deltaTime, 0); } }
}
