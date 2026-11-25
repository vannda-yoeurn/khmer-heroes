// លេងតាមអ៊ីនធឺណិត៖ Netcode for GameObjects + Unity Relay (លេខកូដបន្ទប់) ឬ LAN (IP)
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace KhmerHeroes
{
    public static class Net
    {
        public static string JoinCode;
        public static int pendingHero;
        public static string pendingName = "";
        public static CourseMode pendingMode = CourseMode.Coop;
        static ISession session;
        public const ushort Port = 7777;

        public static bool Online => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        static NetworkManager Ensure()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null) return nm;
            var prefab = Resources.Load<GameObject>("NetPlayer");
            if (prefab == null) throw new Exception("រកមិនឃើញ NetPlayer prefab — សូមចុចម៉ឺនុយ KhmerHeroes > Setup ក្នុង Unity");
            var go = new GameObject("NetworkManager");
            var ut = go.AddComponent<UnityTransport>();
            nm = go.AddComponent<NetworkManager>();
            nm.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = ut,
                PlayerPrefab = prefab,
                EnableSceneManagement = false,
                ConnectionApproval = false,
            };
            try { nm.AddNetworkPrefab(prefab); } catch (Exception e) { Debug.Log("AddNetworkPrefab: " + e.Message); }
            nm.OnClientDisconnectCallback += id =>
            {
                var m = NetworkManager.Singleton;
                if (m == null) return;
                if (!m.IsServer && (id == m.LocalClientId || id == NetworkManager.ServerClientId)) Game.I?.OnNetDisconnected();
            };
            return nm;
        }

        static async Task Services()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                // profile ចៃដន្យ ដើម្បីអាចសាកលេង ២ នាក់លើកុំព្យូទ័រតែមួយបាន
                var opts = new InitializationOptions();
                opts.SetProfile("p" + UnityEngine.Random.Range(1000, 99999));
                await UnityServices.InitializeAsync(opts);
            }
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        /// <summary>បង្កើតបន្ទប់តាមអ៊ីនធឺណិត (Relay) — ទទួលបានលេខកូដ ៦ តួ</summary>
        public static async Task HostRelay()
        {
            Ensure();
            await Services();
            var options = new SessionOptions { MaxPlayers = 4 }.WithRelayNetwork();
            session = await MultiplayerService.Instance.CreateSessionAsync(options);
            JoinCode = session.Code;
        }

        public static async Task JoinRelay(string code)
        {
            Ensure();
            await Services();
            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant());
            JoinCode = session.Code;
        }

        /// <summary>បង្កើតបន្ទប់ក្នុងបណ្ដាញតែមួយ (Wi-Fi/LAN)</summary>
        public static void HostLan()
        {
            var nm = Ensure();
            var ut = nm.GetComponent<UnityTransport>();
            ut.SetConnectionData("0.0.0.0", Port, "0.0.0.0");
            if (!nm.StartHost()) throw new Exception("មិនអាចបើកបន្ទប់ LAN បានទេ");
            JoinCode = LocalIP();
        }

        public static void JoinLan(string ip)
        {
            var nm = Ensure();
            var ut = nm.GetComponent<UnityTransport>();
            ut.SetConnectionData(ip.Trim(), Port);
            if (!nm.StartClient()) throw new Exception("មិនអាចភ្ជាប់ទៅ " + ip + " បានទេ");
            JoinCode = ip.Trim();
        }

        public static async Task Shutdown()
        {
            var s = session; session = null; JoinCode = null;
            if (s != null) { try { await s.LeaveAsync(); } catch (Exception e) { Debug.Log("Leave: " + e.Message); } }
            var nm = NetworkManager.Singleton;
            if (nm != null)
            {
                if (nm.IsListening) nm.Shutdown();
                UnityEngine.Object.Destroy(nm.gameObject);
            }
        }

        public static string LocalIP()
        {
            try
            {
                foreach (var a in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a)) return a.ToString();
            }
            catch { }
            return "127.0.0.1";
        }

        /// <summary>ម្ចាស់បន្ទប់ផ្ញើស្ថានភាពការលេង (mode, ពេលចាប់ផ្ដើម) ទៅគ្រប់គ្នា</summary>
        public static void PushHostState()
        {
            if (!Online || !NetworkManager.Singleton.IsServer) return;
            var po = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (po == null) return;
            var np = po.GetComponent<NetPlayer>();
            if (np != null) np.WriteHostState();
        }

        public static string Explain(Exception e)
        {
            string m = e.Message ?? "";
            if (e.InnerException != null) m += " " + e.InnerException.Message;
            if (e is AggregateException ag) foreach (var x in ag.InnerExceptions) m += " " + x.Message;
            if (m.Contains("project ID") || m.Contains("couldn't be initialized") ||
                m.Contains("project") || m.Contains("Project") || m.Contains("cloud") || m.Contains("Unauthorized") || m.Contains("environment"))
                return "គម្រោងនេះមិនទាន់ភ្ជាប់ទៅ Unity Cloud ទេ។ សូមបើក Edit > Project Settings > Services ហើយភ្ជាប់ (Link) ទៅគម្រោង Unity Cloud ជាមុនសិន។ ឬសាកប្រើ LAN (Wi-Fi តែមួយ)។\n\n(" + m + ")";
            return "សូមពិនិត្យអ៊ីនធឺណិត ឬលេខកូដបន្ទប់ ហើយសាកម្ដងទៀត។\n\n(" + m + ")";
        }
    }
}
