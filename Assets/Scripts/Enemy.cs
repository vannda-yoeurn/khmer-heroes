// សត្រូវ៖ ចោរព្រៃ មេចោរ ជ្រូកព្រៃ ខ្លា ក្រពើ
using UnityEngine;

namespace KhmerHeroes
{
    public enum EnemyKind { Bandit, Archer, Chief, Boar, Tiger, Croc }

    public class Enemy : Actor
    {
        public EnemyKind kind;
        public float hp, hpMax, dmg, speed, aggro, range, cooldown;
        public string displayName;
        public int zone;
        public float leash = 30f;
        public System.Action<Enemy> onDeath;
        Vector3 home, wanderTo;
        HumanRig hRig; BeastRig bRig; Flash flash; HpBar bar;
        float atkReady, stunT, wanderT, hitAt = -1, specialReady, chargeT = -1, deadT = -1;
        Vector3 chargeDir, knockVel;
        bool alerted, enraged, summoned;
        Terrain terrain;

        public override bool Alive => hp > 0;
        public override float Radius => kind == EnemyKind.Croc ? 1.0f : kind == EnemyKind.Chief ? 0.8f : kind == EnemyKind.Tiger ? 0.8f : 0.55f;
        public override Vector3 Center => transform.position + Vector3.up * (kind == EnemyKind.Croc ? 0.4f : kind == EnemyKind.Boar ? 0.6f : 1f);
        public bool IsBoss => kind == EnemyKind.Chief;

        public static Enemy Spawn(EnemyKind kind, Vector3 pos, int zone)
        {
            var g = new GameObject(kind.ToString());
            g.transform.position = World.Ground(pos);
            g.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            var e = g.AddComponent<Enemy>();
            e.kind = kind; e.zone = zone; e.isEnemy = true; e.home = g.transform.position;
            e.Setup();
            return e;
        }

