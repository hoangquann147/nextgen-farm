using NextGenFarm;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace NextGenFarm.Editor
{
    public static class FarmDemoMenu
    {
        [MenuItem("NextGen Farm/Create Demo Scene")]
        public static void CreateDemo()
        {
            if (!(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset))
            { Debug.LogError("Create/open a Unity 6 HDRP project and finish HDRP Wizard first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateSceneForProject();
        }
        public static void CreateSceneForProject()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FarmGenerated")) AssetDatabase.CreateFolder("Assets", "FarmGenerated");
            const string libraryPath = "Assets/FarmGenerated/DemoVisuals.asset";
            var library = AssetDatabase.LoadAssetAtPath<FarmVisualLibrary>(libraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<FarmVisualLibrary>();
                library.grass = CreateMaterial("Grass", new Color(0.19f, 0.30f, 0.10f), 0.15f);
                library.drySoil = CreateMaterial("DrySoil", new Color(0.32f, 0.19f, 0.09f), 0.15f);
                library.wetSoil = CreateMaterial("WetSoil", new Color(0.10f, 0.065f, 0.035f), 0.65f);
                const string weatherPath = "Assets/FarmGenerated/Weather.mat";
                library.precipitationMaterial = AssetDatabase.LoadAssetAtPath<Material>(weatherPath);
                if (library.precipitationMaterial == null)
                {
                    library.precipitationMaterial = new Material(Shader.Find("HDRP/Unlit"));
                    AssetDatabase.CreateAsset(library.precipitationMaterial, weatherPath);
                }
                AssetDatabase.CreateAsset(library, libraryPath);
            }
            if (library.demoPalette == null || library.demoPalette.Length != 10)
            {
                Color[] palette = { new Color(0.19f, 0.30f, 0.10f), new Color(0.32f, 0.19f, 0.09f),
                    new Color(0.10f, 0.065f, 0.035f), new Color(0.20f, 0.46f, 0.12f),
                    new Color(0.30f, 0.56f, 0.16f), new Color(0.85f, 0.56f, 0.12f),
                    new Color(0.12f, 0.28f, 0.50f), new Color(0.72f, 0.12f, 0.06f),
                    new Color(0.82f, 0.80f, 0.72f), new Color(0.26f, 0.15f, 0.08f) };
                library.demoPalette = new Material[palette.Length];
                for (int i = 0; i < palette.Length; i++) library.demoPalette[i] = CreateMaterial("Palette" + i, palette[i], i == 2 ? 0.65f : 0.15f);
                EditorUtility.SetDirty(library);
            }
            FarmArtBuilder.Ensure(library);
            FarmCropMeshBuilder.Ensure(library);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("NextGen Farm"); go.AddComponent<FarmBootstrap>().visuals = library;
            const string scenePath = FarmProjectSetup.ScenePath;
            if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new System.IO.IOException("Cannot save demo scene");
            Selection.activeGameObject = go;
            Debug.Log("Farm demo ready. Press Play: " + scenePath);
        }
        private static Material CreateMaterial(string name, Color color, float smoothness)
        {
            string path = "Assets/FarmGenerated/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find("HDRP/Lit")) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
