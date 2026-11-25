// ការប្រណាំងសត្វជំនិះកំប្លែងៗនៅក្នុងភូមិ — ២ ជុំ មានសំបកចេក ភក់ និងអំពៅជួយឲ្យលឿន
using System.Collections.Generic;
using UnityEngine;

namespace KhmerHeroes
{
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager I;
        public enum St { Idle, Countdown, Running, Done }
        public St state = St.Idle;
        public const int Laps = 2;
        public Vector3[] cps;
        readonly List<Transform> beacons = new List<Transform>();
        public readonly List<Racer> racers = new List<Racer>();
        readonly List<Hazard> hazards = new List<Hazard>();
        int pNext, pLap; float startTime, countT, pFinishTime = -1, resultT;
        readonly List<object> finishOrder = new List<object>();
        public static readonly Vector3[] Grid = { new Vector3(-4.5f, 0, 13), new Vector3(-1.5f, 0, 13), new Vector3(1.5f, 0, 13), new Vector3(4.5f, 0, 13) };

        public static RaceManager Build()
        {
            var g = new GameObject("Race");
            var r = g.AddComponent<RaceManager>();
            I = r;
            r.cps = new[]
            {
                P(World.PathX(40), 40), P(World.PathX(75), 75), P(World.PathX(106), 106), P(11, 114),
                P(13, 90), P(13, 70), P(13, 46), P(13, 22), P(7, 8), P(0, 20),
            };
            for (int i = 0; i < r.cps.Length; i++) r.Arch(i);
            // ឧបសគ្គ និងជំនួយ
            foreach (var p in new[] { P(1.5f, 55), P(-1f, 90), P(12.5f, 100), P(13.5f, 58), P(9f, 15) }) r.hazards.Add(Hazard.Build(Hazard.Kind.Banana, p));
            foreach (var p in new[] { P(1.5f, 30), P(13f, 34), P(6f, 112) }) r.hazards.Add(Hazard.Build(Hazard.Kind.Mud, p));
            foreach (var p in new[] { P(-1.5f, 68), P(13.5f, 82), P(1f, 98) }) r.hazards.Add(Hazard.Build(Hazard.Kind.Sugarcane, p));
            // គូប្រជែង
            r.racers.Add(Racer.Build("ស្រីល័ក្ខ", MountKind.Cat, 12.2f, true, 0.95f, P(-9, 12), new[] { "ឆ្មារបស់ស្រីល័ក្ខ ឈប់លិតក្រញាំសិន!", "ឆ្មាឃើញត្រី! ស្រីល័ក្ខស្រែក «កុំ!»", "ឆ្មាងងុយដេក... ហា៎ម!" }));
            r.racers.Add(Racer.Build("ពូធំ", MountKind.Elephant, 11.2f, false, 1.1f, P(-12, 17), new[] { "ដំរីរបស់ពូធំ ឈប់ញ៉ាំចេកមួយស្និត!", "ដំរីបាញ់ទឹកពីប្រមោយ! ពូធំទទឹកជោក!", "ដំរីរាំ! ពូធំកាន់ស្ទើរមិនជាប់!" }));
            r.racers.Add(Racer.Build("អាតូច", MountKind.Dog, 12.8f, false, 0.82f, P(-9, 22), new[] { "ឆ្កែរបស់អាតូច រត់ដេញមេអំបៅ!", "ឆ្កែឈប់ធុំក្លិនឆ្អឹង!", "ឆ្កែដេញកន្ទុយខ្លួនឯង វិលៗ!" }));
            return r;
        }

        static Vector3 P(float x, float z) => new Vector3(x, 0, z);