        void Setup()
        {
            terrain = Terrain.activeTerrain;
            switch (kind)
            {
                case EnemyKind.Bandit:
                    displayName = "ចោរព្រៃ"; hpMax = 70; dmg = 10; speed = 4.6f; aggro = 16; range = 2.2f; cooldown = 1.4f;
                    hRig = HumanRig.Build(transform, new HumanStyle
                    {
                        skin = new Color(0.55f, 0.38f, 0.26f), top = new Color(0.12f, 0.12f, 0.13f), holBg = new Color(0.18f, 0.16f, 0.14f), holFg = new Color(0.35f, 0.3f, 0.25f), holAccent = new Color(0.4f, 0.1f, 0.1f),
                        kramaA = new Color(0.1f, 0.1f, 0.1f), kramaB = new Color(0.45f, 0.45f, 0.45f), trim = new Color(0.4f, 0.35f, 0.3f), mask = true, weapon = Weapon.Sword, blade = new Color(0.55f, 0.55f, 0.58f)
                    });
                    break;
                case EnemyKind.Archer:
                    displayName = "ចោរបាញ់ធ្នូ"; hpMax = 55; dmg = 8; speed = 4.2f; aggro = 24; range = 18f; cooldown = 2.2f;
                    hRig = HumanRig.Build(transform, new HumanStyle
                    {
                        skin = new Color(0.55f, 0.38f, 0.26f), top = new Color(0.2f, 0.17f, 0.12f), holBg = new Color(0.2f, 0.18f, 0.12f), holFg = new Color(0.35f, 0.3f, 0.2f), holAccent = new Color(0.3f, 0.3f, 0.1f),
                        kramaA = new Color(0.1f, 0.1f, 0.1f), kramaB = new Color(0.5f, 0.4f, 0.3f), trim = new Color(0.4f, 0.35f, 0.3f), mask = true, weapon = Weapon.Bow
                    });
                    break;
                case EnemyKind.Chief:
                    displayName = "មេចោរដៃដែក"; hpMax = 650; dmg = 18; speed = 4.8f; aggro = 30; range = 3.2f; cooldown = 1.3f; leash = 80;
                    hRig = HumanRig.Build(transform, new HumanStyle
                    {
                        skin = new Color(0.5f, 0.34f, 0.22f), top = new Color(0.08f, 0.08f, 0.09f), holBg = new Color(0.35f, 0.05f, 0.05f), holFg = new Color(0.1f, 0.1f, 0.1f), holAccent = new Color(0.8f, 0.6f, 0.2f),
                        kramaA = new Color(0.7f, 0.05f, 0.05f), kramaB = new Color(0.1f, 0.1f, 0.1f), trim = new Color(0.6f, 0.55f, 0.5f), scale = 1.35f, bulk = 1.25f, weapon = Weapon.Sword, bigBlade = true, blade = new Color(0.45f, 0.45f, 0.5f)
                    });
                    break;
                case EnemyKind.Boar:
                    displayName = "ជ្រូកព្រៃ"; hpMax = 60; dmg = 12; speed = 5.2f; aggro = 14; range = 1.8f; cooldown = 1.6f;
                    bRig = BeastRig.Build(transform, BeastKind.Boar);
                    break;
                case EnemyKind.Tiger:
                    displayName = "ខ្លាធំ"; hpMax = 160; dmg = 20; speed = 6.4f; aggro = 20; range = 2.4f; cooldown = 1.5f; leash = 35;
                    bRig = BeastRig.Build(transform, BeastKind.Tiger, 1.15f);
                    break;
                case EnemyKind.Croc:
                    displayName = "ក្រពើ"; hpMax = 130; dmg = 22; speed = 3.6f; aggro = 9; range = 2.8f; cooldown = 2f; leash = 9;
                    bRig = BeastRig.Build(transform, BeastKind.Croc);
                    break;
            }
            hp = hpMax;
            if (hRig != null) hRig.refSpeed = speed; else bRig.refSpeed = speed;
            flash = gameObject.AddComponent<Flash>();
            float barH = kind == EnemyKind.Chief ? 3f : kind == EnemyKind.Croc ? 1.2f : kind == EnemyKind.Boar ? 1.5f : 2.3f;
            if (kind != EnemyKind.Chief) bar = HpBar.Make(transform, barH, kind == EnemyKind.Tiger || kind == EnemyKind.Croc ? 1.5f : 1.1f);
            // collider ធំមួយ សម្រាប់ព្រួញ និងកាមេរ៉ា
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.isTrigger = true; col.radius = Radius; col.height = kind == EnemyKind.Croc ? 1f : 2f; col.center = Vector3.up * (col.height / 2);
            if (kind == EnemyKind.Croc || kind == EnemyKind.Tiger || kind == EnemyKind.Boar) col.direction = 2;
            if (kind == EnemyKind.Croc || kind == EnemyKind.Tiger || kind == EnemyKind.Boar) col.height = kind == EnemyKind.Croc ? 3f : 2f;
            wanderTo = home;
            atkReady = Time.time + Random.Range(0.5f, 1.5f);
        }

