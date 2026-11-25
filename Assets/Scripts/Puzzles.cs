// វត្ថុដែលអាចប្រើបាន៖ អ្នកភូមិ ទ្វារ ថ្មរុញ ចន្លុះ ដងទាញ ស្ពាន ទឹកដូង កញ្ជើស្រូវពូជ
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KhmerHeroes
{
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();
        public virtual string Prompt => "ប្រើ";
        public virtual bool Usable => true;
        public abstract void Use(Player p);
        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        public static Interactable Nearest(Vector3 p, float max)
        {
            Interactable best = null; float bd = max;
            foreach (var i in All)
            {
                if (i == null || !i.Usable) continue;
                float d = Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(i.transform.position.x, 0, i.transform.position.z));
                if (Mathf.Abs(p.y - i.transform.position.y) > 3f) continue;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }
    }

    /// <summary>អ្នកភូមិ៖ និយាយ ហើយអាចសួរសំណួរ</summary>
    public class Npc : Interactable
    {
        public string npcName;
        public Func<Line[]> lines;
        public Action after;
        public Func<bool> marker; // បង្ហាញសញ្ញា "!" ពេលមានរឿងថ្មី
        HumanRig rig; Transform mark;

        public static Npc Spawn(string name, Vector3 pos, float yaw, HumanStyle st)
        {
            var g = new GameObject("NPC_" + name);
            g.transform.position = World.Ground(pos);
            g.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var n = g.AddComponent<Npc>(); n.npcName = name;
            n.rig = HumanRig.Build(g.transform, st);
            var col = g.AddComponent<CapsuleCollider>(); col.height = 1.8f; col.radius = 0.35f; col.center = Vector3.up * 0.9f;
            n.mark = Shapes.Glow(PrimitiveType.Cube, g.transform, Vector3.up * (2.3f * st.scale), new Vector3(0.2f, 0.5f, 0.2f), new Color(1f, 0.8f, 0.2f), 2.5f).transform;
            Shapes.Glow(PrimitiveType.Cube, n.mark, new Vector3(0, -0.8f, 0), new Vector3(1f, 0.3f, 1f), new Color(1f, 0.8f, 0.2f), 2.5f);
            return n;
        }

        public override string Prompt => "និយាយជាមួយ " + npcName;

        public override void Use(Player p)
        {
            var dir = p.transform.position - transform.position; dir.y = 0;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
            var ls = lines != null ? lines() : null;
            if (ls == null || ls.Length == 0) { after?.Invoke(); return; }
            UI.I.Dialogue(ls, () => after?.Invoke());
        }

        void Update()
        {
            bool show = marker == null || marker();
            mark.gameObject.SetActive(show);
            if (show) { mark.localRotation = Quaternion.Euler(0, Time.time * 90, 0); mark.localPosition = new Vector3(0, mark.localPosition.y + Mathf.Sin(Time.time * 3) * 0.002f, 0); }
        }
    }

    /// <summary>ទ្វាររបងឈើ</summary>
    public class Gate : MonoBehaviour
    {
        Transform left, right; float t; public bool open;
        public static Gate Build(Vector3 center, float width, float halfSpan)
        {
            var g = new GameObject("Gate"); g.transform.position = World.Ground(center);
            var gate = g.AddComponent<Gate>();
            var wood = new Color(0.36f, 0.24f, 0.13f);
            // របងបង្គោលឈើ ពីច្រាំងម្ខាងទៅម្ខាង
            for (float x = -halfSpan; x <= halfSpan; x += 0.9f)
            {
                if (Mathf.Abs(x) < width / 2 + 0.6f) continue;
                var p = center + new Vector3(x, 0, 0);
                float gy = World.Height(p);
                float h = 4.2f + Mathf.PerlinNoise(x * 0.7f, center.z) * 1.2f;
                var post = Shapes.Make(PrimitiveType.Cylinder, null, new Vector3(p.x, gy + h / 2 - 0.5f, p.z), new Vector3(0.55f, h / 2, 0.55f), wood * (0.85f + Mathf.PerlinNoise(x, 3) * 0.3f));
                post.transform.SetParent(g.transform, true);
                var tip = Shapes.Make(PrimitiveType.Cube, post.transform, new Vector3(0, 1.02f, 0), new Vector3(0.7f, 0.12f, 0.7f), wood * 0.8f, false);
                tip.transform.localRotation = Quaternion.Euler(45, 0, 45);
            }
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(-halfSpan / 2 - width / 4, 2.6f, -0.35f), new Vector3(halfSpan - width / 2, 0.2f, 0.15f), wood * 0.8f, false);
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(halfSpan / 2 + width / 4, 2.6f, -0.35f), new Vector3(halfSpan - width / 2, 0.2f, 0.15f), wood * 0.8f, false);
            // ស៊ុមទ្វារ មានដំបូលតូចបែបខ្មែរ
            float hw = width / 2;
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(-hw - 0.4f, 2.8f, 0), new Vector3(0.6f, 5.6f, 0.6f), wood * 0.7f);
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(hw + 0.4f, 2.8f, 0), new Vector3(0.6f, 5.6f, 0.6f), wood * 0.7f);
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, 5.7f, 0), new Vector3(width + 2.2f, 0.4f, 0.8f), wood * 0.7f, false);
            var roofL = Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(-width / 4 - 0.4f, 6.3f, 0), new Vector3(width / 2 + 1.6f, 0.15f, 1.6f), new Color(0.55f, 0.2f, 0.12f), false);
            roofL.transform.localRotation = Quaternion.Euler(0, 0, 18);
            var roofR = Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(width / 4 + 0.4f, 6.3f, 0), new Vector3(width / 2 + 1.6f, 0.15f, 1.6f), new Color(0.55f, 0.2f, 0.12f), false);
            roofR.transform.localRotation = Quaternion.Euler(0, 0, -18);
            foreach (var sx in new[] { -1, 1 })
            {
                var horn = Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(sx * (width / 2 + 1.5f), 6.1f, 0), new Vector3(0.15f, 0.7f, 0.15f), new Color(0.85f, 0.65f, 0.25f), false);
                horn.transform.localRotation = Quaternion.Euler(0, 0, sx * -30);
            }
            gate.left = Door(g.transform, new Vector3(-hw, 0, 0), hw, wood, 1);
            gate.right = Door(g.transform, new Vector3(hw, 0, 0), hw, wood, -1);
            return gate;
        }

        static Transform Door(Transform parent, Vector3 hinge, float w, Color wood, int side)
        {
            var h = new GameObject("Door").transform; h.SetParent(parent, false); h.localPosition = hinge;
            Shapes.Make(PrimitiveType.Cube, h, new Vector3(side * w / 2, 2.2f, 0), new Vector3(w, 4.4f, 0.25f), wood * 1.1f);
            for (int i = 0; i < 3; i++) Shapes.Make(PrimitiveType.Cube, h, new Vector3(side * w / 2, 0.8f + i * 1.4f, -0.17f), new Vector3(w * 0.95f, 0.18f, 0.08f), wood * 0.7f, false);
            return h;
        }

        public void Open()
        {
            if (open) return;
            open = true; Sfx.I?.Play("gate"); CamRig.I?.Shake(0.3f);
        }

        void Update()
        {
            if (!open || t >= 1) return;
            t = Mathf.Min(1, t + Time.deltaTime / 1.6f);
            float e = Mathf.SmoothStep(0, 1, t);
            left.localRotation = Quaternion.Euler(0, -100 * e, 0);
            right.localRotation = Quaternion.Euler(0, 100 * e, 0);
        }
    }

    /// <summary>បន្ទះថ្មភ្លឺ ដែលត្រូវដាក់ថ្មពីលើ</summary>
    public class Plate : MonoBehaviour
    {
        public bool pressed; Renderer glow;
        public static Plate Build(Vector3 pos)
        {
            var g = new GameObject("Plate"); g.transform.position = World.Ground(pos);
            var p = g.AddComponent<Plate>();
            Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, 0.05f, 0), new Vector3(2.2f, 0.08f, 2.2f), new Color(0.5f, 0.48f, 0.45f), false);
            p.glow = Shapes.Glow(PrimitiveType.Cylinder, g.transform, new Vector3(0, 0.1f, 0), new Vector3(1.4f, 0.04f, 1.4f), new Color(0.3f, 0.7f, 1f), 1.2f).GetComponent<Renderer>();
            return p;
        }
        public void Set(bool on)
        {
            if (on == pressed) return;
            pressed = on;
            glow.sharedMaterial = Mats.Glow(on ? new Color(0.3f, 1f, 0.4f) : new Color(0.3f, 0.7f, 1f), on ? 2.5f : 1.2f);
            if (on) Sfx.I?.Play("good", 0.6f);
        }
    }

    /// <summary>ថ្មធំសម្រាប់រុញ ម្ដង ២ ម៉ែត្រ</summary>
    public class PushStone : Interactable
    {
        public Rect bounds; public Plate[] plates;
        Vector3 target; bool moving;
        public static PushStone Build(Vector3 pos, Rect bounds, Plate[] plates)
        {
            var g = new GameObject("Stone"); g.transform.position = World.Ground(pos);
            var s = g.AddComponent<PushStone>(); s.bounds = bounds; s.plates = plates; s.target = g.transform.position;
            var rock = Shapes.MakeM(PrimitiveType.Cube, g.transform, new Vector3(0, 0.8f, 0), new Vector3(1.7f, 1.6f, 1.7f),
                Mats.Textured("stone", Tex.Noise(new Color(0.45f, 0.43f, 0.4f), new Color(0.62f, 0.6f, 0.55f), 10, 5), Vector2.one));
            // ក្បាច់ផ្កាចម្លាក់លើថ្ម
            Shapes.Glow(PrimitiveType.Cube, g.transform, new Vector3(0, 0.8f, 0.86f), new Vector3(0.5f, 0.5f, 0.02f), new Color(0.3f, 0.7f, 1f), 0.8f).transform.localRotation = Quaternion.Euler(0, 0, 45);
            Shapes.Glow(PrimitiveType.Cube, g.transform, new Vector3(0, 0.8f, -0.86f), new Vector3(0.5f, 0.5f, 0.02f), new Color(0.3f, 0.7f, 1f), 0.8f).transform.localRotation = Quaternion.Euler(0, 0, 45);
            return s;
        }
        public override string Prompt => "រុញថ្ម";
        public override bool Usable => !moving;
        public override void Use(Player p)
        {
            var d = transform.position - p.transform.position; d.y = 0;
            Vector3 dir = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Sign(d.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(d.z));
            var np = transform.position + dir * 2f;
            if (!bounds.Contains(new Vector2(np.x, np.z))) { UI.I.Toast("ថ្មរុញទៅទិសនេះមិនបានទេ"); Sfx.I?.Play("bad", 0.5f); return; }
            foreach (var other in FindObjectsByType<PushStone>(FindObjectsSortMode.None))
                if (other != this && Vector3.Distance(Flat(other.target), Flat(np)) < 1.5f) { UI.I.Toast("មានថ្មមួយទៀតនៅទីនោះ"); return; }
            target = World.Ground(np); moving = true;
            Sfx.I?.Play("gate", 0.4f);
        }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
        void Update()
        {
            if (!moving) return;
            transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 3f);
            if (Vector3.Distance(transform.position, target) < 0.01f)
            {
                moving = false;
                foreach (var pl in plates)
                {
                    bool any = false;
                    foreach (var s in FindObjectsByType<PushStone>(FindObjectsSortMode.None))
                        if (Vector3.Distance(Flat(s.transform.position), Flat(pl.transform.position)) < 0.8f) any = true;
                    pl.Set(any);
                }
            }
        }
    }

    /// <summary>ចន្លុះ (អុជតាមលំដាប់)</summary>
    public class Torch : Interactable
    {
        public int id; public bool lit; public Action<Torch> onLit;
        ParticleSystem fire;
        public static Torch Build(Vector3 pos, int id, Color banner)
        {
            var g = new GameObject("Torch"); g.transform.position = World.Ground(pos);
            var t = g.AddComponent<Torch>(); t.id = id;
            var wood = new Color(0.35f, 0.22f, 0.12f);
            Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, 1.3f, 0), new Vector3(0.25f, 1.3f, 0.25f), wood);
            Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, 2.7f, 0), new Vector3(0.6f, 0.15f, 0.6f), new Color(0.3f, 0.3f, 0.3f), false);
            // ទង់ពណ៌
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0.45f, 1.9f, 0), new Vector3(0.7f, 0.9f, 0.04f), banner, false);
            Shapes.Glow(PrimitiveType.Sphere, g.transform, new Vector3(0, 3.4f, 0), Vector3.one * 0.25f, banner, 1.5f);
            return t;
        }
        public override string Prompt => lit ? "បានអុជរួចហើយ" : "អុជចន្លុះ";
        public override bool Usable => !lit;
        public override void Use(Player p)
        {
            if (lit) return;
            lit = true;
            fire = Shapes.Fire(transform, new Vector3(0, 2.85f, 0), 1.2f);
            Sfx.I?.Play("pick");
            onLit?.Invoke(this);
        }
        public void PutOut()
        {
            lit = false;
            if (fire) Destroy(fire.gameObject);
        }
    }

    /// <summary>ដងទាញ</summary>
    public class Lever : Interactable
    {
        public bool pulled; public Action onPull; Transform arm;
        public static Lever Build(Vector3 pos, float yaw)
        {
            var g = new GameObject("Lever"); g.transform.position = World.Ground(pos); g.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var l = g.AddComponent<Lever>();
            var wood = new Color(0.35f, 0.22f, 0.12f);
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, 0.5f, 0), new Vector3(1.2f, 1f, 0.8f), new Color(0.4f, 0.38f, 0.35f));
            l.arm = new GameObject("Arm").transform; l.arm.SetParent(g.transform, false); l.arm.localPosition = new Vector3(0, 1f, 0);
            Shapes.Make(PrimitiveType.Cylinder, l.arm, new Vector3(0, 0.8f, 0), new Vector3(0.12f, 0.8f, 0.12f), wood, false);
            Shapes.Glow(PrimitiveType.Sphere, l.arm, new Vector3(0, 1.65f, 0), Vector3.one * 0.3f, new Color(1f, 0.75f, 0.3f), 1.5f);
            l.arm.localRotation = Quaternion.Euler(-40, 0, 0);
            return l;
        }
        public override string Prompt => "ទាញដងទាញ";
        public override bool Usable => !pulled;
        public override void Use(Player p)
        {
            pulled = true; Sfx.I?.Play("gate", 0.6f);
            onPull?.Invoke();
        }
        void Update()
        {
            arm.localRotation = Quaternion.Slerp(arm.localRotation, Quaternion.Euler(pulled ? 40 : -40, 0, 0), Time.deltaTime * 4f);
        }
    }

    /// <summary>ស្ពានឈើ (ពីរចំហៀង លើកចុះ)</summary>
    public class DrawBridge : MonoBehaviour
    {
        Transform a, b; float t; public bool down;
        public static DrawBridge Build(float zNear, float zFar, float deckY, float width)
        {
            var g = new GameObject("Bridge");
            var br = g.AddComponent<DrawBridge>();
            float half = (zFar - zNear) / 2;
            var wood = new Color(0.45f, 0.3f, 0.16f);
            br.a = new GameObject("HalfA").transform; br.a.SetParent(g.transform); br.a.position = new Vector3(0, deckY, zNear);
            br.b = new GameObject("HalfB").transform; br.b.SetParent(g.transform); br.b.position = new Vector3(0, deckY, zFar);
            foreach (var (h, sgn) in new[] { (br.a, 1f), (br.b, -1f) })
            {
                Shapes.Make(PrimitiveType.Cube, h, new Vector3(0, -0.2f, sgn * half / 2), new Vector3(width, 0.4f, half + 0.05f), wood);
                for (int i = 0; i < (int)half; i++)
                    Shapes.Make(PrimitiveType.Cube, h, new Vector3(0, 0.02f, sgn * (i + 0.5f)), new Vector3(width + 0.2f, 0.06f, 0.15f), wood * 0.75f, false);
                foreach (var sx in new[] { -1, 1 })
                    Shapes.Make(PrimitiveType.Cube, h, new Vector3(sx * width / 2, 0.5f, sgn * half / 2), new Vector3(0.12f, 0.1f, half), wood * 0.8f, false);
                // សសរ និងខ្សែពួរ
                foreach (var sx in new[] { -1, 1 })
                    Shapes.Make(PrimitiveType.Cylinder, g.transform, h.position + new Vector3(sx * (width / 2 + 0.4f), 2f, -sgn * 0.4f), new Vector3(0.4f, 3f, 0.4f), wood * 0.7f);
            }
            br.a.localRotation = Quaternion.Euler(-75, 0, 0);
            br.b.localRotation = Quaternion.Euler(75, 0, 0);
            return br;
        }
        public void Lower() { if (!down) { down = true; Sfx.I?.Play("gate"); CamRig.I?.Shake(0.4f); } }
        void Update()
        {
            if (!down || t >= 1) return;
            t = Mathf.Min(1, t + Time.deltaTime / 2.5f);
            float e = Mathf.SmoothStep(0, 1, t);
            a.localRotation = Quaternion.Euler(-75 * (1 - e), 0, 0);
            b.localRotation = Quaternion.Euler(75 * (1 - e), 0, 0);
        }
    }

    /// <summary>ទឹកដូង (បន្ថែមឈាម ហើយជួយអ្នកសន្លប់) និងកាក់មាស</summary>
    public class Pickup : MonoBehaviour
    {
        public bool heal; float baseY;
        public static Pickup Spawn(Vector3 pos, bool heal)
        {
            var g = new GameObject(heal ? "Coconut" : "Coin");
            g.transform.position = World.Ground(pos) + Vector3.up * 0.6f;
            var p = g.AddComponent<Pickup>(); p.heal = heal; p.baseY = g.transform.position.y;
            if (heal)
            {
                Shapes.Make(PrimitiveType.Sphere, g.transform, Vector3.zero, new Vector3(0.5f, 0.55f, 0.5f), new Color(0.35f, 0.55f, 0.15f), false);
                Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, 0.3f, 0), new Vector3(0.18f, 0.04f, 0.18f), new Color(0.95f, 0.95f, 0.85f), false);
                Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0.05f, 0.45f, 0), new Vector3(0.02f, 0.2f, 0.02f), new Color(0.9f, 0.3f, 0.5f), false);
            }
            else
            {
                var c = Shapes.Glow(PrimitiveType.Cylinder, g.transform, Vector3.zero, new Vector3(0.45f, 0.04f, 0.45f), new Color(1f, 0.8f, 0.25f), 1.2f);
                c.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            return p;
        }
        void Update()
        {
            transform.Rotate(0, 90 * Time.deltaTime, 0);
            transform.position = new Vector3(transform.position.x, baseY + Mathf.Sin(Time.time * 2.5f) * 0.12f, transform.position.z);
            var pl = Player.I;
            if (pl && Vector3.Distance(pl.transform.position + Vector3.up * 0.6f, transform.position) < 1.4f)
            {
                if (heal) { pl.Heal(45); UI.I.Toast("ទឹកដូងស្រស់! វីរជនទាំងអស់បានកម្លាំងមកវិញ"); }
                else { pl.coins += 5; }
                Sfx.I?.Play("pick");
                Destroy(gameObject);
            }
        }
    }

    /// <summary>ផ្ទាំងថ្មមានអក្សរ</summary>
    public class Tablet : Interactable
    {
        public Line[] text;
        public static Tablet Build(Vector3 pos, float yaw, Line[] text)
        {
            var g = new GameObject("Tablet"); g.transform.position = World.Ground(pos); g.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var t = g.AddComponent<Tablet>(); t.text = text;
            Shapes.MakeM(PrimitiveType.Cube, g.transform, new Vector3(0, 1.1f, 0), new Vector3(1.6f, 2.2f, 0.35f),
                Mats.Textured("stone", Tex.Noise(new Color(0.45f, 0.43f, 0.4f), new Color(0.62f, 0.6f, 0.55f), 10, 5), Vector2.one));
            for (int i = 0; i < 3; i++)
                Shapes.Glow(PrimitiveType.Cube, g.transform, new Vector3(0, 1.6f - i * 0.45f, -0.19f), new Vector3(1.1f, 0.08f, 0.02f), new Color(1f, 0.85f, 0.5f), 0.9f);
            return t;
        }
        public override string Prompt => "អានផ្ទាំងថ្ម";
        public override void Use(Player p) => UI.I.Dialogue(text, null);
    }

    /// <summary>កញ្ជើស្រូវពូជ — គោលដៅចុងក្រោយ</summary>
    public class SeedBasket : Interactable
    {
        public bool unlocked; public Action onTake; Transform glow;
        public static SeedBasket Build(Vector3 pos)
        {
            var g = new GameObject("SeedBasket"); g.transform.position = World.Ground(pos);
            var b = g.AddComponent<SeedBasket>();
            var weave = Mats.Textured("weave", Tex.Krama(new Color(0.75f, 0.6f, 0.35f), new Color(0.55f, 0.42f, 0.22f)), new Vector2(3, 2));
            Shapes.MakeM(PrimitiveType.Cylinder, g.transform, new Vector3(0, 0.5f, 0), new Vector3(1.2f, 0.5f, 1.2f), weave);
            Shapes.Make(PrimitiveType.Sphere, g.transform, new Vector3(0, 1f, 0), new Vector3(1.1f, 0.5f, 1.1f), new Color(0.9f, 0.78f, 0.35f), false);
            b.glow = Shapes.Glow(PrimitiveType.Sphere, g.transform, new Vector3(0, 1.4f, 0), Vector3.one * 0.4f, new Color(1f, 0.85f, 0.4f), 2.5f).transform;
            return b;
        }
        public override string Prompt => unlocked ? "យកកញ្ជើស្រូវពូជ" : "ត្រូវកម្ចាត់មេចោរសិន";
        public override void Use(Player p)
        {
            if (!unlocked) { UI.I.Toast("មេចោរនៅតែយាមកញ្ជើនេះ!"); return; }
            enabled = false; onTake?.Invoke();
        }
        void Update() { glow.localPosition = new Vector3(0, 1.6f + Mathf.Sin(Time.time * 2) * 0.2f, 0); }
    }
}
