// អ្នកលេងម្នាក់ៗតាមបណ្ដាញ (ត្រូវនៅក្នុងឯកសារឈ្មោះដូចថ្នាក់ ដើម្បីឲ្យ Unity ភ្ជាប់ទៅ prefab បាន)
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace KhmerHeroes
{
    /// <summary>អ្នកលេងម្នាក់ៗតាមបណ្ដាញ — ផ្ញើទីតាំង (NetworkTransform) ឈ្មោះ វីរជន និងលទ្ធផល</summary>
    public class NetPlayer : NetworkBehaviour
    {
        public NetworkVariable<int> hero = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<FixedString64Bytes> pname = new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<float> anim = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<int> cp = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<float> finish = new NetworkVariable<float>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<int> falls = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        // ស្ថានភាពរបស់ម្ចាស់បន្ទប់ (មានតែលើ player របស់ host)
        public NetworkVariable<int> mode = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<double> startAt = new NetworkVariable<double>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> round = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        CourseRunner runner;
        bool IsHostObject => OwnerClientId == NetworkManager.ServerClientId;

        static FixedString64Bytes Fit(string s)
        {
            s = s ?? "";
            while (System.Text.Encoding.UTF8.GetByteCount(s) > 58 && s.Length > 0) s = s.Substring(0, s.Length - 1);
            return new FixedString64Bytes(s);
        }

        public override void OnNetworkSpawn()
        {
            int slot = (int)(OwnerClientId % 4);
            runner = gameObject.AddComponent<CourseRunner>();
            runner.net = this;
            if (IsOwner)
            {
                hero.Value = Net.pendingHero;
                pname.Value = Fit(Net.pendingName);
                runner.Init(true, Net.pendingHero, Net.pendingName, slot);
                runner.Teleport(CourseManager.I != null ? CourseManager.I.SpawnPoint(slot) : Vector3.up * 3);
                if (IsServer) { mode.Value = (int)Net.pendingMode; if (CourseManager.I) CourseManager.I.mode = Net.pendingMode; }
                UI.I?.Toast("បានចូលកោះឧបសគ្គ!", 2f);
            }
            else
            {
                runner.Init(false, hero.Value, pname.Value.ToString(), slot);
                UI.I?.Toast(pname.Value.ToString() + " បានចូលលេង!", 2.5f);
                Sfx.I?.Play("pick");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner && runner != null) UI.I?.Toast(runner.displayName + " បានចាកចេញ", 2.5f);
            if (runner != null) Destroy(runner);
        }

        public void WriteHostState()
        {
            var cm = CourseManager.I; if (cm == null || !IsServer) return;
            mode.Value = (int)cm.mode; startAt.Value = cm.startAt; round.Value = cm.round;
        }

        void Update()
        {
            if (runner == null || !IsSpawned) return;
            if (IsOwner)
            {
                if (Mathf.Abs(anim.Value - runner.animSpeed) > 0.3f) anim.Value = runner.animSpeed;
                if (cp.Value != runner.checkpoint) cp.Value = runner.checkpoint;
                if (!Mathf.Approximately(finish.Value, runner.finishTime)) finish.Value = runner.finishTime;
                if (falls.Value != runner.falls) falls.Value = runner.falls;
            }
            else
            {
                runner.SetHero(hero.Value);
                runner.displayName = pname.Value.ToString();
                runner.animSpeed = anim.Value; runner.checkpoint = cp.Value; runner.finishTime = finish.Value; runner.falls = falls.Value;
            }
            // អ្នកចូលលេង អានស្ថានភាពពីម្ចាស់បន្ទប់
            var cm = CourseManager.I;
            if (cm != null && IsHostObject && !IsServer)
            {
                cm.mode = (CourseMode)mode.Value;
                if (round.Value != cm.round) { cm.round = round.Value; cm.startAt = startAt.Value; cm.OnNewRound(); }
                else cm.startAt = startAt.Value;
            }
        }

        public void Push(CourseRunner target, Vector3 dir)
        {
            if (target == null || target.net == null) return;
            PushRequestRpc(new NetworkObjectReference(target.net.NetworkObject), dir);
        }

        [Rpc(SendTo.Server)]
        void PushRequestRpc(NetworkObjectReference target, Vector3 dir)
        {
            if (target.TryGet(out var no))
            {
                var np = no.GetComponent<NetPlayer>();
                if (np != null) np.PushedRpc(dir);
            }
        }

        [Rpc(SendTo.Owner)]
        void PushedRpc(Vector3 dir) { if (runner != null) runner.Pushed(dir); }

        [Rpc(SendTo.Everyone)]
        public void EmoteRpc(int i) { if (runner != null) runner.ShowEmote(i); }
    }
}
