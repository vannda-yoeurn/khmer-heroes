// អ្នកលេងនៅកោះឧបសគ្គ (ទាំងខ្លួនឯង និងមិត្តភក្ដិតាមអ៊ីនធឺណិត)
using UnityEngine;

namespace KhmerHeroes
{
    public class CourseRunner : MonoBehaviour
    {
        public bool isLocal;
        public int hero = -1, slot, checkpoint, falls;
        public string displayName = "";
        public float finishTime = -1, animSpeed;
        public NetPlayer net;
        HumanRig rig; Transform body; CharacterController cc; Flash flash;
        Vector3 knockVel; float vy, hitCd, pushCd; int builtHero = -1;
        public static readonly string[] Emotes = { "ហាហា!", "មកជួយផង!", "ទៅមុខ!", "សុំទោស!" };

        public void Init(bool local, int heroIndex, string name, int slotIndex)
        {
            isLocal = local; slot = slotIndex; displayName = string.IsNullOrEmpty(name) ? Data.Heroes[Mathf.Clamp(heroIndex, 0, 3)].name : name;
            SetHero(heroIndex);
            if (local)
            {
                cc = gameObject.GetComponent<CharacterController>();
                if (cc == null) cc = gameObject.AddComponent<CharacterController>();
                cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.92f, 0); cc.slopeLimit = 50; cc.stepOffset = 0.5f;
                gameObject.layer = 2;
                if (CamRig.I) { CamRig.I.target = transform; CamRig.I.yaw = 0; CamRig.I.pitch = 18; }
            }
            else
            {
                var col = gameObject.AddComponent<CapsuleCollider>();
                col.height = 1.8f; col.radius = 0.4f; col.center = new Vector3(0, 0.9f, 0);
            }
            if (CourseManager.I != null && !CourseManager.I.runners.Contains(this)) CourseManager.I.runners.Add(this);
        }

        public void SetHero(int h)
        {
            h = Mathf.Clamp(h, 0, Data.Heroes.Length - 1);
            if (h == builtHero) return;
            builtHero = h; hero = h;
            if (body) Destroy(body.gameObject);
            body = new GameObject("Body").transform; body.SetParent(transform, false);
            rig = HumanRig.Build(body, HumanStyle.From(Data.Heroes[h]));
            rig.refSpeed = 7.5f;
            flash = body.gameObject.AddComponent<Flash>();
        }

        void OnDestroy()
        {
            if (CourseManager.I != null) CourseManager.I.runners.Remove(this);
            if (UI.I != null) UI.I.RemoveTag(this);
        }

        public void Teleport(Vector3 p)
        {
            if (cc) cc.enabled = false;
            transform.position = p; vy = 0; knockVel = Vector3.zero;
            if (cc) cc.enabled = true;
        }

        public void ResetForRound()
        {
            checkpoint = 0; finishTime = -1; falls = 0;
            Teleport(CourseManager.I.SpawnPoint(slot));
            transform.rotation = Quaternion.identity;
            if (CamRig.I) CamRig.I.yaw = 0;
        }

        public void Pushed(Vector3 dir)
        {
            if (!isLocal) return;
            dir.y = 0;
            knockVel = dir.normalized * 13f; vy = 6f;
            flash?.Hit(new Color(1f, 0.6f, 0.6f));
            Sfx.I?.Play("hurt", 0.7f);
            UI.I.FloatText(transform.position + Vector3.up * 2.4f, "ត្រូវគេរុញ!", new Color(1f, 0.6f, 0.5f));
        }

        public void ShowEmote(int i)
        {
            i = Mathf.Clamp(i, 0, Emotes.Length - 1);
            UI.I.FloatText(transform.position + Vector3.up * 2.6f, Emotes[i], new Color(1f, 0.95f, 0.6f));
            Sfx.I?.Play(i == 0 ? "boing" : i == 1 ? "bad" : i == 2 ? "go" : "pick", 0.5f);
            rig?.Attack(1, 0.4f);
        }

