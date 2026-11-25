// រាងកាយតួអង្គ៖ មនុស្សស្លៀកពាក់បែបខ្មែរ (សំពត់ចងក្បិន ក្រមា គ្រឿងមាស) និងសត្វព្រៃ — មានចលនាដើរ វាយ គេច ដួល
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KhmerHeroes
{
    public class HumanStyle
    {
        public Color skin = new Color(0.62f, 0.42f, 0.28f);
        public Color top = new Color(0.5f, 0.1f, 0.1f);
        public Color holBg = new Color(0.4f, 0.1f, 0.1f), holFg = new Color(0.9f, 0.7f, 0.3f), holAccent = new Color(0.2f, 0.3f, 0.5f);
        public Color kramaA = new Color(0.7f, 0.1f, 0.1f), kramaB = Color.white;
        public Color trim = new Color(0.93f, 0.74f, 0.30f);
        public Color hair = new Color(0.06f, 0.05f, 0.05f);
        public bool female, mask, headband = true, bareArms = true;
        public float scale = 1f, bulk = 1f;
        public Weapon weapon = Weapon.Sword;
        public Color blade = new Color(0.8f, 0.82f, 0.85f);
        public bool bigBlade;

        public static HumanStyle From(HeroDef h)
        {
            return new HumanStyle
            {
                skin = h.skin, top = h.top, holBg = h.holBg, holFg = h.holFg, holAccent = h.holAccent,
                kramaA = h.kramaA, kramaB = h.kramaB, trim = h.trim, female = h.female, weapon = h.weapon,
                bulk = h.weapon == Weapon.Staff ? 1.15f : 1f
            };
        }
    }

    /// <summary>ពន្លឺក្រហមពេលរងរបួស</summary>
    public class Flash : MonoBehaviour
    {
        Renderer[] rs; float until; MaterialPropertyBlock mpb;
        void Awake() { rs = GetComponentsInChildren<Renderer>(); mpb = new MaterialPropertyBlock(); }
        public void Refresh() { rs = GetComponentsInChildren<Renderer>(); }
        public void Hit(Color c, float dur = 0.12f)
        {
            until = Time.time + dur;
            mpb.SetColor("_Color", c);
            foreach (var r in rs) if (r && !(r is ParticleSystemRenderer)) r.SetPropertyBlock(mpb);
        }
        void Update()
        {
            if (until > 0 && Time.time > until)
            {
                until = 0;
                foreach (var r in rs) if (r) r.SetPropertyBlock(null);
            }
        }
    }

    /// <summary>របារឈាមលើក្បាល</summary>
    public class HpBar : MonoBehaviour
    {
        Transform fill, bg; public float frac = 1; public bool hideWhenFull = true;
        public static HpBar Make(Transform parent, float height, float width = 1.2f)
        {
            var g = new GameObject("HpBar"); g.transform.SetParent(parent, false); g.transform.localPosition = Vector3.up * height;
            var hb = g.AddComponent<HpBar>();
            hb.bg = Quad(g.transform, new Color(0, 0, 0, 0.6f), width, 0.14f, 0);
            hb.fill = Quad(g.transform, new Color(0.9f, 0.2f, 0.2f), width * 0.96f, 0.09f, -0.01f);
            return hb;
        }
        static Transform Quad(Transform p, Color c, float w, float h, float z)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(p, false); q.transform.localPosition = new Vector3(0, 0, z); q.transform.localScale = new Vector3(w, h, 1);
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = Mats.Sprite(c); r.shadowCastingMode = ShadowCastingMode.Off;
            return q.transform;
        }
        void LateUpdate()
        {
            var cam = Camera.main; if (!cam) return;
            bool show = !(hideWhenFull && frac >= 0.999f) && frac > 0;
            bg.gameObject.SetActive(show); fill.gameObject.SetActive(show);
            transform.rotation = cam.transform.rotation;
            float w = bg.localScale.x * 0.96f;
            fill.localScale = new Vector3(w * Mathf.Clamp01(frac), fill.localScale.y, 1);
            fill.localPosition = new Vector3(-w * (1 - Mathf.Clamp01(frac)) / 2, 0, -0.01f);
        }
    }

    /// <summary>រាងមនុស្ស និងចលនា (procedural animation)</summary>
    public class HumanRig : MonoBehaviour
    {
        public Transform model, hips, torso, head, armL, armR, foreL, foreR, legL, legR, shinL, shinR, handL, handR;
        public Weapon weapon;
        public float moveSpeed, refSpeed = 6f;
        public bool grounded = true, dead, sitting;
        float phase, atkT = -1, atkDur = 0.4f, hurtT = -1, rollT = -1, rollDur = 0.5f, spinT = -1, deadT;
        int atkKind;
        float modelBaseY;

        public static HumanRig Build(Transform parent, HumanStyle st)
        {
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * st.scale;
            var rig = parent.gameObject.AddComponent<HumanRig>();
            rig.model = root.transform; rig.weapon = st.weapon;

            var hol = Mats.Textured("hol" + st.holBg + st.holFg, Tex.Hol(st.holBg, st.holFg, st.holAccent), new Vector2(1, 1.5f));
            var krama = Mats.Textured("krama" + st.kramaA + st.kramaB, Tex.Krama(st.kramaA, st.kramaB), new Vector2(2, 2));
            float b = st.bulk;

            rig.hips = Pivot(root.transform, "Hips", new Vector3(0, 0.95f, 0));
            // សំពត់ចងក្បិន៖ ចង្កេះ + ភ្លៅប៉ោង + កន្ទុយក្បិននៅក្រោយ
            Shapes.MakeM(PrimitiveType.Cylinder, rig.hips, new Vector3(0, 0.02f, 0), new Vector3(0.40f * b, 0.11f, 0.28f * b), hol, false);
            Shapes.Make(PrimitiveType.Cylinder, rig.hips, new Vector3(0, 0.1f, 0), new Vector3(0.41f * b, 0.025f, 0.29f * b), st.trim, false); // ខ្សែក្រវាត់មាស
            var kben = Shapes.MakeM(PrimitiveType.Cube, rig.hips, new Vector3(0, -0.16f, -0.12f * b), new Vector3(0.12f, 0.3f, 0.06f), hol, false);
            kben.transform.localRotation = Quaternion.Euler(15, 0, 0);
            if (st.female)
            {
                // សំពត់ចងក្បិនបែបនារី មានក្បាលសំពត់ (ផ្នត់) ខាងមុខ
                Shapes.MakeM(PrimitiveType.Cube, rig.hips, new Vector3(0, -0.1f, 0.13f * b), new Vector3(0.1f, 0.22f, 0.04f), hol, false);
            }

            rig.legL = Pivot(rig.hips, "LegL", new Vector3(-0.11f * b, -0.02f, 0));
            rig.legR = Pivot(rig.hips, "LegR", new Vector3(0.11f * b, -0.02f, 0));
            foreach (var leg in new[] { rig.legL, rig.legR })
            {
                Shapes.MakeM(PrimitiveType.Capsule, leg, new Vector3(0, -0.2f, 0), new Vector3(0.21f * b, 0.24f, 0.22f * b), hol, false);
                var shin = Pivot(leg, "Shin", new Vector3(0, -0.43f, 0));
                Shapes.Make(PrimitiveType.Capsule, shin, new Vector3(0, -0.2f, 0), new Vector3(0.11f * b, 0.22f, 0.11f * b), st.skin, false);
                Shapes.Make(PrimitiveType.Cylinder, shin, new Vector3(0, -0.36f, 0), new Vector3(0.12f * b, 0.02f, 0.12f * b), st.trim, false); // កងជើង
                Shapes.Make(PrimitiveType.Cube, shin, new Vector3(0, -0.45f, 0.05f), new Vector3(0.1f, 0.06f, 0.22f), st.skin * 0.9f, false);
                if (leg == rig.legL) rig.shinL = shin; else rig.shinR = shin;
            }

            rig.torso = Pivot(rig.hips, "Torso", new Vector3(0, 0.1f, 0));
            if (st.female)
            {
                Shapes.Make(PrimitiveType.Capsule, rig.torso, new Vector3(0, 0.28f, 0), new Vector3(0.36f, 0.26f, 0.24f), st.top, false);
                // ក្រមាស្ពាយឆៀង (ស្បៃ)
                var sb = Shapes.MakeM(PrimitiveType.Cube, rig.torso, new Vector3(0, 0.3f, 0), new Vector3(0.08f, 0.62f, 0.27f), krama, false);
                sb.transform.localRotation = Quaternion.Euler(0, 0, 35);
            }
            else
            {
                Shapes.Make(PrimitiveType.Capsule, rig.torso, new Vector3(0, 0.3f, 0), new Vector3(0.42f * b, 0.27f, 0.26f * b), st.top, false);
                var sash = Shapes.MakeM(PrimitiveType.Cube, rig.torso, new Vector3(0, 0.3f, 0), new Vector3(0.09f, 0.66f, 0.29f * b), krama, false);
                sash.transform.localRotation = Quaternion.Euler(0, 0, -35);
            }
            // ខ្សែកមាស
            Shapes.Make(PrimitiveType.Cylinder, rig.torso, new Vector3(0, 0.52f, 0.02f), new Vector3(0.26f, 0.02f, 0.2f), st.trim, false);
            Shapes.Make(PrimitiveType.Capsule, rig.torso, new Vector3(0, 0.58f, 0), new Vector3(0.11f, 0.06f, 0.11f), st.skin, false);

            rig.head = Pivot(rig.torso, "Head", new Vector3(0, 0.62f, 0));
            Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.14f, 0), new Vector3(0.24f, 0.27f, 0.25f), st.skin, false);
            Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(-0.055f, 0.16f, 0.115f), Vector3.one * 0.035f, Color.black, false);
            Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0.055f, 0.16f, 0.115f), Vector3.one * 0.035f, Color.black, false);
            Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.12f, 0.125f), new Vector3(0.03f, 0.04f, 0.03f), st.skin * 0.9f, false);
            // សក់
            Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.2f, -0.02f), new Vector3(0.26f, 0.21f, 0.26f), st.hair, false);
            if (st.female)
            {
                Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.3f, -0.1f), Vector3.one * 0.13f, st.hair, false); // សក់បួង
                Shapes.Make(PrimitiveType.Cylinder, rig.head, new Vector3(0, 0.33f, -0.1f), new Vector3(0.02f, 0.09f, 0.02f), st.trim, false); // ម្ជុលសក់មាស
                Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(-0.12f, 0.1f, 0), Vector3.one * 0.035f, st.trim, false); // ក្រវិល
                Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0.12f, 0.1f, 0), Vector3.one * 0.035f, st.trim, false);
            }
            if (st.headband)
            {
                var hb = Shapes.MakeM(PrimitiveType.Cylinder, rig.head, new Vector3(0, 0.22f, 0), new Vector3(0.265f, 0.025f, 0.265f), krama, false);
                hb.transform.localRotation = Quaternion.Euler(-8, 0, 0);
                if (!st.female)
                {
                    var tail = Shapes.MakeM(PrimitiveType.Cube, rig.head, new Vector3(0.03f, 0.14f, -0.15f), new Vector3(0.06f, 0.18f, 0.02f), krama, false);
                    tail.transform.localRotation = Quaternion.Euler(20, 0, 15);
                }
            }
            if (st.mask)
            {
                // ក្រមាខ្មៅបិទមុខ (ចោរ)
                Shapes.MakeM(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.07f, 0.03f), new Vector3(0.255f, 0.13f, 0.25f), krama, false);
            }

            rig.armL = Pivot(rig.torso, "ArmL", new Vector3(-0.25f * b, 0.5f, 0));
            rig.armR = Pivot(rig.torso, "ArmR", new Vector3(0.25f * b, 0.5f, 0));
            foreach (var arm in new[] { rig.armL, rig.armR })
            {
                Shapes.Make(PrimitiveType.Capsule, arm, new Vector3(0, -0.14f, 0), new Vector3(0.1f * b, 0.16f, 0.1f * b), st.bareArms ? st.skin : st.top, false);
                Shapes.Make(PrimitiveType.Cylinder, arm, new Vector3(0, -0.06f, 0), new Vector3(0.115f * b, 0.02f, 0.115f * b), st.trim, false); // កងដៃ
                var fore = Pivot(arm, "Fore", new Vector3(0, -0.29f, 0));
                Shapes.Make(PrimitiveType.Capsule, fore, new Vector3(0, -0.13f, 0), new Vector3(0.09f * b, 0.14f, 0.09f * b), st.skin, false);
                Shapes.Make(PrimitiveType.Cylinder, fore, new Vector3(0, -0.2f, 0), new Vector3(0.1f, 0.015f, 0.1f), st.trim, false);
                var hand = Pivot(fore, "Hand", new Vector3(0, -0.28f, 0));
                Shapes.Make(PrimitiveType.Sphere, hand, Vector3.zero, Vector3.one * 0.09f, st.skin, false);
                if (arm == rig.armL) { rig.foreL = fore; rig.handL = hand; } else { rig.foreR = fore; rig.handR = hand; }
            }

            BuildWeapon(rig, st);
            rig.modelBaseY = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
            return rig;
        }

        static Transform Pivot(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = pos; return t;
        }

        static void BuildWeapon(HumanRig rig, HumanStyle st)
        {
            var wood = new Color(0.42f, 0.27f, 0.14f);
            switch (st.weapon)
            {
                case Weapon.Sword:
                {
                    var w = Pivot(rig.handR, "Sword", Vector3.zero); w.localRotation = Quaternion.Euler(90, 0, 0);
                    Shapes.Make(PrimitiveType.Cylinder, w, new Vector3(0, 0.02f, 0), new Vector3(0.04f, 0.1f, 0.04f), wood, false);
                    Shapes.Make(PrimitiveType.Cube, w, new Vector3(0, 0.13f, 0), new Vector3(0.18f, 0.03f, 0.05f), st.trim, false);
                    float len = st.bigBlade ? 1.1f : 0.85f, wid = st.bigBlade ? 0.12f : 0.07f;
                    Shapes.Make(PrimitiveType.Cube, w, new Vector3(0, 0.15f + len / 2, 0), new Vector3(wid, len, 0.02f), st.blade, false);
                    break;
                }
                case Weapon.Knives:
                    foreach (var h in new[] { rig.handL, rig.handR })
                    {
                        var w = Pivot(h, "Knife", Vector3.zero); w.localRotation = Quaternion.Euler(90, 0, 0);
                        Shapes.Make(PrimitiveType.Cylinder, w, new Vector3(0, 0.02f, 0), new Vector3(0.035f, 0.07f, 0.035f), wood, false);
                        Shapes.Make(PrimitiveType.Cube, w, new Vector3(0, 0.25f, 0), new Vector3(0.05f, 0.35f, 0.015f), st.blade, false);
                    }
                    break;
                case Weapon.Staff:
                {
                    var w = Pivot(rig.handR, "Staff", Vector3.zero); w.localRotation = Quaternion.Euler(90, 0, 0);
                    Shapes.Make(PrimitiveType.Cylinder, w, new Vector3(0, 0.3f, 0), new Vector3(0.05f, 0.95f, 0.05f), wood, false);
                    Shapes.Make(PrimitiveType.Cylinder, w, new Vector3(0, 1.2f, 0), new Vector3(0.06f, 0.05f, 0.06f), st.trim, false);
                    Shapes.Make(PrimitiveType.Cylinder, w, new Vector3(0, -0.6f, 0), new Vector3(0.06f, 0.05f, 0.06f), st.trim, false);
                    break;
                }
                case Weapon.Bow:
                {
                    var w = Pivot(rig.handL, "Bow", Vector3.zero);
                    for (int i = -2; i <= 2; i++)
                    {
                        var seg = Shapes.Make(PrimitiveType.Cube, w, new Vector3(0, i * 0.17f, 0.06f - Mathf.Abs(i) * Mathf.Abs(i) * 0.025f), new Vector3(0.035f, 0.18f, 0.035f), wood, false);
                        seg.transform.localRotation = Quaternion.Euler(i * 14, 0, 0);
                    }
                    Shapes.Make(PrimitiveType.Cube, w, new Vector3(0, 0, -0.06f), new Vector3(0.006f, 0.8f, 0.006f), Color.white * 0.9f, false);
                    // បំពង់ព្រួញនៅខ្នង
                    var q = Shapes.Make(PrimitiveType.Cylinder, rig.torso, new Vector3(0.1f, 0.35f, -0.17f), new Vector3(0.1f, 0.22f, 0.1f), wood, false);
                    q.transform.localRotation = Quaternion.Euler(0, 0, -20);
                    break;
                }
            }
        }

        // ---------- ចលនា ----------
        public void Attack(int kind, float dur) { atkKind = kind; atkDur = Mathf.Max(0.1f, dur); atkT = 0; }
        public void Hurt() { hurtT = 0; }
        public void Roll(float dur) { rollDur = dur; rollT = 0; }
        public void Spin() { spinT = 0; }
        public bool Busy => atkT >= 0;

        static Quaternion X(float a) => Quaternion.Euler(a, 0, 0);
        static Quaternion XYZ(float x, float y, float z) => Quaternion.Euler(x, y, z);

        void Update()
        {
            float dt = Time.deltaTime;
            if (dead)
            {
                deadT = Mathf.Min(1, deadT + dt * 2.5f);
                model.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(-85, 0, 0), deadT);
                model.localPosition = new Vector3(0, Mathf.Lerp(0, 0.2f, deadT), Mathf.Lerp(0, -0.5f, deadT));
                return;
            }
            deadT = 0;
            float s = Mathf.Clamp01(moveSpeed / refSpeed);
            phase += dt * (4f + 6f * s) * (s > 0.05f ? 1 : 0);
            float sw = Mathf.Sin(phase), cw = Mathf.Cos(phase);
            float legA = sw * 38 * s, armA = sw * 30 * s;

            Quaternion lL = X(-legA), lR = X(legA);
            Quaternion kL = X(Mathf.Max(0, -cw) * 50 * s + 4), kR = X(Mathf.Max(0, cw) * 50 * s + 4);
            Quaternion aL = XYZ(legA * 0.8f, 0, -6), aR = XYZ(-legA * 0.8f, 0, 6);
            Quaternion fL = X(-15 - 10 * s), fR = X(-15 - 10 * s);
            Quaternion tor = XYZ(6 * s, sw * 6 * s, 0), hd = X(-4 * s);
            float bob = Mathf.Abs(sw) * 0.05f * s;
            if (s < 0.05f) { float br = Mathf.Sin(Time.time * 2f); tor = X(br * 1.5f); bob = br * 0.008f; }

            // អាវុធក្នុងដៃពេលឈរ
            if (weapon == Weapon.Bow) { aL = XYZ(-25 + legA * 0.3f, 0, -10); fL = X(-30); }
            if (weapon == Weapon.Staff) { aR = XYZ(-35, 0, 10); fR = X(-50); aL = XYZ(-30, 0, -10); fL = X(-60); }

            if (!grounded) { lL = X(-35); lR = X(-10); kL = X(60); kR = X(30); aL = XYZ(-40, 0, -25); aR = XYZ(-40, 0, 25); }

            // វាយ
            if (atkT >= 0)
            {
                atkT += dt / atkDur;
                float t = Mathf.Clamp01(atkT);
                float up = t < 0.35f ? t / 0.35f : t < 0.6f ? 1 - (t - 0.35f) / 0.25f : 0;
                float down = t < 0.35f ? 0 : t < 0.6f ? (t - 0.35f) / 0.25f : 1 - (t - 0.6f) / 0.4f;
                switch (weapon)
                {
                    case Weapon.Sword:
                    case Weapon.Knives:
                        if (atkKind % 3 == 1)
                        {   // ការវាយផ្ដេក
                            aR = XYZ(-90, Mathf.Lerp(70, -60, Mathf.Clamp01((t - 0.2f) / 0.4f)), 0); fR = X(-20);
                            tor = XYZ(0, Mathf.Lerp(30, -35, Mathf.Clamp01((t - 0.2f) / 0.4f)), 0);
                        }
                        else
                        {   // ការកាប់ពីលើចុះក្រោម
                            aR = XYZ(-160 * up - 30 * down, 0, 10); fR = X(-40 * up - 10);
                            tor = XYZ(-10 * up + 18 * down, -10, 0);
                        }
                        if (weapon == Weapon.Knives) { aL = XYZ(-80 * down - 40 * up, 0, -15); fL = X(-30); }
                        break;
                    case Weapon.Staff:
                        aR = XYZ(-170 * up - 40 * down, 0, 10); aL = XYZ(-170 * up - 40 * down, 0, -10); fR = fL = X(-20);
                        tor = X(-12 * up + 25 * down);
                        break;
                    case Weapon.Bow:
                        aL = XYZ(-90, 0, 0); fL = X(0);
                        aR = XYZ(-90, 0, 25); fR = X(-120 * (1 - Mathf.Clamp01((t - 0.5f) / 0.1f)));
                        tor = XYZ(0, -20, 0);
                        break;
                }
                if (atkT >= 1) atkT = -1;
            }

            // អង្គុយលើសត្វជំនិះ
            if (sitting)
            {
                lL = XYZ(-80, 0, -24); lR = XYZ(-80, 0, 24); kL = kR = X(85);
                if (atkT < 0)
                {
                    if (weapon == Weapon.Bow) { aR = XYZ(-45, 0, 12); fR = X(-45); }
                    else { aL = XYZ(-45, 0, -12); aR = XYZ(-45, 0, 12); fL = fR = X(-45); }
                    tor = X(8);
                }
                bob = 0;
            }

            // គេច (ក្រឡាប់)
            Quaternion modelRot = Quaternion.identity; Vector3 modelPos = Vector3.zero;
            if (rollT >= 0)
            {
                rollT += dt / rollDur;
                float t = Mathf.Clamp01(rollT);
                modelRot = Quaternion.Euler(360 * t, 0, 0);
                modelPos = new Vector3(0, 0.5f * Mathf.Sin(t * Mathf.PI) * 0.6f, 0);
                lL = lR = X(-80); kL = kR = X(120); aL = aR = X(-60);
                if (rollT >= 1) rollT = -1;
            }
            if (spinT >= 0)
            {
                spinT += dt / 0.6f;
                modelRot = Quaternion.Euler(0, 720 * Mathf.Clamp01(spinT), 0);
                aR = XYZ(-90, 0, 80); fR = X(0); aL = XYZ(-60, 0, -70);
                if (spinT >= 1) spinT = -1;
            }
            if (hurtT >= 0)
            {
                hurtT += dt / 0.3f;
                tor = tor * X(-18 * Mathf.Sin(Mathf.Clamp01(hurtT) * Mathf.PI));
                if (hurtT >= 1) hurtT = -1;
            }

            float k = 1 - Mathf.Exp(-dt * 18f);
            legL.localRotation = Quaternion.Slerp(legL.localRotation, lL, k);
            legR.localRotation = Quaternion.Slerp(legR.localRotation, lR, k);
            shinL.localRotation = Quaternion.Slerp(shinL.localRotation, kL, k);
            shinR.localRotation = Quaternion.Slerp(shinR.localRotation, kR, k);
            float ka = atkT >= 0 ? 1 - Mathf.Exp(-dt * 40f) : k;
            armL.localRotation = Quaternion.Slerp(armL.localRotation, aL, ka);
            armR.localRotation = Quaternion.Slerp(armR.localRotation, aR, ka);
            foreL.localRotation = Quaternion.Slerp(foreL.localRotation, fL, ka);
            foreR.localRotation = Quaternion.Slerp(foreR.localRotation, fR, ka);
            torso.localRotation = Quaternion.Slerp(torso.localRotation, tor, ka);
            head.localRotation = Quaternion.Slerp(head.localRotation, hd, k);
            hips.localPosition = new Vector3(0, 0.95f + bob, 0);
            model.localRotation = modelRot;
            model.localPosition = modelPos + Vector3.up * modelBaseY;
        }
    }

    public enum BeastKind { Boar, Tiger, Croc }

    /// <summary>រាងសត្វ ៤ ជើង និងចលនា</summary>
    public class BeastRig : MonoBehaviour
    {
        public Transform model, body, head, jaw, tail;
        public readonly List<Transform> legs = new List<Transform>();
        public float moveSpeed, refSpeed = 6f;
        public bool dead;
        float phase, atkT = -1, atkDur = 0.5f, deadT;
        BeastKind kind;

        public static BeastRig Build(Transform parent, BeastKind kind, float scale = 1f)
        {
            var root = new GameObject("Model"); root.transform.SetParent(parent, false); root.transform.localScale = Vector3.one * scale;
            var rig = parent.gameObject.AddComponent<BeastRig>(); rig.model = root.transform; rig.kind = kind;
            Color main, belly; Material skinMat = null;
            float len, h, w, legLen;
            switch (kind)
            {
                case BeastKind.Boar: main = new Color(0.25f, 0.18f, 0.13f); belly = new Color(0.35f, 0.27f, 0.2f); len = 1.3f; h = 0.6f; w = 0.55f; legLen = 0.4f; break;
                case BeastKind.Tiger:
                    main = new Color(0.85f, 0.45f, 0.12f); belly = new Color(0.95f, 0.9f, 0.8f); len = 1.7f; h = 0.75f; w = 0.55f; legLen = 0.6f;
                    skinMat = Mats.Textured("tiger", Tex.Stripes(main, new Color(0.08f, 0.06f, 0.05f), 9), new Vector2(2, 1));
                    break;
                default: main = new Color(0.22f, 0.3f, 0.16f); belly = new Color(0.55f, 0.55f, 0.35f); len = 2.6f; h = 0.18f; w = 0.65f; legLen = 0.25f;
                    skinMat = Mats.Textured("croc", Tex.Noise(main, main * 0.55f, 18, 3), new Vector2(3, 1));
                    break;
            }
            var bodyT = new GameObject("Body").transform; bodyT.SetParent(root.transform, false); bodyT.localPosition = new Vector3(0, legLen + h / 2, 0);
            rig.body = bodyT;
            var torsoG = skinMat != null
                ? Shapes.MakeM(PrimitiveType.Capsule, bodyT, Vector3.zero, new Vector3(w, len / 2, h), skinMat, false)
                : Shapes.Make(PrimitiveType.Capsule, bodyT, Vector3.zero, new Vector3(w, len / 2, h), main, false);
            torsoG.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Shapes.Make(PrimitiveType.Capsule, bodyT, new Vector3(0, -h * 0.2f, 0), new Vector3(w * 0.8f, len / 2.3f, h * 0.7f), belly, false).transform.localRotation = Quaternion.Euler(90, 0, 0);

            rig.head = new GameObject("Head").transform; rig.head.SetParent(bodyT, false); rig.head.localPosition = new Vector3(0, h * 0.15f, len / 2 + 0.05f);
            rig.jaw = new GameObject("Jaw").transform; rig.jaw.SetParent(rig.head, false);
            switch (kind)
            {
                case BeastKind.Boar:
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0, 0.1f), new Vector3(0.42f, 0.42f, 0.5f), main, false);
                    Shapes.Make(PrimitiveType.Cylinder, rig.head, new Vector3(0, -0.05f, 0.38f), new Vector3(0.2f, 0.08f, 0.2f), new Color(0.4f, 0.3f, 0.28f), false).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    foreach (var sx in new[] { -1, 1 })
                    {
                        var tusk = Shapes.Make(PrimitiveType.Capsule, rig.head, new Vector3(sx * 0.11f, -0.05f, 0.33f), new Vector3(0.04f, 0.1f, 0.04f), new Color(0.95f, 0.92f, 0.85f), false);
                        tusk.transform.localRotation = Quaternion.Euler(-40, 0, sx * 20);
                        Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(sx * 0.13f, 0.1f, 0.25f), Vector3.one * 0.05f, Color.black, false);
                        var ear = Shapes.Make(PrimitiveType.Cube, rig.head, new Vector3(sx * 0.15f, 0.2f, 0), new Vector3(0.1f, 0.14f, 0.03f), main, false);
                        ear.transform.localRotation = Quaternion.Euler(0, 0, sx * -25);
                    }
                    // សរសៃរោមខ្នង
                    Shapes.Make(PrimitiveType.Cube, bodyT, new Vector3(0, h * 0.5f, 0), new Vector3(0.08f, 0.12f, len * 0.7f), main * 0.6f, false);
                    break;
                case BeastKind.Tiger:
                    Shapes.MakeM(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.08f, 0.15f), new Vector3(0.48f, 0.42f, 0.45f), skinMat, false);
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, -0.02f, 0.36f), new Vector3(0.26f, 0.18f, 0.2f), belly, false);
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.04f, 0.46f), new Vector3(0.07f, 0.05f, 0.04f), new Color(0.3f, 0.15f, 0.15f), false);
                    foreach (var sx in new[] { -1, 1 })
                    {
                        Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(sx * 0.11f, 0.15f, 0.34f), new Vector3(0.07f, 0.05f, 0.04f), new Color(0.9f, 0.8f, 0.2f), false);
                        Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(sx * 0.17f, 0.32f, 0.08f), new Vector3(0.12f, 0.12f, 0.05f), main, false);
                    }
                    Shapes.Make(PrimitiveType.Sphere, rig.jaw, new Vector3(0, -0.12f, 0.3f), new Vector3(0.22f, 0.1f, 0.2f), belly, false);
                    break;
                default:
                    Shapes.MakeM(PrimitiveType.Cube, rig.head, new Vector3(0, 0.03f, 0.45f), new Vector3(0.38f, 0.1f, 0.9f), skinMat, false);
                    Shapes.MakeM(PrimitiveType.Cube, rig.jaw, new Vector3(0, -0.06f, 0.42f), new Vector3(0.34f, 0.07f, 0.85f), skinMat, false);
                    for (int i = 0; i < 5; i++)
                        foreach (var sx in new[] { -1, 1 })
                            Shapes.Make(PrimitiveType.Cube, rig.head, new Vector3(sx * 0.16f, -0.03f, 0.2f + i * 0.15f), new Vector3(0.02f, 0.05f, 0.02f), Color.white, false);
                    foreach (var sx in new[] { -1, 1 })
                        Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(sx * 0.12f, 0.12f, 0.1f), Vector3.one * 0.09f, new Color(0.8f, 0.7f, 0.2f), false);
                    // ស្រកានៅខ្នង
                    for (int i = 0; i < 7; i++)
                        Shapes.Make(PrimitiveType.Cube, bodyT, new Vector3(0, h * 0.55f, -len / 2 + 0.3f + i * 0.33f), new Vector3(0.1f, 0.08f, 0.12f), main * 0.6f, false);
                    break;
            }

            // ជើង
            float lx = w * 0.38f, lz = len * 0.32f;
            foreach (var p in new[] { new Vector3(-lx, 0, lz), new Vector3(lx, 0, lz), new Vector3(-lx, 0, -lz), new Vector3(lx, 0, -lz) })
            {
                var leg = new GameObject("Leg").transform; leg.SetParent(root.transform, false); leg.localPosition = p + Vector3.up * (legLen + h * 0.2f);
                if (kind == BeastKind.Croc) { leg.localPosition = new Vector3(p.x * 1.6f, legLen + 0.05f, p.z); }
                float thick = kind == BeastKind.Tiger ? 0.17f : kind == BeastKind.Boar ? 0.12f : 0.14f;
                Shapes.Make(PrimitiveType.Capsule, leg, new Vector3(0, -(legLen + h * 0.2f) / 2, 0), new Vector3(thick, (legLen + h * 0.2f) / 2, thick), kind == BeastKind.Tiger ? main : main * 0.85f, false);
                rig.legs.Add(leg);
            }
            // កន្ទុយ
            rig.tail = new GameObject("Tail").transform; rig.tail.SetParent(bodyT, false); rig.tail.localPosition = new Vector3(0, 0, -len / 2);
            float tl = kind == BeastKind.Croc ? 1.6f : kind == BeastKind.Tiger ? 0.9f : 0.25f;
            var tg = kind == BeastKind.Croc
                ? Shapes.MakeM(PrimitiveType.Cube, rig.tail, new Vector3(0, -0.02f, -tl / 2), new Vector3(0.35f, 0.14f, tl), skinMat, false)
                : (kind == BeastKind.Tiger ? Shapes.MakeM(PrimitiveType.Capsule, rig.tail, new Vector3(0, 0.15f, -tl / 2), new Vector3(0.08f, tl / 2, 0.08f), skinMat, false)
                                            : Shapes.Make(PrimitiveType.Capsule, rig.tail, new Vector3(0, 0, -tl / 2), new Vector3(0.05f, tl / 2, 0.05f), main, false));
            if (kind != BeastKind.Croc) tg.transform.localRotation = Quaternion.Euler(kind == BeastKind.Tiger ? 70 : 90, 0, 0);
            foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
            return rig;
        }

        public void Attack(float dur) { atkDur = dur; atkT = 0; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dead)
            {
                deadT = Mathf.Min(1, deadT + dt * 2f);
                model.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(0, 0, 90), deadT);
                return;
            }
            float s = Mathf.Clamp01(moveSpeed / refSpeed);
            phase += dt * (3f + 9f * s);
            float sw = Mathf.Sin(phase);
            float amp = (kind == BeastKind.Croc ? 25 : 40) * s;
            for (int i = 0; i < legs.Count; i++)
            {
                float a = ((i == 0 || i == 3) ? sw : -sw) * amp;
                legs[i].localRotation = kind == BeastKind.Croc ? Quaternion.Euler(0, a, 0) : Quaternion.Euler(a, 0, 0);
            }
            float bodyPitch = 0, headZ = 0, jawA = 0;
            if (atkT >= 0)
            {
                atkT += dt / atkDur;
                float t = Mathf.Clamp01(atkT);
                float lunge = t < 0.4f ? -t / 0.4f * 0.3f : t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f;
                headZ = lunge * 0.3f;
                bodyPitch = kind == BeastKind.Tiger ? -15 * Mathf.Max(0, lunge) : 6 * lunge;
                jawA = kind == BeastKind.Boar ? 0 : 35 * Mathf.Max(0, lunge);
                if (atkT >= 1) atkT = -1;
            }
            body.localRotation = Quaternion.Euler(bodyPitch + Mathf.Sin(phase * 2) * 2 * s, 0, 0);
            head.localRotation = Quaternion.Euler(-headZ * 40 + Mathf.Sin(Time.time * 1.5f) * 3, 0, 0);
            jaw.localRotation = Quaternion.Euler(jawA, 0, 0);
            tail.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * (kind == BeastKind.Croc ? 2f : 3f) + phase) * (kind == BeastKind.Croc ? 15 : 25), 0);
        }
    }
}
