// ការប្រយុទ្ធ៖ តួអង្គមានឈាម ការវាយលើតំបន់ ព្រួញ និងកាមេរ៉ា
using System.Collections.Generic;
using UnityEngine;

namespace KhmerHeroes
{
    public abstract class Actor : MonoBehaviour
    {
        public static readonly List<Actor> All = new List<Actor>();
        public bool isEnemy;
        public abstract bool Alive { get; }
        public virtual Vector3 Center => transform.position + Vector3.up * 1f;
        public virtual float Radius => 0.5f;
        public abstract void Damage(float dmg, Vector3 from, float knock = 0, float stun = 0);
        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }
    }

    public static class Combat
    {
        /// <summary>វាយលើតំបន់ជារាងកង្ហារ ឬជារង្វង់ (arc = 360)</summary>
        public static int Hit(Vector3 origin, Vector3 forward, float radius, float arcDeg, float dmg, bool hitEnemies, float knock = 0, float stun = 0, HashSet<Actor> skip = null)
        {
            int n = 0;
            forward.y = 0; forward.Normalize();
            for (int i = Actor.All.Count - 1; i >= 0; i--)
            {
                if (i >= Actor.All.Count) continue;
                var a = Actor.All[i];
                if (a == null || !a.Alive || a.isEnemy != hitEnemies) continue;
                if (skip != null && skip.Contains(a)) continue;
                var d = a.transform.position - origin; d.y = 0;
                if (Mathf.Abs(a.transform.position.y - origin.y) > 3f) continue;
                if (d.magnitude > radius + a.Radius) continue;
                if (arcDeg < 359 && d.sqrMagnitude > 0.01f && Vector3.Angle(forward, d) > arcDeg / 2) continue;
                a.Damage(dmg, origin, knock, stun);
                skip?.Add(a);
                n++;
            }
            return n;
        }

        public static Actor Nearest(Vector3 p, float maxDist, bool enemies)
        {
            Actor best = null; float bd = maxDist;
            foreach (var a in Actor.All)
            {
                if (a == null || !a.Alive || a.isEnemy != enemies) continue;
                float d = Vector3.Distance(a.transform.position, p);
                if (d < bd) { bd = d; best = a; }
            }
            return best;
        }

        /// <summary>រលកធូលីពេលបោកដី</summary>
        public static void Ring(Vector3 at, float radius, Color c)
        {
            var g = Shapes.Glow(PrimitiveType.Cylinder, null, at + Vector3.up * 0.1f, new Vector3(0.5f, 0.02f, 0.5f), c, 1.5f);
            g.AddComponent<RingFx>().max = radius * 2;
        }
    }

    public class RingFx : MonoBehaviour
    {
        public float max = 6; float t;
        void Update()
        {
            t += Time.deltaTime / 0.35f;
            float s = Mathf.Lerp(0.5f, max, t);
            transform.localScale = new Vector3(s, 0.02f, s);
            if (t >= 1) Destroy(gameObject);
        }
    }

    /// <summary>ព្រួញ</summary>
    public class Arrow : MonoBehaviour
    {
        Vector3 vel; float dmg, life = 2.5f; bool fromEnemy;
        public static void Fire(Vector3 from, Vector3 dir, float speed, float dmg, bool fromEnemy)
        {
            var g = new GameObject("Arrow");
            g.transform.position = from;
            g.transform.rotation = Quaternion.LookRotation(dir);
            var shaft = Shapes.Make(PrimitiveType.Cylinder, g.transform, Vector3.zero, new Vector3(0.03f, 0.4f, 0.03f), new Color(0.45f, 0.3f, 0.15f), false);
            shaft.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, 0, 0.42f), new Vector3(0.06f, 0.06f, 0.1f), new Color(0.7f, 0.7f, 0.72f), false);
            Shapes.Make(PrimitiveType.Cube, g.transform, new Vector3(0, 0, -0.35f), new Vector3(0.1f, 0.01f, 0.12f), Color.white, false);
            Shapes.Glow(PrimitiveType.Sphere, g.transform, new Vector3(0, 0, -0.3f), Vector3.one * 0.08f, new Color(1f, 0.9f, 0.5f), 1.2f);
            var a = g.AddComponent<Arrow>();
            a.vel = dir.normalized * speed; a.dmg = dmg; a.fromEnemy = fromEnemy;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            vel += Vector3.down * 3f * dt;
            var step = vel * dt;
            if (Physics.Raycast(transform.position, step.normalized, out var hit, step.magnitude + 0.1f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<Actor>() == null) { transform.position = hit.point; enabled = false; Destroy(gameObject, 3f); return; }
            }
            transform.position += step;
            transform.rotation = Quaternion.LookRotation(vel);
            foreach (var a in Actor.All)
            {
                if (a == null || !a.Alive || a.isEnemy == fromEnemy) continue;
                if (Vector3.Distance(a.Center, transform.position) < a.Radius + 0.5f)
                {
                    a.Damage(dmg, transform.position - vel.normalized, 2f, 0);
                    Destroy(gameObject);
                    return;
                }
            }
            life -= dt; if (life <= 0) Destroy(gameObject);
        }
    }

    /// <summary>កាមេរ៉ាតាមក្រោយតួអង្គ (third-person)</summary>
    public class CamRig : MonoBehaviour
    {
        public static CamRig I;
        public Transform target;
        public float yaw, pitch = 15, dist = 6.5f;
        float shake; Camera cam;
        public static float Sensitivity = 2.2f;

        void Awake() { I = this; cam = GetComponent<Camera>(); }
        public void Shake(float s) { shake = Mathf.Max(shake, s); }
        public Vector3 Forward => Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        public Vector3 Right => Quaternion.Euler(0, yaw, 0) * Vector3.right;

        void LateUpdate()
        {
            if (!target) return;
            if (Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0)
            {
                yaw += InputX.MouseX * Sensitivity;
                pitch = Mathf.Clamp(pitch - InputX.MouseY * Sensitivity, -25, 60);
                dist = Mathf.Clamp(dist - Input.mouseScrollDelta.y * 0.6f, 3f, 11f);
            }
            var rot = Quaternion.Euler(pitch, yaw, 0);
            float seat = Player.I != null && Player.I.Riding ? Player.I.mount.rig.seatY : 0;
            var pivot = target.position + Vector3.up * (1.6f + seat * 0.9f);
            var want = pivot - rot * Vector3.forward * (dist + seat * 1.2f) + rot * Vector3.right * 0.5f;
            var dir = want - pivot;
            float d = dir.magnitude;
            if (Physics.SphereCast(pivot, 0.3f, dir.normalized, out var hit, d, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<Actor>() == null)
                want = pivot + dir.normalized * Mathf.Max(0.8f, hit.distance - 0.1f);
            var t = Terrain.activeTerrain;
            if (t) { float gh = t.SampleHeight(want) + t.transform.position.y + 0.4f; if (want.y < gh) want.y = gh; }
            transform.position = want;
            transform.rotation = rot;
            if (shake > 0)
            {
                transform.position += Random.insideUnitSphere * shake * 0.25f;
                shake = Mathf.MoveTowards(shake, 0, Time.unscaledDeltaTime * 2.5f);
            }
        }

        /// <summary>ចំណុចដែលកណ្ដាលអេក្រង់សម្លឹងទៅ (សម្រាប់បាញ់ព្រួញ)</summary>
        public Vector3 AimPoint(float max = 60)
        {
            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.55f, 0));
            float best = max; Vector3 p = ray.origin + ray.direction * max;
            foreach (var h in Physics.RaycastAll(ray, max, ~0, QueryTriggerInteraction.Ignore))
            {
                if (target && h.collider.transform.IsChildOf(target)) continue;
                if (h.distance < dist || h.distance >= best) continue;
                best = h.distance; p = h.point;
            }
            return p;
        }
    }
}
