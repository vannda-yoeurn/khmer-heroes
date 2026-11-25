// ចំណុចចាប់ផ្ដើមហ្គេម និងដំណើររឿង — ដំណើរការដោយស្វ័យប្រវត្តិ ទោះបី Scene ទទេក៏ដោយ
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KhmerHeroes
{
    public class Game : MonoBehaviour
    {
        public static Game I;
        enum State { Title, Select, Play, Over, Won }
        State state;
        bool paused;

        Camera cam;
        Player player;
        int selected;
        readonly List<HumanRig> preview = new List<HumanRig>();
        GameObject previewRing;

        // ដំណើររឿង
        bool talkedElder, campStarted, chiefDead, hunterDone, boatmanDone;
        Gate gate0, gate1, gate2, gateCamp;
        Plate[] plates;
        Torch[] torches; int nextTorch; bool torchesDone;
        int tigersLeft, leversPulled;
        DrawBridge bridge;
        Enemy chief;
        SeedBasket basket;
        int maxZone = -1;
        float startTime, battleCheck;
        int quizRight, quizTotal;
        readonly Queue<Quiz> quizQueue = new Queue<Quiz>();

        public bool Playing => state == State.Play && !paused && UI.I != null && !UI.I.Busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        }

        void Awake()
        {
            I = this;
            gameObject.AddComponent<Sfx>();
            gameObject.AddComponent<UI>();
            Application.targetFrameRate = 60;
        }

        void Start() { StartCoroutine(Setup()); }

        IEnumerator Setup()
        {
            // សម្អាត Scene (កាមេរ៉ា ពន្លឺ ចាស់ៗ)
            foreach (var g in SceneManager.GetActiveScene().GetRootGameObjects())
                if (g != gameObject) Destroy(g);
            yield return null;
            Time.timeScale = 1;
            ResetFlags();
            var cg = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = cg.AddComponent<Camera>();
            cam.farClipPlane = 600; cam.nearClipPlane = 0.2f; cam.fieldOfView = 60;
            cg.AddComponent<AudioListener>();
            cg.AddComponent<CamRig>();
            World.Build();
            SpawnContent();
            ShowTitle();
        }

        void ResetFlags()
        {
            talkedElder = campStarted = chiefDead = hunterDone = boatmanDone = torchesDone = false;
            nextTorch = 0; tigersLeft = 0; leversPulled = 0; maxZone = -1; quizRight = quizTotal = 0; paused = false;
            quizQueue.Clear();
            preview.Clear();
            var list = new List<Quiz>(Data.Quizzes);
            for (int i = 0; i < list.Count; i++) { int j = Random.Range(i, list.Count); (list[i], list[j]) = (list[j], list[i]); }
            foreach (var q in list) quizQueue.Enqueue(q);
        }

        Quiz[] NextQuiz(int n)
        {
            var r = new Quiz[n];
            for (int i = 0; i < n; i++)
            {
                if (quizQueue.Count == 0) foreach (var q in Data.Quizzes) quizQueue.Enqueue(q);
                r[i] = quizQueue.Dequeue();
            }
            return r;
        }

        void RunQuiz(int n, int need, System.Action<int> done)
        {
            UI.I.QuizSession(NextQuiz(n), need, ok => { quizRight += ok; quizTotal += n; done(ok); });
        }

        // ---------------- អេក្រង់ ----------------
        void ShowTitle()
        {
            state = State.Title;
            Sfx.I.Music("title");
            UI.I.Title(ShowSelect, () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        void ShowSelect()
        {
            state = State.Select;
            if (preview.Count == 0)
            {
                for (int i = 0; i < Data.Heroes.Length; i++)
                {
                    var g = new GameObject("Preview_" + i);
                    g.transform.position = World.Ground(new Vector3(-4.5f + i * 3f, 0, 14));
                    g.transform.rotation = Quaternion.Euler(0, 180, 0);
                    var rig = HumanRig.Build(g.transform, HumanStyle.From(Data.Heroes[i]));
                    preview.Add(rig);
                }
                previewRing = Shapes.Glow(PrimitiveType.Cylinder, null, Vector3.zero, new Vector3(1.6f, 0.02f, 1.6f), UI.Gold, 1.6f);
            }
            UI.I.Select(selected, i => { selected = i; ShowSelect(); preview[i].Attack(0, 0.6f); }, Begin);
        }

        void Begin()
        {
            UI.I.ClearScreen();
            foreach (var r in preview) if (r) Destroy(r.gameObject);
            preview.Clear();
            if (previewRing) Destroy(previewRing);
            player = Player.Create(World.Ground(new Vector3(0, 0, 9)) + Vector3.up * 0.1f, selected);
            player.gameObject.layer = 2;
            CamRig.I.target = player.transform; CamRig.I.yaw = 0; CamRig.I.pitch = 15;
            state = State.Play;
            startTime = Time.time;
            UI.I.ShowHud(true);
            UI.I.Dialogue(Data.Intro, () => UI.I.Toast("ទៅជួបតាមាស ចាស់ភូមិ (មានសញ្ញាពណ៌មាសលើក្បាល)", 4f));
        }

        // ---------------- ដាក់តួអង្គ និងល្បែង ----------------
        static HumanStyle Villager(Color top, Color hol, bool female, float scale = 1f)
        {
            return new HumanStyle
            {
                skin = new Color(0.6f, 0.42f, 0.3f), top = top, holBg = hol, holFg = new Color(0.85f, 0.8f, 0.6f), holAccent = new Color(0.3f, 0.2f, 0.15f),
                kramaA = new Color(0.75f, 0.15f, 0.12f), kramaB = Color.white, trim = new Color(0.8f, 0.65f, 0.3f), female = female, weapon = Weapon.Staff, scale = scale,
                hair = female ? new Color(0.55f, 0.55f, 0.55f) : new Color(0.7f, 0.7f, 0.7f)
            };
        }

        void SpawnContent()
        {
            // ---- តំបន់ ០៖ ភូមិ ----
            var elder = Npc.Spawn("តាមាស", new Vector3(7, 0, 30), 200, Villager(new Color(0.85f, 0.82f, 0.75f), new Color(0.25f, 0.2f, 0.35f), false, 0.95f));
            elder.lines = () => talkedElder ? new[] { new Line("តាមាស", "ទៅមុខចុះក្មួយ! ពូជ័យកំពុងរង់ចាំនៅទ្វារភូមិ។") } : Data.Elder;
            elder.after = () => { if (!talkedElder) { talkedElder = true; UI.I.Toast("ភារកិច្ចថ្មី៖ ទៅជួបពូជ័យនៅទ្វារភូមិ"); } };
            elder.marker = () => !talkedElder;

            var guardStyle = Villager(new Color(0.2f, 0.25f, 0.4f), new Color(0.15f, 0.2f, 0.35f), false);
            guardStyle.hair = new Color(0.06f, 0.05f, 0.05f); guardStyle.weapon = Weapon.Sword;
            var guard = Npc.Spawn("ពូជ័យ", new Vector3(5, 0, 119), 200, guardStyle);
            guard.lines = () =>
                !talkedElder ? new[] { new Line("ពូជ័យ", "សួស្ដីក្មួយ! ទៅជួបតាមាសនៅកណ្ដាលភូមិសិនមក។") } :
                gate0.open ? new[] { new Line("ពូជ័យ", "ផ្លូវបើកហើយ។ ប្រយ័ត្នជ្រូកព្រៃនៅវាលស្រែផង!") } : Data.Guard;
            guard.after = () =>
            {
                if (!talkedElder || gate0.open) return;
                RunQuiz(3, 2, ok =>
                {
                    if (ok >= 2) { gate0.Open(); UI.I.Dialogue(new[] { new Line("ពូជ័យ", "ពូកែណាស់! ត្រូវ " + Kh.Num(ok) + " ក្នុង ៣។ ពូបើកទ្វារឲ្យហើយ។") }, () => UI.I.Dialogue(Data.Field, null)); }
                    else UI.I.Dialogue(new[] { new Line("ពូជ័យ", "ត្រូវតែ " + Kh.Num(ok) + " ក្នុង ៣ ទេ។ គិតឲ្យបានល្អិតល្អន់ ហើយមកនិយាយជាមួយពូម្ដងទៀត!") }, null);
                });
            };
            guard.marker = () => talkedElder && !gate0.open;
            gate0 = Gate.Build(new Vector3(0, 0, 126), 5, 46);

            // ---- សត្វជំនិះ និងការប្រណាំង ----
            Mount.Spawn(MountKind.Horse, new Vector3(-14, 0, 4), 10);
            Mount.Spawn(MountKind.Elephant, new Vector3(-21, 0, 4.5f), 5);
            Mount.Spawn(MountKind.Buffalo, new Vector3(-28, 0, 4), 15);
            Mount.Spawn(MountKind.Cat, new Vector3(14, 0, 4), -10);
            Mount.Spawn(MountKind.Dog, new Vector3(20, 0, 4), -5);
            Mount.Spawn(MountKind.Tiger, new Vector3(26, 0, 4), -15);
            RaceManager.Build();
            var racerSt = Villager(new Color(0.9f, 0.75f, 0.2f), new Color(0.6f, 0.1f, 0.1f), false);
            racerSt.hair = new Color(0.06f, 0.05f, 0.05f); racerSt.headband = true;
            var chan = Npc.Spawn("ពូចាន់", new Vector3(-6, 0, 17), 90, racerSt);
            chan.lines = () => RaceManager.I.Racing ? null : Data.RaceIntro;
            chan.after = () => RaceManager.I.Begin();
            chan.marker = () => !RaceManager.I.Racing;
            Pickup.Spawn(new Vector3(-8, 0, 75), true);

            // ---- តំបន់ ១៖ វាលស្រែ និងព្រៃឫស្សី ----
            foreach (var p in new[] { new Vector3(-15, 0, 150), new Vector3(18, 0, 166), new Vector3(-10, 0, 190), new Vector3(14, 0, 212), new Vector3(-20, 0, 228) })
                SpawnEnemy(EnemyKind.Boar, p, 1);
            SpawnEnemy(EnemyKind.Bandit, new Vector3(6, 0, 222), 1);
            plates = new[] { Plate.Build(new Vector3(-6, 0, 250)), Plate.Build(new Vector3(6, 0, 250)) };
            var area = new Rect(-16, 232, 32, 24);
            PushStone.Build(new Vector3(-6, 0, 242), area, plates);
            PushStone.Build(new Vector3(12, 0, 250), area, plates);
            Tablet.Build(new Vector3(0, 0, 236), 180, new[]
            {
                new Line("ផ្ទាំងថ្ម", "«រុញថ្មធំទាំងពីរ ឲ្យទៅលើបន្ទះថ្មភ្លឺ ទើបទ្វារព្រៃឫស្សីបើក។»"),
                new Line("ផ្ទាំងថ្ម", "(ឈរជាប់ថ្ម ហើយចុច F — ថ្មនឹងរំកិលចេញពីអ្នក ម្ដង ២ ម៉ែត្រ)"),
            });
            gate1 = Gate.Build(new Vector3(0, 0, 262), 5, 46);
            Pickup.Spawn(new Vector3(12, 0, 180), true);
            Pickup.Spawn(new Vector3(-10, 0, 255), true);

            // ---- តំបន់ ២៖ ភ្នំខ្លា ----
            SpawnEnemy(EnemyKind.Archer, new Vector3(-8, 0, 302), 2);
            SpawnEnemy(EnemyKind.Archer, new Vector3(10, 0, 308), 2);
            SpawnEnemy(EnemyKind.Bandit, new Vector3(2, 0, 315), 2);
            foreach (var p in new[] { new Vector3(-6, 0, 342), new Vector3(8, 0, 358) })
            {
                var t = SpawnEnemy(EnemyKind.Tiger, p, 2);
                tigersLeft++;
                t.onDeath += _ => { tigersLeft--; if (tigersLeft == 0) UI.I.Toast("ខ្លាទាំងពីរត្រូវបានកម្ចាត់!"); CheckGate2(); };
            }
            var hunter = Npc.Spawn("តាព្រាន", new Vector3(-13, 0, 288), 90, Villager(new Color(0.35f, 0.32f, 0.2f), new Color(0.3f, 0.25f, 0.15f), false));
            hunter.lines = () => hunterDone ? new[] { new Line("តាព្រាន", "សូមឲ្យក្មួយធ្វើដំណើរដោយសុវត្ថិភាព!") } : Data.Hunter;
            hunter.after = () => { if (!hunterDone) { hunterDone = true; RunQuiz(2, 0, ok => Reward(ok, "តាព្រាន")); } };
            hunter.marker = () => !hunterDone;
            // ចន្លុះ៖ លំដាប់ ខៀវ → បៃតង → ក្រហម
            torches = new[]
            {
                Torch.Build(new Vector3(0, 0, 377), 0, new Color(0.2f, 0.45f, 0.95f)),
                Torch.Build(new Vector3(9, 0, 380), 1, new Color(0.2f, 0.75f, 0.25f)),
                Torch.Build(new Vector3(-9, 0, 380), 2, new Color(0.85f, 0.12f, 0.1f)),
            };
            foreach (var t in torches) t.onLit = OnTorch;
            Tablet.Build(new Vector3(0, 0, 368), 180, new[]
            {
                new Line("ផ្ទាំងថ្ម", "«ភ្លើងទីមួយ៖ ពណ៌មេឃ។ ភ្លើងទីពីរ៖ ពណ៌ស្លឹកឈើ។ ភ្លើងទីបី៖ ពណ៌ឈាម។»"),
                new Line("ផ្ទាំងថ្ម", "(បើអុជខុសលំដាប់ ចន្លុះទាំងអស់នឹងរលត់)"),
            });
            gate2 = Gate.Build(new Vector3(0, 0, 391), 5, 32);
            Pickup.Spawn(new Vector3(9, 0, 322), true);
            Pickup.Spawn(new Vector3(-12, 0, 370), true);

            // ---- តំបន់ ៣៖ ទន្លេក្រពើ ----
            var boat = Npc.Spawn("យាយស", new Vector3(-13, 0, 437), 100, Villager(new Color(0.75f, 0.7f, 0.6f), new Color(0.3f, 0.15f, 0.25f), true));
            boat.lines = () => boatmanDone ? new[] { new Line("យាយស", "ទាញដងទាញទាំងពីរ ហើយស្ពាននឹងចុះ។ ប្រយ័ត្នក្រពើណាចៅ!") } : Data.Boatman;
            boat.after = () =>
            {
                if (boatmanDone) return;
                boatmanDone = true;
                UI.I.Dialogue(new[] { new Line("យាយស", "ចាំយាយសួរចៅពីរសំណួរសិន។ បើត្រូវ យាយឲ្យទឹកដូង!") }, () => RunQuiz(2, 0, ok => Reward(ok, "យាយស")));
            };
            boat.marker = () => !boatmanDone;
            float deckY = World.H(0, 461) + 0.1f;
            bridge = DrawBridge.Build(462, 488, deckY, 5);
            foreach (var x in new[] { -22f, 22f })
            {
                var lv = Lever.Build(new Vector3(x, 0, 451), 0);
                lv.onPull = () =>
                {
                    leversPulled++;
                    if (leversPulled >= 2) { bridge.Lower(); UI.I.Toast("ស្ពានកំពុងចុះ!", 3f); }
                    else UI.I.Toast("ដងទាញ ១/២ — រកដងទាញមួយទៀតនៅម្ខាងទៀត");
                };
            }
            SpawnEnemy(EnemyKind.Bandit, new Vector3(-17, 0, 446), 3);
            SpawnEnemy(EnemyKind.Bandit, new Vector3(17, 0, 444), 3);
            SpawnEnemy(EnemyKind.Archer, new Vector3(4, 0, 432), 3);
            SpawnEnemy(EnemyKind.Croc, new Vector3(-13, 0, 464), 3);
            SpawnEnemy(EnemyKind.Croc, new Vector3(15, 0, 465), 3);
            SpawnEnemy(EnemyKind.Croc, new Vector3(-6, 0, 487), 3);
            SpawnEnemy(EnemyKind.Bandit, new Vector3(-7, 0, 520), 3);
            SpawnEnemy(EnemyKind.Bandit, new Vector3(9, 0, 528), 3);
            SpawnEnemy(EnemyKind.Archer, new Vector3(0, 0, 545), 3);
            Pickup.Spawn(new Vector3(10, 0, 425), true);
            Pickup.Spawn(new Vector3(-10, 0, 505), true);
            Pickup.Spawn(new Vector3(12, 0, 550), true);

            // ---- តំបន់ ៤៖ ជំរុំចោរ ----
            gateCamp = Gate.Build(new Vector3(0, 0, 574), 6, 31);
            foreach (var p in new[] { new Vector3(-10, 0, 608), new Vector3(10, 0, 614), new Vector3(-14, 0, 638), new Vector3(14, 0, 644) })
                SpawnEnemy(EnemyKind.Bandit, p, 4);
            SpawnEnemy(EnemyKind.Archer, new Vector3(-9, 0, 666), 4);
            SpawnEnemy(EnemyKind.Archer, new Vector3(9, 0, 668), 4);
            chief = SpawnEnemy(EnemyKind.Chief, new Vector3(0, 0, 662), 4);
            chief.aggro = 0; // រង់ចាំរហូតដល់ការសន្ទនាចប់
            chief.onDeath += _ =>
            {
                chiefDead = true; basket.unlocked = true; UI.I.Boss(null, 0);
                UI.I.Toast("មេចោរចាញ់ហើយ! ទៅយកកញ្ជើស្រូវពូជ", 4f);
                Sfx.I.Play("good");
            };
            basket = SeedBasket.Build(new Vector3(0, 0, 680));
            basket.transform.position += Vector3.up * 0.5f;
            basket.onTake = () =>
            {
                Sfx.I.Play("good");
                UI.I.Dialogue(Data.Ending, () =>
                {
                    state = State.Won;
                    UI.I.ShowHud(false);
                    UI.I.Won(player.coins, (Time.time - startTime) / 60f, quizRight, quizTotal, () => StartCoroutine(Setup()));
                });
            };
            for (int i = 0; i < 3; i++) Pickup.Spawn(new Vector3(-20 + i * 20, 0, 595), true);
            foreach (var p in new[] { new Vector3(-6, 0, 30), new Vector3(10, 0, 140), new Vector3(-4, 0, 300), new Vector3(4, 0, 420), new Vector3(-3, 0, 600) })
                for (int i = 0; i < 4; i++) Pickup.Spawn(p + new Vector3(i * 1.6f, 0, i * 1.2f), false);
            // ចន្លុះ និងភ្លើងជំរុំ (តុបតែង)
            foreach (var p in new[] { new Vector3(-6, 0, 625), new Vector3(6, 0, 650) })
            {
                var g = new GameObject("Campfire"); g.transform.position = World.Ground(p);
                for (int k = 0; k < 6; k++) { var l = Shapes.Make(PrimitiveType.Cylinder, g.transform, new Vector3(0, 0.15f, 0), new Vector3(0.15f, 0.6f, 0.15f), new Color(0.3f, 0.2f, 0.1f), false); l.transform.localRotation = Quaternion.Euler(80, k * 60, 0); }
                Shapes.Fire(g.transform, new Vector3(0, 0.2f, 0), 1.6f);
            }
        }

        Enemy SpawnEnemy(EnemyKind kind, Vector3 pos, int zone)
        {
            var e = Enemy.Spawn(kind, pos, zone);
            return e;
        }

        public void SpawnExtra(EnemyKind kind, Vector3 pos, int zone)
        {
            var e = Enemy.Spawn(kind, pos, zone);
            e.aggro = 40;
        }

        void Reward(int ok, string who)
        {
            if (ok > 0) { player.Heal(40 * ok); player.coins += 10 * ok; }
            UI.I.Dialogue(new[] { new Line(who, ok > 0 ? "ល្អណាស់! ត្រូវ " + Kh.Num(ok) + " សំណួរ។ យកទឹកដូង និងកាក់មាសនេះទៅ!" : "មិនអីទេ ចៅ។ រៀនបន្ថែម ហើយថ្ងៃក្រោយនឹងឆ្លើយបាន!") }, null);
        }

        void OnTorch(Torch t)
        {
            if (torchesDone) return;
            if (t.id == nextTorch)
            {
                nextTorch++;
                if (nextTorch >= torches.Length) { torchesDone = true; UI.I.Toast("ចន្លុះទាំងបីបានអុជត្រូវលំដាប់!", 3f); Sfx.I.Play("good"); CheckGate2(); }
            }
            else
            {
                UI.I.Toast("លំដាប់ខុស! ចន្លុះរលត់អស់ហើយ — អានផ្ទាំងថ្មម្ដងទៀត", 3f);
                Sfx.I.Play("bad");
                StartCoroutine(PutOut());
            }
        }

        IEnumerator PutOut()
        {
            yield return new WaitForSeconds(0.7f);
            foreach (var x in torches) x.PutOut();
            nextTorch = 0;
        }

        void CheckGate2()
        {
            if (torchesDone && tigersLeft <= 0 && !gate2.open) { gate2.Open(); UI.I.Toast("ទ្វារភ្នំបើកហើយ!", 3f); }
        }

        // ---------------- រង្វិលជុំ ----------------
        void Update()
        {
            if (UI.I == null || cam == null) return;
            bool modal = UI.I.ModalOpen || UI.I.DialogueOpen;
            Time.timeScale = (state == State.Play && (modal || paused)) ? 0 : 1;
            bool lockCursor = Playing;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;

            if (state == State.Title)
            {
                float a = Time.time * 0.05f;
                var c = new Vector3(Mathf.Sin(a) * 45, 0, 60 + Mathf.Cos(a) * 45);
                c.y = World.H(c.x, c.z) + 14;
                cam.transform.position = c;
                cam.transform.LookAt(new Vector3(0, 9, 60));
                return;
            }
            if (state == State.Select)
            {
                var focus = World.Ground(new Vector3(0, 0, 14));
                cam.transform.position = Vector3.Lerp(cam.transform.position, focus + new Vector3(0, 1.9f, -8.2f), Time.deltaTime * 3);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(focus + Vector3.up * 0.4f - cam.transform.position), Time.deltaTime * 3);
                if (selected < preview.Count && previewRing) previewRing.transform.position = preview[selected].transform.position + Vector3.up * 0.05f;
                return;
            }
            if (state != State.Play || player == null) return;

            if (InputX.Pause)
            {
                if (paused) { paused = false; UI.I.CloseModal(); }
                else if (!UI.I.Busy) { paused = true; UI.I.Pause(() => paused = false, () => { paused = false; player.Teleport(player.checkpoint); }, () => { paused = false; StartCoroutine(Setup()); }); }
            }

            // តំបន់ និងចំណុចត្រឡប់
            int zone = World.ZoneOf(player.transform.position.z);
            if (zone > maxZone)
            {
                maxZone = zone;
                Vector3[] cps = { new Vector3(0, 0, 9), new Vector3(0, 0, 134), new Vector3(0, 0, 274), new Vector3(0, 0, 418), new Vector3(0, 0, 562) };
                player.checkpoint = World.Ground(cps[zone]) + Vector3.up * 0.2f;
                if (zone > 0) UI.I.Toast("— " + Data.ZoneNames[zone] + " —", 3.5f);
            }
            if (zone >= 4 && !gateCamp.open && player.transform.position.z > 556) gateCamp.Open();
            if (!campStarted && player.transform.position.z > 586)
            {
                campStarted = true;
                UI.I.Dialogue(Data.CampIntro, () => { chief.aggro = 40; });
            }
            if (campStarted && !chiefDead && chief != null) UI.I.Boss(chief.displayName, chief.hp / chief.hpMax);

            // ទ្វារព្រៃឫស្សី
            if (!gate1.open && plates[0].pressed && plates[1].pressed) { gate1.Open(); UI.I.Toast("ទ្វារព្រៃឫស្សីបើកហើយ!", 3f); }

            // ភ្លេង៖ ប្រយុទ្ធ ឬ ធម្មតា
            battleCheck -= Time.deltaTime;
            if (battleCheck <= 0)
            {
                battleCheck = 0.5f;
                bool fight = campStarted && !chiefDead;
                foreach (var a in Actor.All)
                    if (a != null && a.isEnemy && a.Alive && Vector3.Distance(a.transform.position, player.transform.position) < 16) { fight = true; break; }
                Sfx.I.Music(RaceManager.I != null && RaceManager.I.Racing ? "race" : fight ? "battle" : "field");
            }

            UI.I.Objective(Data.ZoneNames[zone], ObjectiveText(zone));
        }

        string ObjectiveText(int zone)
        {
            switch (zone)
            {
                case 0:
                {
                    const string race = "\n• (ជម្រើស) ជិះសត្វនៅក្រោល ហើយប្រណាំងជាមួយពូចាន់";
                    if (!talkedElder) return "• ជួបតាមាស ចាស់ភូមិ នៅកណ្ដាលភូមិ" + race;
                    if (!gate0.open) return "• ឆ្លើយសំណួររបស់ពូជ័យ នៅទ្វារភូមិ" + race;
                    return "• ចេញទៅវាលស្រែ" + race;
                }
                case 1:
                    if (gate1.open) return "• ទ្វារព្រៃឫស្សីបើកហើយ — ឡើងទៅភ្នំ";
                    int pr = (plates[0].pressed ? 1 : 0) + (plates[1].pressed ? 1 : 0);
                    return "• រុញថ្មទៅលើបន្ទះភ្លឺ (" + Kh.Num(pr) + "/២)\n• អានផ្ទាំងថ្មនៅមុខទ្វារ\n• ប្រយ័ត្នជ្រូកព្រៃ!";
                case 2:
                    if (gate2.open) return "• ទ្វារភ្នំបើកហើយ — ចុះទៅទន្លេ";
                    return "• កម្ចាត់ខ្លា (" + Kh.Num(2 - Mathf.Max(0, tigersLeft)) + "/២)\n• អុជចន្លុះតាមលំដាប់ (" + Kh.Num(torchesDone ? 3 : nextTorch) + "/៣)" + (hunterDone ? "" : "\n• (ជម្រើស) ជួបតាព្រាន");
                case 3:
                    if (bridge.down) return "• ឆ្លងស្ពាន ទៅជំរុំចោរព្រៃ";
                    return "• ទាញដងទាញ (" + Kh.Num(leversPulled) + "/២) ដើម្បីបន្ទាបស្ពាន\n• កុំចុះទឹក — មានក្រពើ!" + (boatmanDone ? "" : "\n• (ជម្រើស) ជួបយាយស");
                default:
                    if (!campStarted) return "• ចូលទៅក្នុងជំរុំចោរ";
                    if (!chiefDead) return "• កម្ចាត់មេចោរដៃដែក";
                    return "• យកកញ្ជើស្រូវពូជនៅលើវេទិកា";
            }
        }

        public void GameOver()
        {
            if (state != State.Play) return;
            state = State.Over;
            UI.I.GameOver(() =>
            {
                player.ReviveAll(0.7f);
                player.Teleport(player.checkpoint);
                state = State.Play;
            }, () => StartCoroutine(Setup()));
        }
    }
}