        void Update()
        {
            var cm = CourseManager.I;
            if (cm == null || rig == null) return;
            if (!isLocal)
            {
                rig.moveSpeed = animSpeed; rig.grounded = true;
                return;
            }
            float dt = Time.deltaTime;
            bool input = Game.I != null && Game.I.CourseInput;
            var mv = input ? InputX.Move() : Vector2.zero;
            var cam = CamRig.I;
            Vector3 wish = cam ? cam.Forward * mv.y + cam.Right * mv.x : Vector3.zero;
            const float speed = 7.5f;

            // ឈរលើក្បូនដែលផ្លាស់ទី / ស្គរលោត
            Vector3 carry = Vector3.zero;
            bool grounded = cc.isGrounded;
            if (Physics.Raycast(transform.position + Vector3.up * 0.4f, Vector3.down, out var hit, 0.8f, ~(1 << 2), QueryTriggerInteraction.Ignore))
            {
                var m = hit.collider.GetComponentInParent<Mover>();
                if (m != null) carry = m.Velocity;
                var drum = hit.collider.GetComponentInParent<BounceDrum>();
                if (drum != null && vy <= 0.5f && hit.distance < 0.6f)
                {
                    vy = drum.power; drum.Pop(); grounded = false;
                    UI.I.FloatText(transform.position + Vector3.up * 2.4f, "ប៉ោង!", new Color(1f, 0.85f, 0.4f));
                }
            }
            if (grounded && vy < 0) vy = -2f;
            if (input && InputX.Jump && grounded) { vy = 8.6f; Sfx.I?.Play("boing", 0.35f); }
            vy -= 24f * dt;

            // ឧបសគ្គ
            hitCd -= dt;
            if (hitCd <= 0)
                foreach (var o in Obstacle.All)
                {
                    if (o == null) continue;
                    if (o.HitTest(transform.position, out var knock))
                    {
                        knockVel = new Vector3(knock.x, 0, knock.z); vy = knock.y;
                        hitCd = 0.6f;
                        flash?.Hit(new Color(1f, 0.6f, 0.6f));
                        rig.Hurt();
                        Sfx.I?.Play("hurt", 0.7f);
                        string[] ouch = { "អូយ!", "ឈឺណាស់!", "ហោះហើយ!", "ម៉ាក់អើយ!" };
                        UI.I.FloatText(transform.position + Vector3.up * 2.4f, ouch[Random.Range(0, ouch.Length)], new Color(1f, 0.6f, 0.5f));
                        break;
                    }
                }

            var horiz = wish * speed + knockVel;
            knockVel = Vector3.MoveTowards(knockVel, Vector3.zero, dt * (grounded ? 30f : 8f));
            cc.Move((horiz + carry + Vector3.up * vy) * dt);
            if (wish.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(wish), 1 - Mathf.Exp(-dt * 14f));
            animSpeed = new Vector3(horiz.x, 0, horiz.z).magnitude;
            rig.moveSpeed = animSpeed; rig.grounded = cc.isGrounded;

            // ធ្លាក់ទឹក
            if (transform.position.y < 0.4f)
            {
                falls++;
                Sfx.I?.Play("slip", 0.6f);
                Teleport(cm.RespawnFor(this));
                string[] splash = { "ប៉ាច! ធ្លាក់ទឹក", "ទឹកត្រជាក់ណាស់!", "ងូតទឹកម្ដងទៀត!" };
                UI.I.FloatText(transform.position + Vector3.up * 2.4f, splash[falls % splash.Length], new Color(0.5f, 0.8f, 1f));
            }

            // ចំណុចត្រួតពិនិត្យ និងទីបញ្ចប់
            if (cm.Started)
            {
                int cp = cm.CheckpointAt(transform.position);
                if (cp > checkpoint) { checkpoint = cp; Sfx.I?.Play("pick"); UI.I.Toast("ចំណុចត្រួតពិនិត្យ " + Kh.Num(cp) + "/៥", 1.6f); }
                if (finishTime < 0 && cm.AtFinish(transform.position))
                {
                    finishTime = CourseManager.Now - (float)cm.startAt;
                    checkpoint = 5;
                    UI.I.Big("ដល់ពានមាសហើយ!", 2.5f);
                    Sfx.I?.Play("cheer");
                }
            }

            // រុញគូប្រកួត (ប្រកួតតែប៉ុណ្ណោះ)
            pushCd -= dt;
            if (input && cm.mode == CourseMode.Versus && InputX.Attack && pushCd <= 0)
            {
                pushCd = 0.8f;
                rig.Attack(1, 0.4f);
                Sfx.I?.Play("swing", 0.6f);
                foreach (var o in cm.runners)
                {
                    if (o == null || o == this) continue;
                    var d = o.transform.position - transform.position; d.y = 0;
                    if (d.magnitude < 2.4f && Vector3.Dot(d.normalized, transform.forward) > 0.2f)
                    {
                        if (net != null) net.Push(o, d.normalized);
                        else o.Pushed(d.normalized);
                        Sfx.I?.Play("hit", 0.7f);
                        break;
                    }
                }
            }

            // ពាក្យកំប្លែង (៥ ៦ ៧ ៨)
            if (input)
                for (int k = 0; k < 4; k++)
                    if (Input.GetKeyDown(KeyCode.Alpha5 + k))
                    {
                        if (net != null) net.EmoteRpc(k); else ShowEmote(k);
                    }
        }
    }
}