        void Arch(int i)
        {
            var c = cps[i];
            var prev = cps[(i - 1 + cps.Length) % cps.Length]; var next = cps[(i + 1) % cps.Length];
            var dir = (next - prev); dir.y = 0; dir.Normalize();
            var perp = new Vector3(dir.z, 0, -dir.x);
            bool finish = i == cps.Length - 1;
            var g = new GameObject("Arch_" + i);
            g.transform.position = World.Ground(c);
            g.transform.rotation = Quaternion.LookRotation(dir);
            var red = new Color(0.7f, 0.1f, 0.1f); var gold = new Color(0.93f, 0.74f, 0.3f);
            float half = finish ? 6f : 4.5f;
            foreach (var sx in new[] { -1, 1 })
            {
                var p = c + perp * sx * half;
                float gy = World.Height(p);
                var post = Shapes.Make(PrimitiveType.Cylinder, null, new Vector3(p.x, gy + 2.2f, p.z), new Vector3(0.3f, 2.2f, 0.3f), i % 2 == 0 ? red : gold);
                post.transform.SetParent(g.transform, true);
                var flag = Shapes.Make(PrimitiveType.Cube, post.transform, new Vector3(0, 1.05f, 0), new Vector3(3f, 0.25f, 0.05f), finish ? Color.white : (i % 2 == 0 ? gold : red), false);
                flag.transform.localPosition = new Vector3(0, 1.1f, 0);
            }
            var bannerMat = finish ? Mats.Textured("finish", Tex.Krama(Color.black, Color.white), new Vector2(6, 1)) : Mats.Get(i % 2 == 0 ? red : gold);
            var banner = Shapes.MakeM(PrimitiveType.Cube, g.transform, new Vector3(0, 4.3f, 0), new Vector3(half * 2 + 0.4f, finish ? 1f : 0.5f, 0.12f), bannerMat, false);
            var b = Shapes.Glow(PrimitiveType.Sphere, g.transform, new Vector3(0, 5.6f, 0), Vector3.one * 0.9f, gold, 3f);
            b.SetActive(false);
            beacons.Add(b.transform);
        }

        public bool Racing => state == St.Countdown || state == St.Running;

        /// <summary>ពូចាន់ចាប់ផ្ដើមការប្រណាំង</summary>
        public void Begin()
        {
            var pl = Player.I;
            if (Racing) return;
            if (!pl.Riding) { UI.I.Dialogue(new[] { new Line("ពូចាន់", "ក្មួយត្រូវជិះសត្វជំនិះសិន! ទៅរើសនៅក្រោលសត្វ ខាងក្រោយពូនេះ — សេះ ដំរី ខ្លា ឆ្មា ឆ្កែ ក្របី មានគ្រប់!") }, null); return; }
            pl.Teleport(World.Ground(Grid[3]) + Vector3.up * 0.2f);
            pl.transform.rotation = Quaternion.identity;
            CamRig.I.yaw = 0;
            for (int i = 0; i < racers.Count; i++) racers[i].ToGrid(World.Ground(Grid[i]));
            pNext = 0; pLap = 0; pFinishTime = -1; finishOrder.Clear();
            state = St.Countdown; countT = 3.99f;
            pl.frozen = true;
            Sfx.I.Music("race");
        }

        public void Cancel(string why)
        {
            if (!Racing) return;
            state = St.Idle;
            Player.I.frozen = false;
            foreach (var r in racers) r.GoHome();
            foreach (var b in beacons) b.gameObject.SetActive(false);
            UI.I.RaceHud("");
            UI.I.Toast(why, 3f);
        }