        void SetAnim(float s)
        {
            if (hRig != null) hRig.moveSpeed = s; else bRig.moveSpeed = s;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (deadT >= 0)
            {
                deadT += dt;
                if (deadT > 2.5f) transform.position += Vector3.down * dt * 0.5f;
                if (deadT > 5f) Destroy(gameObject);
                return;
            }
            var pl = Player.I;
            if (pl == null || Game.I == null) return;
            if (!Game.I.Playing) { SetAnim(0); return; }

            stunT -= dt;
            if (bar) bar.frac = hp / hpMax;
            Vector3 move = Vector3.zero;
            var toP = pl.transform.position - transform.position; toP.y = 0;
            float dist = toP.magnitude;
            float homeDist = Vector3.Distance(Flat(transform.position), Flat(home));
            bool canSee = pl.Alive && dist < aggro && Mathf.Abs(pl.transform.position.y - transform.position.y) < 6;
            if (canSee) alerted = true;
            if (alerted && (!pl.Alive || dist > aggro * 2.2f || homeDist > leash)) alerted = false;

            // ការវាយដែលកំពុងរង់ចាំ (telegraph)
            if (hitAt >= 0 && Time.time >= hitAt)
            {
                hitAt = -1;
                ResolveHit(pl, dist);
            }

            if (stunT > 0) { SetAnim(0); }
            else if (chargeT >= 0)
            {
                chargeT -= dt;
                move = chargeDir * speed * 2.6f;
                if (dist < Radius + 1.1f && pl.Alive) { pl.Damage(dmg * 1.3f, transform.position, 6f); chargeT = -1; atkReady = Time.time + cooldown; }
            }
            else if (alerted && pl.Alive)
            {
                // មេចោរ៖ ខឹងពេលឈាមនៅពាក់កណ្ដាល
                if (kind == EnemyKind.Chief && !enraged && hp < hpMax * 0.5f)
                {
                    enraged = true; speed *= 1.25f; cooldown *= 0.7f;
                    UI.I.Toast("មេចោរខឹងខ្លាំង! វាហៅគ្នាមកជួយ!", 2.5f);
                    Sfx.I?.Play("roar");
                    if (!summoned) { summoned = true; for (int i = 0; i < 2; i++) Game.I.SpawnExtra(EnemyKind.Bandit, transform.position + new Vector3(i == 0 ? -6 : 6, 0, 4), zone); }
                }
                Face(toP, dt);
                bool busy = hitAt >= 0;
                bool ranged = kind == EnemyKind.Archer;
                if (!busy)
                {
                    if (Time.time >= atkReady && dist <= range) StartAttack();
                    else if (Time.time >= specialReady && (kind == EnemyKind.Boar || kind == EnemyKind.Chief) && dist > 5 && dist < 13) StartCharge(toP);
                    else if (Time.time >= specialReady && kind == EnemyKind.Tiger && dist > 4 && dist < 10) StartPounce(toP);
                    else if (ranged && dist < 8) move = -toP.normalized * speed * 0.7f; // ថយចេញ
                    else if (dist > range * 0.85f) move = toP.normalized * speed;
                    // កុំឈរជាន់គ្នា
                    move += Separation() * 2f;
                }
            }
            else
            {
                // ដើរលេងជុំវិញផ្ទះ ឬត្រឡប់ទៅផ្ទះ
                if (homeDist > leash * 0.6f) wanderTo = home;
                wanderT -= dt;
                if (wanderT <= 0)
                {
                    wanderT = Random.Range(3f, 7f);
                    var r = Random.insideUnitCircle * Mathf.Min(6f, leash * 0.5f);
                    wanderTo = home + new Vector3(r.x, 0, r.y);
                    if (hp < hpMax) hp = Mathf.Min(hpMax, hp + hpMax * 0.25f);
                }
                var tw = Flat(wanderTo) - Flat(transform.position);
                if (tw.magnitude > 0.6f) { move = tw.normalized * speed * 0.35f; Face(tw, dt); }
            }

            move += knockVel;
            knockVel = Vector3.MoveTowards(knockVel, Vector3.zero, dt * 20f);
            var next = transform.position + move * dt;
            // កុំចុះទឹក (លើកលែងតែក្រពើ) និងកុំឡើងជ្រលងចោត
            float gh = World.Height(next);
            float cur = World.Height(transform.position);
            bool blocked = (kind != EnemyKind.Croc && gh < World.WaterY + 0.2f) || gh - cur > 1.2f * Mathf.Max(0.2f, (move * dt).magnitude) + 0.4f || World.Blocked(next);
            if (!blocked) transform.position = new Vector3(next.x, gh, next.z);
            else { transform.position = new Vector3(transform.position.x, cur, transform.position.z); if (chargeT >= 0) chargeT = -1; }
            var flat = move; flat.y = 0;
            SetAnim(blocked ? 0 : flat.magnitude);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void Face(Vector3 dir, float dt)
        {
            dir.y = 0; if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1 - Mathf.Exp(-dt * 8f));
        }

        Vector3 Separation()
        {
            Vector3 s = Vector3.zero;
            foreach (var a in All)
            {
                if (a == this || a == null || !a.isEnemy || !a.Alive) continue;
                var d = transform.position - a.transform.position; d.y = 0;
                float m = d.magnitude;
                if (m < 1.6f && m > 0.01f) s += d / m * (1.6f - m);
            }
            return s;
        }

