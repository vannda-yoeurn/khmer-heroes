// វីរជនទាំងបួន (ក្រុម)៖ ដើរ លោត គេច វាយ ក្បាច់ពិសេស ប្ដូរតួអង្គ
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KhmerHeroes
{
    public class Player : Actor
    {
        public static Player I;
        public HeroDef[] defs;
        public float[] hp;
        public float[] skillReady;
        public int active;
        public int coins;
        public Vector3 checkpoint;
        HumanRig[] rigs; GameObject[] bodies;
        CharacterController cc;
        float vy, atkReady, dodgeReady, switchReady, comboReset, invuln, downTimer = -1;
        int combo;
        float rollT = -1, dashT = -1; Vector3 rollDir, dashDir, knockVel;
        HashSet<Actor> dashHit;
        Flash[] flashes;
        struct Pending { public float t; public Action act; }
        readonly List<Pending> pending = new List<Pending>();
        float stepT;

        public HeroDef Def => defs[active];
        public HumanRig Rig => rigs[active];
        public override bool Alive => hp != null && hp[active] > 0;
        public override Vector3 Center => transform.position + Vector3.up * 1.1f;
        public bool AnyAlive { get { foreach (var h in hp) if (h > 0) return true; return false; } }
        public float SkillCooldownLeft => Mathf.Max(0, skillReady[active] - Time.time);

        public static Player Create(Vector3 pos, int start)
        {
            var g = new GameObject("Player");
            g.transform.position = pos;
            var p = g.AddComponent<Player>();
            p.Init(start);
            return p;
        }

        void Init(int start)
        {
            I = this; isEnemy = false;
            defs = Data.Heroes;
            hp = new float[defs.Length]; skillReady = new float[defs.Length];
            rigs = new HumanRig[defs.Length]; bodies = new GameObject[defs.Length]; flashes = new Flash[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                hp[i] = defs[i].hp;
                var b = new GameObject("Hero_" + i); b.transform.SetParent(transform, false);
                rigs[i] = HumanRig.Build(b.transform, HumanStyle.From(defs[i]));
                rigs[i].refSpeed = defs[i].speed;
                flashes[i] = b.AddComponent<Flash>();
                bodies[i] = b; b.SetActive(i == start);
            }
            active = start;
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.92f, 0); cc.slopeLimit = 45; cc.stepOffset = 0.45f;
            checkpoint = transform.position;
        }

        void Later(float delay, Action a) => pending.Add(new Pending { t = Time.time + delay, act = a });

        void Update()
        {
            float dt = Time.deltaTime;
            // វីរជនដែលសម្រាក បានឈាមមកវិញបន្តិចម្ដងៗ
            for (int i = 0; i < hp.Length; i++)
                if (i != active && hp[i] > 0 && hp[i] < defs[i].hp) hp[i] = Mathf.Min(defs[i].hp, hp[i] + 2.5f * dt);

            for (int i = pending.Count - 1; i >= 0; i--)
                if (Time.time >= pending[i].t) { var a = pending[i].act; pending.RemoveAt(i); if (Alive) a(); }

            if (downTimer >= 0)
            {
                downTimer -= dt;
                if (downTimer < 0)
                {
                    int next = -1;
                    for (int k = 1; k <= hp.Length; k++) { int j = (active + k) % hp.Length; if (hp[j] > 0) { next = j; break; } }
                    if (next >= 0) Switch(next, true); else Game.I.GameOver();
                }
                Rig.moveSpeed = 0;
                return;
            }
            if (!Game.I.Playing) { Rig.moveSpeed = 0; return; }
            if (!Alive) return;
            invuln -= dt;

            var mv = InputX.Move();
            var cam = CamRig.I;
            Vector3 wish = cam.Forward * mv.y + cam.Right * mv.x;
            bool attacking = Rig.Busy;
            float spd = Def.speed * (attacking ? (Def.weapon == Weapon.Bow ? 0.55f : 0.35f) : 1f);
            Vector3 horiz;

            if (rollT >= 0)
            {
                rollT += dt / 0.45f;
                horiz = rollDir * 11f * (1 - rollT * 0.5f);
                if (rollT >= 1) rollT = -1;
            }
            else if (dashT >= 0)
            {
                dashT += dt / 0.28f;
                horiz = dashDir * 34f;
                Combat.Hit(transform.position, dashDir, 2.2f, 360, Def.dmg * 2.2f, true, 3f, 0.5f, dashHit);
                if (dashT >= 1) dashT = -1;
            }
            else
            {
                horiz = wish * spd;
                if (wish.sqrMagnitude > 0.01f && !attacking)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(wish), 1 - Mathf.Exp(-dt * 14f));
            }
            horiz += knockVel;
            knockVel = Vector3.MoveTowards(knockVel, Vector3.zero, dt * 25f);

            bool grounded = cc.isGrounded;
            if (grounded && vy < 0) vy = -2f;
            if (InputX.Jump && grounded && rollT < 0 && dashT < 0) vy = 7f;
            vy -= 22f * dt;
            cc.Move((horiz + Vector3.up * vy) * dt);
            var flat = horiz; flat.y = 0;
            Rig.moveSpeed = rollT >= 0 || dashT >= 0 ? 0 : flat.magnitude;
            Rig.grounded = grounded || vy > -3f && vy < 0;
            if (grounded && flat.magnitude > 1f) { stepT -= dt * flat.magnitude; if (stepT <= 0) { stepT = 2.2f; Sfx.I?.Play("step", 0.5f); } }

            // ---------- វាយ ----------
            if (InputX.Attack && Time.time >= atkReady && rollT < 0 && dashT < 0) DoAttack();
            if (InputX.Skill && Time.time >= skillReady[active] && rollT < 0 && dashT < 0) DoSkill();
            if (InputX.Dodge && Time.time >= dodgeReady && rollT < 0 && dashT < 0)
            {
                rollT = 0; dodgeReady = Time.time + 0.8f; invuln = 0.45f;
                rollDir = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.forward;
                transform.rotation = Quaternion.LookRotation(rollDir);
                Rig.Roll(0.45f);
                Sfx.I?.Play("swing", 0.5f);
            }

            // ---------- ប្រើ / និយាយ ----------
            var it = Interactable.Nearest(transform.position, 2.8f);
            UI.I.Prompt(it != null ? "[F] " + it.Prompt : "");
            if (InputX.Interact && it != null) it.Use(this);

            int hk = InputX.HeroKey();
            if (hk >= 0 && hk != active && Time.time >= switchReady)
            {
                if (hp[hk] > 0) Switch(hk, false);
                else UI.I.Toast(defs[hk].name + " សន្លប់ — ត្រូវការទឹកដូងដើម្បីជួយ");
            }

            // ធ្លាក់ទឹក ឬធ្លាក់ចេញពីពិភព
            if (transform.position.y < World.WaterY - 1.0f || transform.position.y < -20)
            {
                UI.I.Toast("ធ្លាក់ទឹក! ត្រឡប់ទៅកន្លែងសុវត្ថិភាពវិញ");
                Hurt(20, true);
                if (Alive) Teleport(checkpoint);
            }
        }

        Vector3 AimDir()
        {
            var f = CamRig.I.Forward;
            return f;
        }

        void Face(Vector3 dir)
        {
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
        }

        void DoAttack()
        {
            atkReady = Time.time + Def.atkCd;
            if (Def.weapon == Weapon.Bow)
            {
                Face(AimDir());
                Rig.Attack(0, Def.atkCd * 0.95f);
                var dmg = Def.dmg;
                Later(Def.atkCd * 0.45f, () =>
                {
                    var sp = transform.position + Vector3.up * 1.45f + transform.forward * 0.6f;
                    var aim = CamRig.I.AimPoint();
                    Arrow.Fire(sp, (aim - sp).normalized, 40f, dmg, false);
                    Sfx.I?.Play("arrow", 0.7f);
                });
                return;
            }
            var tgt = Combat.Nearest(transform.position, Def.range + 3f, true);
            Face(tgt != null ? tgt.transform.position - transform.position : (InputX.Move().sqrMagnitude > 0.01f ? transform.forward : AimDir()));
            combo = Time.time < comboReset ? combo + 1 : 0;
            comboReset = Time.time + Def.atkCd + 0.5f;
            int c = combo;
            Rig.Attack(c, Def.atkCd * 0.95f);
            Sfx.I?.Play("swing", 0.6f);
            Later(Def.atkCd * 0.4f, () =>
            {
                float mult = c % 3 == 2 ? 1.5f : 1f;
                int n = Combat.Hit(transform.position, transform.forward, Def.range, 150, Def.dmg * mult, true, c % 3 == 2 ? 5f : 2.5f);
                if (n > 0) { Sfx.I?.Play("hit", 0.9f); CamRig.I.Shake(0.15f); }
            });
        }

        void DoSkill()
        {
            skillReady[active] = Time.time + Def.skillCd;
            Sfx.I?.Play("skill", 0.8f);
            float dmg = Def.dmg;
            switch (Def.weapon)
            {
                case Weapon.Sword:
                    Rig.Spin();
                    for (int k = 0; k < 2; k++)
                        Later(0.15f + k * 0.25f, () =>
                        {
                            if (Combat.Hit(transform.position, transform.forward, 3.8f, 360, dmg * 1.6f, true, 4f) > 0) Sfx.I?.Play("hit");
                            Combat.Ring(transform.position, 3.8f, new Color(1f, 0.8f, 0.4f));
                        });
                    break;
                case Weapon.Bow:
                    Face(AimDir());
                    Rig.Attack(0, 0.5f);
                    Later(0.25f, () =>
                    {
                        var sp = transform.position + Vector3.up * 1.45f + transform.forward * 0.6f;
                        var dir = (CamRig.I.AimPoint() - sp).normalized;
                        for (int k = -1; k <= 1; k++) Arrow.Fire(sp, Quaternion.Euler(0, k * 10, 0) * dir, 42f, dmg * 1.4f, false);
                        Sfx.I?.Play("arrow");
                    });
                    break;
                case Weapon.Staff:
                    Rig.Attack(0, 0.65f);
                    Later(0.36f, () =>
                    {
                        Combat.Hit(transform.position, transform.forward, 5f, 360, dmg * 1.8f, true, 8f, 2f);
                        Combat.Ring(transform.position, 5f, new Color(0.9f, 0.75f, 0.5f));
                        CamRig.I.Shake(0.8f); Sfx.I?.Play("hit");
                    });
                    break;
                case Weapon.Knives:
                    var tgt = Combat.Nearest(transform.position, 10f, true);
                    dashDir = tgt != null ? (tgt.transform.position - transform.position) : (InputX.Move().sqrMagnitude > 0.01f ? transform.forward : AimDir());
                    dashDir.y = 0; dashDir.Normalize();
                    Face(dashDir);
                    dashT = 0; invuln = 0.35f; dashHit = new HashSet<Actor>();
                    Rig.Attack(1, 0.3f);
                    break;
            }
        }

        public void Switch(int k, bool forced)
        {
            bodies[active].SetActive(false);
            active = k;
            bodies[k].SetActive(true);
            Rig.dead = false;
            switchReady = Time.time + 0.8f; invuln = 0.6f; downTimer = -1; rollT = -1; dashT = -1;
            Combat.Ring(transform.position, 2f, new Color(0.6f, 0.9f, 1f));
            Sfx.I?.Play("pick");
            UI.I.Toast((forced ? "ជំនួស៖ " : "") + Def.name + " · " + Def.role, 1.6f);
        }

        public override void Damage(float dmg, Vector3 from, float knock = 0, float stun = 0)
        {
            if (invuln > 0 || rollT >= 0 || dashT >= 0 || !Alive) return;
            var d = transform.position - from; d.y = 0;
            if (d.sqrMagnitude > 0.001f) knockVel = d.normalized * knock * 1.5f;
            Hurt(dmg, false);
        }

        void Hurt(float dmg, bool silent)
        {
            if (!Alive) return;
            hp[active] -= dmg;
            flashes[active].Hit(new Color(1, 0.35f, 0.35f));
            Rig.Hurt();
            Sfx.I?.Play("hurt", 0.8f);
            CamRig.I.Shake(0.35f);
            invuln = 0.35f;
            if (hp[active] <= 0)
            {
                hp[active] = 0;
                Rig.dead = true;
                downTimer = 1.3f;
                UI.I.Toast(Def.name + " សន្លប់ហើយ!", 2f);
            }
        }

        public void Heal(float amount)
        {
            for (int i = 0; i < hp.Length; i++) hp[i] = Mathf.Min(defs[i].hp, hp[i] + amount);
            Combat.Ring(transform.position, 1.6f, new Color(0.4f, 1f, 0.5f));
        }

        public void ReviveAll(float frac)
        {
            for (int i = 0; i < hp.Length; i++) hp[i] = Mathf.Max(hp[i], defs[i].hp * frac);
            Rig.dead = false; downTimer = -1;
        }

        public void Teleport(Vector3 p)
        {
            cc.enabled = false; transform.position = p; cc.enabled = true; vy = 0; knockVel = Vector3.zero;
        }
    }
}