        float Progress(int lap, int next, Vector3 pos)
        {
            var target = cps[next];
            var from = cps[(next - 1 + cps.Length) % cps.Length];
            float seg = Mathf.Max(1, Vector3.Distance(Flat(from), Flat(target)));
            float d = Vector3.Distance(Flat(pos), Flat(target));
            return lap * cps.Length + next + Mathf.Clamp01(1 - d / seg);
        }
        public float PlayerProgress => Progress(pLap, pNext, Player.I.transform.position);
        public float RacerProgress(Racer r) => r.finished ? 1000 - r.finishRank : Progress(r.lap, r.next, r.transform.position);
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void Update()
        {
            var pl = Player.I;
            if (pl == null) return;
            // ឧបសគ្គ ដំណើរការជានិច្ច (ទោះមិនប្រណាំងក៏ដោយ)
            if (Game.I.Playing)
                foreach (var h in hazards) h.Check(pl, racers);

            if (!Racing && state != St.Done) { UI.I.RaceHud(""); return; }
            if (!Game.I.Playing) return;

            if (state == St.Countdown)
            {
                int before = Mathf.CeilToInt(countT);
                countT -= Time.deltaTime;
                int now = Mathf.CeilToInt(countT);
                if (now != before || before == 4)
                {
                    if (now >= 1 && now <= 3) { UI.I.Big(Kh.Num(now), 0.9f); Sfx.I.Play("count"); }
                }
                if (countT <= 0)
                {
                    state = St.Running; startTime = Time.time; pl.frozen = false;
                    UI.I.Big("ទៅ!", 1.2f, new Color(0.5f, 1f, 0.5f));
                    Sfx.I.Play("go");
                    foreach (var r in racers) r.Go();
                }
                UI.I.RaceHud("ត្រៀមខ្លួន...");
                return;
            }

            if (state == St.Done)
            {
                resultT -= Time.deltaTime;
                if (resultT <= 0) { state = St.Idle; ShowResults(); }
                return;
            }

            // ពិនិត្យការឆ្លងទ្វារទង់របស់អ្នកលេង
            if (pFinishTime < 0)
            {
                if (Vector3.Distance(Flat(pl.transform.position), Flat(cps[pNext])) < 6.5f)
                {
                    pNext++;
                    Sfx.I.Play("pick", 0.5f);
                    if (pNext >= cps.Length)
                    {
                        pNext = 0; pLap++;
                        if (pLap >= Laps) { pFinishTime = Time.time - startTime; finishOrder.Add(pl); Sfx.I.Play("cheer"); UI.I.Big("បញ្ចប់!", 2f); state = St.Done; resultT = 2f; }
                        else { UI.I.Big("ជុំចុងក្រោយ!", 1.5f, new Color(1f, 0.7f, 0.4f)); Sfx.I.Play("good"); }
                    }
                }
            }
            for (int i = 0; i < beacons.Count; i++) beacons[i].gameObject.SetActive(state == St.Running && i == pNext);
            foreach (var r in racers) if (r.finished && !finishOrder.Contains(r)) finishOrder.Add(r);

            if (pl.transform.position.z > 123 || !pl.AnyAlive) { Cancel("ការប្រណាំងត្រូវបានលុបចោល — ក្មួយចេញក្រៅផ្លូវប្រណាំង!"); return; }

            // ចំណាត់ថ្នាក់
            float pp = PlayerProgress; int rank = 1;
            foreach (var r in racers) if (RacerProgress(r) > pp) rank++;
            float t = Time.time - startTime; int sec = (int)t;
            UI.I.RaceHud("ជុំ " + Kh.Num(Mathf.Min(pLap + 1, Laps)) + "/" + Kh.Num(Laps) + "   ·   ចំណាត់ថ្នាក់ ទី" + Kh.Num(rank) + "/" + Kh.Num(racers.Count + 1)
                         + "   ·   " + Kh.Num(sec / 60) + ":" + Kh.Num(sec % 60 / 10) + Kh.Num(sec % 10));
        }

        public int RankOf(Racer r) { int i = finishOrder.IndexOf(r); return i; }
        public void RacerFinished(Racer r) { if (!finishOrder.Contains(r)) finishOrder.Add(r); r.finishRank = finishOrder.Count; }

