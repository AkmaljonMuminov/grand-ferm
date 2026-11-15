#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrandFerm.EditorTools
{
    /// <summary>
    /// Makes a fresh checkout playable: creates the base material and the (empty) Main scene,
    /// registers it in Build Settings and applies mobile Player Settings. Runs once automatically.
    /// The game itself is built from code at runtime (see Scripts/Core), so the scene stays empty.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/GrandFerm/Scenes/Main.unity";
        const string MatPath = "Assets/GrandFerm/Resources/Materials/Base.mat";

        static ProjectSetup() { EditorApplication.delayCall += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode) EnsureAll(); }; }

        [MenuItem("GrandFerm/Repair project setup")]
        public static void EnsureAll()
        {
            EnsureBaseMaterial();
            EnsureScene();
            ApplyPlayerSettings();
        }

        static void EnsureBaseMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(MatPath) != null) return;
            Directory.CreateDirectory(Path.GetDirectoryName(MatPath));
            var sh = Shader.Find("Standard");
            if (sh == null) return;
            AssetDatabase.CreateAsset(new Material(sh), MatPath);
            AssetDatabase.SaveAssets();
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
            }
            var current = EditorBuildSettings.scenes;
            foreach (var s in current) if (s.path == ScenePath) return;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ApplyPlayerSettings()
        {
            if (PlayerSettings.productName == "GRAND FERM") return; // already configured once
            PlayerSettings.companyName = "GrandFerm";
            PlayerSettings.productName = "GRAND FERM";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.grandferm.game");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.bundleVersion = "0.1.0";
        }
    }

    public class GrandFermPostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            var t = (TextureImporter)assetImporter;
            if (assetPath.Contains("/Resources/Icons/"))
            {
                t.textureType = TextureImporterType.Sprite;
                t.spriteImportMode = SpriteImportMode.Single;
                t.mipmapEnabled = false;
                t.alphaIsTransparency = true;
            }
            else if (assetPath.Contains("/Resources/Textures/"))
            {
                t.wrapMode = TextureWrapMode.Repeat;
                t.maxTextureSize = 256;
            }
        }

        void OnPreprocessModel()
        {
            if (!assetPath.Contains("/Resources/Models/")) return;
            var m = (ModelImporter)assetImporter;
            m.materialImportMode = ModelImporterMaterialImportMode.None;
            m.importNormals = ModelImporterNormals.Calculate;
            m.normalSmoothingAngle = 30f;
            m.importCameras = false;
            m.importLights = false;
            m.isReadable = false;
        }
    }

    public static class BuildScript
    {
        /// <summary>Used by CI: -executeMethod GrandFerm.EditorTools.BuildScript.BuildAndroid [-customBuildPath dir]</summary>
        public static void BuildAndroid()
        {
            ProjectSetup.EnsureAll();
            string dir = "Builds/Android";
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-customBuildPath") dir = args[i + 1];
            Directory.CreateDirectory(dir);
            EditorUserBuildSettings.buildAppBundle = false;
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = Path.Combine(dir, "GrandFerm.apk"),
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts).summary.result;
            if (report != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
#endif
