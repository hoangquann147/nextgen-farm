using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NextGenFarm.Editor
{
    [InitializeOnLoad]
    public static class FarmProjectSetup
    {
        public const string ScenePath = "Assets/FarmGenerated/FarmDemo.unity";
        private const string PipelinePath = "Assets/FarmGenerated/FarmHDRP.asset";
        private static double nextCheck;
        static FarmProjectSetup()
        {
            if (!Application.isBatchMode) EditorApplication.update += FirstOpen;
        }
        private static void FirstOpen()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= FirstOpen;
            if (File.Exists(ScenePath)) return;
            try { Configure(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
        [MenuItem("NextGen Farm/Prepare Project")]
        public static void Configure()
        {
            PlayerSettings.companyName = "NextGenFarm";
            PlayerSettings.productName = "NextGen Farm";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            if (!AssetDatabase.IsValidFolder("Assets/FarmGenerated")) AssetDatabase.CreateFolder("Assets", "FarmGenerated");
            var pipeline = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
                pipeline.name = "Farm HDRP";
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            // Pinned Unity 6000.0/HDRP 17 setup. This protected entry point initializes
            // HDRP's global settings/resources, just as pipeline creation in the Editor does.
            var initialize = typeof(HDRenderPipelineAsset).GetMethod("EnsureGlobalSettings", BindingFlags.Instance | BindingFlags.NonPublic);
            if (initialize == null) throw new MissingMethodException("HDRP 17 EnsureGlobalSettings is unavailable; use the pinned Unity version.");
            initialize.Invoke(pipeline, null);
            KeepInstancingVariants();
            IncludeUIShader("UI/Default"); RemoveExplicitBuiltinTextShader();
            AssetDatabase.SaveAssets();
            FarmDemoMenu.CreateSceneForProject();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("FARM_PROJECT_READY: " + ScenePath);
        }
        private static void KeepInstancingVariants()
        {
            var serialized = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var stripping = serialized.FindProperty("m_InstancingStripping");
            if (stripping != null) { stripping.intValue = 2; serialized.ApplyModifiedPropertiesWithoutUndo(); } // Keep All
        }
        private static void IncludeUIShader(string name)
        {
            var shader=Shader.Find(name);if(shader==null)return;
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var list=settings.FindProperty("m_AlwaysIncludedShaders");
            for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==shader)return;
            int index=list.arraySize;list.InsertArrayElementAtIndex(index);list.GetArrayElementAtIndex(index).objectReferenceValue=shader;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void RemoveExplicitBuiltinTextShader()
        {
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var list=settings.FindProperty("m_AlwaysIncludedShaders");
            for(int i=list.arraySize-1;i>=0;i--)
            {
                var shader=list.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if(shader==null||shader.name!="GUI/Text Shader")continue;
                int size=list.arraySize;list.DeleteArrayElementAtIndex(i);if(list.arraySize==size)list.DeleteArrayElementAtIndex(i);
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("NextGen Farm/Build Windows Player")]
        public static void BuildWindows()
        {
            Configure();
            FarmUnityChecks.Run();
            string output = Path.GetFullPath("Builds/Windows/NextGenFarm.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath },
                locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result + " / " + report.summary.totalErrors + " errors");
            Debug.Log("FARM_BUILD_SUCCEEDED: " + output);
        }
        public static void VerifyProject()
        {
            Configure();
            FarmUnityChecks.Run();
            if (!(GraphicsSettings.defaultRenderPipeline is HDRenderPipelineAsset)) throw new Exception("HDRP not configured");
            if (!File.Exists(ScenePath)) throw new Exception("Missing playable scene");
            Debug.Log("FARM_PROJECT_CHECKS_PASSED");
        }
    }
}
