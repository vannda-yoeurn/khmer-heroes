// រៀបចំគម្រោងដោយស្វ័យប្រវត្តិ (ដំណើរការតែក្នុង Unity Editor)
// - បង្កើត Scene "Main" ហើយដាក់ក្នុង Build Settings
// - ដាក់ Shader ដែលហ្គេមប្រើ ក្នុង "Always Included Shaders" ដើម្បីកុំឲ្យ Build ចេញពណ៌ផ្កាឈូក
// - បើក Input Manager ចាស់ (ហ្គេមប្រើ Input.GetKey)
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KhmerHeroes.EditorTools
{
    [InitializeOnLoad]
    public static class KhmerSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        static KhmerSetup() { EditorApplication.delayCall += Run; }

        [MenuItem("KhmerHeroes/Setup")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            PlayerSettings.companyName = "Deokairos";
            PlayerSettings.productName = "វីរជនខ្មែរ";
            PlayerSettings.runInBackground = true;                 // លេងអនឡាញ៖ កុំឈប់ពេលបង្អួចមិនផ្ដោត
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;

            if (!System.IO.File.Exists(ScenePath))
            {
                System.IO.Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
                Debug.Log("[វីរជនខ្មែរ] បានបង្កើត " + ScenePath + " — ចុច Play ដើម្បីលេង");
            }
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == ScenePath))
            {
                list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = list.ToArray();
            }
            var active = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path) && !active.isDirty) EditorSceneManager.OpenScene(ScenePath);

            IncludeShaders("Standard", "Sprites/Default", "Skybox/Procedural", "Nature/Terrain/Standard", "Nature/Terrain/Diffuse",
                           "Hidden/TerrainEngine/Details/WavingDoublePass", "Hidden/TerrainEngine/Details/BillboardWavingDoublePass",
                           "Hidden/TerrainEngine/Splatmap/Standard-AddPass", "Hidden/TerrainEngine/Splatmap/Standard-Base", "Hidden/TerrainEngine/Splatmap/Diffuse-AddPass", "Hidden/TerrainEngine/Splatmap/Diffuse-Base", "Diffuse");
            EnsureLegacyInput();
            BuildNetPrefab();
        }

        [MenuItem("KhmerHeroes/Build Windows (.exe)")]
        public static void BuildWindows()
        {
            Run();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/KhmerHeroes.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var rep = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[វីរជនខ្មែរ] Build: " + rep.summary.result + " → " + opts.locationPathName);
            if (rep.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) return;
            // បង្កើតឯកសារ zip សម្រាប់ចែកចាយ (ដកថតសម្រាប់ debug ចេញ)
            const string zip = "Builds/KhmerHeroes-Windows.zip";
            try
            {
                if (System.IO.File.Exists(zip)) System.IO.File.Delete(zip);
                var src = System.IO.Path.GetFullPath("Builds/Windows");
                using (var fs = new System.IO.FileStream(zip, System.IO.FileMode.Create))
                using (var za = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create))
                {
                    foreach (var f in System.IO.Directory.GetFiles(src, "*", System.IO.SearchOption.AllDirectories))
                    {
                        var rel = f.Substring(src.Length + 1).Replace('\\', '/');
                        if (rel.Contains("DontShipItWithYourGame")) continue;
                        var entry = za.CreateEntry("KhmerHeroes/" + rel, System.IO.Compression.CompressionLevel.Optimal);
                        using (var es = entry.Open())
                        using (var inp = System.IO.File.OpenRead(f)) inp.CopyTo(es);
                    }
                }
                Debug.Log("[វីរជនខ្មែរ] បានបង្កើត " + zip);
                EditorUtility.RevealInFinder(zip);
            }
            catch (System.Exception ex) { Debug.LogWarning("Zip: " + ex.Message); EditorUtility.RevealInFinder(opts.locationPathName); }
        }

        const string NetPrefabPath = "Assets/Resources/NetPlayer.prefab";

        /// <summary>បង្កើត prefab សម្រាប់អ្នកលេងតាមអ៊ីនធឺណិត (NetworkObject + NetworkTransform + NetPlayer)</summary>
        static void BuildNetPrefab()
        {
            if (System.IO.File.Exists(NetPrefabPath))
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(NetPrefabPath);
                if (existing != null && existing.GetComponent<KhmerHeroes.NetPlayer>() != null) return;
                AssetDatabase.DeleteAsset(NetPrefabPath);
            }
            var go = new GameObject("NetPlayer");
            go.AddComponent<Unity.Netcode.NetworkObject>();
            var nt = go.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            nt.AuthorityMode = Unity.Netcode.Components.NetworkTransform.AuthorityModes.Owner;
            nt.SyncRotAngleX = false; nt.SyncRotAngleZ = false;
            nt.SyncScaleX = false; nt.SyncScaleY = false; nt.SyncScaleZ = false;
            go.AddComponent<KhmerHeroes.NetPlayer>();
            System.IO.Directory.CreateDirectory("Assets/Resources");
            PrefabUtility.SaveAsPrefabAsset(go, NetPrefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
            Debug.Log("[វីរជនខ្មែរ] បានបង្កើត " + NetPrefabPath);
        }

        static void IncludeShaders(params string[] names)
        {
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (gs == null || gs.Length == 0) return;
            var so = new SerializedObject(gs[0]);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) return;
            bool changed = false;
            foreach (var n in names)
            {
                var sh = Shader.Find(n);
                if (sh == null) continue;
                bool found = false;
                for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) { found = true; break; }
                if (found) continue;
                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
                changed = true;
            }
            if (changed) { so.ApplyModifiedProperties(); AssetDatabase.SaveAssets(); }
        }

        static void EnsureLegacyInput()
        {
            var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (ps == null || ps.Length == 0) return;
            var so = new SerializedObject(ps[0]);
            var p = so.FindProperty("activeInputHandler");
            if (p == null || p.intValue != 1) return; // 0 = ចាស់, 2 = ទាំងពីរ
            p.intValue = 2;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("វីរជនខ្មែរ", "បានបើក Input Manager (ចាស់) រួចហើយ។ សូមបិទ Unity ហើយបើកគម្រោងឡើងវិញម្ដង។", "យល់ព្រម");
        }
    }
}
#endif