        void StartAttack()
        {
            atkReady = Time.time + cooldown;
            float wind = kind == EnemyKind.Croc ? 0.6f : kind == EnemyKind.Chief ? (enraged ? 0.45f : 0.6f) : kind == EnemyKind.Archer ? 0.7f : 0.5f;
            hitAt = Time.time + wind;
            if (hRig != null) hRig.Attack(Random.Range(0, 3), wind + 0.35f); else bRig.Attack(wind + 0.3f);
            flash.Hit(new Color(1f, 0.85f, 0.4f), 0.15f); // សញ្ញាប្រាប់មុនវាយ
            if (kind == EnemyKind.Tiger || kind == EnemyKind.Croc) Sfx.I?.Play("roar", 0.5f);
        }

        void ResolveHit(Player pl, float dist)
        {
            if (!pl.Alive) return;
            if (kind == EnemyKind.Archer)
            {
                var sp = transform.position + Vector3.up * 1.45f + transform.forward * 0.6f;
                var tp = pl.Center + Vector3.up * 0.1f;
                Arrow.Fire(sp, (tp - sp).normalized, 26f, dmg, true);
                Sfx.I?.Play("arrow", 0.5f);
                return;
            }
            var toP = pl.transform.position - transform.position; toP.y = 0;
            float arc = kind == EnemyKind.Chief && enraged ? 360 : 120;
            if (dist <= range + 0.7f && (arc >= 359 || Vector3.Angle(transform.forward, toP) < arc / 2))
            {
                pl.Damage(dmg, transform.position, kind == EnemyKind.Chief ? 6f : 3f);
                Sfx.I?.Play("hit", 0.7f);
            }
            if (kind == EnemyKind.Chief && enraged) { Combat.Ring(transform.position, range + 0.7f, new Color(1f, 0.3f, 0.2f)); hRig.Spin(); }
        }

        void StartCharge(Vector3 toP)
        {
            specialReady = Time.time + Random.Range(5f, 8f);
            chargeDir = toP.normalized; chargeT = 0.9f;
            flash.Hit(new Color(1f, 0.6f, 0.3f), 0.3f);
            Sfx.I?.Play(kind == EnemyKind.Boar ? "roar" : "swing", 0.6f);
            if (kind == EnemyKind.Chief) UI.I.Toast("ប្រយ័ត្ន! មេចោរស្ទុះមក — ចុច Shift ដើម្បីគេច", 1.6f);
        }

        void StartPounce(Vector3 toP)
        {
            specialReady = Time.time + Random.Range(5f, 7f);
            chargeDir = toP.normalized; chargeT = 0.55f;
            bRig.Attack(0.6f);
            Sfx.I?.Play("roar", 0.8f);
        }

        public override void Damage(float d, Vector3 from, float knock = 0, float stun = 0)
        {
            if (!Alive) return;
            hp -= d;
            alerted = true;
            flash.Hit(new Color(1f, 0.3f, 0.3f));
            var dir = transform.position - from; dir.y = 0;
            float resist = kind == EnemyKind.Chief ? 0.3f : kind == EnemyKind.Croc ? 0.4f : 1f;
            if (dir.sqrMagnitude > 0.001f) knockVel = dir.normalized * knock * resist * 1.6f;
            if (stun > 0) { stunT = stun * resist; hitAt = -1; chargeT = -1; }
            if (hRig != null) hRig.Hurt();
            UI.I.DamageNumber(Center + Vector3.up * 0.6f, Mathf.RoundToInt(d));
            if (hp <= 0) Die();
        }

        void Die()
        {
            hp = 0; deadT = 0;
            if (hRig != null) hRig.dead = true; else bRig.dead = true;
            if (bar) Destroy(bar.gameObject);
            var col = GetComponent<Collider>(); if (col) col.enabled = false;
            Player.I.coins += kind == EnemyKind.Chief ? 50 : kind == EnemyKind.Tiger || kind == EnemyKind.Croc ? 8 : 4;
            if (Random.value < (kind == EnemyKind.Bandit || kind == EnemyKind.Archer ? 0.35f : 0.25f)) Pickup.Spawn(transform.position + transform.forward * 0.8f, true);
            onDeath?.Invoke(this);
            All.Remove(this);
        }

        void StopAndHome() { alerted = false; }
    }
}