        void ShowResults()
        {
            var pl = Player.I;
            pl.frozen = false;
            foreach (var b in beacons) b.gameObject.SetActive(false);
            // អ្នកមិនទាន់ដល់ទី តម្រៀបតាមវឌ្ឍនភាព
            var rest = new List<Racer>();
            foreach (var r in racers) if (!finishOrder.Contains(r)) rest.Add(r);
            rest.Sort((a, b) => RacerProgress(b).CompareTo(RacerProgress(a)));
            foreach (var r in rest) finishOrder.Add(r);
            var rows = new List<string>(); int myRank = 1;
            for (int i = 0; i < finishOrder.Count; i++)
            {
                var o = finishOrder[i];
                if (o is Player) { myRank = i + 1; rows.Add(Kh.Num(i + 1) + ".  " + pl.Def.name + " ជិះ" + pl.mount?.def.name + "  (អ្នក)"); }
                else { var r = (Racer)o; rows.Add(Kh.Num(i + 1) + ".  " + r.riderName + " ជិះ" + MountDef.Of(r.kind).name); }
            }
            int[] prize = { 60, 30, 15, 5 };
            int coins = prize[Mathf.Clamp(myRank - 1, 0, 3)];
            pl.coins += coins;
            if (myRank == 1) pl.Heal(60);
            string[] notes =
            {
                "ឈ្នះលេខ ១! អ្នកភូមិអបអរសាទរ! ទទួលបាន " + Kh.Num(coins) + " កាក់មាស និងឈាមពេញ!",
                "លេខ ២ ល្អណាស់! ទទួលបាន " + Kh.Num(coins) + " កាក់មាស។ សាកម្ដងទៀតដើម្បីលេខ ១!",
                "លេខ ៣! ទទួលបាន " + Kh.Num(coins) + " កាក់មាស។ សត្វជំនិះរបស់ក្មួយហត់បន្តិចហើយ!",
                "លេខចុងក្រោយ... សត្វជំនិះរបស់ក្មួយ សុំសម្រាកញ៉ាំបាយសិន! (" + Kh.Num(coins) + " កាក់មាស)",
            };
            Sfx.I.Play(myRank == 1 ? "cheer" : "good");
            UI.I.RaceHud("");
            UI.I.RaceResults(rows.ToArray(), notes[Mathf.Clamp(myRank - 1, 0, 3)], () => { foreach (var r in racers) r.GoHome(); });
        }
    }

    /// <summary>គូប្រជែង (AI) ជិះសត្វ</summary>
    public class Racer : MonoBehaviour
    {
        public string riderName; public MountKind kind;
        public int lap, next; public bool racing, finished; public int finishRank;
        float baseSpeed, lane, eventT, pauseT, slipT = -1, mudUntil, boostUntil, veer;
        Vector3 home; MountRig rig; HumanRig rider; string[] jokes;

        public static Racer Build(string name, MountKind kind, float speed, bool female, float scale, Vector3 home, string[] jokes)
        {
            var g = new GameObject("Racer_" + name);
            g.transform.position = World.Ground(home);
            g.transform.rotation = Quaternion.Euler(0, 90, 0);
            var r = g.AddComponent<Racer>();
            r.riderName = name; r.kind = kind; r.baseSpeed = speed; r.home = home; r.jokes = jokes;
            r.rig = MountRig.Build(g.transform, kind);
            var rb = new GameObject("Rider"); rb.transform.SetParent(g.transform, false);
            rb.transform.localPosition = new Vector3(0, r.rig.seatY + 0.1f - 0.95f * scale, -0.1f);
            var st = new HumanStyle
            {
                skin = new Color(0.6f, 0.42f, 0.3f), female = female, scale = scale, weapon = Weapon.Staff,
                top = female ? new Color(0.85f, 0.45f, 0.6f) : kind == MountKind.Elephant ? new Color(0.9f, 0.75f, 0.2f) : new Color(0.3f, 0.6f, 0.9f),
                holBg = female ? new Color(0.5f, 0.15f, 0.4f) : new Color(0.2f, 0.3f, 0.15f), holFg = new Color(0.95f, 0.8f, 0.4f), holAccent = new Color(0.9f, 0.9f, 0.9f),
                kramaA = female ? new Color(0.9f, 0.3f, 0.5f) : new Color(0.2f, 0.5f, 0.85f), kramaB = Color.white,
            };
            r.rider = HumanRig.Build(rb.transform, st);
            r.rider.sitting = true;
            var staff = rb.transform.Find("Model/Hips/Torso/ArmR/Fore/Hand/Staff");
            if (staff != null) Destroy(staff.gameObject);
            var col = g.AddComponent<CapsuleCollider>(); col.isTrigger = true; col.radius = 0.8f;
            return r;
        }

        public void ToGrid(Vector3 p)
        {
            transform.position = p; transform.rotation = Quaternion.identity;
            lap = 0; next = 0; racing = false; finished = false; finishRank = 0;
            lane = Random.Range(-1.6f, 1.6f); eventT = Random.Range(5f, 9f); pauseT = 0; slipT = -1; veer = 0;
        }
        public void Go() { racing = true; }
        public void GoHome()
        {
            racing = false;
            transform.position = World.Ground(home); transform.rotation = Quaternion.Euler(0, 90, 0);
            rig.moveSpeed = 0;
        }

        public void Slip() { if (slipT < 0) { slipT = 0; Sfx.I?.Play("slip", 0.5f); UI.I.Toast(riderName + " រអិលសំបកចេក វិលជុំៗ! ហាហា", 2.2f); } }
        public void InMud() { mudUntil = Time.time + 0.15f; }
        public void Boost(float s) { boostUntil = Time.time + s; }

        void Update()
        {
            var rm = RaceManager.I;
            if (!racing || finished || rm == null || !Game.I.Playing) { rig.moveSpeed = 0; return; }
            float dt = Time.deltaTime;
            var target = rm.cps[next];
            var prev = rm.cps[(next - 1 + rm.cps.Length) % rm.cps.Length];
            var dir = target - prev; dir.y = 0; dir.Normalize();
            var perp = new Vector3(dir.z, 0, -dir.x);
            var aim = target + perp * (lane + veer);
            var to = aim - transform.position; to.y = 0;

            // រឿងកំប្លែងតាមផ្លូវ
            eventT -= dt;
            if (eventT <= 0)
            {
                eventT = Random.Range(6f, 11f);
                if (Random.value < 0.45f)
                {
                    pauseT = Random.Range(1.2f, 2f);
                    if (kind == MountKind.Dog) veer = Random.value < 0.5f ? -6 : 6;
                    var pl = Player.I;
                    if (pl && Vector3.Distance(pl.transform.position, transform.position) < 40) UI.I.Toast(jokes[Random.Range(0, jokes.Length)], 2.2f);
                    Sfx.I?.Play(MountDef.Of(kind).sound, 0.5f);
                    rig.Happy();
                }
            }

            float speed = baseSpeed;
            // ធ្វើឲ្យការប្រណាំងស្មើៗគ្នា (rubber band)
            float diff = rm.PlayerProgress - rm.RacerProgress(this);
            speed *= Mathf.Clamp(1 + diff * 0.06f, 0.8f, 1.25f);
            if (Time.time < mudUntil) speed *= 0.45f;
            if (Time.time < boostUntil) speed *= 1.6f;
            if (pauseT > 0) { pauseT -= dt; speed = kind == MountKind.Dog ? speed * 0.6f : 0; if (pauseT <= 0) veer = 0; }
            if (slipT >= 0) { slipT += dt / 0.9f; speed *= 0.2f; transform.Rotate(0, 1000 * dt, 0); if (slipT >= 1) slipT = -1; }
            else if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 1 - Mathf.Exp(-dt * 5f));

            var step = (slipT >= 0 ? transform.forward : to.normalized) * speed * dt;
            var np = transform.position + step;
            transform.position = new Vector3(np.x, World.Height(np), np.z);
            rig.moveSpeed = speed; rig.refSpeed = baseSpeed;

            var flatD = transform.position - target; flatD.y = 0;
            if (flatD.magnitude < 5f)
            {
                next++;
                if (next >= rm.cps.Length)
                {
                    next = 0; lap++;
                    if (lap >= RaceManager.Laps) { finished = true; rm.RacerFinished(this); rig.Happy(); Sfx.I?.Play(MountDef.Of(kind).sound, 0.5f); }
                }
            }
        }
    }

    /// <summary>សំបកចេក (រអិល) ភក់ (យឺត) អំពៅ (លឿន)</summary>
    public class Hazard : MonoBehaviour
    {
        public enum Kind { Banana, Mud, Sugarcane }
        public Kind kind; float hiddenUntil; GameObject vis; bool wasIn;

        public static Hazard Build(Kind k, Vector3 pos)
        {
            var g = new GameObject(k.ToString());
            g.transform.position = World.Ground(pos);
            var h = g.AddComponent<Hazard>(); h.kind = k;
            h.vis = new GameObject("Vis"); h.vis.transform.SetParent(g.transform, false);
            var t = h.vis.transform;
            switch (k)
            {
                case Kind.Banana:
                    var yel = new Color(1f, 0.88f, 0.2f);
                    Shapes.Make(PrimitiveType.Sphere, t, new Vector3(0, 0.08f, 0), new Vector3(0.35f, 0.12f, 0.35f), yel * 0.9f, false);
                    for (int i = 0; i < 3; i++)
                    {
                        var peel = Shapes.Make(PrimitiveType.Cube, t, Quaternion.Euler(0, i * 120, 0) * new Vector3(0, 0.05f, 0.3f), new Vector3(0.15f, 0.04f, 0.45f), yel, false);
                        peel.transform.localRotation = Quaternion.Euler(-10, i * 120, 0);
                    }
                    Shapes.Make(PrimitiveType.Cylinder, t, new Vector3(0, 0.22f, 0), new Vector3(0.06f, 0.08f, 0.06f), new Color(0.35f, 0.3f, 0.1f), false);
                    break;
                case Kind.Mud:
                    Shapes.Make(PrimitiveType.Cylinder, t, new Vector3(0, 0.03f, 0), new Vector3(6f, 0.03f, 5f), new Color(0.3f, 0.2f, 0.12f), false);
                    for (int i = 0; i < 5; i++) Shapes.Make(PrimitiveType.Sphere, t, new Vector3(Random.Range(-2f, 2f), 0.06f, Random.Range(-1.6f, 1.6f)), Vector3.one * Random.Range(0.2f, 0.4f), new Color(0.38f, 0.27f, 0.16f), false);
                    break;
                default:
                    for (int i = 0; i < 4; i++)
                    {
                        var st = new GameObject("Cane").transform; st.SetParent(t, false); st.localPosition = new Vector3(i * 0.12f - 0.18f, 0, 0); st.localRotation = Quaternion.Euler(0, 0, (i - 1.5f) * 6);
                        Shapes.Make(PrimitiveType.Cylinder, st, new Vector3(0, 0.9f, 0), new Vector3(0.1f, 0.9f, 0.1f), new Color(0.45f, 0.2f, 0.4f), false);
                        for (float y = 0.3f; y < 1.8f; y += 0.35f) Shapes.Make(PrimitiveType.Cylinder, st, new Vector3(0, y, 0), new Vector3(0.12f, 0.02f, 0.12f), new Color(0.6f, 0.55f, 0.3f), false);
                    }
                    Shapes.Glow(PrimitiveType.Sphere, t, new Vector3(0, 2.1f, 0), Vector3.one * 0.3f, new Color(0.5f, 1f, 0.5f), 1.5f);
                    break;
            }
            return h;
        }

        float Radius => kind == Kind.Mud ? 2.8f : 1.4f;

        public void Check(Player pl, List<Racer> racers)
        {
            bool hidden = Time.time < hiddenUntil;
            vis.SetActive(!hidden);
            if (kind == Kind.Sugarcane && !hidden) vis.transform.Rotate(0, 60 * Time.deltaTime, 0);
            if (hidden) return;
            var pos = transform.position;
            float pr = Radius + (pl.Riding ? pl.mount.def.radius * 0.6f : 0.3f);
            bool pin = Flat(pl.transform.position - pos).magnitude < pr && Mathf.Abs(pl.transform.position.y - pos.y) < 2f;
            if (pin) Hit(pl, null);
            if (kind == Kind.Mud)
            {
                if (pin && !wasIn) { UI.I.FloatText(pl.transform.position + Vector3.up * 2.5f, "ភក់! យឺតៗ...", new Color(0.75f, 0.55f, 0.35f)); Sfx.I?.Play("moo", 0.3f); }
                wasIn = pin;
            }
            foreach (var r in racers)
                if (r.racing && Flat(r.transform.position - pos).magnitude < Radius + 0.4f) Hit(null, r);
        }

        void Hit(Player pl, Racer r)
        {
            switch (kind)
            {
                case Kind.Banana:
                    if (pl != null) { pl.Slip(); UI.I.FloatText(pl.transform.position + Vector3.up * 2.5f, "រអិល!", new Color(1f, 0.9f, 0.3f)); }
                    else r.Slip();
                    hiddenUntil = Time.time + 6f;
                    break;
                case Kind.Mud:
                    if (pl != null) pl.InMud(); else r.InMud();
                    break;
                default:
                    if (pl != null) { pl.Boost(2.5f); UI.I.FloatText(pl.transform.position + Vector3.up * 2.5f, "អំពៅ! លឿនខ្លាំង!", new Color(0.6f, 1f, 0.5f)); }
                    else r.Boost(2.5f);
                    hiddenUntil = Time.time + 8f;
                    break;
            }
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}
