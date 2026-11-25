// សត្វជំនិះកំប្លែងៗ៖ សេះ ដំរី ខ្លា ឆ្មាយក្ស ឆ្កែ ក្របី — មានភ្នែកធំៗ កន្ទុយគ្រវី និងសំឡេងកំប្លែង
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KhmerHeroes
{
    public enum MountKind { Horse, Elephant, Tiger, Cat, Dog, Buffalo }

    public class MountDef
    {
        public MountKind kind;
        public string name, sound, desc;
        public float speed, jump, ram, radius;
        public static readonly MountDef[] All =
        {
            new MountDef { kind = MountKind.Horse,    name = "សេះ",      sound = "neigh",   speed = 13f,   jump = 8f,  ram = 20, radius = 0.7f, desc = "លឿន ហើយលោតបានល្អ" },
            new MountDef { kind = MountKind.Elephant, name = "ដំរី",     sound = "trumpet", speed = 9.5f,  jump = 5f,  ram = 45, radius = 1.3f, desc = "យឺត តែបុកសត្រូវហោះឆ្ងាយ" },
            new MountDef { kind = MountKind.Tiger,    name = "ខ្លាស្លូត", sound = "roar",    speed = 12f,   jump = 10f, ram = 25, radius = 0.7f, desc = "លោតខ្ពស់ ហើយបុកខ្លាំង" },
            new MountDef { kind = MountKind.Cat,      name = "ឆ្មាយក្ស", sound = "meow",    speed = 11f,   jump = 13f, ram = 14, radius = 0.75f, desc = "លោតខ្ពស់ជាងគេ មៀវៗ" },
            new MountDef { kind = MountKind.Dog,      name = "ឆ្កែ",     sound = "bark",    speed = 13.5f, jump = 8f,  ram = 12, radius = 0.65f, desc = "លឿនជាងគេ កន្ទុយគ្រវីមិនឈប់" },
            new MountDef { kind = MountKind.Buffalo,  name = "ក្របី",    sound = "moo",     speed = 10.5f, jump = 6f,  ram = 35, radius = 0.95f, desc = "មាំ ស្នែងធំ បុកខ្លាំង" },
        };
        public static MountDef Of(MountKind k) { foreach (var d in All) if (d.kind == k) return d; return All[0]; }
    }

    /// <summary>រាងសត្វជំនិះ និងចលនា</summary>
    public class MountRig : MonoBehaviour
    {
        public Transform model, body, head, tail, earL, earR, trunk;
        public readonly List<Transform> legs = new List<Transform>();
        public float seatY, length, moveSpeed, refSpeed = 10f;
        public MountKind kind;
        float phase, happy;

        static void Eyes(Transform head, float x, float y, float z, float size)
        {
            // ភ្នែកធំៗកំប្លែង (ស + ប្រស្រីខ្មៅ + ចំណុចភ្លឺ)
            foreach (var sx in new[] { -1, 1 })
            {
                Shapes.Make(PrimitiveType.Sphere, head, new Vector3(sx * x, y, z), Vector3.one * size, Color.white, false);
                Shapes.Make(PrimitiveType.Sphere, head, new Vector3(sx * x, y, z + size * 0.35f), Vector3.one * size * 0.55f, new Color(0.05f, 0.05f, 0.05f), false);
                Shapes.Make(PrimitiveType.Sphere, head, new Vector3(sx * x + size * 0.1f, y + size * 0.15f, z + size * 0.5f), Vector3.one * size * 0.18f, Color.white, false);
            }
        }

        static Transform Piv(Transform p, string n, Vector3 pos) { var t = new GameObject(n).transform; t.SetParent(p, false); t.localPosition = pos; return t; }

        public static MountRig Build(Transform parent, MountKind kind)
        {
            var root = Piv(parent, "MountModel", Vector3.zero);
            var rig = parent.gameObject.AddComponent<MountRig>();
            rig.model = root; rig.kind = kind;
            Color main, belly, dark; Material skin = null;
            float legLen, legT, len, h, w;
            switch (kind)
            {
                case MountKind.Horse: main = new Color(0.55f, 0.33f, 0.18f); belly = new Color(0.65f, 0.45f, 0.28f); dark = new Color(0.15f, 0.1f, 0.08f); legLen = 1.0f; legT = 0.2f; len = 1.9f; h = 0.8f; w = 0.65f; break;
                case MountKind.Elephant: main = new Color(0.52f, 0.52f, 0.55f); belly = new Color(0.6f, 0.6f, 0.62f); dark = new Color(0.4f, 0.4f, 0.43f); legLen = 1.25f; legT = 0.6f; len = 2.8f; h = 1.7f; w = 1.8f; break;
                case MountKind.Tiger: main = new Color(0.9f, 0.5f, 0.15f); belly = new Color(0.97f, 0.93f, 0.85f); dark = new Color(0.08f, 0.06f, 0.05f); legLen = 0.75f; legT = 0.25f; len = 1.9f; h = 0.8f; w = 0.7f;
                    skin = Mats.Textured("tigerM", Tex.Stripes(main, dark, 9), new Vector2(2, 1)); break;
                case MountKind.Cat: main = new Color(0.95f, 0.62f, 0.25f); belly = new Color(1f, 0.97f, 0.92f); dark = new Color(0.8f, 0.42f, 0.12f); legLen = 0.7f; legT = 0.28f; len = 1.7f; h = 0.9f; w = 0.8f;
                    skin = Mats.Textured("catM", Tex.Stripes(main, dark, 14), new Vector2(2, 1)); break;
                case MountKind.Dog: main = new Color(0.82f, 0.62f, 0.38f); belly = new Color(0.98f, 0.95f, 0.9f); dark = new Color(0.45f, 0.3f, 0.18f); legLen = 0.75f; legT = 0.24f; len = 1.6f; h = 0.75f; w = 0.62f; break;
                default: main = new Color(0.28f, 0.28f, 0.3f); belly = new Color(0.36f, 0.36f, 0.38f); dark = new Color(0.12f, 0.12f, 0.13f); legLen = 0.85f; legT = 0.32f; len = 2.2f; h = 1.05f; w = 1.0f; break;
            }
            rig.length = len;
            rig.body = Piv(root, "Body", new Vector3(0, legLen + h / 2, 0));
            var bodyG = skin != null ? Shapes.MakeM(PrimitiveType.Capsule, rig.body, Vector3.zero, new Vector3(w, len / 2, h), skin, false)
                                     : Shapes.Make(PrimitiveType.Capsule, rig.body, Vector3.zero, new Vector3(w, len / 2, h), main, false);
            bodyG.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Shapes.Make(PrimitiveType.Capsule, rig.body, new Vector3(0, -h * 0.18f, 0), new Vector3(w * 0.85f, len / 2.3f, h * 0.72f), belly, false).transform.localRotation = Quaternion.Euler(90, 0, 0);

            // កែបបែបខ្មែរ៖ ក្រណាត់ក្រហមគែមមាស + ក្រមា
            var red = new Color(0.65f, 0.08f, 0.1f); var gold = new Color(0.93f, 0.74f, 0.3f);
            float top = h / 2;
            if (kind == MountKind.Elephant)
            {
                // ហោដា (កៀបដំរី) តុបតែងមាស
                Shapes.Make(PrimitiveType.Cube, rig.body, new Vector3(0, top - 0.05f, 0), new Vector3(w * 1.05f, 0.08f, 1.8f), red, false);
                Shapes.Make(PrimitiveType.Cube, rig.body, new Vector3(0, top - 0.5f, 0), new Vector3(w * 1.08f, 0.9f, 1.4f), red, false);
                Shapes.Make(PrimitiveType.Cube, rig.body, new Vector3(0, top - 0.05f, 0), new Vector3(w * 1.1f, 0.12f, 1.85f), gold, false);
                var seat = Shapes.Make(PrimitiveType.Cube, rig.body, new Vector3(0, top + 0.25f, -0.1f), new Vector3(1.1f, 0.4f, 1.1f), gold, false);
                foreach (var c in new[] { new Vector3(-0.5f, 0, -0.6f), new Vector3(0.5f, 0, -0.6f), new Vector3(-0.5f, 0, 0.4f), new Vector3(0.5f, 0, 0.4f) })
                    Shapes.Make(PrimitiveType.Cylinder, rig.body, c + new Vector3(0, top + 0.8f, 0), new Vector3(0.06f, 0.4f, 0.06f), gold, false);
                Shapes.Make(PrimitiveType.Cube, rig.body, new Vector3(0, top + 1.25f, -0.1f), new Vector3(1.3f, 0.1f, 1.3f), red, false);
                rig.seatY = legLen + h + 0.35f;
            }
            else
            {
                var saddle = Shapes.Make(PrimitiveType.Cube, rig.body, new Vector3(0, top - 0.02f, -0.05f), new Vector3(w * 1.08f, 0.1f, 0.75f), red, false);
                Shapes.Make(PrimitiveType.Cube, rig.body, new Vector3(0, top + 0.01f, -0.05f), new Vector3(w * 1.12f, 0.05f, 0.8f), gold, false);
                Shapes.Make(PrimitiveType.Cube, rig.body, new Vector3(0, top - h * 0.45f, -0.05f), new Vector3(w * 1.12f, h * 0.9f, 0.08f), red * 0.8f, false);
                rig.seatY = legLen + h * 0.95f + 0.05f;
            }

            // ក្បាល
            rig.head = Piv(rig.body, "Head", new Vector3(0, h * 0.35f, len / 2 + 0.05f));
            switch (kind)
            {
                case MountKind.Horse:
                {
                    var neck = Shapes.Make(PrimitiveType.Capsule, rig.head, new Vector3(0, 0.35f, 0.05f), new Vector3(0.38f, 0.5f, 0.38f), main, false);
                    neck.transform.localRotation = Quaternion.Euler(35, 0, 0);
                    var face = Shapes.Make(PrimitiveType.Capsule, rig.head, new Vector3(0, 0.75f, 0.45f), new Vector3(0.32f, 0.38f, 0.34f), main, false);
                    face.transform.localRotation = Quaternion.Euler(70, 0, 0);
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.62f, 0.78f), new Vector3(0.3f, 0.25f, 0.25f), belly, false);
                    Eyes(rig.head, 0.14f, 0.88f, 0.45f, 0.16f);
                    // រោមក (mane)
                    for (int i = 0; i < 6; i++) Shapes.Make(PrimitiveType.Cube, rig.head, new Vector3(0, 0.15f + i * 0.14f, -0.12f + i * 0.08f), new Vector3(0.08f, 0.22f, 0.12f), dark, false);
                    rig.earL = Piv(rig.head, "EarL", new Vector3(-0.1f, 1.02f, 0.38f)); rig.earR = Piv(rig.head, "EarR", new Vector3(0.1f, 1.02f, 0.38f));
                    foreach (var e in new[] { rig.earL, rig.earR }) Shapes.Make(PrimitiveType.Cube, e, new Vector3(0, 0.08f, 0), new Vector3(0.07f, 0.18f, 0.05f), main, false);
                    break;
                }
                case MountKind.Elephant:
                {
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.1f, 0.25f), new Vector3(1.3f, 1.25f, 1.1f), main, false);
                    Eyes(rig.head, 0.35f, 0.3f, 0.72f, 0.22f);
                    rig.earL = Piv(rig.head, "EarL", new Vector3(-0.55f, 0.15f, 0.1f)); rig.earR = Piv(rig.head, "EarR", new Vector3(0.55f, 0.15f, 0.1f));
                    foreach (var (e, sx) in new[] { (rig.earL, -1f), (rig.earR, 1f) })
                    {
                        var ear = Shapes.Make(PrimitiveType.Cylinder, e, new Vector3(sx * 0.45f, 0, -0.1f), new Vector3(1.1f, 0.04f, 1.2f), dark, false);
                        ear.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    }
                    rig.trunk = Piv(rig.head, "Trunk", new Vector3(0, -0.15f, 0.75f));
                    var t = rig.trunk;
                    for (int i = 0; i < 6; i++)
                    {
                        var seg = Piv(t, "Seg", new Vector3(0, -0.22f, 0.02f));
                        Shapes.Make(PrimitiveType.Sphere, seg, Vector3.zero, Vector3.one * (0.32f - i * 0.03f), main, false);
                        t = seg;
                    }
                    foreach (var sx in new[] { -1, 1 })
                    {
                        var tusk = Shapes.Make(PrimitiveType.Capsule, rig.head, new Vector3(sx * 0.28f, -0.35f, 0.7f), new Vector3(0.09f, 0.3f, 0.09f), new Color(0.97f, 0.95f, 0.88f), false);
                        tusk.transform.localRotation = Quaternion.Euler(-60, 0, 0);
                    }
                    // គ្រឿងលម្អក្បាលដំរី (មាស)
                    Shapes.Make(PrimitiveType.Cube, rig.head, new Vector3(0, 0.62f, 0.55f), new Vector3(0.7f, 0.25f, 0.3f), gold, false);
                    break;
                }
                case MountKind.Tiger:
                {
                    Shapes.MakeM(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.12f, 0.2f), new Vector3(0.6f, 0.55f, 0.55f), skin, false);
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.0f, 0.45f), new Vector3(0.34f, 0.24f, 0.24f), belly, false);
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.08f, 0.56f), new Vector3(0.09f, 0.06f, 0.05f), new Color(0.9f, 0.45f, 0.5f), false);
                    Eyes(rig.head, 0.13f, 0.22f, 0.4f, 0.15f);
                    rig.earL = Piv(rig.head, "EarL", new Vector3(-0.2f, 0.42f, 0.12f)); rig.earR = Piv(rig.head, "EarR", new Vector3(0.2f, 0.42f, 0.12f));
                    foreach (var e in new[] { rig.earL, rig.earR }) Shapes.Make(PrimitiveType.Sphere, e, Vector3.zero, new Vector3(0.16f, 0.16f, 0.06f), main, false);
                    // ស្នាមញញឹម
                    Shapes.Make(PrimitiveType.Cube, rig.head, new Vector3(0, -0.07f, 0.56f), new Vector3(0.14f, 0.025f, 0.02f), dark, false);
                    break;
                }
                case MountKind.Cat:
                {
                    Shapes.MakeM(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.25f, 0.2f), new Vector3(0.85f, 0.75f, 0.7f), skin, false);
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.08f, 0.5f), new Vector3(0.38f, 0.26f, 0.22f), belly, false);
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.15f, 0.6f), new Vector3(0.08f, 0.06f, 0.05f), new Color(1f, 0.5f, 0.6f), false);
                    Eyes(rig.head, 0.17f, 0.36f, 0.47f, 0.22f);
                    rig.earL = Piv(rig.head, "EarL", new Vector3(-0.25f, 0.6f, 0.15f)); rig.earR = Piv(rig.head, "EarR", new Vector3(0.25f, 0.6f, 0.15f));
                    foreach (var e in new[] { rig.earL, rig.earR })
                    {
                        var ear = Shapes.Make(PrimitiveType.Cube, e, new Vector3(0, 0.08f, 0), new Vector3(0.2f, 0.2f, 0.06f), main, false);
                        ear.transform.localRotation = Quaternion.Euler(0, 0, 45);
                    }
                    // ពុកមាត់
                    foreach (var sx in new[] { -1, 1 })
                        for (int i = 0; i < 3; i++)
                        {
                            var wh = Shapes.Make(PrimitiveType.Cube, rig.head, new Vector3(sx * 0.3f, 0.08f + i * 0.05f, 0.55f), new Vector3(0.4f, 0.012f, 0.012f), Color.white, false);
                            wh.transform.localRotation = Quaternion.Euler(0, 0, sx * (i - 1) * 12);
                        }
                    break;
                }
                case MountKind.Dog:
                {
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.25f, 0.15f), new Vector3(0.55f, 0.52f, 0.52f), main, false);
                    Shapes.Make(PrimitiveType.Capsule, rig.head, new Vector3(0, 0.15f, 0.45f), new Vector3(0.28f, 0.2f, 0.28f), belly, false).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, 0.22f, 0.66f), new Vector3(0.12f, 0.09f, 0.08f), new Color(0.08f, 0.06f, 0.05f), false);
                    // អណ្ដាតលៀន
                    var tongue = Shapes.Make(PrimitiveType.Cube, rig.head, new Vector3(0.05f, 0.0f, 0.55f), new Vector3(0.09f, 0.2f, 0.03f), new Color(0.95f, 0.45f, 0.5f), false);
                    tongue.transform.localRotation = Quaternion.Euler(20, 0, 10);
                    Eyes(rig.head, 0.13f, 0.36f, 0.36f, 0.16f);
                    rig.earL = Piv(rig.head, "EarL", new Vector3(-0.25f, 0.4f, 0.1f)); rig.earR = Piv(rig.head, "EarR", new Vector3(0.25f, 0.4f, 0.1f));
                    foreach (var (e, sx) in new[] { (rig.earL, -1f), (rig.earR, 1f) })
                        Shapes.Make(PrimitiveType.Cube, e, new Vector3(sx * 0.06f, -0.15f, 0), new Vector3(0.1f, 0.35f, 0.2f), dark, false);
                    break;
                }
                default:
                {
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, -0.05f, 0.25f), new Vector3(0.7f, 0.65f, 0.75f), main, false);
                    Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0, -0.2f, 0.6f), new Vector3(0.45f, 0.32f, 0.3f), new Color(0.2f, 0.2f, 0.22f), false);
                    Eyes(rig.head, 0.2f, 0.12f, 0.52f, 0.17f);
                    // ស្នែងកោងធំ
                    foreach (var sx in new[] { -1, 1 })
                    {
                        var hp = Piv(rig.head, "Horn", new Vector3(sx * 0.3f, 0.25f, 0.2f));
                        Transform cur = hp;
                        for (int i = 0; i < 5; i++)
                        {
                            var seg = Piv(cur, "Seg", new Vector3(sx * 0.17f, 0.02f, 0));
                            seg.localRotation = Quaternion.Euler(0, sx * 10, sx * 28);
                            Shapes.Make(PrimitiveType.Sphere, seg, Vector3.zero, new Vector3(0.22f, 0.14f - i * 0.015f, 0.14f - i * 0.015f), new Color(0.85f, 0.8f, 0.7f), false);
                            cur = seg;
                        }
                    }
                    rig.earL = Piv(rig.head, "EarL", new Vector3(-0.4f, 0.05f, 0.1f)); rig.earR = Piv(rig.head, "EarR", new Vector3(0.4f, 0.05f, 0.1f));
                    foreach (var e in new[] { rig.earL, rig.earR }) Shapes.Make(PrimitiveType.Sphere, e, Vector3.zero, new Vector3(0.22f, 0.08f, 0.12f), main, false);
                    break;
                }
            }

            // ជើង
            float lx = w * 0.32f, lz = len * 0.33f;
            foreach (var p in new[] { new Vector3(-lx, 0, lz), new Vector3(lx, 0, lz), new Vector3(-lx, 0, -lz), new Vector3(lx, 0, -lz) })
            {
                var leg = Piv(root, "Leg", p + Vector3.up * (legLen + h * 0.2f));
                float L = legLen + h * 0.2f;
                Shapes.Make(PrimitiveType.Capsule, leg, new Vector3(0, -L / 2, 0), new Vector3(legT, L / 2, legT), kind == MountKind.Tiger || kind == MountKind.Cat ? main : main * 0.92f, false);
                Shapes.Make(PrimitiveType.Sphere, leg, new Vector3(0, -L + 0.05f, 0.04f), new Vector3(legT * 1.15f, legT * 0.6f, legT * 1.3f), kind == MountKind.Cat || kind == MountKind.Dog ? belly : dark, false);
                rig.legs.Add(leg);
            }

            // កន្ទុយ
            rig.tail = Piv(rig.body, "Tail", new Vector3(0, h * 0.2f, -len / 2));
            switch (kind)
            {
                case MountKind.Cat:
                    for (int i = 0; i < 5; i++) Shapes.MakeM(PrimitiveType.Sphere, rig.tail, new Vector3(0, 0.15f + i * 0.2f, -0.12f - Mathf.Sin(i * 0.5f) * 0.15f), Vector3.one * 0.2f, skin, false);
                    break;
                case MountKind.Tiger:
                    Shapes.MakeM(PrimitiveType.Capsule, rig.tail, new Vector3(0, 0.2f, -0.45f), new Vector3(0.1f, 0.5f, 0.1f), skin, false).transform.localRotation = Quaternion.Euler(-60, 0, 0);
                    break;
                case MountKind.Elephant:
                    Shapes.Make(PrimitiveType.Capsule, rig.tail, new Vector3(0, -0.35f, -0.05f), new Vector3(0.08f, 0.35f, 0.08f), dark, false);
                    break;
                case MountKind.Dog:
                    Shapes.Make(PrimitiveType.Capsule, rig.tail, new Vector3(0, 0.2f, -0.15f), new Vector3(0.1f, 0.25f, 0.1f), main, false).transform.localRotation = Quaternion.Euler(-40, 0, 0);
                    break;
                default:
                    var tl = Shapes.Make(PrimitiveType.Capsule, rig.tail, new Vector3(0, -0.25f, -0.15f), new Vector3(0.12f, 0.35f, 0.12f), dark, false);
                    tl.transform.localRotation = Quaternion.Euler(25, 0, 0);
                    break;
            }
            foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
            return rig;
        }

        public void Happy() { happy = 1f; }

        void Update()
        {
            float dt = Time.deltaTime;
            float s = Mathf.Clamp01(moveSpeed / refSpeed);
            phase += dt * (2f + 11f * s);
            float sw = Mathf.Sin(phase);
            float amp = (kind == MountKind.Elephant ? 22 : 45) * s;
            for (int i = 0; i < legs.Count; i++)
            {
                // ជំហានរត់ (gallop)
                float off = i < 2 ? 0 : (kind == MountKind.Elephant ? Mathf.PI : 0.9f);
                float a = Mathf.Sin(phase + off + (i % 2) * 0.35f) * amp;
                legs[i].localRotation = Quaternion.Euler(a, 0, 0);
            }
            happy = Mathf.MoveTowards(happy, 0, dt);
            float bounce = Mathf.Abs(Mathf.Sin(phase)) * 0.12f * s * (kind == MountKind.Elephant ? 0.5f : 1f);
            body.localPosition = new Vector3(body.localPosition.x, body.localPosition.y, 0);
            model.localPosition = new Vector3(0, bounce + happy * Mathf.Abs(Mathf.Sin(Time.time * 18)) * 0.25f, 0);
            body.localRotation = Quaternion.Euler(Mathf.Cos(phase) * 4 * s, 0, 0);
            head.localRotation = Quaternion.Euler(Mathf.Sin(phase) * 6 * s + Mathf.Sin(Time.time * 1.3f) * 4, Mathf.Sin(Time.time * 0.7f) * 8 * (1 - s), 0);
            float wag = kind == MountKind.Dog ? 55 : kind == MountKind.Cat ? 15 : 25;
            float wagSpeed = kind == MountKind.Dog ? 22 : 4;
            tail.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * wagSpeed) * wag, 0);
            if (earL)
            {
                float flap = kind == MountKind.Elephant ? Mathf.Sin(Time.time * 3) * 25 : Mathf.Sin(Time.time * 5) * 6;
                earL.localRotation = Quaternion.Euler(0, flap, 0);
                earR.localRotation = Quaternion.Euler(0, -flap, 0);
            }
            if (trunk) trunk.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 2) * 15 - 40 * s - happy * 60, Mathf.Sin(Time.time * 1.3f) * 15, 0);
        }
    }

    /// <summary>សត្វជំនិះដែលឈរក្នុងពិភព — ចុច F ដើម្បីជិះ</summary>
    public class Mount : Interactable
    {
        public MountDef def;
        public MountRig rig;
        public bool ridden;
        float soundT;
        Collider col;

        public static Mount Spawn(MountKind kind, Vector3 pos, float yaw)
        {
            var g = new GameObject("Mount_" + kind);
            g.transform.position = World.Ground(pos);
            g.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var m = g.AddComponent<Mount>();
            m.def = MountDef.Of(kind);
            m.rig = MountRig.Build(g.transform, kind);
            var c = g.AddComponent<CapsuleCollider>();
            c.direction = 2; c.radius = m.def.radius; c.height = m.rig.length + 1f; c.center = new Vector3(0, m.rig.seatY * 0.55f, 0);
            m.col = c;
            m.soundT = Random.Range(4f, 10f);
            return m;
        }

        public override string Prompt => "ជិះ" + def.name + " (" + def.desc + ")";
        public override bool Usable => !ridden;
        public override void Use(Player p) => p.Ride(this);

        public void SetRidden(bool on)
        {
            ridden = on;
            col.enabled = !on;
            rig.moveSpeed = 0;
        }

        public void Voice(float vol = 0.8f) { Sfx.I?.Play(def.sound, vol); rig.Happy(); }

        void Update()
        {
            if (ridden) return;
            soundT -= Time.deltaTime;
            if (soundT <= 0)
            {
                soundT = Random.Range(7f, 14f);
                var pl = Player.I;
                if (pl && Vector3.Distance(pl.transform.position, transform.position) < 14) Voice(0.5f);
            }
        }
    }
}
