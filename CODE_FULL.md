# Toàn bộ mã C# — NextGen Farm / Unity 6 HDRP 17.x

Các khối dưới đây chứa nguyên văn mọi file C# trong Assets/Farm, không lược bỏ thân hàm. Tạo từng file đúng đường dẫn; giữ Editor riêng. Đọc README.vi.md để cài đặt và biết phạm vi kiểm chứng. Đây là source kit của bốn nhóm hệ thống; chưa có co-op thực hoặc asset photorealistic.

## Assets/Farm/Editor/FarmArtBuilder.cs

```csharp
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace NextGenFarm.Editor
{
    public static class FarmArtBuilder
    {
        public static void Ensure(FarmVisualLibrary library)
        {
            if(!AssetDatabase.IsValidFolder("Assets/FarmGenerated/Art")) AssetDatabase.CreateFolder("Assets/FarmGenerated","Art");
            string[] colors={"397D27","78A83F","6B4832","A78052","F5E7C5","875342","AAA798","359BAB","515E60","8BC8D4","F4C69A","9B522A","3C83AD","263735","F5F0D8","EDBE43","D66D46","684A30","F1D8AE","779B8A","DCE5D9","79A32D"};
            library.artMaterials=new Material[colors.Length];
            for(int i=0;i<colors.Length;i++)
            {
                string path="Assets/FarmGenerated/Art/"+((FarmSurface)i)+".mat";
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(m==null){m=new Material(Shader.Find("HDRP/Lit"));AssetDatabase.CreateAsset(m,path);}
                ColorUtility.TryParseHtmlString("#"+colors[i],out var color); m.SetColor("_BaseColor",color);
                m.SetFloat("_Smoothness",i==(int)FarmSurface.Water?.88f:i==(int)FarmSurface.Metal?.5f:.16f);
                if(m.HasProperty("_Metallic")) m.SetFloat("_Metallic",i==(int)FarmSurface.Metal?.55f:i==(int)FarmSurface.Water?.18f:0);
                m.enableInstancing=true; library.artMaterials[i]=m; EditorUtility.SetDirty(m);
            }
            SetGround(library.grass, "Meadow", new Color(.39f,.56f,.14f), 0);
            SetGround(library.drySoil, "Earth", new Color(.43f,.29f,.15f), 1);
            SetGround(library.wetSoil, "MoistEarth", new Color(.24f,.18f,.105f), 1);
            library.wetSoil.SetFloat("_Smoothness",.46f);
            library.pathMaterial=library.artMaterials[(int)FarmSurface.Stone];
            SetGround(library.pathMaterial,"Cobblestone",new Color(.59f,.60f,.46f),2);
            library.cliffMaterial=library.artMaterials[(int)FarmSurface.Soil];
            library.demoPalette[3].SetColor("_BaseColor",new Color(.35f,.62f,.10f));
            library.demoPalette[4].SetColor("_BaseColor",new Color(.23f,.52f,.055f));
            library.demoPalette[5].SetColor("_BaseColor",new Color(.88f,.64f,.16f));
            foreach(var m in library.demoPalette) EditorUtility.SetDirty(m);
            const string grassPath="Assets/FarmGenerated/Art/GrassTuft.asset";
            library.detailGrassMesh=AssetDatabase.LoadAssetAtPath<Mesh>(grassPath);
            if(library.detailGrassMesh==null){library.detailGrassMesh=Grass();AssetDatabase.CreateAsset(library.detailGrassMesh,grassPath);}
            const string soilPath="Assets/FarmGenerated/Art/TerrainColumn.asset";
            library.groundMesh=AssetDatabase.LoadAssetAtPath<Mesh>(soilPath);
            if(library.groundMesh==null){library.groundMesh=Ground();AssetDatabase.CreateAsset(library.groundMesh,soilPath);}
            EditorUtility.SetDirty(library);
        }
        private static void SetGround(Material mat,string id,Color color,int style)
        {
            string path="Assets/FarmGenerated/Art/"+id+"-v2.asset";
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(tex==null)
            {
                const int size=128; tex=new Texture2D(size,size,TextureFormat.RGBA32,true); tex.name=id; tex.wrapMode=TextureWrapMode.Repeat;
                for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                {
                    float u=x/(float)size,v=y/(float)size;
                    float noise=Noise(u*8,v*8,8)*.55f+Noise(u*24,v*24,24)*.3f+Noise(u*56,v*56,56)*.15f;
                    float tone=.9f+noise*.22f;
                    if(style==1)
                    {
                        float ridges=Mathf.Sin(v*Mathf.PI*12+noise)*.035f;
                        float stones=Mathf.Pow(Noise(u*16,v*16,16),4);
                        tone=.74f+noise*.32f+ridges+stones*.3f;
                    }
                    if(style==2)
                    {
                        float row=Mathf.Floor(v*4), uu=Mathf.Repeat(u*4+(row%2)*.5f,1), vv=Mathf.Repeat(v*4,1);
                        if(uu<.055f||vv<.055f||uu>.945f||vv>.945f) tone=.48f;
                        else tone=.86f+noise*.25f;
                    }
                    tex.SetPixel(x,y,new Color(color.r*tone,color.g*tone,color.b*tone,1));
                }
                tex.Apply(true,false);AssetDatabase.CreateAsset(tex,path);
            }
            mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseColorMap",tex); mat.enableInstancing=true; EditorUtility.SetDirty(mat);
        }
        private static float Hash(int x,int y,int period)
        {
            x=(x%period+period)%period;y=(y%period+period)%period;
            unchecked { uint h=(uint)(x*374761393+y*668265263+177);h=(h^(h>>13))*1274126177u;return (h&65535)/65535f; }
        }
        private static float Noise(float x,float y,int period)
        {
            int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);float a=x-ix,b=y-iy;a=a*a*(3-2*a);b=b*b*(3-2*b);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix,iy,period),Hash(ix+1,iy,period),a),Mathf.Lerp(Hash(ix,iy+1,period),Hash(ix+1,iy+1,period),a),b);
        }
        private static Mesh Grass()
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<9;i++)
            {
                float a=i*2.39996f; Vector3 outwards=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Vector3 side=Vector3.Cross(Vector3.up,outwards)*.035f;
                Vector3 p=outwards*.11f, tip=p+outwards*.16f+Vector3.up*(.3f+(i%3)*.12f);int n=v.Count;
                v.Add(p-side);v.Add(tip);v.Add(p+side);v.Add(p+side);v.Add(tip);v.Add(p-side);
                t.AddRange(new[]{n,n+1,n+2,n+3,n+4,n+5});
            }
            var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        private static Mesh Ground()
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var source=cube.GetComponent<MeshFilter>().sharedMesh;
            var m=Object.Instantiate(source);Object.DestroyImmediate(cube);
            var top=new List<int>();var sides=new List<int>();var triangles=m.triangles;var normals=m.normals;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var target=normals[triangles[i]].y>.5f?top:sides;
                target.Add(triangles[i]);target.Add(triangles[i+1]);target.Add(triangles[i+2]);
            }
            m.subMeshCount=2;m.SetTriangles(top,0);m.SetTriangles(sides,1);return m;
        }
    }
}
```

## Assets/Farm/Editor/FarmCropMeshBuilder.cs

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NextGenFarm.Editor
{
    // Original procedural demo foliage. One mesh/material per growth stage for instancing.
    public static class FarmCropMeshBuilder
    {
        public static void Ensure(FarmVisualLibrary library)
        {
            if (!AssetDatabase.IsValidFolder("Assets/FarmGenerated/Crops")) AssetDatabase.CreateFolder("Assets/FarmGenerated", "Crops");
            var definitions = new CropVisual[3];
            for (int species = 0; species < 3; species++)
            {
                string id = species == 0 ? "carrot" : species == 1 ? "wheat" : "corn";
                var visual = new CropVisual { cropId = id, scale = Vector3.one };
                for (int stage = 0; stage < 3; stage++)
                {
                    string path = "Assets/FarmGenerated/Crops/" + id + "-stage" + stage + "-v3.asset";
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (mesh == null)
                    {
                        mesh = Create(id, stage); mesh.name = id + " stage " + stage;
                        AssetDatabase.CreateAsset(mesh, path);
                    }
                    visual.stages[stage] = mesh;
                    visual.materials[stage] = library.demoPalette[species == 1 && stage == 2 ? 5 : stage == 0 ? 3 : 4];
                    if(species==2) visual.accentMaterials[stage]=library.artMaterials[(int)FarmSurface.Gold];
                }
                definitions[species] = visual;
            }
            library.crops = definitions;
            EditorUtility.SetDirty(library);
        }
        private sealed class Geometry
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<int> indices = new List<int>();
            public void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int index = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                indices.Add(index); indices.Add(index + 1); indices.Add(index + 2);
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool doubleSided = false)
            {
                Triangle(a, b, c); Triangle(a, c, d);
                if (doubleSided) { Triangle(c, b, a); Triangle(d, c, a); }
            }
            public void Stem(Vector3 origin, float radius, float height)
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3, b = (i + 1) * Mathf.PI / 3;
                    Vector3 p = origin + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                    Vector3 q = origin + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * radius;
                    Quad(p, p + Vector3.up * height, q + Vector3.up * height, q);
                }
            }
            public void Leaf(Vector3 origin, Vector3 outward, float height, float reach, float width)
            {
                Vector3 side = Vector3.Cross(Vector3.up, outward).normalized;
                for (int segment = 0; segment < 8; segment++)
                {
                    float t0 = segment / 8f, t1 = (segment + 1) / 8f;
                    Vector3 c0 = origin + Vector3.up * (height * Mathf.Sin(t0 * Mathf.PI * 0.85f)) + outward * (reach * t0 * t0);
                    Vector3 c1 = origin + Vector3.up * (height * Mathf.Sin(t1 * Mathf.PI * 0.85f)) + outward * (reach * t1 * t1);
                    float w0 = width * Mathf.Sin(t0 * Mathf.PI), w1 = width * Mathf.Sin(t1 * Mathf.PI);
                    Quad(c0 - side * w0, c1 - side * w1, c1 + side * w1, c0 + side * w0, true);
                }
            }
            public void Ear(Vector3 center, float width, float height)
            {
                const int sides = 6, rings = 5;
                for (int ring = 0; ring < rings; ring++)
                    for (int side = 0; side < sides; side++)
                    {
                        Vector3 a = EarPoint(center, width, height, ring, side, rings, sides);
                        Vector3 b = EarPoint(center, width, height, ring + 1, side, rings, sides);
                        Vector3 c = EarPoint(center, width, height, ring + 1, side + 1, rings, sides);
                        Vector3 d = EarPoint(center, width, height, ring, side + 1, rings, sides);
                        Quad(a, b, c, d);
                    }
            }
            private static Vector3 EarPoint(Vector3 c, float w, float h, int ring, int side, int rings, int sides)
            {
                float t = ring / (float)rings, a = side * Mathf.PI * 2 / sides;
                float r = Mathf.Sin(t * Mathf.PI) * w;
                return c + new Vector3(Mathf.Cos(a) * r, (t - 0.5f) * h, Mathf.Sin(a) * r);
            }
            public Mesh Finish()
            {
                var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
            }
        }
        private static Mesh Create(string id, int stage)
        {
            var g = new Geometry(); float growth = stage == 0 ? 0.28f : stage == 1 ? 0.65f : 1;
            if(id=="corn")
            {
                var ears=new Geometry(); growth=stage==0?.45f:stage==1?.85f:1;
                for(int i=0;i<4;i++)
                {
                    float a=i*Mathf.PI*.5f+.3f; Vector3 p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.43f;
                    float h=(1.9f+(i%2)*.16f)*growth;g.Stem(p,.055f*growth,h);
                    for(int leaf=0;leaf<6;leaf++) { float angle=a+leaf*2.3f; g.Leaf(p+Vector3.up*(h*(.15f+leaf*.105f)),new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)),h*.25f,.73f*growth,.22f*growth); }
                    for(int e=0;e<2;e++)
                    {
                        Vector3 origin=p+new Vector3(e==0?.14f:-.14f,h*(e==0?.55f:.72f),0);
                        ears.Ear(origin,.115f*growth,.38f*growth);
                    }
                }
                var green=g.Finish();var yellow=ears.Finish();
                var combined=new Mesh();combined.CombineMeshes(new[]{new CombineInstance{mesh=green,transform=Matrix4x4.identity},new CombineInstance{mesh=yellow,transform=Matrix4x4.identity}},false,false);
                Object.DestroyImmediate(green);Object.DestroyImmediate(yellow);return combined;
            }
            if (id == "carrot")
            {
                for (int i = 0; i < 9; i++)
                {
                    float angle = i * Mathf.PI * 2 / 9;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    g.Leaf(Vector3.zero, direction, (i % 2 == 0 ? 0.85f : 0.65f) * growth, 0.65f * growth, 0.09f * growth);
                }
            }
            else
            {
                for (int i = 0; i < 7; i++)
                {
                    float angle = i * Mathf.PI * 2 / 7;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    Vector3 origin = direction * 0.26f;
                    float height = (0.95f + (i % 3) * 0.13f) * growth;
                    g.Stem(origin, 0.022f * growth, height);
                    g.Leaf(origin + Vector3.up * (height * 0.25f), direction, height * 0.55f, 0.25f * growth, 0.04f * growth);
                    g.Ear(origin + Vector3.up * height, 0.065f * growth, 0.30f * growth);
                }
            }
            return g.Finish();
        }
    }
}
```

## Assets/Farm/Editor/FarmDemoMenu.cs

```csharp
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
```

## Assets/Farm/Editor/FarmPlayModeChecks.cs

```csharp
using System;
using System.IO;
using NextGenFarm.Core;
using UnityEditor;
using UnityEngine;

namespace NextGenFarm.Editor
{
    [InitializeOnLoad]
    public static class FarmPlayModeChecks
    {
        private const string Active = "NextGenFarm.Smoke.Active";
        private const string Phase = "NextGenFarm.Smoke.Phase";
        private const string Deadline = "NextGenFarm.Smoke.Deadline";
        static FarmPlayModeChecks() { if (SessionState.GetBool(Active, false)) EditorApplication.update += Pump; }
        public static void Run()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NEXTGEN_FARM_SAVE_DIR")))
                throw new InvalidOperationException("Set NEXTGEN_FARM_SAVE_DIR to an isolated test directory before the smoke run.");
            FarmProjectSetup.Configure();
            SessionState.SetBool(Active, true); SessionState.SetInt(Phase, 0); SessionState.SetInt("FarmSmokeExit", 0);
            SetDeadline(90);
            EditorApplication.update -= Pump; EditorApplication.update += Pump;
            EditorApplication.EnterPlaymode();
        }
        private static void SetDeadline(float seconds) => SessionState.SetFloat(Deadline, (float)EditorApplication.timeSinceStartup + seconds);
        private static void Pump()
        {
            if (!SessionState.GetBool(Active, false)) return;
            int phase = SessionState.GetInt(Phase, 0);
            try
            {
                if (phase == 0)
                {
                    if (EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) return;
                    if (EditorApplication.isPlaying)
                    { SessionState.SetInt(Phase, 1); SetDeadline(3); }
                    else if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Deadline, 0)) throw new TimeoutException("Play Mode did not start");
                }
                else if (phase == 1 && EditorApplication.timeSinceStartup > SessionState.GetFloat(Deadline, 0))
                {
                    ExerciseGame();
                    SessionState.SetInt(Phase, 2); SetDeadline(3);
                }
                else if (phase == 2 && EditorApplication.timeSinceStartup > SessionState.GetFloat(Deadline, 0))
                {
                    var driver = UnityEngine.Object.FindFirstObjectByType<TractorController>();
                    Require(driver != null && driver.Automatic, "auto is active");
                    var before = new Vector3(SessionState.GetFloat("FarmSmokeX", 0), 0, SessionState.GetFloat("FarmSmokeZ", 0));
                    Vector3 after = driver.transform.position; after.y = 0;
                    Require(Vector3.Distance(before, after) > 0.1f, "auto tractor physically moves in Play Mode");
                    driver.StopAuto();
                    Directory.CreateDirectory("artifacts");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/unity-playmode.png"));
                    SessionState.SetInt(Phase, 3); SetDeadline(3);
                }
                else if (phase == 3 && EditorApplication.timeSinceStartup > SessionState.GetFloat(Deadline, 0))
                { SessionState.SetInt(Phase, 4); EditorApplication.ExitPlaymode(); }
                else if (phase == 4 && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetBool(Active, false); EditorApplication.update -= Pump;
                    int result = SessionState.GetInt("FarmSmokeExit", 0);
                    if (result == 0) Debug.Log("FARM_PLAYMODE_CHECKS_PASSED");
                    EditorApplication.Exit(result);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex); SessionState.SetInt("FarmSmokeExit", 1);
                SessionState.SetInt(Phase, 4); EditorApplication.ExitPlaymode();
            }
        }
        private static void ExerciseGame()
        {
            UnityEngine.Object.FindFirstObjectByType<FarmGameFlow>()?.Play();
            var grid = UnityEngine.Object.FindFirstObjectByType<FarmGridManager>();
            var animals = UnityEngine.Object.FindFirstObjectByType<AnimalPenSystem>();
            var driver = UnityEngine.Object.FindFirstObjectByType<TractorController>();
            var saves = UnityEngine.Object.FindFirstObjectByType<SaveLoadSystem>();
            var renderer = UnityEngine.Object.FindFirstObjectByType<FarmInstanceRenderer>();
            Require(grid != null && animals != null && driver != null && saves != null, "bootstrap systems exist");
            Require(Camera.main != null && Camera.main.GetComponent<FarmCamera>() != null, "camera follows player");
            Require(SystemInfo.supportsInstancing && renderer != null && renderer.DrawCalls > 0, "instancing submits farm draw batches");
            var center = new Vector2Int(3, 3);
            Require(grid.Act(center, FarmAction.Plow, "carrot", true) == 9, "Unity plow 3x3");
            Require(grid.Act(center, FarmAction.Sow, "carrot", true) == 9, "Unity sow 3x3");
            Require(grid.Act(center, FarmAction.Water, "carrot", true) == 9, "Unity water 3x3");
            for (int z = 2; z <= 4; z++)
                for (int x = 2; x <= 4; x++) grid.World.Get(x, z).readyUtc = grid.Clock.Now - 1;
            Require(grid.Act(center, FarmAction.Harvest, "carrot", true) == 9, "Unity harvest 3x3");
            Require(grid.World.Wallet.inventory.Find(i => i.id == "carrot").count == 27, "Unity harvest yield");
            Require(animals.FeedAll() == 1, "Unity feed pen");
            animals.World.Pens[0].readyUtc = grid.Clock.Now - 1;
            Require(animals.HarvestAll() == 1, "Unity harvest animal");
            Require(saves.Save(), "Unity runtime save");
            var snapshot = JsonUtility.FromJson<FarmSnapshot>(File.ReadAllText(saves.SavePath));
            var restored = snapshot.Restore(new[] { new CropDefinition(), new CropDefinition { id = "wheat" } },
                new[] { new AnimalDefinition() }, out _);
            Require(restored.Wallet.inventory.Find(i => i.id == "carrot").count == 27, "Unity runtime save roundtrip");
            driver.SetMounted(true);
            SessionState.SetFloat("FarmSmokeX", driver.transform.position.x); SessionState.SetFloat("FarmSmokeZ", driver.transform.position.z);
            driver.ToggleAuto(); Require(driver.Automatic, "Unity auto tractor starts");
            Debug.Log("FARM_RUNTIME_ACTIONS_PASSED");
        }
        private static void Require(bool condition, string label)
        { if (!condition) throw new InvalidOperationException("Play Mode check failed: " + label); }
    }
}
```

## Assets/Farm/Editor/FarmProjectSetup.cs

```csharp
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
```

## Assets/Farm/Editor/FarmUnityChecks.cs

```csharp
using System;
using System.IO;
using NextGenFarm.Core;
using UnityEditor;
using UnityEngine;

namespace NextGenFarm.Editor
{
    public static class FarmUnityChecks
    {
        // Also usable with -batchmode -executeMethod NextGenFarm.Editor.FarmUnityChecks.Run -quit.
        [MenuItem("NextGen Farm/Verify Unity JSON and Save IO")]
        public static void Run()
        {
            var crops = new[] { new CropDefinition() };
            var definitions = new[] { new AnimalDefinition() };
            const double now = 1700000000;
            var farm = new FarmWorld(8, 8, crops, now);
            var animals = new AnimalWorld(farm, definitions);
            Require(animals.Place("pen", "cow", 4, 4), "place pen");
            Require(animals.Feed("pen", now), "feed");
            Require(farm.Apply(1, 1, FarmAction.Plow, null, now), "plow");
            Require(farm.Apply(1, 1, FarmAction.Sow, "carrot", now), "sow");
            Require(farm.Apply(1, 1, FarmAction.Water, null, now + 1), "water");
            var snapshot = new FarmSnapshot { width = 8, depth = 8, epochUtc = now, savedUtc = now + 2,
                cellSize = 2, tiles = farm.Tiles, wallet = farm.Wallet, pens = animals.Pens };
            string json = JsonUtility.ToJson(snapshot);
            var restored = JsonUtility.FromJson<FarmSnapshot>(json).Restore(crops, definitions, out var restoredAnimals);
            Require(restored.Get(1, 1).wateredThisCycle && restored.Get(1, 1).Ready(now + 10000), "Unity JSON crop offline");
            Require(restoredAnimals.Pens[0].Ready(now + 10000), "Unity JSON animal offline");
            Require(restored.Wallet.coins == farm.Wallet.coins, "Unity JSON wallet");
            string directory = Path.Combine(Application.temporaryCachePath, "farm-check-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "farm.json");
            try
            {
                var store = new FarmSaveStore(path);
                store.Write(snapshot, value => JsonUtility.ToJson(value));
                store.Write(snapshot, value => JsonUtility.ToJson(value));
                File.WriteAllText(path, "invalid");
                Require(store.TryLoad(JsonUtility.FromJson<FarmSnapshot>, crops, definitions, out _, out _, out _, out _), "Unity JSON backup recovery");
                Debug.Log("FARM_UNITY_CHECKS_PASSED: Unity JSON, offline progression and backup recovery");
            }
            finally
            {
                foreach (string suffix in new[] { "", ".bak", ".tmp" }) if (File.Exists(path + suffix)) File.Delete(path + suffix);
                if (Directory.Exists(directory)) Directory.Delete(directory);
            }
        }
        private static void Require(bool condition, string label)
        { if (!condition) throw new InvalidOperationException("Farm Unity check failed: " + label); }
    }
}
```

## Assets/Farm/Runtime/AnimalPenSystem.cs

```csharp
using NextGenFarm.Core;
using UnityEngine;

namespace NextGenFarm
{
    public sealed class AnimalPenSystem : MonoBehaviour
    {
        public AnimalWorld World { get; private set; }
        private SeasonTimeManager clock;
        public void Initialize(AnimalWorld world, SeasonTimeManager time) { World = world; clock = time; }
        public int FeedAll()
        {
            int count = 0;
            foreach (var pen in World.Pens) if (World.Feed(pen.id, clock.Now)) count++;
            return count;
        }
        public int HarvestAll()
        {
            int count = 0;
            foreach (var pen in World.Pens) if (World.Harvest(pen.id, clock.Now)) count++;
            return count;
        }
    }
}
```

## Assets/Farm/Runtime/Core/AnimalModel.cs

```csharp
using System;
using System.Collections.Generic;

namespace NextGenFarm.Core
{
    [Serializable]
    public sealed class AnimalDefinition
    {
        public string id = "cow";
        public string productId = "milk";
        public double productionSeconds = 180;
        public int feedCost = 5;
        public int productsPerAnimal = 1;
        public int sellPrice = 12;
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(productId) ||
                !ClockMath.Finite(productionSeconds) || productionSeconds < 1 || feedCost < 0 ||
                productsPerAnimal < 1 || sellPrice < 0) throw new ArgumentException("Invalid animal definition");
        }
    }

    [Serializable]
    public sealed class AnimalPen
    {
        public string id;
        public string animalId;
        public int x, z;
        public int width = 3, depth = 3;
        public int count = 3;
        public int capacity = 9;
        public bool fed;
        public double readyUtc;
        public bool Ready(double now) => fed && now >= readyUtc;
    }

    public sealed class AnimalWorld
    {
        public readonly List<AnimalPen> Pens = new List<AnimalPen>();
        public readonly Dictionary<string, AnimalDefinition> Definitions = new Dictionary<string, AnimalDefinition>();
        private readonly FarmWorld farm;
        public AnimalWorld(FarmWorld farm, IEnumerable<AnimalDefinition> definitions)
        {
            this.farm = farm;
            foreach (var def in definitions) { def.Validate(); Definitions.Add(def.id, def); }
        }
        public bool Place(string id, string animalId, int x, int z, int width = 3, int depth = 3, int count = 3)
        {
            if (string.IsNullOrWhiteSpace(id) || Pens.Exists(p => p.id == id) || !Definitions.ContainsKey(animalId) ||
                width < 1 || depth < 1 || width > 64 || depth > 64 || count < 1 || count > width * depth ||
                !farm.Contains(x, z) || !farm.Contains(x + width - 1, z + depth - 1)) return false;
            for (int dz = 0; dz < depth; dz++)
                for (int dx = 0; dx < width; dx++)
                {
                    var t = farm.Get(x + dx, z + dz);
                    if (t.blocked || t.road || t.HasCrop || t.tilled || Math.Abs(t.height - farm.Get(x, z).height) > 0.01) return false;
                }
            Pens.Add(new AnimalPen { id = id, animalId = animalId, x = x, z = z, width = width,
                depth = depth, count = count, capacity = width * depth });
            for (int dz = 0; dz < depth; dz++)
                for (int dx = 0; dx < width; dx++)
                {
                    int index = farm.Index(x + dx, z + dz);
                    farm.Tiles[index].blocked = true; farm.Notify(index);
                }
            return true;
        }
        public bool Feed(string id, double now)
        {
            if (!ClockMath.Finite(now) || now < 0) return false;
            var pen = Pens.Find(p => p.id == id);
            if (pen == null || pen.fed || !Definitions.TryGetValue(pen.animalId, out var def) ||
                !farm.Wallet.Spend((long)def.feedCost * pen.count)) return false;
            pen.fed = true; pen.readyUtc = now + def.productionSeconds;
            return true;
        }
        public bool Harvest(string id, double now)
        {
            var pen = Pens.Find(p => p.id == id);
            if (!ClockMath.Finite(now) || pen == null || !pen.Ready(now) || !Definitions.TryGetValue(pen.animalId, out var def)) return false;
            farm.Wallet.Add(def.productId, checked(pen.count * def.productsPerAnimal));
            pen.fed = false; pen.readyUtc = 0;
            return true;
        }
    }
}
```

## Assets/Farm/Runtime/Core/FarmLandscapePlan.cs

```csharp
using System;

namespace NextGenFarm.Core
{
    public static class FarmLandscapePlan
    {
        public static void Seed(FarmWorld world, AnimalWorld animals, double now)
        {
            if (world.Width < 30 || world.Depth < 30) return;
            for (int z = 9; z <= 18; z++)
                for (int x = 4; x <= 25; x++)
                {
                    string id = x <= 7 ? "carrot" : x >= 21 ? "wheat" : x >= 9 && x <= 18 ? "corn" : null;
                    if (id == null || !world.Crops.TryGetValue(id, out var def)) continue;
                    var tile = world.Get(x, z); tile.tilled = true; tile.cropId = id;
                    tile.plantedUtc = now - def.growSeconds * 0.53;
                    tile.readyUtc = now + def.growSeconds * 0.47;
                }
            animals.Place("starter-pen", "cow", 24, 21, 3, 3, 3);
            ApplyPaths(world);
        }
        public static void ApplyPaths(FarmWorld world)
        {
            if (world.Width < 30 || world.Depth < 30) return;
            for (int z = 0; z < world.Depth; z++)
                for (int x = 0; x < world.Width; x++)
                {
                    var tile = world.Get(x, z);
                    bool path = (z == 20 && x >= 2 && x <= 29) || (x == 19 && z >= 4 && z <= 27);
                    if (path && !tile.HasCrop && !tile.blocked) { tile.road = true; tile.tilled = false; }
                    if (z >= 24 && x >= 3 && x <= 29 && !tile.HasCrop && !tile.blocked && tile.height == 0)
                        tile.height = z == 24 ? 0.5f : 1;
                }
        }
    }
}
```

## Assets/Farm/Runtime/Core/FarmModel.cs

```csharp
using System;
using System.Collections.Generic;

namespace NextGenFarm.Core
{
    public enum Season { Spring, Summer, Autumn, Winter }
    public enum SoilState { Grass, Dry, Wet }
    public enum FarmAction { Plow, Sow, Water, Harvest }

    [Serializable]
    public sealed class CropDefinition
    {
        public string id = "carrot";
        public double growSeconds = 120;
        public double waterReductionSeconds = 30;
        public double wetSeconds = 45;
        public int seedCost = 2;
        public int yield = 3;
        public int sellPrice = 4;
        public int seasonMask = 15;
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(id) || !ClockMath.Finite(growSeconds) || growSeconds < 1 ||
                !ClockMath.Finite(waterReductionSeconds) || waterReductionSeconds < 0 ||
                !ClockMath.Finite(wetSeconds) || wetSeconds < 1 || seedCost < 0 || yield < 1 ||
                sellPrice < 0 || seasonMask < 1 || seasonMask > 15)
                throw new ArgumentException("Invalid crop definition: " + id);
        }
    }

    [Serializable]
    public sealed class FarmTile
    {
        public bool tilled;
        public bool blocked;
        public bool road;
        public float height;
        public string cropId = "";
        public double plantedUtc;
        public double readyUtc;
        public double wetUntilUtc;
        public bool wateredThisCycle;
        public bool HasCrop => !string.IsNullOrEmpty(cropId);
        public SoilState Soil(double now) => now < wetUntilUtc ? SoilState.Wet : tilled ? SoilState.Dry : SoilState.Grass;
        public bool Ready(double now) => HasCrop && now >= readyUtc;
        public float Growth(double now)
        {
            if (!HasCrop) return 0;
            return (float)Math.Max(0, Math.Min(1, (now - plantedUtc) / Math.Max(1, readyUtc - plantedUtc)));
        }
    }

    [Serializable]
    public sealed class InventoryEntry
    {
        public string id;
        public int count;
        public InventoryEntry() { }
        public InventoryEntry(string id, int count) { this.id = id; this.count = count; }
    }

    [Serializable]
    public sealed class Wallet
    {
        public long coins = 500;
        public long diamonds;
        public long medals;
        public int planted, watered, harvested;
        public bool waterRewardClaimed, harvestRewardClaimed;
        public List<InventoryEntry> inventory = new List<InventoryEntry>();
        public bool Spend(long amount)
        {
            if (amount < 0 || coins < amount) return false;
            coins -= amount;
            return true;
        }
        public void Add(string id, int amount)
        {
            if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
            var entry = inventory.Find(v => v.id == id);
            if (entry == null) inventory.Add(new InventoryEntry(id, amount));
            else entry.count = checked(entry.count + amount);
        }
        public long Sell(string id, int unitPrice)
        {
            if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
            var entry = inventory.Find(v => v.id == id);
            if (entry == null) return 0;
            long value = checked((long)entry.count * unitPrice);
            coins = checked(coins + value);
            inventory.Remove(entry);
            return value;
        }
    }

    public static class ClockMath
    {
        public const double SeasonSeconds = 17 * 60;
        public static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
        public static Season SeasonAt(double now, double epoch)
        {
            long index = (long)Math.Floor(Math.Max(0, now - epoch) / SeasonSeconds);
            return (Season)(index % 4);
        }
        public static double SeasonRemaining(double now, double epoch)
            => SeasonSeconds - Math.Max(0, now - epoch) % SeasonSeconds;
    }

    // Pure C#: no per-tile Update(), coroutine, GameObject or Unity dependency.
    public sealed class FarmWorld
    {
        public const int MaxCells = 262144;
        public readonly int Width;
        public readonly int Depth;
        public readonly FarmTile[] Tiles;
        public readonly Dictionary<string, CropDefinition> Crops;
        public Wallet Wallet;
        public double EpochUtc;
        public event Action<int> Changed;

        public FarmWorld(int width, int depth, IEnumerable<CropDefinition> crops, double epoch)
        {
            if (width < 1 || depth < 1 || (long)width * depth > MaxCells)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (!ClockMath.Finite(epoch) || epoch < 0) throw new ArgumentOutOfRangeException(nameof(epoch));
            Width = width; Depth = depth; EpochUtc = epoch;
            Tiles = new FarmTile[width * depth];
            for (int i = 0; i < Tiles.Length; i++) Tiles[i] = new FarmTile();
            Wallet = new Wallet();
            Crops = new Dictionary<string, CropDefinition>(StringComparer.Ordinal);
            foreach (var crop in crops) { crop.Validate(); Crops.Add(crop.id, crop); }
        }
        public bool Contains(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Depth;
        public int Index(int x, int z) => z * Width + x;
        public FarmTile Get(int x, int z) => Contains(x, z) ? Tiles[Index(x, z)] : null;
        public void Notify(int index) => Changed?.Invoke(index);

        public bool Apply(int x, int z, FarmAction action, string cropId, double now)
        {
            if (!ClockMath.Finite(now) || now < 0) return false;
            var tile = Get(x, z);
            if (tile == null || tile.blocked || tile.road) return false;
            switch (action)
            {
                case FarmAction.Plow:
                    if (tile.tilled || tile.HasCrop) return false;
                    tile.tilled = true;
                    break;
                case FarmAction.Sow:
                    if (!tile.tilled || tile.HasCrop || cropId == null || !Crops.TryGetValue(cropId, out var crop) ||
                        (crop.seasonMask & (1 << (int)ClockMath.SeasonAt(now, EpochUtc))) == 0 || !Wallet.Spend(crop.seedCost))
                        return false;
                    tile.cropId = crop.id; tile.plantedUtc = now;
                    tile.readyUtc = now + crop.growSeconds;
                    tile.wateredThisCycle = now < tile.wetUntilUtc;
                    if (tile.wateredThisCycle) tile.readyUtc = Math.Max(now, tile.readyUtc - crop.waterReductionSeconds);
                    Wallet.planted++;
                    break;
                case FarmAction.Water:
                    // Once per crop cycle: key repeat/reloading cannot compound the bonus.
                    if (tile.HasCrop)
                    {
                        if (tile.Ready(now) || tile.wateredThisCycle || !Crops.TryGetValue(tile.cropId, out var planted)) return false;
                        tile.readyUtc = Math.Max(now, tile.readyUtc - planted.waterReductionSeconds);
                        tile.wetUntilUtc = now + planted.wetSeconds;
                        tile.wateredThisCycle = true;
                        Wallet.watered++;
                    }
                    else
                    {
                        if (!tile.tilled || now < tile.wetUntilUtc) return false;
                        tile.wetUntilUtc = now + 45;
                    }
                    break;
                case FarmAction.Harvest:
                    if (!tile.Ready(now) || !Crops.TryGetValue(tile.cropId, out var harvest)) return false;
                    Wallet.Add(tile.cropId, harvest.yield);
                    tile.cropId = ""; tile.plantedUtc = tile.readyUtc = 0;
                    tile.wateredThisCycle = false;
                    Wallet.harvested++;
                    break;
                default: return false;
            }
            Notify(Index(x, z));
            return true;
        }

        public int ApplySquare(int x, int z, int radius, FarmAction action, string cropId, double now)
        {
            if (radius < 0 || radius > 8) throw new ArgumentOutOfRangeException(nameof(radius));
            int changed = 0;
            for (int dz = -radius; dz <= radius; dz++)
                for (int dx = -radius; dx <= radius; dx++)
                    if (Apply(x + dx, z + dz, action, cropId, now)) changed++;
            return changed;
        }

        public bool SetHeight(int x, int z, float height)
        {
            var tile = Get(x, z);
            if (tile == null || tile.blocked || tile.road || tile.HasCrop || float.IsNaN(height) || float.IsInfinity(height)) return false;
            tile.height = Math.Max(-3, Math.Min(8, height));
            Notify(Index(x, z)); return true;
        }
        public bool ClaimCareReward(bool watering)
        {
            if (watering)
            {
                if (Wallet.watered < 9 || Wallet.waterRewardClaimed) return false;
                Wallet.diamonds = checked(Wallet.diamonds + 5); Wallet.waterRewardClaimed = true;
            }
            else
            {
                if (Wallet.harvested < 9 || Wallet.harvestRewardClaimed) return false;
                Wallet.medals = checked(Wallet.medals + 1); Wallet.harvestRewardClaimed = true;
            }
            return true;
        }
    }
}
```

## Assets/Farm/Runtime/Core/FarmSaveStore.cs

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NextGenFarm.Core
{
    // Serializer injected so production uses Unity JsonUtility and tests can run without Unity.
    public sealed class FarmSaveStore
    {
        public readonly string Path;
        public string Status { get; private set; } = "Ready";
        public FarmSaveStore(string path) { Path = path; }
        public bool TryLoad(Func<string, FarmSnapshot> deserialize, IEnumerable<CropDefinition> crops,
            IEnumerable<AnimalDefinition> definitions, out FarmWorld world, out AnimalWorld animals,
            out double savedUtc, out float cellSize)
        {
            world = null; animals = null; savedUtc = 0; cellSize = 2;
            foreach (string path in new[] { Path, Path + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    if (new FileInfo(path).Length > 64 * 1024 * 1024) throw new FormatException("Save exceeds 64 MB");
                    var data = deserialize(File.ReadAllText(path, Encoding.UTF8));
                    if (data == null) throw new FormatException("Empty save");
                    var candidate = data.Restore(crops, definitions, out var candidateAnimals);
                    world = candidate; animals = candidateAnimals;
                    savedUtc = data.savedUtc; cellSize = data.cellSize;
                    Status = path.EndsWith(".bak", StringComparison.Ordinal) ? "Recovered backup" : "Loaded";
                    return true;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                    ex is ArgumentException || ex is FormatException || ex is OverflowException)
                { Status = "Load failed: " + ex.Message; }
            }
            return false;
        }
        public void Write(FarmSnapshot snapshot, Func<FarmSnapshot, string> serialize)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(serialize(snapshot));
            if (bytes.Length > 64 * 1024 * 1024) throw new IOException("Save exceeds 64 MB");
            string directory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path));
            Directory.CreateDirectory(directory);
            using (var stream = new FileStream(Path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(Path)) File.Replace(Path + ".tmp", Path, Path + ".bak");
            else File.Move(Path + ".tmp", Path);
            Status = "Saved";
        }
    }
}
```

## Assets/Farm/Runtime/Core/FarmSnapshot.cs

```csharp
using System;
using System.Collections.Generic;

namespace NextGenFarm.Core
{
    [Serializable]
    public sealed class FarmSnapshot
    {
        public int version = 1;
        public int width, depth;
        public double epochUtc, savedUtc;
        public float cellSize = 2;
        public FarmTile[] tiles;
        public Wallet wallet;
        public List<AnimalPen> pens;

        // Validate everything into temporary objects; a corrupt save never half-replaces live state.
        public FarmWorld Restore(IEnumerable<CropDefinition> crops, IEnumerable<AnimalDefinition> animals,
            out AnimalWorld animalWorld)
        {
            if (version != 1 || !ClockMath.Finite(savedUtc) || savedUtc < 0 ||
                !ClockMath.Finite(epochUtc) || epochUtc < 0 || epochUtc > savedUtc ||
                float.IsNaN(cellSize) || float.IsInfinity(cellSize) || cellSize < 0.5f || cellSize > 20)
                throw new FormatException("Unsupported or invalid save header");
            var world = new FarmWorld(width, depth, crops, epochUtc);
            animalWorld = new AnimalWorld(world, animals);
            if (tiles == null || tiles.Length != world.Tiles.Length || wallet == null ||
                wallet.coins < 0 || wallet.diamonds < 0 || wallet.medals < 0 || wallet.planted < 0 || wallet.watered < 0 || wallet.harvested < 0 || wallet.inventory == null ||
                wallet.inventory.Count > 4096 || pens == null || pens.Count > world.Tiles.Length)
                throw new FormatException("Invalid save data");
            var inventoryIds = new HashSet<string>();
            foreach (var entry in wallet.inventory)
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.id.Length > 128 ||
                    entry.count < 0 || !inventoryIds.Add(entry.id)) throw new FormatException("Invalid inventory");
            for (int i = 0; i < tiles.Length; i++)
            {
                var t = tiles[i];
                if (t == null || !ClockMath.Finite(t.height) || t.height < -3 || t.height > 8 ||
                    !ClockMath.Finite(t.plantedUtc) || t.plantedUtc < 0 || !ClockMath.Finite(t.readyUtc) ||
                    t.readyUtc < 0 || !ClockMath.Finite(t.wetUntilUtc) || t.wetUntilUtc < 0 ||
                    (t.HasCrop && (!t.tilled || t.blocked || t.road || !world.Crops.ContainsKey(t.cropId) || t.readyUtc < t.plantedUtc)))
                    throw new FormatException("Invalid tile " + i);
                // Copy to avoid sharing mutable deserialization data.
                world.Tiles[i] = new FarmTile { tilled = t.tilled, blocked = t.blocked, road = t.road, height = t.height,
                    cropId = t.cropId ?? "", plantedUtc = t.plantedUtc, readyUtc = t.readyUtc,
                    wetUntilUtc = t.wetUntilUtc, wateredThisCycle = t.wateredThisCycle };
            }
            var occupied = new HashSet<int>();
            var penIds = new HashSet<string>();
            foreach (var p in pens)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.id) || !penIds.Add(p.id) ||
                    !animalWorld.Definitions.ContainsKey(p.animalId ?? "") || p.width < 1 || p.width > 64 ||
                    p.depth < 1 || p.depth > 64 || p.capacity != p.width * p.depth || p.count < 1 ||
                    p.count > p.capacity || !ClockMath.Finite(p.readyUtc) || p.readyUtc < 0 ||
                    !world.Contains(p.x, p.z) || !world.Contains(p.x + p.width - 1, p.z + p.depth - 1))
                    throw new FormatException("Invalid pen");
                for (int z = p.z; z < p.z + p.depth; z++)
                    for (int x = p.x; x < p.x + p.width; x++)
                    {
                        int i = world.Index(x, z);
                        if (!occupied.Add(i) || !world.Tiles[i].blocked || world.Tiles[i].HasCrop)
                            throw new FormatException("Overlapping or unreserved pen");
                    }
                animalWorld.Pens.Add(new AnimalPen { id = p.id, animalId = p.animalId, x = p.x, z = p.z,
                    width = p.width, depth = p.depth, count = p.count, capacity = p.capacity,
                    fed = p.fed, readyUtc = p.readyUtc });
            }
            world.Wallet = new Wallet { coins = wallet.coins, diamonds = wallet.diamonds,
                medals = wallet.medals, planted = wallet.planted, watered = wallet.watered, harvested = wallet.harvested,
                waterRewardClaimed = wallet.waterRewardClaimed, harvestRewardClaimed = wallet.harvestRewardClaimed,
                inventory = new List<InventoryEntry>() };
            foreach (var entry in wallet.inventory) world.Wallet.inventory.Add(new InventoryEntry(entry.id, entry.count));
            return world;
        }
    }
}
```

## Assets/Farm/Runtime/Core/GridNavigation.cs

```csharp
using System;
using System.Collections.Generic;

namespace NextGenFarm.Core
{
    public struct Cell
    {
        public int x, z;
        public Cell(int x, int z) { this.x = x; this.z = z; }
    }
    public static class GridNavigation
    {
        public static bool Passable(FarmWorld world, Cell a, Cell b, float maxStep)
        {
            var from = world.Get(a.x, a.z); var to = world.Get(b.x, b.z);
            return from != null && to != null && !to.blocked && Math.Abs(from.height - to.height) <= maxStep;
        }
        public static List<Cell> FindPath(FarmWorld world, Cell start, Cell goal, float maxStep = 0.75f)
        {
            if (!world.Contains(start.x, start.z) || !world.Contains(goal.x, goal.z) ||
                world.Get(goal.x, goal.z).blocked) return null;
            int[] previous = new int[world.Tiles.Length];
            for (int i = 0; i < previous.Length; i++) previous[i] = -1;
            int startIndex = world.Index(start.x, start.z), goalIndex = world.Index(goal.x, goal.z);
            var queue = new Queue<int>(); queue.Enqueue(startIndex); previous[startIndex] = startIndex;
            int[] dx = { 0, 1, 0, -1 }, dz = { 1, 0, -1, 0 };
            while (queue.Count > 0 && previous[goalIndex] < 0)
            {
                int index = queue.Dequeue(); var a = new Cell(index % world.Width, index / world.Width);
                for (int d = 0; d < 4; d++)
                {
                    var b = new Cell(a.x + dx[d], a.z + dz[d]);
                    if (!world.Contains(b.x, b.z) || !Passable(world, a, b, maxStep)) continue;
                    int next = world.Index(b.x, b.z);
                    if (previous[next] >= 0) continue;
                    previous[next] = index; queue.Enqueue(next);
                }
            }
            if (previous[goalIndex] < 0) return null;
            var path = new List<Cell>();
            for (int i = goalIndex; i != startIndex; i = previous[i]) path.Add(new Cell(i % world.Width, i / world.Width));
            path.Reverse(); return path;
        }
        public static List<Cell> Serpentine(int width, int depth)
        {
            var route = new List<Cell>();
            bool reverse = false;
            for (int z = 0; z < depth; z += 3)
            {
                var row = new List<Cell>();
                for (int x = 0; x < width; x += 3) row.Add(new Cell(Math.Min(x + 1, width - 1), Math.Min(z + 1, depth - 1)));
                if (reverse) row.Reverse();
                route.AddRange(row); reverse = !reverse;
            }
            return route;
        }
    }
}
```

## Assets/Farm/Runtime/FarmActionFeedback.cs

```csharp
using NextGenFarm.Core;
using UnityEngine;

namespace NextGenFarm
{
    public sealed class FarmActionFeedback : MonoBehaviour
    {
        private FarmGridManager grid;
        private ParticleSystem particles;
        private Material material;
        public void Initialize(FarmGridManager farm)
        {
            grid=farm;var go=new GameObject("Tool feedback");go.transform.SetParent(transform,false);
            particles=go.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.playOnAwake=false;main.loop=false;main.startLifetime=.65f;main.startSpeed=2;main.startSize=.065f;main.gravityModifier=.6f;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=particles.emission;emission.enabled=false;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.7f;
            material=new Material(Shader.Find("HDRP/Unlit"));material.SetColor("_UnlitColor",new Color(.3f,.7f,1));particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
            grid.ActionApplied+=OnAction;
        }
        private void OnAction(Vector2Int cell,FarmAction action,int count)
        {
            Color color=action==FarmAction.Water?new Color(.2f,.7f,1):action==FarmAction.Harvest?new Color(1,.74f,.18f):new Color(.6f,.38f,.15f);
            material.SetColor("_UnlitColor",color*150);particles.transform.position=grid.CellCenter(cell.x,cell.y)+Vector3.up*.2f;
            particles.Emit(Mathf.Min(80,8*count));
        }
        private void OnDestroy(){if(grid!=null)grid.ActionApplied-=OnAction;if(material!=null)Destroy(material);}
    }
}
```

## Assets/Farm/Runtime/FarmAudio.cs

```csharp
using System;
using UnityEngine;

namespace NextGenFarm
{
    // Two independent buses. Replace the generated clips with imported licensed audio as needed.
    public sealed class FarmAudio : MonoBehaviour
    {
        public AudioSource MusicSource { get; private set; }
        public AudioSource SfxSource { get; private set; }
        private FarmSettings settings;
        private FarmGridManager grid;
        private AudioClip music, click, action;
        public void Initialize(FarmSettings preferences, FarmGridManager farm)
        {
            settings = preferences; grid = farm;
            MusicSource = gameObject.AddComponent<AudioSource>();
            SfxSource = gameObject.AddComponent<AudioSource>();
            MusicSource.playOnAwake = SfxSource.playOnAwake = false;
            MusicSource.spatialBlend = SfxSource.spatialBlend = 0;
            music = MakeMusic(); click = MakeTone("UI tick", 660, .09f); action = MakeTone("Farm action", 330, .2f);
            MusicSource.clip = music; MusicSource.loop = true;
            settings.Changed += Apply; grid.ActionApplied += OnAction; Apply(); MusicSource.Play();
        }
        private void Apply()
        {
            // Multiplication preserves independent music/SFX levels when master changes.
            MusicSource.volume = settings.Data.master * settings.Data.music;
            SfxSource.volume = settings.Data.master * settings.Data.sfx;
        }
        public void Click() { if (SfxSource != null) SfxSource.PlayOneShot(click, .35f); }
        private void OnAction(Vector2Int cell, Core.FarmAction tool, int count) { SfxSource.PlayOneShot(action, .3f); }
        private static AudioClip MakeTone(string name, float frequency, float duration)
        {
            const int rate = 22050; var samples = new float[(int)(rate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * Mathf.Exp(-t * 32) * Mathf.Min(t * 200, 1) * .3f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private static AudioClip MakeMusic()
        {
            const int rate = 22050, seconds = 24; var samples = new float[rate * seconds];
            float[] notes = { 261.63f, 329.63f, 392, 523.25f, 440, 349.23f, 329.63f, 293.66f };
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate, beat = t % 3;
                int n = (int)(t / 3) % notes.Length;
                float envelope = Mathf.Min(beat * 4, 1) * Mathf.Exp(-beat * 1.5f);
                samples[i] = (Mathf.Sin(2 * Mathf.PI * notes[n] * t) + .25f * Mathf.Sin(4 * Mathf.PI * notes[n] * t)) * envelope * .08f;
            }
            var clip = AudioClip.Create("Valley arpeggio (procedural)", samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private void OnDestroy()
        {
            if (settings != null) settings.Changed -= Apply;
            if (grid != null) grid.ActionApplied -= OnAction;
            if (MusicSource != null) MusicSource.Stop(); if (SfxSource != null) SfxSource.Stop();
            if (music != null) Destroy(music); if (click != null) Destroy(click); if (action != null) Destroy(action);
        }
    }
}
```

## Assets/Farm/Runtime/FarmBootstrap.cs

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using NextGenFarm.Core;
using UnityEngine;

namespace NextGenFarm
{
    // Add this one component to an empty GameObject in a Unity 6 HDRP scene.
    public sealed class FarmBootstrap : MonoBehaviour
    {
        [Min(4)] public int width = 32;
        [Min(4)] public int depth = 32;
        [Range(0.5f, 20)] public float cellSize = 2;
        public FarmVisualLibrary visuals;
        public CropDefinition[] crops = { new CropDefinition(),
            new CropDefinition { id = "wheat", growSeconds = 300, waterReductionSeconds = 60, seedCost = 3, yield = 4, sellPrice = 5 },
            new CropDefinition { id = "corn", growSeconds = 600, waterReductionSeconds = 90, seedCost = 3, yield = 4, sellPrice = 9 } };
        public AnimalDefinition[] animalDefinitions = { new AnimalDefinition() };
        private FarmGridManager grid;
        private AnimalPenSystem animals;
        private SeasonTimeManager clock;
        private SaveLoadSystem saves;
        private TractorController driver;
        private FarmInstanceRenderer rendererSystem;
        private Material[] materials;
        private Mesh cube;
        private GameObject root;
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private int cropIndex;
        private string message = "Welcome! Plow, sow, water, then harvest.";
        private bool capturingPhoto;
        private FarmSceneArt art;
        private FarmHUD hud;
        public FarmGridManager Grid => grid;
        public TractorController Driver => driver;
        public SeasonTimeManager Clock => clock;
        public AnimalPenSystem Animals => animals;
        public SaveLoadSystem Saves => saves;

        private void Awake()
        {
            if (FindObjectsByType<FarmBootstrap>(FindObjectsSortMode.None).Length > 1)
            { Debug.LogError("Use exactly one FarmBootstrap per scene."); enabled = false; return; }
            try { Build(); }
            catch (Exception ex) { Debug.LogException(ex); message = ex.Message; enabled = false; }
        }
        private void Build()
        {
            if (crops == null || crops.Length == 0 || animalDefinitions == null) throw new ArgumentException("Missing definitions");
            saves = gameObject.AddComponent<SaveLoadSystem>();
            bool loaded = saves.TryLoad(crops, animalDefinitions, out var world, out var animalWorld, out double saved, out float loadedSize);
            if (!loaded)
            {
                if (System.IO.File.Exists(saves.SavePath) || System.IO.File.Exists(saves.SavePath + ".bak"))
                    throw new InvalidOperationException("Cannot load save or backup. Original files preserved at " + saves.SavePath + ". " + saves.Status);
                world = new FarmWorld(width, depth, crops, SeasonTimeManager.UtcNow);
                animalWorld = new AnimalWorld(world, animalDefinitions);
                if(width >= 30 && depth >= 30) FarmLandscapePlan.Seed(world,animalWorld,SeasonTimeManager.UtcNow);
                else if (animalDefinitions.Length > 0 && width >= 8 && depth >= 8)
                    animalWorld.Place("starter-pen", animalDefinitions[0].id, width - 5, depth - 5);
            }
            else cellSize = loadedSize;
            FarmLandscapePlan.ApplyPaths(world);
            clock = gameObject.AddComponent<SeasonTimeManager>(); clock.Initialize(world.EpochUtc, saved);
            grid = gameObject.AddComponent<FarmGridManager>(); grid.Initialize(world, clock, cellSize);
            animals = gameObject.AddComponent<AnimalPenSystem>(); animals.Initialize(animalWorld, clock);
            root = new GameObject("Farm Demo Presentation"); root.transform.SetParent(transform, false);
            art = root.AddComponent<FarmSceneArt>(); art.Initialize(grid,visuals);
            var temporary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube = Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh); owned.Add(cube);
            temporary.SetActive(false); Destroy(temporary);
            Color[] colors = { new Color(0.19f, 0.30f, 0.10f), new Color(0.32f, 0.19f, 0.09f),
                new Color(0.10f, 0.065f, 0.035f), new Color(0.20f, 0.46f, 0.12f),
                new Color(0.30f, 0.56f, 0.16f), new Color(0.85f, 0.56f, 0.12f),
                new Color(0.12f, 0.28f, 0.50f), new Color(0.72f, 0.12f, 0.06f),
                new Color(0.82f, 0.80f, 0.72f), new Color(0.26f, 0.15f, 0.08f) };
            materials = new Material[colors.Length];
            Shader shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No supported Lit shader. Use Unity HDRP template.");
            for (int i = 0; i < colors.Length; i++)
            {
                Material source = visuals != null && visuals.demoPalette != null && visuals.demoPalette.Length > i ? visuals.demoPalette[i] : null;
                var mat = source != null ? new Material(source) : new Material(shader);
                mat.name = "Farm demo " + i; mat.enableInstancing = true;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colors[i]);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", colors[i]);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", i == 2 ? 0.65f : 0.15f);
                materials[i] = mat; owned.Add(mat);
            }
            rendererSystem = gameObject.AddComponent<FarmInstanceRenderer>(); rendererSystem.Initialize(grid, visuals, cube, materials);
            CreatePlayer(); CreateAnimals();
            CreateBackground();
            art.Landscape();
            rendererSystem.Track(driver);
            var camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Farm Camera"); go.transform.SetParent(root.transform, false);
                go.tag = "MainCamera"; camera = go.AddComponent<Camera>();
                if (FindFirstObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
            }
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 1000;
            camera.fieldOfView = 62;
            camera.transform.position = driver.transform.position + new Vector3(0, 12, -14);
            var follow = camera.GetComponent<FarmCamera>() ?? camera.gameObject.AddComponent<FarmCamera>(); follow.target = driver.transform;
            var environment = gameObject.AddComponent<FarmEnvironment>();
            environment.Initialize(clock, camera, root.transform, visuals != null ? visuals.precipitationMaterial : null);
            saves.Initialize(grid, animals);
            hud=gameObject.AddComponent<FarmHUD>();hud.Initialize(this);
            gameObject.AddComponent<FarmGameFlow>().Initialize(this,hud,follow);
            gameObject.AddComponent<FarmActionFeedback>().Initialize(grid);
            gameObject.AddComponent<FarmCaptureRunner>();
        }
        private GameObject Shape(string name, Transform parent, Vector3 position, Vector3 scale, Material material,
            PrimitiveType primitive = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(primitive); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var collider = go.GetComponent<Collider>(); if (collider != null) Destroy(collider);
            return go;
        }
        private void CreateBackground()
        {
            float w = grid.World.Width * cellSize, d = grid.World.Depth * cellSize;
            Shape("Landscape west", root.transform, new Vector3(-40, -0.35f, d / 2), new Vector3(80, 0.3f, d + 160), materials[0]);
            Shape("Landscape east", root.transform, new Vector3(w + 40, -0.35f, d / 2), new Vector3(80, 0.3f, d + 160), materials[0]);
            Shape("Landscape south", root.transform, new Vector3(w / 2, -0.35f, -40), new Vector3(w, 0.3f, 80), materials[0]);
            Shape("Landscape north", root.transform, new Vector3(w / 2, -0.35f, d + 40), new Vector3(w, 0.3f, 80), materials[0]);
        }
        private void CreatePlayer()
        {
            var player = new GameObject("Farmer and Tractor"); player.transform.SetParent(root.transform, false);
            Cell spawn = new Cell(Mathf.Min(13, grid.World.Width - 1), Mathf.Min(8, grid.World.Depth - 1));
            if (grid.World.Get(spawn.x, spawn.z).blocked)
                for (int i = 0; i < grid.World.Tiles.Length; i++)
                    if (!grid.World.Tiles[i].blocked) { spawn = new Cell(i % grid.World.Width, i / grid.World.Width); break; }
            player.transform.position = grid.CellCenter(spawn.x, spawn.z);
            GameObject person;
            if (visuals != null && visuals.farmerPrefab != null) person = Instantiate(visuals.farmerPrefab, player.transform);
            else person = art.Farmer(player.transform);
            GameObject tractor;
            if (visuals != null && visuals.tractorPrefab != null) tractor = Instantiate(visuals.tractorPrefab, player.transform);
            else
            {
                tractor = art.Tractor(player.transform);
            }
            driver = player.AddComponent<TractorController>(); driver.Initialize(grid, person, tractor); driver.SelectedCrop = grid.World.Crops.ContainsKey("corn")?"corn":crops[0].id;
            driver.SelectedAction=FarmAction.Water;
        }
        private void CreateAnimals()
        {
            foreach (var pen in animals.World.Pens)
            {
                var container = new GameObject("Pen " + pen.id); container.transform.SetParent(root.transform, false);
                for (int i = 0; i < pen.count; i++)
                {
                    Vector3 p = grid.CellCenter(pen.x + i % pen.width, pen.z + i / pen.width);
                    if (visuals != null && visuals.cowPrefab != null) Instantiate(visuals.cowPrefab, p, Quaternion.identity, container.transform);
                    else
                    {
                        art.Cow(container.transform,p);
                    }
                }
                Vector3 origin = grid.CellCenter(pen.x, pen.z) - new Vector3(cellSize / 2, 0, cellSize / 2);
                Shape("Fence west", container.transform, origin - container.transform.position + new Vector3(0, 0.6f, pen.depth * cellSize / 2),
                    new Vector3(0.12f, 1.2f, pen.depth * cellSize), materials[9]);
                Shape("Fence east", container.transform, origin - container.transform.position + new Vector3(pen.width * cellSize, 0.6f, pen.depth * cellSize / 2),
                    new Vector3(0.12f, 1.2f, pen.depth * cellSize), materials[9]);
                Shape("Fence north", container.transform, origin - container.transform.position + new Vector3(pen.width * cellSize / 2, 0.6f, pen.depth * cellSize),
                    new Vector3(pen.width * cellSize, 1.2f, 0.12f), materials[9]);
            }
        }
        private void Update()
        {
            if (driver == null) return;
            if (FarmInput.PhotoPressed && !capturingPhoto) StartCoroutine(CapturePhoto());
            if (FarmInput.Press('f')) message = "Fed pens: " + animals.FeedAll();
            if (FarmInput.Press('g')) message = "Harvested pens: " + animals.HarvestAll();
            if (FarmInput.Press('v')) SellAll();
            if (FarmInput.Press('p')) saves.Save();
            if (FarmInput.Press('b')&&hud!=null) hud.OpenShop(0);
            if (FarmInput.Press('+')) ChangeHeight(0.25f);
            if (FarmInput.Press('-')) ChangeHeight(-0.25f);
        }
        private void ChangeHeight(float amount)
        {
            if (driver.Automatic) { message = "Stop auto before editing terrain"; return; }
            var c = driver.TargetCell; var tile = grid.World.Get(c.x, c.y);
            message = tile != null && grid.World.SetHeight(c.x, c.y, tile.height + amount) ? "Terrain edited" : "Cannot edit occupied terrain";
        }
        public void SellAll()
        {
            long total = 0;
            foreach (var c in crops) total += grid.World.Wallet.Sell(c.id, c.sellPrice);
            var sold = new HashSet<string>();
            foreach (var a in animalDefinitions) if (sold.Add(a.productId)) total += grid.World.Wallet.Sell(a.productId, a.sellPrice);
            message = "Town sale: +" + total + " coins";
            if(hud!=null)hud.ShowToast("Đã bán nông sản  ·  +"+total.ToString("N0")+" xu");
        }
        private void OnGUI() { FarmInput.CaptureGUIEvent(Event.current); }
        public void TakePhoto() { if(!capturingPhoto) StartCoroutine(CapturePhoto()); }
        private IEnumerator CapturePhoto()
        {
            if(capturingPhoto)yield break;
            capturingPhoto=true;
            string directory=System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName,"Screenshots");
            System.IO.Directory.CreateDirectory(directory);
            string path=System.IO.Path.Combine(directory,"farm-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+".png");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return null;
            capturingPhoto=false;
            if(hud!=null)hud.ShowToast("Đã chụp ảnh nông trại");
            Debug.Log("FARM_SCREENSHOT: "+path);
        }
        private void OnApplicationFocus(bool focused) { if (!focused) FarmInput.ClearGUIState(); }
        private void OnDestroy()
        {
            foreach (var asset in owned) if (asset != null) Destroy(asset);
        }
    }
}
```

## Assets/Farm/Runtime/FarmCamera.cs

```csharp
using UnityEngine;

namespace NextGenFarm
{
    public sealed class FarmCamera : MonoBehaviour
    {
        public Transform target;
        public bool FreezeFollow { get; set; }
        public bool MenuOrbit { get; set; }
        private float zoom = 0.08f;
        private float yaw, pitchOffset;
        public void AdjustZoom(float amount) { zoom = Mathf.Clamp01(zoom + amount); }
        private void LateUpdate()
        {
            if (target == null || FreezeFollow) return;
            if (MenuOrbit) yaw = Mathf.Repeat(yaw + Time.unscaledDeltaTime * 2.5f, 360);
            else
            {
                zoom = Mathf.Clamp01(zoom - FarmInput.Scroll * 0.08f);
                Vector2 look=FarmInput.Look; yaw=Mathf.Repeat(yaw+look.x*.12f,360); pitchOffset=Mathf.Clamp(pitchOffset-look.y*.08f,-8,45);
            }
            float pitch = MenuOrbit ? 28 : Mathf.Clamp(Mathf.Lerp(11, 66, zoom)+pitchOffset, 5, 82);
            float distance = MenuOrbit ? 30 : Mathf.Lerp(5.5f, 32, zoom);
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 destination = target.position + Vector3.up * 1.3f - rotation * Vector3.forward * distance;
            float blend = 1 - Mathf.Exp(-7 * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, destination, blend);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, blend);
        }
    }
}
```

## Assets/Farm/Runtime/FarmCaptureRunner.cs

```csharp
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using NextGenFarm.Core;

namespace NextGenFarm
{
    // Engine-native QA capture. It does not automate or capture another desktop app.
    public sealed class FarmCaptureRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            string[] args=Environment.GetCommandLineArgs();string path=null;float delay=5;bool quit=false,shop=false,verify=false,menu=false,settings=false,flow=false;
            for(int i=0;i<args.Length;i++)
            {
                if(args[i]=="-farmCapture"&&i+1<args.Length)path=args[++i];
                else if(args[i]=="-farmCaptureDelay"&&i+1<args.Length)float.TryParse(args[++i],out delay);
                else if(args[i]=="-farmCaptureAndQuit")quit=true;
                else if(args[i]=="-farmCaptureShop")shop=true;
                else if(args[i]=="-farmVerifyPresentation")verify=true;
                else if(args[i]=="-farmCaptureMenu")menu=true;
                else if(args[i]=="-farmCaptureSettings")settings=true;
                else if(args[i]=="-farmVerifyFlow")flow=true;
            }
            if(path==null)yield break;
            FarmInput.Suppressed=true;
            var gameFlow=FindFirstObjectByType<FarmGameFlow>();
            if(!menu&&!settings)gameFlow.Play();
            path=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(path));
            yield return new WaitForSecondsRealtime(Mathf.Max(2,delay));
            if(flow)
            {
                bool valid=false;
                try { FarmFlowChecks.Verify(gameFlow); valid=true; } catch(Exception ex){Debug.LogException(ex);}
                if(!valid){Application.Quit(2);yield break;}
            }
            if(verify)
            {
                bool valid=false;
                try { VerifyPresentation(); valid=true; }
                catch(Exception ex){Debug.LogException(ex);}
                if(!valid){Application.Quit(2);yield break;}
                yield return new WaitForSecondsRealtime(.8f);
            }
            if(menu)gameFlow.ShowMainMenu();
            if(settings)gameFlow.OpenSettings();
            if(menu||settings)yield return new WaitForSecondsRealtime(1);
            if(shop){FindFirstObjectByType<FarmHUD>().OpenShop(0);yield return new WaitForSecondsRealtime(1);}
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);
            float deadline=Time.realtimeSinceStartup+8;
            while(!File.Exists(path)&&Time.realtimeSinceStartup<deadline)yield return null;
            if(File.Exists(path))Debug.Log("FARM_NATIVE_CAPTURE_PASSED: "+path);else Debug.LogError("Farm capture did not write PNG: "+path);
            yield return new WaitForSecondsRealtime(.3f);
            FarmInput.Suppressed=false;
            if(quit)Application.Quit(File.Exists(path)?0:1);
        }
        private static void VerifyPresentation()
        {
            if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NEXTGEN_FARM_SAVE_DIR")))throw new InvalidOperationException("Presentation QA requires an isolated save directory");
            var app=FindFirstObjectByType<FarmBootstrap>();var hud=FindFirstObjectByType<FarmHUD>();
            if(app==null||hud==null)throw new Exception("Missing presentation systems");
            app.Driver.SetMounted(false);app.Driver.SelectedAction=FarmAction.Water;
            Button action=null;
            foreach(var button in hud.GetComponentsInChildren<Button>()) if(button.GetComponentInChildren<Text>()?.text=="SPACE   Thao tác") { action=button;break; }
            if(action==null)throw new Exception("Missing use-tool button");
            action.onClick.Invoke();
            if(app.Driver.LastAffected!=1)throw new Exception("Context button did not water one crop");
            hud.OpenShop(0);Button corn=null;
            foreach(var button in hud.GetComponentsInChildren<Button>())if(button.transform.parent.name=="corn")corn=button;
            if(corn==null)throw new Exception("Missing corn catalog card");corn.onClick.Invoke();
            if(app.Driver.SelectedCrop!="corn"||app.Driver.SelectedAction!=FarmAction.Sow)throw new Exception("Catalog did not select seed and sow tool");
            app.Grid.Act(new Vector2Int(13,12),FarmAction.Water,"corn",true);
            if(!app.Grid.World.ClaimCareReward(true)||app.Grid.World.ClaimCareReward(true))throw new Exception("Care reward is missing or repeatable");
            if(app.Grid.World.Wallet.diamonds!=5||!app.Saves.Save())throw new Exception("Reward/save mismatch");
            var saved=JsonUtility.FromJson<FarmSnapshot>(File.ReadAllText(app.Saves.SavePath));
            if(!saved.wallet.waterRewardClaimed||saved.wallet.diamonds!=5)throw new Exception("Claim state did not persist");
            app.Driver.SelectedAction=FarmAction.Water;
            Debug.Log("FARM_PRESENTATION_INTERACTION_PASSED: context button, seed catalog, watering, one-time reward, save persistence");
        }
    }
}
```

## Assets/Farm/Runtime/FarmEnvironment.cs

```csharp
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NextGenFarm
{
    // HDRP 17.x presentation only. Weather does not grant offline watering bonuses.
    public sealed class FarmEnvironment : MonoBehaviour
    {
        private SeasonTimeManager clock;
        private Camera view;
        private Light sun;
        private Light moon;
        private Exposure exposure;
        private VolumeProfile profile;
        private GradientSky sky;
        private Fog fog;
        private ParticleSystem precipitation;
        private ParticleSystemRenderer particlesRenderer;
        private Material particleMaterial;
        private float nextTick;
        public string Weather { get; private set; }

        public void Initialize(SeasonTimeManager time, Camera camera, Transform root, Material weatherMaterial = null)
        {
            clock = time; view = camera;
            var cameraData = camera.GetComponent<HDAdditionalCameraData>() ?? camera.gameObject.AddComponent<HDAdditionalCameraData>();
            cameraData.volumeLayerMask = ~0;
            var lightObject = new GameObject("Farm Sun"); lightObject.transform.SetParent(root, false);
            sun = lightObject.AddComponent<Light>(); sun.type = LightType.Directional;
            if (lightObject.GetComponent<HDAdditionalLightData>() == null) lightObject.AddComponent<HDAdditionalLightData>();
            sun.lightUnit = LightUnit.Lux; sun.shadows = LightShadows.Soft;
            var moonObject = new GameObject("Farm Moon Fill"); moonObject.transform.SetParent(root, false);
            moon = moonObject.AddComponent<Light>(); moon.type = LightType.Directional;
            if (moonObject.GetComponent<HDAdditionalLightData>() == null) moonObject.AddComponent<HDAdditionalLightData>();
            moon.lightUnit = LightUnit.Lux; moon.color = new Color(0.45f, 0.62f, 1);
            moon.transform.rotation = Quaternion.Euler(55, 145, 0);
            var volumeObject = new GameObject("Farm Environment Volume"); volumeObject.transform.SetParent(root, false);
            var volume = volumeObject.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 10;
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = profile;
            var visual = profile.Add<VisualEnvironment>(true); visual.skyType.Override(3); // HDRP GradientSky ID
            sky = profile.Add<GradientSky>(true);
            sky.updateMode.Override(EnvironmentUpdateMode.Realtime); sky.updatePeriod.Override(2);
            exposure = profile.Add<Exposure>(true); exposure.mode.Override(ExposureMode.Fixed); exposure.fixedExposure.Override(12.5f);
            var tonemapping = profile.Add<Tonemapping>(true); tonemapping.mode.Override(TonemappingMode.ACES);
            fog = profile.Add<Fog>(true); fog.enabled.Override(true); fog.meanFreePath.Override(250);
            fog.enableVolumetricFog.Override(true);
            var particles = new GameObject("Cosmetic Rain Snow"); particles.transform.SetParent(root, false);
            precipitation = particles.AddComponent<ParticleSystem>(); precipitation.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = precipitation.main; main.loop = true; main.maxParticles = 5000;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startLifetime = 2; main.startSpeed = 12;
            main.startSize = 0.035f; main.playOnAwake = false;
            var shape = precipitation.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(35, 35, 0.1f);
            particles.transform.rotation = Quaternion.Euler(90, 0, 0);
            particlesRenderer = particles.GetComponent<ParticleSystemRenderer>();
            var particleShader = Shader.Find("HDRP/Unlit");
            if (weatherMaterial == null && particleShader == null) throw new InvalidOperationException("Missing HDRP/Unlit shader");
            particleMaterial = weatherMaterial != null ? new Material(weatherMaterial) : new Material(particleShader);
            particleMaterial.SetColor("_UnlitColor", new Color(0.55f, 0.65f, 0.75f));
            particlesRenderer.sharedMaterial = particleMaterial;
        }
        private void Update()
        {
            if (clock == null || Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 0.2f;
            float day = Mathf.Max(0, Mathf.Sin((clock.DayFraction - 0.25f) * 2 * Mathf.PI));
            sun.transform.rotation = Quaternion.Euler(clock.DayFraction * 360 - 90, -25, 0);
            sun.intensity = Mathf.Lerp(300, 100000, day);
            moon.intensity = 300 * (1 - day);
            float groundIlluminance = Mathf.Max(300, 100000 * day * day);
            exposure.fixedExposure.Override(12.95f + Mathf.Log(groundIlluminance / 100000, 2));
            sun.color = Color.Lerp(new Color(1, 0.56f, 0.3f), Color.white, day);
            sky.exposure.Override(Mathf.Lerp(5, 12.8f, day));
            sky.top.Override(new Color(0.13f, 0.30f, 0.62f));
            sky.middle.Override(new Color(0.5f, 0.65f, 0.8f));
            sky.bottom.Override(new Color(0.17f, 0.22f, 0.12f));
            bool snowy = clock.CurrentSeason == Core.Season.Winter;
            long minute = (long)Math.Floor(Math.Max(0, clock.Now - clock.EpochUtc) / 60);
            bool rainy = !snowy && clock.CurrentSeason != Core.Season.Summer && minute % 3 == 1;
            Weather = snowy ? "Snow" : rainy ? "Rain" : "Clear";
            fog.meanFreePath.Override(snowy ? 100 : rainy ? 180 : 850);
            precipitation.transform.position = view.transform.position + view.transform.forward * 12 + Vector3.up * 14;
            var emission = precipitation.emission; emission.rateOverTime = snowy ? 150 : rainy ? 650 : 0;
            var main = precipitation.main; main.startSpeed = snowy ? 2 : 12;
            main.startLifetime = snowy ? 10 : 2; main.startSize = snowy ? 0.06f : 0.035f;
            particleMaterial.SetColor("_UnlitColor", snowy ? Color.white * 4 : new Color(0.5f, 0.6f, 0.7f) * 4);
            particlesRenderer.renderMode = snowy ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Stretch;
            particlesRenderer.lengthScale = snowy ? 1 : 4;
            if (!precipitation.isPlaying) precipitation.Play();
        }
        private void OnDestroy()
        {
            if (profile != null)
            {
                foreach (var component in profile.components) if (component != null) Destroy(component);
                Destroy(profile);
            }
            if (particleMaterial != null) Destroy(particleMaterial);
        }
    }
}
```

## Assets/Farm/Runtime/FarmFlowChecks.cs

```csharp
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.HighDefinition;

namespace NextGenFarm
{
    // Invoked only by the explicit native QA flag; never mutates the user's normal save/settings.
    public static class FarmFlowChecks
    {
        public static void Verify(FarmGameFlow flow)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NEXTGEN_FARM_SAVE_DIR")))
                throw new InvalidOperationException("Flow checks require an isolated save directory");
            flow.ShowMainMenu(); Require(flow.Screen == FarmScreen.MainMenu && !FarmGameFlow.GameplayAllowed, "main menu blocks farming");
            Require(Cursor.visible && Cursor.lockState == CursorLockMode.None, "main menu cursor");
            Click(flow, "Cài đặt"); Require(flow.Screen == FarmScreen.Settings, "main settings callback");
            var master = Slider(flow, "Master slider"); var music = Slider(flow, "Music slider"); var sfx = Slider(flow, "SFX slider");
            master.value = .6f; music.value = .3f; sfx.value = .7f;
            Require(Mathf.Approximately(flow.Audio.MusicSource.volume, .18f) && Mathf.Approximately(flow.Audio.SfxSource.volume, .42f), "independent audio buses");
            var hd = Camera.main.GetComponent<HDAdditionalCameraData>();
            for (int i = 0; i < 4; i++)
            {
                flow.Settings.SetQuality(i);
                Require(flow.Settings.Data.quality == i && hd.renderingPathCustomFrameSettings.IsEnabled(FrameSettingsField.Volumetrics) == (i >= 2), "HDRP quality preset " + i);
            }
            foreach (int cap in new[] { 30, 45, 60, -1 })
            {
                flow.Settings.SetFPS(cap); Require(Application.targetFrameRate == cap && QualitySettings.vSyncCount == 0, "FPS cap " + cap);
            }
            flow.Settings.SetQuality(2); flow.Settings.SetFPS(45);
            Click(flow, "Lưu và quay lại"); Require(flow.Screen == FarmScreen.MainMenu, "settings returns to main");
            var saved = JsonUtility.FromJson<FarmSettingsData>(File.ReadAllText(flow.Settings.StoragePath));
            Require(saved.fps == 45 && saved.quality == 2 && Mathf.Approximately(saved.master, .6f), "settings persistence");
            Click(flow, "Chơi"); Require(FarmGameFlow.GameplayAllowed && flow.Screen == FarmScreen.Playing, "play callback");
            flow.TogglePointer(); Require(!FarmGameFlow.GameplayAllowed && Cursor.visible, "pointer releases controls");
            flow.TogglePointer(); Require(flow.Screen == FarmScreen.Playing, "pointer resume");
            var hud = UnityEngine.Object.FindFirstObjectByType<FarmHUD>(); hud.OpenShop(0);
            Require(flow.Screen == FarmScreen.Pointer && hud.IsShopOpen, "catalog unlocks cursor");
            flow.Escape(); Require(flow.Screen == FarmScreen.Playing && !hud.IsShopOpen, "escape closes catalog first");
            flow.ShowPause(); Click(flow, "Cài đặt"); Click(flow, "Lưu và quay lại");
            Require(flow.Screen == FarmScreen.Paused, "settings returns to pause");
            flow.Escape(); Require(flow.Screen == FarmScreen.Playing, "escape resumes");
            var invalid = new FarmSettingsData { fps = 1, quality = 50, master = float.NaN, music = -5, sfx = 5 }; invalid.Validate();
            Require(invalid.fps == 60 && invalid.quality == 3 && invalid.master == .5f && invalid.music == 0 && invalid.sfx == 1, "invalid settings sanitized");
            flow.Settings.SetFPS(60); flow.Settings.Save();
            Debug.Log("FARM_FLOW_CHECKS_PASSED: menu callbacks, pointer/catalog/pause transitions, audio buses, four HDRP presets, FPS caps, settings persistence, invalid data");
        }
        private static Slider Slider(FarmGameFlow flow, string name)
        {
            foreach (var slider in flow.GetComponentsInChildren<Slider>()) if (slider.name == name) return slider;
            throw new Exception("Missing slider: " + name);
        }
        private static void Click(FarmGameFlow flow, string caption)
        {
            foreach (var button in flow.GetComponentsInChildren<Button>())
                if (button.GetComponentInChildren<Text>()?.text == caption) { button.onClick.Invoke(); return; }
            throw new Exception("Missing button: " + caption);
        }
        private static void Require(bool condition, string message) { if (!condition) throw new Exception("Flow check failed: " + message); }
    }
}
```

## Assets/Farm/Runtime/FarmGameFlow.cs

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace NextGenFarm
{
    public enum FarmScreen { MainMenu, Playing, Pointer, Paused, Settings }

    // Owns cursor and screen transitions. No Time.timeScale changes: crop deadlines stay in UTC.
    [DefaultExecutionOrder(-150)]
    public sealed class FarmGameFlow : MonoBehaviour
    {
        public static FarmGameFlow Instance { get; private set; }
        public static bool GameplayAllowed => Instance == null || Instance.Screen == FarmScreen.Playing;
        public FarmScreen Screen { get; private set; }
        public FarmSettings Settings { get; private set; }
        public FarmAudio Audio { get; private set; }
        public bool IsWorldVisible => Screen == FarmScreen.Playing || Screen == FarmScreen.Pointer;
        private FarmBootstrap app;
        private FarmHUD hud;
        private FarmCamera cameraRig;
        private RectTransform root, panel;
        private Font font;
        private FarmScreen settingsOrigin;
        private float ignoreLookUntil;
        private bool focused = true;
        private readonly Color cream = new Color(1, .97f, .86f), ink = new Color(.15f, .28f, .22f);
        public bool CanLook => GameplayAllowed && focused && UnityEngine.Cursor.lockState == CursorLockMode.Locked && Time.unscaledTime >= ignoreLookUntil;

        public void Initialize(FarmBootstrap bootstrap, FarmHUD gameHud, FarmCamera camera)
        {
            Instance = this; app = bootstrap; hud = gameHud; cameraRig = camera;
            Settings = gameObject.AddComponent<FarmSettings>(); Settings.Initialize(camera.GetComponent<Camera>());
            Audio = gameObject.AddComponent<FarmAudio>(); Audio.Initialize(Settings, app.Grid);
            font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 24);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("Farm Menus", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false); root = go.GetComponent<RectTransform>();
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            ShowMainMenu();
        }
        private void Update()
        {
            if (!focused || FarmInput.Suppressed) return;
            bool escape = FarmInput.MenuPress(KeyCode.Escape);
            bool leftAlt = FarmInput.MenuPress(KeyCode.LeftAlt), rightAlt = FarmInput.MenuPress(KeyCode.RightAlt), tab = FarmInput.MenuPress(KeyCode.Tab);
            if (escape) Escape();
            else if (leftAlt || rightAlt || tab) TogglePointer();
        }
        public void Escape()
        {
            if (Screen == FarmScreen.Settings) CloseSettings();
            else if (Screen == FarmScreen.Paused) Play();
            else if (Screen == FarmScreen.Playing || Screen == FarmScreen.Pointer)
            {
                if (hud.IsShopOpen) hud.CloseShop();
                else ShowPause();
            }
        }
        public void TogglePointer()
        {
            if (Screen == FarmScreen.Playing) SetState(FarmScreen.Pointer);
            else if (Screen == FarmScreen.Pointer) { hud.CloseShop(); SetState(FarmScreen.Playing); }
        }
        public void CatalogOpened() { if (IsWorldVisible) SetState(FarmScreen.Pointer); }
        public void CatalogClosed() { if (Screen == FarmScreen.Pointer) SetState(FarmScreen.Playing); }
        public void Play() { Clear(); hud.CloseShop(); SetState(FarmScreen.Playing); }
        public void ShowMainMenu()
        {
            if (app != null && app.Saves != null) app.Saves.Save();
            hud.CloseShop(); SetState(FarmScreen.MainMenu); Clear();
            panel = Box(root, "Main menu", new Vector2(440, 560), new Vector2(.23f, .5f), new Color(.08f, .19f, .15f, .94f));
            TextAt(panel, "ĐỒNG NỘI", 48, new Vector2(0, 190), new Vector2(400, 70), cream);
            TextAt(panel, "Một khoảng bình yên giữa thung lũng", 20, new Vector2(0, 130), new Vector2(400, 55), new Color(.77f, .85f, .64f));
            MakeButton(panel, "Chơi", new Vector2(0, 35), Play);
            MakeButton(panel, "Cài đặt", new Vector2(0, -55), OpenSettings);
            MakeButton(panel, "Thoát", new Vector2(0, -145), ExitGame);
            TextAt(panel, "Nông trại vẫn lớn lên khi bạn nghỉ ngơi", 16, new Vector2(0, -233), new Vector2(400, 44), cream);
        }
        public void ShowPause()
        {
            hud.CloseShop(); app.Saves.Save(); SetState(FarmScreen.Paused); Clear();
            panel = Overlay("TẠM NGHỈ", new Vector2(500, 590));
            MakeButton(panel, "Tiếp tục", new Vector2(0, 110), Play);
            MakeButton(panel, "Cài đặt", new Vector2(0, 20), OpenSettings);
            MakeButton(panel, "Về sảnh chính", new Vector2(0, -70), ShowMainMenu);
            MakeButton(panel, "Thoát game", new Vector2(0, -160), ExitGame);
            TextAt(panel, "Cây vẫn tăng trưởng theo thời gian thực", 17, new Vector2(0, -245), new Vector2(450, 40), ink);
        }
        public void OpenSettings()
        {
            if (Screen == FarmScreen.Settings) return;
            settingsOrigin = Screen;
            if (hud.IsShopOpen) { hud.CloseShop(); settingsOrigin = FarmScreen.Playing; }
            SetState(FarmScreen.Settings); Clear();
            panel = Overlay("CÀI ĐẶT", new Vector2(1040, 830));
            TextAt(panel, "ÂM THANH", 23, new Vector2(-340, 295), new Vector2(280, 40), ink);
            VolumeRow("Âm lượng tổng", "Master", 220, Settings.Data.master, v => Settings.SetVolumes(v, Settings.Data.music, Settings.Data.sfx));
            VolumeRow("Nhạc nền", "Music", 140, Settings.Data.music, v => Settings.SetVolumes(Settings.Data.master, v, Settings.Data.sfx));
            VolumeRow("Hiệu ứng", "SFX", 60, Settings.Data.sfx, v => Settings.SetVolumes(Settings.Data.master, Settings.Data.music, v));
            TextAt(panel, "HÌNH ẢNH", 23, new Vector2(-340, -35), new Vector2(280, 40), ink);
            ChoiceRow("Chất lượng", "Quality", -112, new[] { "Thấp", "Trung bình", "Cao", "Siêu cao" }, Settings.Data.quality, Settings.SetQuality);
            int[] caps = { 30, 45, 60, -1 }; int index = Array.IndexOf(caps, Settings.Data.fps);
            ChoiceRow("Giới hạn FPS", "FPS", -197, new[] { "30 FPS", "45 FPS", "60 FPS", "Không giới hạn" }, index, i => Settings.SetFPS(caps[i]));
            TextAt(panel, "Thay đổi áp dụng ngay • Tắt VSync để dùng giới hạn FPS", 17, new Vector2(0, -275), new Vector2(950, 40), ink);
            MakeButton(panel, "Lưu và quay lại", new Vector2(0, -350), CloseSettings);
        }
        public void CloseSettings()
        {
            bool saved = Settings.Save();
            if (settingsOrigin == FarmScreen.MainMenu) ShowMainMenu();
            else if (settingsOrigin == FarmScreen.Paused) ShowPause();
            else { Play(); if (settingsOrigin == FarmScreen.Pointer) SetState(FarmScreen.Pointer); }
            if (!saved) hud.ShowToast("Chưa lưu được cài đặt. Kiểm tra quyền ghi thư mục.");
        }
        private void SetState(FarmScreen state)
        {
            Screen = state; FarmInput.ClearGUIState();
            if (hud != null) hud.SetVisible(IsWorldVisible);
            if (hud != null) hud.SetPointerInteraction(state == FarmScreen.Pointer);
            if (EventSystem.current != null)
            {
                EventSystem.current.sendNavigationEvents = state != FarmScreen.Playing;
                EventSystem.current.SetSelectedGameObject(null);
            }
            if (cameraRig != null) cameraRig.MenuOrbit = state == FarmScreen.MainMenu || (state == FarmScreen.Settings && settingsOrigin == FarmScreen.MainMenu);
            RefreshCursor();
        }
        private void RefreshCursor()
        {
            bool locked = focused && Screen == FarmScreen.Playing;
            UnityEngine.Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            UnityEngine.Cursor.visible = !locked;
            ignoreLookUntil = Time.unscaledTime + .12f;
        }
        private void OnApplicationFocus(bool value)
        {
            focused = value;
            if (!value && Screen == FarmScreen.Playing && app != null) SetState(FarmScreen.Pointer);
            RefreshCursor();
        }
        private void ExitGame() { Settings.Save(); app.Saves.Save(); Application.Quit(); }
        private void Clear()
        {
            foreach (Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }
        private RectTransform Box(Transform parent, string name, Vector2 size, Vector2 anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.sprite = FarmUIArt.Rounded; image.type = Image.Type.Sliced; image.color = color;
            return rect;
        }
        private RectTransform Overlay(string title, Vector2 size)
        {
            var shade = Box(root, "Menu backdrop", Vector2.zero, Vector2.zero, new Color(.02f, .08f, .06f, .55f));
            shade.anchorMax = Vector2.one; shade.offsetMin = shade.offsetMax = Vector2.zero;
            var card = Box(shade, title, size, new Vector2(.5f, .5f), cream);
            TextAt(card, title, 34, new Vector2(0, size.y / 2 - 55), new Vector2(size.x - 70, 60), ink);
            return card;
        }
        private Text TextAt(Transform parent, string value, int size, Vector2 position, Vector2 dimensions, Color color)
        {
            var go = new GameObject(value, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            var label = go.GetComponent<Text>(); label.font = font; label.text = value; label.fontSize = size; label.color = color;
            label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false; return label;
        }
        private Button MakeButton(Transform parent, string caption, Vector2 position, Action clicked, Vector2? size = null)
        {
            var rect = Box(parent, caption, size ?? new Vector2(350, 66), new Vector2(.5f, .5f), new Color(.82f, .88f, .63f)); rect.anchoredPosition = position;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            TextAt(rect, caption, 23, Vector2.zero, (size ?? new Vector2(350, 66)) - new Vector2(10, 4), ink);
            button.onClick.AddListener(() => { Audio.Click(); clicked(); }); return button;
        }
        private void VolumeRow(string title, string id, float y, float value, Action<float> changed)
        {
            TextAt(panel, title, 22, new Vector2(-312, y), new Vector2(300, 50), ink);
            var track = Box(panel, id + " slider", new Vector2(420, 32), new Vector2(.5f, .5f), new Color(.7f, .76f, .65f)); track.anchoredPosition = new Vector2(120, y);
            var handle = Box(track, "Handle", new Vector2(30, 44), new Vector2(.5f, .5f), new Color(.22f, .44f, .31f));
            var slider = track.gameObject.AddComponent<Slider>(); slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0; slider.maxValue = 1; slider.SetValueWithoutNotify(value);
            var number = TextAt(panel, Mathf.RoundToInt(value * 100) + "%", 21, new Vector2(420, y), new Vector2(100, 45), ink);
            slider.onValueChanged.AddListener(v => { changed(v); number.text = Mathf.RoundToInt(v * 100) + "%"; });
        }
        private void ChoiceRow(string title, string id, float y, string[] choices, int selected, Action<int> changed)
        {
            TextAt(panel, title, 22, new Vector2(-312, y), new Vector2(300, 50), ink);
            var value = TextAt(panel, choices[selected], 24, new Vector2(135, y), new Vector2(380, 58), ink);
            int current = selected;
            MakeButton(panel, "‹", new Vector2(-75, y), () => { current = (current + choices.Length - 1) % choices.Length; changed(current); value.text = choices[current]; }, new Vector2(65, 58)).name = id + " Previous";
            MakeButton(panel, "›", new Vector2(345, y), () => { current = (current + 1) % choices.Length; changed(current); value.text = choices[current]; }, new Vector2(65, 58)).name = id + " Next";
        }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnityEngine.Cursor.lockState = CursorLockMode.None; UnityEngine.Cursor.visible = true;
            if (font != null) Destroy(font);
        }
    }
}
```

## Assets/Farm/Runtime/FarmGridManager.cs

```csharp
using NextGenFarm.Core;
using UnityEngine;

namespace NextGenFarm
{
    public sealed class FarmGridManager : MonoBehaviour
    {
        public event System.Action<Vector2Int, FarmAction, int> ActionApplied;
        public FarmWorld World { get; private set; }
        public SeasonTimeManager Clock { get; private set; }
        public float CellSize { get; private set; } = 2;
        public void Initialize(FarmWorld world, SeasonTimeManager clock, float cellSize)
        {
            World = world; Clock = clock; CellSize = cellSize;
        }
        public Vector2Int WorldToCell(Vector3 position)
        {
            var p = position - transform.position;
            return new Vector2Int(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.z / CellSize));
        }
        public Vector3 CellCenter(int x, int z)
        {
            var tile = World.Get(x, z);
            return transform.position + new Vector3((x + 0.5f) * CellSize, tile?.height ?? 0, (z + 0.5f) * CellSize);
        }
        public int Act(Vector2Int center, FarmAction action, string cropId, bool tractor)
        {
            int count = World.ApplySquare(center.x, center.y, tractor ? 1 : 0, action, cropId, Clock.Now);
            if (count > 0) ActionApplied?.Invoke(center, action, count);
            return count;
        }
        public bool CanDrive(int x, int z) => World.Contains(x, z) && !World.Get(x, z).blocked;
    }
}
```

## Assets/Farm/Runtime/FarmHUD.cs

```csharp
using System;
using System.Collections.Generic;
using NextGenFarm.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace NextGenFarm
{
    public enum FarmIcon { Leaf, Coin, Diamond, Medal, Plow, Seed, Water, Harvest, Tractor, Auto, Shop, Sun, Corn, Carrot, Wheat, Photo, Save }
    public static class FarmUIArt
    {
        private static readonly Dictionary<FarmIcon,Sprite> icons=new Dictionary<FarmIcon,Sprite>();
        private static Sprite rounded;
        public static Sprite Rounded
        {
            get
            {
                if(rounded!=null)return rounded;
                const int n=64;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false);tex.filterMode=FilterMode.Bilinear;
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                {
                    Vector2 q=new Vector2(Mathf.Abs(x-31.5f)-15.5f,Mathf.Abs(y-31.5f)-15.5f);
                    float d=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-15;
                    tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(.5f-d)));
                }
                tex.Apply();rounded=Sprite.Create(tex,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(22,22,22,22));return rounded;
            }
        }
        public static Sprite Icon(FarmIcon type)
        {
            if(icons.TryGetValue(type,out var s))return s;
            const int n=96;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false);var pixels=new Color[n*n];
            Color green=new Color(.29f,.51f,.15f),gold=new Color(.95f,.72f,.22f),brown=new Color(.43f,.28f,.14f),blue=new Color(.16f,.65f,.79f),white=new Color(1,.98f,.86f);
            Action<float,float,float,Color> circle=(cx,cy,r,c)=>{for(int y=Mathf.Max(0,(int)(cy-r-1));y<Mathf.Min(n,cy+r+2);y++)for(int x=Mathf.Max(0,(int)(cx-r-1));x<Mathf.Min(n,cx+r+2);x++){float a=Mathf.Clamp01(r+.5f-Vector2.Distance(new Vector2(x,y),new Vector2(cx,cy)));if(a>0)pixels[y*n+x]=Color.Lerp(pixels[y*n+x],c,a);}};
            Action<Vector2,Vector2,float,Color> line=(a,b,w,c)=>{int steps=Mathf.CeilToInt(Vector2.Distance(a,b)*1.5f);for(int i=0;i<=steps;i++){var p=Vector2.Lerp(a,b,i/(float)Mathf.Max(1,steps));circle(p.x,p.y,w,c);}};
            Action<float,float,float,float,Color> rect=(x,y,w,h,c)=>{for(int py=Mathf.Max(0,(int)y);py<Mathf.Min(n,y+h);py++)for(int px=Mathf.Max(0,(int)x);px<Mathf.Min(n,x+w);px++)pixels[py*n+px]=c;};
            if(type==FarmIcon.Coin||type==FarmIcon.Medal||type==FarmIcon.Sun)
            {circle(48,48,31,brown);circle(48,51,29,gold);circle(48,51,22,new Color(1,.84f,.37f));if(type==FarmIcon.Medal){line(new Vector2(35,23),new Vector2(26,7),7,blue);line(new Vector2(61,23),new Vector2(70,7),7,blue);}else{rect(43,35,10,32,white);rect(35,44,27,8,white);}}
            else if(type==FarmIcon.Diamond)
            {for(int y=15;y<82;y++)for(int x=12;x<84;x++)if(Mathf.Abs(x-48)/35f+Mathf.Abs(y-49)/34f<1)pixels[y*n+x]=x<48?new Color(.38f,.83f,.85f):blue;line(new Vector2(31,64),new Vector2(48,78),2,white);}
            else if(type==FarmIcon.Water)
            {for(int y=24;y<77;y++){float w=y>48?(77-y)*.65f:Mathf.Sqrt(Mathf.Max(0,25*25-(y-46)*(y-46)));for(int x=(int)(48-w);x<48+w;x++)pixels[y*n+x]=blue;}circle(40,38,6,white);}
            else if(type==FarmIcon.Tractor||type==FarmIcon.Auto)
            {rect(23,34,52,25,new Color(.86f,.38f,.2f));rect(30,59,26,16,blue);rect(34,63,18,9,white);circle(29,29,17,brown);circle(73,29,12,brown);circle(29,29,9,gold);circle(73,29,6,gold);rect(67,59,5,16,brown);}
            else if(type==FarmIcon.Plow||type==FarmIcon.Harvest)
            {line(new Vector2(24,17),new Vector2(65,72),5,brown);if(type==FarmIcon.Plow){line(new Vector2(48,71),new Vector2(79,57),9,blue);}else{for(int i=0;i<20;i++){float a=(20+i*7)*Mathf.Deg2Rad;circle(50+Mathf.Cos(a)*27,56+Mathf.Sin(a)*27,5,gold);}}}
            else if(type==FarmIcon.Shop)
            {rect(20,20,56,41,brown);rect(27,25,42,30,white);for(int i=0;i<5;i++)rect(16+i*13,57,13,18,i%2==0?green:gold);rect(45,24,13,25,green);}
            else if(type==FarmIcon.Photo||type==FarmIcon.Save)
            {rect(17,23,62,46,brown);rect(28,67,23,10,brown);circle(48,46,20,blue);circle(48,46,12,white);circle(53,51,5,blue);}
            else if(type==FarmIcon.Corn||type==FarmIcon.Wheat)
            {for(int row=0;row<7;row++)for(int col=0;col<3;col++)circle(37+col*10,28+row*7,5,gold);line(new Vector2(22,19),new Vector2(37,43),7,green);line(new Vector2(75,20),new Vector2(58,47),7,green);}
            else if(type==FarmIcon.Carrot)
            {for(int y=15;y<61;y++){float w=(y-10)*.28f;for(int x=(int)(48-w);x<48+w;x++)pixels[y*n+x]=new Color(.96f,.46f,.13f);}line(new Vector2(48,59),new Vector2(25,79),5,green);line(new Vector2(48,60),new Vector2(51,85),5,green);line(new Vector2(49,60),new Vector2(73,78),5,green);}
            else
            {for(int i=0;i<23;i++){float t=i/22f;circle(27+t*40,25+t*48,Mathf.Sin(t*Mathf.PI)*17,green);}line(new Vector2(23,18),new Vector2(67,73),2,white);}
            tex.SetPixels(pixels);tex.Apply();s=Sprite.Create(tex,new Rect(0,0,n,n),new Vector2(.5f,.5f));icons.Add(type,s);return s;
        }
    }

    public sealed class FarmHUD : MonoBehaviour
    {
        private FarmBootstrap app;
        private Canvas canvas;
        private RectTransform root,modal,modalBlocker;
        private Font font,bold;
        private Text coins,diamonds,medals,season,seasonTime,contextTitle,contextInfo,toast,questWater,questHarvest;
        private Image contextProgress,minimap;
        private RectTransform playerDot;
        private Texture2D mapTexture;
        private readonly List<Image> toolFaces=new List<Image>();
        private readonly List<RectTransform> waterMarkers=new List<RectTransform>();
        private readonly List<Text> toolCaptions=new List<Text>();
        private float nextTick,toastUntil;
        private readonly Color cream=new Color(.985f,.955f,.86f),ink=new Color(.18f,.28f,.20f),green=new Color(.28f,.46f,.19f),gold=new Color(.82f,.67f,.36f);
        public void Initialize(FarmBootstrap bootstrap)
        {
            app=bootstrap;font=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Arial"},20);bold=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI Semibold","Arial"},24);
            if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(bold==null)bold=font;
            var go=new GameObject("Đồng Nội · HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            root=go.GetComponent<RectTransform>();
            if(FindFirstObjectByType<EventSystem>()==null)
            {
                var events=new GameObject("Farm UI input",typeof(EventSystem));events.transform.SetParent(transform,false);
#if ENABLE_LEGACY_INPUT_MANAGER
                events.AddComponent<StandaloneInputModule>();
#elif ENABLE_INPUT_SYSTEM
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#endif
            }
            for(int i=0;i<12;i++)
            {
                var m=Panel(root,"Water marker",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(42,48),new Color(1,.91f,.56f));
                Icon(m,FarmIcon.Water,new Vector2(4,5),new Vector2(34,37));m.gameObject.SetActive(false);waterMarkers.Add(m);
            }
            Header();Dock();Context();Quests();MiniMap();
            toast=Label(root,"",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-116),new Vector2(600,38),22,Color.white,TextAnchor.MiddleCenter,true);
            toast.gameObject.AddComponent<Shadow>().effectColor=new Color(0,0,0,.6f);
            app.Grid.ActionApplied+=OnAction;
            ShowToast("Chào mừng đến Đồng Nội · Tưới ngô để cây lớn nhanh hơn");
        }
        private RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;return r;}
        private Image Image(RectTransform r,Color color)
        {var image=r.gameObject.AddComponent<Image>();image.sprite=FarmUIArt.Rounded;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=color;image.raycastTarget=false;return image;}
        private RectTransform Panel(Transform p,string name,Vector2 a,Vector2 pv,Vector2 pos,Vector2 size,Color color)
        {var r=Rect(p,name,a,pv,pos,size);var im=Image(r,color);var shadow=im.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.08f,.16f,.10f,.19f);shadow.effectDistance=new Vector2(0,-4);return r;}
        private Text Label(Transform p,string value,Vector2 a,Vector2 pv,Vector2 pos,Vector2 size,int fontSize,Color color,TextAnchor alignment=TextAnchor.MiddleLeft,bool strong=false)
        {var r=Rect(p,value,a,pv,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=strong?bold:font;t.text=value;t.fontSize=fontSize;t.color=color;t.alignment=alignment;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        private void Icon(Transform p,FarmIcon icon,Vector2 pos,Vector2 size)
        {var r=Rect(p,icon.ToString(),Vector2.zero,Vector2.zero,pos,size);var im=r.gameObject.AddComponent<Image>();im.sprite=FarmUIArt.Icon(icon);im.raycastTarget=false;}
        private Button Button(Transform p,string title,Vector2 a,Vector2 pv,Vector2 pos,Vector2 size,Action action,Color? color=null)
        {var r=Panel(p,title,a,pv,pos,size,color??cream);var im=r.GetComponent<Image>();im.raycastTarget=true;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;var colors=b.colors;colors.highlightedColor=new Color(1,.96f,.77f);colors.pressedColor=new Color(.8f,.88f,.64f);b.colors=colors;b.onClick.AddListener(()=>action());if(!string.IsNullOrEmpty(title))Label(r,title,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(12,4),18,ink,TextAnchor.MiddleCenter,true);return b;}
        private void Header()
        {
            var brand=Panel(root,"Farm identity",new Vector2(0,1),new Vector2(0,1),new Vector2(26,-24),new Vector2(340,80),new Color(.12f,.28f,.22f,.93f));
            Icon(brand,FarmIcon.Leaf,new Vector2(14,13),new Vector2(54,54));
            Label(brand,"ĐỒNG NỘI",Vector2.zero,Vector2.zero,new Vector2(82,32),new Vector2(240,35),27,cream,TextAnchor.MiddleLeft,true);
            Label(brand,"Nông trại bên thung lũng",Vector2.zero,Vector2.zero,new Vector2(83,13),new Vector2(230,24),15,new Color(.78f,.86f,.66f));
            coins=Resource(FarmIcon.Coin,400);diamonds=Resource(FarmIcon.Diamond,590);medals=Resource(FarmIcon.Medal,755);
            var weather=Panel(root,"Season",new Vector2(1,1),new Vector2(1,1),new Vector2(-26,-24),new Vector2(266,80),cream);
            Icon(weather,FarmIcon.Sun,new Vector2(16,16),new Vector2(48,48));
            season=Label(weather,"MÙA HẠ",Vector2.zero,Vector2.zero,new Vector2(80,40),new Vector2(175,27),21,ink,TextAnchor.MiddleLeft,true);
            seasonTime=Label(weather,"Ngày yên bình",Vector2.zero,Vector2.zero,new Vector2(80,15),new Vector2(175,24),16,green);
        }
        private Text Resource(FarmIcon icon,float x)
        {var r=Panel(root,icon.ToString(),new Vector2(0,1),new Vector2(0,1),new Vector2(x,-32),new Vector2(icon==FarmIcon.Coin?172:148,60),cream);Icon(r,icon,new Vector2(12,10),new Vector2(40,40));return Label(r,"0",Vector2.zero,Vector2.zero,new Vector2(60,10),new Vector2(100,40),24,ink,TextAnchor.MiddleLeft,true);}
        private void Dock()
        {
            var dock=Panel(root,"Tools",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,28),new Vector2(768,116),new Color(.13f,.27f,.21f,.95f));
            var icons=new[]{FarmIcon.Plow,FarmIcon.Seed,FarmIcon.Water,FarmIcon.Harvest,FarmIcon.Tractor,FarmIcon.Auto,FarmIcon.Shop};
            string[] names={"Cày đất","Gieo hạt","Tưới cây","Thu hoạch","Máy cày","Tự lái","Cửa hàng"};string[] keys={"1","2","3","4","T","R","B"};
            for(int i=0;i<7;i++)
            {
                int index=i;var b=Button(dock,"",Vector2.zero,Vector2.zero,new Vector2(14+i*106,13),new Vector2(98,90),()=>Tool(index),i==2?new Color(.94f,.85f,.55f):cream);
                Icon(b.transform,icons[i],new Vector2(29,36),new Vector2(40,40));
                toolCaptions.Add(Label(b.transform,names[i],Vector2.zero,Vector2.zero,new Vector2(0,8),new Vector2(98,22),14,ink,TextAnchor.MiddleCenter,true));
                Label(b.transform,keys[i],new Vector2(1,1),new Vector2(1,1),new Vector2(-6,-4),new Vector2(18,18),12,green,TextAnchor.MiddleCenter,true);
                toolFaces.Add(b.GetComponent<Image>());
            }
            Label(root,"WASD  Di chuyển   •   Chuột  Xoay camera   •   Alt / Tab  Con trỏ   •   Esc  Menu",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,5),new Vector2(950,20),14,Color.white,TextAnchor.MiddleCenter);
        }
        private void Tool(int index)
        {if(index<4){app.Driver.SelectedAction=(FarmAction)index;ShowToast(new[]{"Chọn đất trống rồi nhấn Space để cày","Chọn giống trong cửa hàng, nhấn Space để gieo","Tưới một lần mỗi vụ để giảm thời gian chờ","Cây chín luôn chờ bạn quay lại"}[index]);}else if(index==4)app.Driver.SetMounted(!app.Driver.Mounted);else if(index==5)app.Driver.ToggleAuto();else OpenShop(0);}
        private void Context()
        {
            var card=Panel(root,"Crop context",new Vector2(0,0),Vector2.zero,new Vector2(26,28),new Vector2(322,174),cream);
            Icon(card,FarmIcon.Corn,new Vector2(15,93),new Vector2(54,54));
            contextTitle=Label(card,"Ngô",Vector2.zero,Vector2.zero,new Vector2(82,118),new Vector2(224,30),23,ink,TextAnchor.MiddleLeft,true);
            contextInfo=Label(card,"Đang sinh trưởng",Vector2.zero,Vector2.zero,new Vector2(82,87),new Vector2(224,30),15,green);
            var bar=Panel(card,"Growth",Vector2.zero,Vector2.zero,new Vector2(18,70),new Vector2(286,7),new Color(.83f,.86f,.7f));
            contextProgress=Image(Rect(bar,"Fill",Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(0,7)),green);
            Button(card,"SPACE   Thao tác",Vector2.zero,Vector2.zero,new Vector2(16,18),new Vector2(172,38),()=>app.Driver.UseTool(),new Color(.86f,.89f,.69f));
            Button(card,"Giống cây",Vector2.zero,Vector2.zero,new Vector2(196,18),new Vector2(110,38),()=>OpenShop(0));
        }
        private void Quests()
        {
            var card=Panel(root,"Farm requests",new Vector2(1,1),new Vector2(1,1),new Vector2(-26,-125),new Vector2(286,183),cream);
            Label(card,"MỘT NGÀY Ở ĐỒNG NỘI",new Vector2(0,1),new Vector2(0,1),new Vector2(18,-12),new Vector2(252,28),16,ink,TextAnchor.MiddleLeft,true);
            questWater=Label(card,"Tưới cây   0 / 9",Vector2.zero,Vector2.zero,new Vector2(18,91),new Vector2(250,29),17,green);
            questHarvest=Label(card,"Thu hoạch   0 / 9",Vector2.zero,Vector2.zero,new Vector2(18,58),new Vector2(250,29),17,green);
            Button(card,"Nhận quà chăm sóc",Vector2.zero,Vector2.zero,new Vector2(17,13),new Vector2(252,35),()=>{bool got=app.Grid.World.ClaimCareReward(true)|app.Grid.World.ClaimCareReward(false);ShowToast(got?"Đã nhận phần thưởng chăm sóc nông trại":"Hoàn thành 9 ô tưới hoặc 9 ô thu hoạch để nhận quà");if(got)app.Saves.Save();},new Color(.88f,.9f,.74f));
            Button(root,"Ảnh  F12",new Vector2(1,1),new Vector2(1,1),new Vector2(-26,-322),new Vector2(135,38),()=>app.TakePhoto());
            Button(root,"Lưu",new Vector2(1,1),new Vector2(1,1),new Vector2(-171,-322),new Vector2(100,38),()=>{app.Saves.Save();ShowToast("Nông trại đã được lưu");});
        }
        private void MiniMap()
        {
            var card=Panel(root,"Farm map",new Vector2(1,0),new Vector2(1,0),new Vector2(-26,28),new Vector2(232,218),cream);
            Label(card,"THUNG LŨNG XANH",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-8),new Vector2(220,26),14,ink,TextAnchor.MiddleCenter,true);
            var r=Rect(card,"Map",Vector2.zero,Vector2.zero,new Vector2(17,15),new Vector2(198,168));mapTexture=new Texture2D(64,64,TextureFormat.RGBA32,false);mapTexture.filterMode=FilterMode.Point;
            minimap=r.gameObject.AddComponent<Image>();minimap.sprite=Sprite.Create(mapTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f));minimap.raycastTarget=false;
            playerDot=Rect(r,"You",Vector2.zero,new Vector2(.5f,.5f),Vector2.zero,new Vector2(10,10));Image(playerDot,new Color(1,.9f,.5f));
        }
        public void ShowToast(string text){if(toast!=null){toast.text=text;toastUntil=Time.unscaledTime+4.5f;}}
        public void SetVisible(bool visible) { if(root!=null)root.gameObject.SetActive(visible); }
        public void SetPointerInteraction(bool enabled) { if(canvas!=null)canvas.GetComponent<GraphicRaycaster>().enabled=enabled; }
        private void OnAction(Vector2Int tile,FarmAction action,int count)
        {ShowToast(action==FarmAction.Harvest?"Thu hoạch thành công  ·  "+count+" ô":action==FarmAction.Water?"Đã tưới "+count+" ô  ·  Cây lớn nhanh hơn":action==FarmAction.Sow?"Đã gieo "+count+" ô "+CropName(app.Driver.SelectedCrop):"Đã cày "+count+" ô đất");}
        public static string CropName(string id)=>id=="corn"?"Ngô":id=="wheat"?"Lúa mì":id=="carrot"?"Cà rốt":id;
        private void Update()
        {
            if(app==null||app.Grid==null)return;
            if(Time.unscaledTime>toastUntil)toast.text="";
            for(int i=0;i<toolFaces.Count;i++) toolFaces[i].color=i<4&&i==(int)app.Driver.SelectedAction?new Color(.95f,.85f,.48f):i==4&&app.Driver.Mounted?new Color(.84f,.91f,.65f):i==5&&app.Driver.Automatic?new Color(.67f,.87f,.78f):cream;
            if(Time.unscaledTime<nextTick)return;nextTick=Time.unscaledTime+.15f;
            var w=app.Grid.World;coins.text=w.Wallet.coins.ToString("N0");diamonds.text=w.Wallet.diamonds.ToString("N0");medals.text=w.Wallet.medals.ToString("N0");
            string[] names={"MÙA XUÂN","MÙA HẠ","MÙA THU","MÙA ĐÔNG"};season.text=names[(int)app.Clock.CurrentSeason];
            var remaining=TimeSpan.FromSeconds(app.Clock.SecondsRemaining);seasonTime.text=remaining.ToString(@"mm\:ss")+"  ·  "+(app.Clock.DayFraction>.25f&&app.Clock.DayFraction<.75f?"Ban ngày":"Ban đêm");
            var c=app.Driver.TargetCell;var tile=w.Get(c.x,c.y);
            contextTitle.text=tile!=null&&tile.HasCrop?CropName(tile.cropId):tile!=null&&tile.road?"Đường đá":"Đất nông trại";
            contextInfo.text=tile==null?"Ngoài ranh giới nông trại":tile.HasCrop?(tile.Ready(app.Clock.Now)?"Đã chín · Sẵn sàng thu hoạch":Math.Max(0,tile.readyUtc-app.Clock.Now).ToString("F0")+" giây  ·  "+(tile.wateredThisCycle?"Đã tưới":"Cần tưới")):(tile.tilled?"Sẵn sàng gieo hạt":"Chọn công cụ để bắt đầu");
            contextProgress.rectTransform.sizeDelta=new Vector2(286*(tile!=null?tile.Growth(app.Clock.Now):0),7);
            questWater.text="Tưới cây    "+Mathf.Min(9,w.Wallet.watered)+" / 9"+(w.Wallet.waterRewardClaimed?"  ✓":"");
            questHarvest.text="Thu hoạch    "+Mathf.Min(9,w.Wallet.harvested)+" / 9"+(w.Wallet.harvestRewardClaimed?"  ✓":"");
            Markers();Map();
        }
        private void Markers()
        {
            int used=0;var player=app.Grid.WorldToCell(app.Driver.transform.position);var camera=Camera.main;
            for(int z=player.y-1;z<=player.y+6&&used<waterMarkers.Count;z++)for(int x=player.x-4;x<=player.x+4&&used<waterMarkers.Count;x++)
            {
                var tile=app.Grid.World.Get(x,z);if((x+z)%2!=0||tile==null||!tile.HasCrop||tile.wateredThisCycle||tile.Ready(app.Clock.Now))continue;
                Vector3 p=camera.WorldToScreenPoint(app.Grid.CellCenter(x,z)+Vector3.up*(tile.cropId=="corn"?2.05f:1.15f));
                if(p.z<0||p.x<20||p.x>Screen.width-20||p.y<130||p.y>Screen.height-60)continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,new Vector2(p.x,p.y),null,out var local);
                var marker=waterMarkers[used++];marker.gameObject.SetActive(true);marker.anchoredPosition=local;
            }
            for(int i=used;i<waterMarkers.Count;i++)waterMarkers[i].gameObject.SetActive(false);
        }
        private void Map()
        {
            var w=app.Grid.World;
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {var t=w.Get(x*w.Width/64,y*w.Depth/64);mapTexture.SetPixel(x,y,t.blocked?new Color(.7f,.48f,.3f):t.road?new Color(.85f,.8f,.62f):t.HasCrop?t.cropId=="corn"?new Color(.38f,.64f,.2f):t.cropId=="wheat"?new Color(.83f,.65f,.25f):new Color(.3f,.53f,.22f):new Color(.55f,.67f,.35f));}
            mapTexture.Apply();var p=app.Grid.WorldToCell(app.Driver.transform.position);playerDot.anchoredPosition=new Vector2((p.x+.5f)/w.Width*198,(p.y+.5f)/w.Depth*168);
        }
        public void OpenShop(int page)
        {
            CloseShop();
            FarmGameFlow.Instance?.CatalogOpened();
            modalBlocker=Rect(root,"Catalog backdrop",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(4000,2400));
            Image(modalBlocker,new Color(.04f,.12f,.09f,.55f)).raycastTarget=true;
            modal=Panel(modalBlocker,"Catalog",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(960,600),cream);
            modal.GetComponent<Image>().raycastTarget=true;
            Label(modal,"CỬA HÀNG NÔNG TRẠI",new Vector2(0,1),new Vector2(0,1),new Vector2(35,-26),new Vector2(800,42),30,ink,TextAnchor.MiddleLeft,true);
            Button(modal,"×",new Vector2(1,1),new Vector2(1,1),new Vector2(-20,-20),new Vector2(44,44),CloseShop);
            string[] tabs={"Hạt giống","Kho nông sản","Vật nuôi"};for(int i=0;i<3;i++){int index=i;Button(modal,tabs[i],new Vector2(0,1),new Vector2(0,1),new Vector2(36+i*230,-90),new Vector2(216,45),()=>OpenShop(index),i==page?new Color(.85f,.89f,.66f):new Color(.94f,.91f,.8f));}
            if(page==0)
            {
                for(int i=0;i<app.crops.Length;i++)
                {
                    var crop=app.crops[i];var card=Panel(modal,crop.id,new Vector2(0,1),new Vector2(0,1),new Vector2(36+i*295,-160),new Vector2(274,362),Color.white);
                    Icon(card,crop.id=="corn"?FarmIcon.Corn:crop.id=="wheat"?FarmIcon.Wheat:FarmIcon.Carrot,new Vector2(83,214),new Vector2(108,108));
                    Label(card,CropName(crop.id),Vector2.zero,Vector2.zero,new Vector2(22,171),new Vector2(230,38),27,ink,TextAnchor.MiddleCenter,true);
                    Label(card,crop.seedCost+" xu / ô   ·   "+Mathf.RoundToInt((float)crop.growSeconds/60)+" phút",Vector2.zero,Vector2.zero,new Vector2(22,129),new Vector2(230,30),18,green,TextAnchor.MiddleCenter);
                    Label(card,"Tưới giảm "+crop.waterReductionSeconds+" giây",Vector2.zero,Vector2.zero,new Vector2(22,96),new Vector2(230,30),16,green,TextAnchor.MiddleCenter);
                    Button(card,"Chọn giống",Vector2.zero,Vector2.zero,new Vector2(22,25),new Vector2(230,48),()=>{app.Driver.SelectedCrop=crop.id;app.Driver.SelectedAction=FarmAction.Sow;CloseShop();ShowToast("Đã chọn "+CropName(crop.id)+" · Nhấn Space để gieo");},new Color(.84f,.9f,.65f));
                }
            }
            else if(page==1)
            {
                int row=0;foreach(var entry in app.Grid.World.Wallet.inventory){Label(modal,CropName(entry.id)+"                         "+entry.count, new Vector2(0,1),new Vector2(0,1),new Vector2(48,-172-row*42),new Vector2(820,36),22,ink);row++;}
                if(row==0)Label(modal,"Kho đang trống. Thu hoạch cây chín để có nông sản nhé!",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(800,80),24,green,TextAnchor.MiddleCenter);
                Button(modal,"Bán toàn bộ nông sản",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,35),new Vector2(340,50),()=>{app.SellAll();OpenShop(1);},new Color(.84f,.9f,.65f));
            }
            else
            {
                Label(modal,"Chuồng bò · Sữa tươi mỗi đợt cho ăn",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,40),new Vector2(800,70),26,green,TextAnchor.MiddleCenter,true);
                Button(modal,"Cho ăn",new Vector2(.5f,.5f),new Vector2(1,.5f),new Vector2(-15,-60),new Vector2(250,55),()=>{int n=app.Animals.FeedAll();ShowToast(n>0?"Đã cho vật nuôi ăn":"Chuồng đã có thức ăn hoặc chưa đủ xu");});
                Button(modal,"Thu sản phẩm",new Vector2(.5f,.5f),new Vector2(0,.5f),new Vector2(15,-60),new Vector2(250,55),()=>{int n=app.Animals.HarvestAll();ShowToast(n>0?"Đã thu sữa vào kho":"Sản phẩm chưa sẵn sàng");});
            }
        }
        public bool IsShopOpen => modalBlocker != null;
        public void CloseShop(){bool wasOpen=IsShopOpen;if(modalBlocker!=null){modalBlocker.gameObject.SetActive(false);Destroy(modalBlocker.gameObject);}modal=null;modalBlocker=null;if(wasOpen)FarmGameFlow.Instance?.CatalogClosed();}
        private void OnDestroy(){if(app!=null&&app.Grid!=null)app.Grid.ActionApplied-=OnAction;if(mapTexture!=null)Destroy(mapTexture);if(font!=null)Destroy(font);if(bold!=null&&bold!=font)Destroy(bold);}
    }
}
```

## Assets/Farm/Runtime/FarmInput.cs

```csharp
using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace NextGenFarm
{
    // Works with either Unity input backend; no named Input Manager axes required.
    public static class FarmInput
    {
        public static bool Suppressed { get; set; }
        private static bool Available => Application.isFocused && !Suppressed;
        private static bool Active => Available && FarmGameFlow.GameplayAllowed;
        public static Vector2 Look
        {
            get
            {
                if(!Active || (FarmGameFlow.Instance != null && !FarmGameFlow.Instance.CanLook))return Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                if(Mouse.current!=null)return Mouse.current.delta.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Cursor.lockState == CursorLockMode.Locked) return new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*12;
#endif
                return Vector2.zero;
            }
        }
        private static readonly Dictionary<KeyCode, int> guiPresses = new Dictionary<KeyCode, int>();
        private static readonly Dictionary<KeyCode, int> handledFrames = new Dictionary<KeyCode, int>();
        private static readonly HashSet<KeyCode> guiHeld = new HashSet<KeyCode>();
        public static void ClearGUIState() { guiPresses.Clear(); guiHeld.Clear(); }
        public static void CaptureGUIEvent(Event evt)
        {
            if(!Available)return;
            if (evt == null || (evt.type != EventType.KeyDown && evt.type != EventType.KeyUp)) return;
            KeyCode code = evt.keyCode;
            bool characterOnly = code == KeyCode.None;
            if (characterOnly && evt.character != '\0') code = (KeyCode)char.ToLowerInvariant(evt.character);
            if (!IsGameKey(code)) return;
            if (evt.type == EventType.KeyUp) { guiHeld.Remove(code); return; }
            if (!characterOnly && !guiHeld.Add(code)) return;
            if (handledFrames.TryGetValue(code, out int frame) && frame >= Time.frameCount - 1) return;
            guiPresses[code] = Time.frameCount + 2;
        }
        private static bool IsGameKey(KeyCode code) => code == KeyCode.W || code == KeyCode.A || code == KeyCode.S || code == KeyCode.D ||
            code == KeyCode.UpArrow || code == KeyCode.DownArrow || code == KeyCode.LeftArrow || code == KeyCode.RightArrow ||
            code == KeyCode.T || code == KeyCode.R || code == KeyCode.F || code == KeyCode.G || code == KeyCode.B ||
            code == KeyCode.V || code == KeyCode.P || code == KeyCode.Space || code == KeyCode.F12 ||
            code == KeyCode.Escape || code == KeyCode.Tab || code == KeyCode.LeftAlt || code == KeyCode.RightAlt ||
            code == KeyCode.Equals || code == KeyCode.Minus || (code >= KeyCode.Alpha1 && code <= KeyCode.Alpha4);
        private static bool Consume(KeyCode code, bool hardware)
        {
            bool buffered = guiPresses.TryGetValue(code, out int expires) && expires >= Time.frameCount;
            guiPresses.Remove(code);
            if (!hardware && !buffered) return false;
            if (handledFrames.TryGetValue(code, out int frame) && frame >= Time.frameCount - 1) return false;
            handledFrames[code] = Time.frameCount;
            return true;
        }
        public static bool MenuPress(KeyCode code)
        {
            if (!Available) return false;
            bool pressed = false;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                switch (code)
                {
                    case KeyCode.Escape: pressed = keyboard.escapeKey.wasPressedThisFrame; break;
                    case KeyCode.Tab: pressed = keyboard.tabKey.wasPressedThisFrame; break;
                    case KeyCode.LeftAlt: pressed = keyboard.leftAltKey.wasPressedThisFrame; break;
                    case KeyCode.RightAlt: pressed = keyboard.rightAltKey.wasPressedThisFrame; break;
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            pressed |= Input.GetKeyDown(code);
#endif
            return Consume(code, pressed);
        }
        public static Vector2 Move
        {
            get
            {
                if(!Active)return Vector2.zero;
                Vector2 native = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                if (k != null) native = new Vector2((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0));
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (native.sqrMagnitude == 0) native = new Vector2((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
#endif
                if (native.sqrMagnitude > 0) return native;
                return new Vector2((guiHeld.Contains(KeyCode.D) || guiHeld.Contains(KeyCode.RightArrow) ? 1 : 0) -
                    (guiHeld.Contains(KeyCode.A) || guiHeld.Contains(KeyCode.LeftArrow) ? 1 : 0),
                    (guiHeld.Contains(KeyCode.W) || guiHeld.Contains(KeyCode.UpArrow) ? 1 : 0) -
                    (guiHeld.Contains(KeyCode.S) || guiHeld.Contains(KeyCode.DownArrow) ? 1 : 0));
            }
        }
        public static bool Press(char key)
        {
            if(!Active)return false;
            KeyCode code = key == '+' ? KeyCode.Equals : key == '-' ? KeyCode.Minus : (KeyCode)key;
            bool native = NativePress(key);
#if ENABLE_LEGACY_INPUT_MANAGER
            native |= Input.GetKeyDown(code);
#endif
            return Consume(code, native);
        }
        private static bool NativePress(char key)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case '1': return k.digit1Key.wasPressedThisFrame;
                case '2': return k.digit2Key.wasPressedThisFrame;
                case '3': return k.digit3Key.wasPressedThisFrame;
                case '4': return k.digit4Key.wasPressedThisFrame;
                case 't': return k.tKey.wasPressedThisFrame;
                case 'r': return k.rKey.wasPressedThisFrame;
                case 'f': return k.fKey.wasPressedThisFrame;
                case 'g': return k.gKey.wasPressedThisFrame;
                case 'b': return k.bKey.wasPressedThisFrame;
                case 'v': return k.vKey.wasPressedThisFrame;
                case 'p': return k.pKey.wasPressedThisFrame;
                case '+': return k.equalsKey.wasPressedThisFrame;
                case '-': return k.minusKey.wasPressedThisFrame;
                case ' ': return k.spaceKey.wasPressedThisFrame;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key == '+' ? KeyCode.Equals : key == '-' ? KeyCode.Minus : (KeyCode)key);
#else
            return false;
#endif
        }
        public static bool ActionHeld => Active && (NativeActionHeld || guiHeld.Contains(KeyCode.Space) || Press(' '));
        public static bool PhotoPressed
        {
            get
            {
                if(!Active)return false;
                bool pressed = false;
#if ENABLE_INPUT_SYSTEM
                pressed = Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                pressed |= Input.GetKeyDown(KeyCode.F12);
#endif
                return Consume(KeyCode.F12, pressed);
            }
        }
        private static bool NativeActionHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKey(KeyCode.Space);
#else
                return false;
#endif
            }
        }
        public static float Scroll
        {
            get
            {
                if(!Active)return 0;
#if ENABLE_INPUT_SYSTEM
                return Mouse.current == null ? 0 : Mouse.current.scroll.ReadValue().y / 120f;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.mouseScrollDelta.y;
#else
                return 0;
#endif
            }
        }
    }
}
```

## Assets/Farm/Runtime/FarmInstanceRenderer.cs

```csharp
using System;
using System.Collections.Generic;
using NextGenFarm.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace NextGenFarm
{
    public sealed class FarmInstanceRenderer : MonoBehaviour
    {
        private const int ChunkSize = 16;
        private sealed class Batch
        {
            public Mesh mesh;
            public Material material;
            public readonly Matrix4x4[] matrices = new Matrix4x4[ChunkSize * ChunkSize];
            public int count;
            public int submesh;
        }
        private sealed class Chunk
        {
            public int x, z;
            public bool dirty = true;
            public double nextChange = double.PositiveInfinity;
            public readonly List<Batch> batches = new List<Batch>();
        }
        private FarmGridManager grid;
        private FarmVisualLibrary library;
        private Mesh cube;
        private Material[] fallback;
        private Chunk[] chunks;
        private int chunksX;
        private bool subscribed;
        private TractorController driver;
        private readonly Matrix4x4[] marker = new Matrix4x4[4];
        public int DrawCalls { get; private set; }
        public void Track(TractorController tractor) { driver = tractor; }
        public void Initialize(FarmGridManager farm, FarmVisualLibrary visuals, Mesh mesh, Material[] materials)
        {
            grid = farm; library = visuals; cube = mesh; fallback = materials;
            chunksX = (grid.World.Width + ChunkSize - 1) / ChunkSize;
            int chunksZ = (grid.World.Depth + ChunkSize - 1) / ChunkSize;
            chunks = new Chunk[chunksX * chunksZ];
            for (int z = 0; z < chunksZ; z++)
                for (int x = 0; x < chunksX; x++) chunks[z * chunksX + x] = new Chunk { x = x, z = z };
            grid.World.Changed += Dirty; subscribed = true;
            if (!SystemInfo.supportsInstancing)
            { Debug.LogError("This farm renderer requires GPU instancing support."); enabled = false; }
        }
        private void Dirty(int index)
        {
            int x = index % grid.World.Width, z = index / grid.World.Width;
            chunks[(z / ChunkSize) * chunksX + x / ChunkSize].dirty = true;
        }
        private void Add(Chunk chunk, Mesh mesh, Material material, Matrix4x4 matrix, int submesh = 0)
        {
            if (mesh == null || material == null) return;
            Batch batch = null;
            foreach (var candidate in chunk.batches)
                if (candidate.mesh == mesh && candidate.material == material && candidate.submesh == submesh && candidate.count < candidate.matrices.Length)
                { batch = candidate; break; }
            if (batch == null)
            {
                material.enableInstancing = true;
                batch = new Batch { mesh = mesh, material = material, submesh = submesh }; chunk.batches.Add(batch);
            }
            batch.matrices[batch.count++] = matrix;
        }
        private void Rebuild(Chunk chunk, double now)
        {
            foreach (var batch in chunk.batches) batch.count = 0;
            chunk.nextChange = double.PositiveInfinity;
            int endX = Math.Min(grid.World.Width, (chunk.x + 1) * ChunkSize);
            int endZ = Math.Min(grid.World.Depth, (chunk.z + 1) * ChunkSize);
            for (int z = chunk.z * ChunkSize; z < endZ; z++)
                for (int x = chunk.x * ChunkSize; x < endX; x++)
                {
                    var tile = grid.World.Get(x, z);
                    var soil = tile.Soil(now);
                    Material ground = library == null ? null : soil == SoilState.Wet ? library.wetSoil :
                        soil == SoilState.Dry ? library.drySoil : library.grass;
                    Vector3 center = grid.CellCenter(x, z);
                    if (tile.road && library != null && library.pathMaterial != null) ground = library.pathMaterial;
                    float h = 0.3f + Mathf.Max(0, tile.height);
                    var groundMatrix = Matrix4x4.TRS(center - Vector3.up * (h * 0.5f), Quaternion.identity, new Vector3(grid.CellSize, h, grid.CellSize));
                    Add(chunk, library != null && library.groundMesh != null ? library.groundMesh : cube,
                        ground != null ? ground : fallback[(int)soil],
                        groundMatrix);
                    if (library != null && library.groundMesh != null && library.groundMesh.subMeshCount > 1)
                        Add(chunk, library.groundMesh, library.cliffMaterial, groundMatrix, 1);
                    if (soil == SoilState.Grass && !tile.road && !tile.blocked && library != null && library.detailGrassMesh != null)
                        for (int tuft = 0; tuft < 3; tuft++)
                        {
                            float a = ((x * 73 + z * 41 + tuft * 29) % 101) / 101f;
                            float b = ((x * 19 + z * 97 + tuft * 47) % 103) / 103f;
                            Add(chunk, library.detailGrassMesh, library.artMaterials[(int)FarmSurface.GrassBlade], Matrix4x4.TRS(
                                center + new Vector3((a - 0.5f) * grid.CellSize, 0.01f, (b - 0.5f) * grid.CellSize),
                                Quaternion.Euler(0, a * 360, 0), Vector3.one * (0.65f + b * 0.6f)));
                        }
                    if (tile.wetUntilUtc > now) chunk.nextChange = Math.Min(chunk.nextChange, tile.wetUntilUtc);
                    if (!tile.HasCrop) continue;
                    float growth = tile.Growth(now);
                    int stage = tile.Ready(now) ? 2 : growth < 0.33f ? 0 : 1;
                    double duration = Math.Max(1, tile.readyUtc - tile.plantedUtc);
                    double transition = stage == 0 ? tile.plantedUtc + duration * 0.33 : tile.readyUtc;
                    if (transition > now) chunk.nextChange = Math.Min(chunk.nextChange, transition);
                    CropVisual visual = null;
                    if (library != null && library.crops != null)
                        foreach (var candidate in library.crops)
                            if (candidate != null && candidate.cropId == tile.cropId) { visual = candidate; break; }
                    Mesh cropMesh = visual != null && visual.stages != null && visual.stages.Length > stage ? visual.stages[stage] : null;
                    Material cropMaterial = visual != null && visual.materials != null && visual.materials.Length > stage ? visual.materials[stage] : null;
                    Vector3 scale = visual != null ? visual.scale : new Vector3(0.55f, stage == 0 ? 0.2f : stage == 1 ? 0.6f : 1, 0.55f);
                    // Imported crop meshes must use a ground-level pivot; fallback cube has a center pivot.
                    Vector3 position = cropMesh != null ? center : center + Vector3.up * (scale.y * 0.5f);
                    Add(chunk, cropMesh != null ? cropMesh : cube, cropMaterial != null ? cropMaterial : fallback[3 + stage],
                        Matrix4x4.TRS(position, Quaternion.Euler(0, (x * 73 + z * 37) % 360, 0), scale));
                    if (cropMesh != null && cropMesh.subMeshCount > 1 && visual.accentMaterials != null && visual.accentMaterials.Length > stage)
                        Add(chunk, cropMesh, visual.accentMaterials[stage], Matrix4x4.TRS(position, Quaternion.Euler(0, (x * 73 + z * 37) % 360, 0), scale), 1);
                }
            chunk.dirty = false;
        }
        private void LateUpdate()
        {
            if (grid == null) return;
            double now = grid.Clock.Now;
            DrawCalls = 0;
            foreach (var chunk in chunks)
            {
                if (chunk.dirty || now >= chunk.nextChange) Rebuild(chunk, now);
                foreach (var batch in chunk.batches)
                {
                    if (batch.count == 0) continue;
                    var parameters = new RenderParams(batch.material) { shadowCastingMode = ShadowCastingMode.On,
                        receiveShadows = true, layer = gameObject.layer };
                    Graphics.RenderMeshInstanced(parameters, batch.mesh, batch.submesh, batch.matrices, batch.count);
                    DrawCalls++;
                }
            }
            if (driver != null)
            {
                var target = driver.TargetCell;
                if (!grid.World.Contains(target.x, target.y)) return;
                float size = grid.CellSize * (driver.Mounted ? 3 : 1);
                Vector3 center = grid.CellCenter(target.x, target.y) + Vector3.up * 0.035f;
                marker[0] = Matrix4x4.TRS(center + Vector3.forward * (size / 2), Quaternion.identity, new Vector3(size, 0.045f, 0.065f));
                marker[1] = Matrix4x4.TRS(center - Vector3.forward * (size / 2), Quaternion.identity, new Vector3(size, 0.045f, 0.065f));
                marker[2] = Matrix4x4.TRS(center + Vector3.right * (size / 2), Quaternion.identity, new Vector3(0.065f, 0.045f, size));
                marker[3] = Matrix4x4.TRS(center - Vector3.right * (size / 2), Quaternion.identity, new Vector3(0.065f, 0.045f, size));
                var parameters = new RenderParams(fallback[5]) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
                Graphics.RenderMeshInstanced(parameters, cube, 0, marker, marker.Length);
            }
        }
        private void OnDestroy() { if (subscribed) grid.World.Changed -= Dirty; }
    }
}
```

## Assets/Farm/Runtime/FarmSceneArt.cs

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NextGenFarm
{
    public enum FarmSurface { Leaf, LeafLight, Bark, Wood, Cream, Roof, Stone, Water, Metal, Glass, Skin, Hair, Denim, Rubber, White, Gold, Coral, Soil, Flower, Mountain, Snow, GrassBlade }

    public sealed class FarmSceneryRenderer : MonoBehaviour
    {
        private sealed class Batch { public Mesh mesh; public Material mat; public Matrix4x4[] matrices = new Matrix4x4[256]; public int count; }
        private readonly List<Batch> batches = new List<Batch>();
        public void Add(Mesh mesh, Material mat, Matrix4x4 matrix)
        {
            Batch target = null;
            foreach (var b in batches) if (b.mesh == mesh && b.mat == mat && b.count < 256) { target = b; break; }
            if (target == null) { target = new Batch { mesh = mesh, mat = mat }; mat.enableInstancing = true; batches.Add(target); }
            target.matrices[target.count++] = matrix;
        }
        private void LateUpdate()
        {
            foreach (var b in batches) Graphics.RenderMeshInstanced(new RenderParams(b.mat) {
                shadowCastingMode = ShadowCastingMode.On, receiveShadows = true }, b.mesh, 0, b.matrices, b.count);
        }
    }
    public sealed class FarmRotor : MonoBehaviour
    { private void Update() { transform.Rotate(0, 0, 14 * Time.deltaTime, Space.Self); } }

    public sealed class FarmActorMotion : MonoBehaviour
    {
        public Transform[] legs, arms, wheels;
        private Vector3 previous;
        private float phase;
        private void OnEnable() { previous = transform.parent != null ? transform.parent.position : transform.position; }
        private void LateUpdate()
        {
            Vector3 position = transform.parent != null ? transform.parent.position : transform.position;
            float distance = Vector3.Distance(previous, position); previous = position;
            phase += distance * 6;
            if (legs != null) for (int i = 0; i < legs.Length; i++) legs[i].localRotation = Quaternion.Euler(distance > 0.001f ? Mathf.Sin(phase + i * Mathf.PI) * 24 : 0, 0, 0);
            if (arms != null) for (int i = 0; i < arms.Length; i++) arms[i].localRotation = Quaternion.Euler(distance > 0.001f ? -Mathf.Sin(phase + i * Mathf.PI) * 19 : -5, 0, i == 0 ? -6 : 6);
            if (wheels != null) foreach (var wheel in wheels) wheel.Rotate(0, distance * 120, 0, Space.Self);
        }
    }

    public sealed class FarmSceneArt : MonoBehaviour
    {
        private FarmVisualLibrary library;
        private FarmGridManager grid;
        private FarmSceneryRenderer instances;
        private readonly Dictionary<PrimitiveType, Mesh> primitives = new Dictionary<PrimitiveType, Mesh>();
        private readonly List<Mesh> owned = new List<Mesh>();
        public void Initialize(FarmGridManager farm, FarmVisualLibrary art)
        { grid = farm; library = art; instances = gameObject.AddComponent<FarmSceneryRenderer>(); }
        public Material Mat(FarmSurface surface) => library.artMaterials[(int)surface];
        private Mesh Primitive(PrimitiveType type)
        {
            if (primitives.TryGetValue(type, out var mesh)) return mesh;
            var go = GameObject.CreatePrimitive(type); mesh = go.GetComponent<MeshFilter>().sharedMesh;
            go.SetActive(false); Destroy(go); primitives.Add(type, mesh); return mesh;
        }
        private Transform Root(string name, Transform parent, Vector3 position)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; return go.transform; }
        private Transform Piece(string name, Transform parent, PrimitiveType type, FarmSurface surface, Vector3 position, Vector3 scale, Vector3 euler = default)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale; go.transform.localRotation = Quaternion.Euler(euler);
            go.AddComponent<MeshFilter>().sharedMesh = Primitive(type); go.AddComponent<MeshRenderer>().sharedMaterial = Mat(surface);
            return go.transform;
        }
        private void Stamp(PrimitiveType type, FarmSurface surface, Vector3 p, Vector3 size, Vector3 rotation = default)
            => instances.Add(Primitive(type), Mat(surface), Matrix4x4.TRS(grid.transform.position + p, Quaternion.Euler(rotation), size));
        public GameObject Farmer(Transform parent)
        {
            var root = Root("Farmer · denim & ponytail", parent, Vector3.zero);
            Piece("Shirt", root, PrimitiveType.Capsule, FarmSurface.White, new Vector3(0, 1.12f, 0), new Vector3(.46f,.33f,.28f));
            Piece("Overalls", root, PrimitiveType.Capsule, FarmSurface.Denim, new Vector3(0,.91f,.025f), new Vector3(.43f,.27f,.3f));
            Piece("Bib", root, PrimitiveType.Cube, FarmSurface.Denim, new Vector3(0,1.21f,.145f), new Vector3(.27f,.30f,.035f));
            for (int side = -1; side <= 1; side += 2)
            {
                Piece("Strap", root, PrimitiveType.Cube, FarmSurface.Denim, new Vector3(side * .12f,1.34f,.12f), new Vector3(.055f,.28f,.055f));
                Piece("Back strap", root, PrimitiveType.Cube, FarmSurface.Denim, new Vector3(side*.12f,1.18f,-.145f),new Vector3(.06f,.48f,.035f));
                Piece("Brass button", root, PrimitiveType.Sphere, FarmSurface.Gold, new Vector3(side * .12f,1.27f,.17f), Vector3.one * .035f);
            }
            Piece("Head", root, PrimitiveType.Sphere, FarmSurface.Skin, new Vector3(0,1.59f,.015f), new Vector3(.38f,.43f,.35f));
            Piece("Hair", root, PrimitiveType.Sphere, FarmSurface.Hair, new Vector3(0,1.73f,-.035f), new Vector3(.43f,.30f,.4f));
            Piece("Ponytail", root, PrimitiveType.Capsule, FarmSurface.Hair, new Vector3(0,1.51f,-.25f), new Vector3(.18f,.24f,.16f), new Vector3(15,0,0));
            Piece("Tie", root, PrimitiveType.Sphere, FarmSurface.Coral, new Vector3(0,1.69f,-.24f), Vector3.one * .12f);
            for (int side=-1;side<=1;side+=2)
            {
                Piece("Eye",root,PrimitiveType.Sphere,FarmSurface.Rubber,new Vector3(side*.085f,1.61f,.177f),new Vector3(.035f,.045f,.025f));
                Piece("Cheek",root,PrimitiveType.Sphere,FarmSurface.Coral,new Vector3(side*.13f,1.55f,.165f),new Vector3(.05f,.025f,.015f));
            }
            var legs = new Transform[2]; var arms = new Transform[2];
            for (int i=0;i<2;i++)
            {
                int side = i==0?-1:1;
                legs[i]=Root("Leg joint",root,new Vector3(side*.13f,.78f,0));
                Piece("Denim trouser",legs[i],PrimitiveType.Capsule,FarmSurface.Denim,new Vector3(0,-.28f,0),new Vector3(.2f,.28f,.22f));
                Piece("Leather boot",legs[i],PrimitiveType.Capsule,FarmSurface.Wood,new Vector3(0,-.64f,.06f),new Vector3(.22f,.14f,.32f));
                arms[i]=Root("Shoulder",root,new Vector3(side*.27f,1.28f,0));
                Piece("Sleeve",arms[i],PrimitiveType.Capsule,FarmSurface.White,new Vector3(side*.025f,-.12f,0),new Vector3(.18f,.15f,.18f));
                Piece("Arm",arms[i],PrimitiveType.Capsule,FarmSurface.Skin,new Vector3(side*.03f,-.36f,.01f),new Vector3(.12f,.18f,.12f));
                Piece("Hand",arms[i],PrimitiveType.Sphere,FarmSurface.Skin,new Vector3(side*.03f,-.55f,.02f),Vector3.one*.14f);
            }
            var motion=root.gameObject.AddComponent<FarmActorMotion>(); motion.legs=legs; motion.arms=arms;
            return root.gameObject;
        }
        public GameObject Tractor(Transform parent)
        {
            var root=Root("Cedar tractor",parent,Vector3.zero);
            Piece("Chassis",root,PrimitiveType.Cube,FarmSurface.Metal,new Vector3(0,.55f,0),new Vector3(1.2f,.22f,2.3f));
            Piece("Rounded bonnet",root,PrimitiveType.Capsule,FarmSurface.Coral,new Vector3(0,1.0f,.65f),new Vector3(.95f,.7f,.65f),new Vector3(90,0,0));
            Piece("Grille",root,PrimitiveType.Cube,FarmSurface.Metal,new Vector3(0,.98f,1.3f),new Vector3(.72f,.48f,.08f));
            for(int i=0;i<6;i++) Piece("Chrome grille slat",root,PrimitiveType.Cube,FarmSurface.Cream,new Vector3(-.29f+i*.115f,.98f,1.35f),new Vector3(.032f,.42f,.035f));
            Piece("Exhaust",root,PrimitiveType.Cylinder,FarmSurface.Metal,new Vector3(-.32f,1.6f,.35f),new Vector3(.09f,.48f,.09f));
            Piece("Seat",root,PrimitiveType.Cube,FarmSurface.Rubber,new Vector3(0,.96f,-.55f),new Vector3(.6f,.15f,.55f));
            Piece("Seat back",root,PrimitiveType.Cube,FarmSurface.Rubber,new Vector3(0,1.15f,-.8f),new Vector3(.6f,.43f,.13f));
            var wheels=new Transform[4]; int n=0;
            for(int s=-1;s<=1;s+=2) for(int e=-1;e<=1;e+=2)
            {
                bool rear=e==-1; float radius=rear?.55f:.36f;
                var wheel=Root("Wheel axle",root,new Vector3(s*.77f,radius,e*.8f)); wheel.localRotation=Quaternion.Euler(0,0,90); wheels[n++]=wheel;
                Piece("Tire",wheel,PrimitiveType.Cylinder,FarmSurface.Rubber,Vector3.zero,new Vector3(radius*2,.18f,radius*2));
                Piece("Hub",wheel,PrimitiveType.Cylinder,FarmSurface.Cream,new Vector3(0,-s*.19f,0),new Vector3(radius,.045f,radius));
                for(int t=0;t<12;t++)
                {
                    float a=t*Mathf.PI/6;
                    Piece("Tread",wheel,PrimitiveType.Cube,FarmSurface.Rubber,new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius),new Vector3(.13f,.39f,.07f),new Vector3(0,a*Mathf.Rad2Deg,12));
                }
                if(rear) Piece("Mudguard",root,PrimitiveType.Capsule,FarmSurface.Coral,new Vector3(s*.75f,1.16f,-.8f),new Vector3(.32f,.13f,1.15f),new Vector3(90,0,0));
                else Piece("Headlight",root,PrimitiveType.Sphere,FarmSurface.Gold,new Vector3(s*.4f,1.12f,1.31f),Vector3.one*.17f);
            }
            var driver=Farmer(root); driver.transform.localPosition=new Vector3(0,.67f,-.5f); driver.transform.localScale=Vector3.one*.65f;
            Destroy(driver.GetComponent<FarmActorMotion>());
            root.gameObject.AddComponent<FarmActorMotion>().wheels=wheels;
            return root.gameObject;
        }
        public void Cow(Transform parent, Vector3 position)
        {
            var root=Root("Cow",parent,position-grid.transform.position);
            Piece("Body",root,PrimitiveType.Sphere,FarmSurface.White,new Vector3(0,1.0f,0),new Vector3(.9f,.9f,1.5f));
            Piece("Head",root,PrimitiveType.Sphere,FarmSurface.White,new Vector3(0,1.2f,.83f),new Vector3(.6f,.62f,.65f));
            Piece("Muzzle",root,PrimitiveType.Sphere,FarmSurface.Skin,new Vector3(0,1.05f,1.1f),new Vector3(.52f,.27f,.25f));
            for(int s=-1;s<=1;s+=2)
            {
                Piece("Ear",root,PrimitiveType.Sphere,FarmSurface.Rubber,new Vector3(s*.35f,1.42f,.8f),new Vector3(.33f,.12f,.18f));
                Piece("Eye",root,PrimitiveType.Sphere,FarmSurface.Rubber,new Vector3(s*.23f,1.33f,1.03f),Vector3.one*.07f);
                Piece("Horn",root,PrimitiveType.Capsule,FarmSurface.Cream,new Vector3(s*.2f,1.58f,.7f),new Vector3(.07f,.15f,.07f),new Vector3(0,0,-s*20));
                for(int e=-1;e<=1;e+=2) Piece("Leg",root,PrimitiveType.Capsule,FarmSurface.Rubber,new Vector3(s*.3f,.42f,e*.49f),new Vector3(.17f,.39f,.17f));
                Piece("Patch",root,PrimitiveType.Sphere,FarmSurface.Rubber,new Vector3(s*.39f,1.12f,-.22f),new Vector3(.13f,.47f,.65f));
            }
        }
        public void Landscape()
        {
            Terrain(); Barn(new Vector3(48,1,57)); Windmill(new Vector3(9,1,58)); Pond(new Vector3(-5,-.05f,34));
            var rng=new System.Random(812);
            for(int i=0;i<54;i++)
            {
                float angle=i*Mathf.PI*2/54, radius=42+(float)rng.NextDouble()*25;
                Vector3 p=new Vector3(32+Mathf.Cos(angle)*radius,0,34+Mathf.Sin(angle)*radius);
                Tree(p, .8f+(float)rng.NextDouble()*.65f, i%3);
            }
            Tree(new Vector3(12,0,35),1,0); Tree(new Vector3(54,0,30),1.1f,1); Tree(new Vector3(4,1,51),1.2f,2);
            for(int i=0;i<40;i++)
            {
                float x=3+i*1.55f;
                Stamp(PrimitiveType.Sphere,FarmSurface.Stone,new Vector3(x,.45f,48),new Vector3(1.6f,.9f,.8f),new Vector3(0,i*23,0));
            }
            for(int i=0;i<4;i++) Stamp(PrimitiveType.Cube,FarmSurface.Stone,new Vector3(39,.125f*(i+1),47.5f+i*.65f),new Vector3(4,.25f*(i+1),.7f));
            for(int i=0;i<140;i++)
            {
                float x=35+(float)rng.NextDouble()*9, z=8+(float)rng.NextDouble()*30;
                if(x>38 && x<40) continue;
                Vector3 p=new Vector3(x,0,z);
                Stamp(PrimitiveType.Cylinder,FarmSurface.Leaf,p+Vector3.up*.18f,new Vector3(.025f,.18f,.025f));
                for(int k=0;k<5;k++) { float a=k*Mathf.PI*.4f; Stamp(PrimitiveType.Sphere,i%3==0?FarmSurface.Coral:FarmSurface.Flower,p+new Vector3(Mathf.Cos(a)*.09f,.38f,Mathf.Sin(a)*.09f),new Vector3(.14f,.05f,.1f)); }
                Stamp(PrimitiveType.Sphere,FarmSurface.Gold,p+Vector3.up*.4f,Vector3.one*.09f);
            }
            for(int z=44;z<=58;z+=2) for(int x=20;x<=56;x+=2)
                if(z==44 || z==58) FencePost(new Vector3(x,0,z));
        }
        private void FencePost(Vector3 p)
        {
            Stamp(PrimitiveType.Cube,FarmSurface.Cream,p+Vector3.up*.65f,new Vector3(.15f,1.3f,.15f));
            Stamp(PrimitiveType.Cube,FarmSurface.Wood,p+new Vector3(1,.45f,0),new Vector3(2,.1f,.1f));
            Stamp(PrimitiveType.Cube,FarmSurface.Wood,p+new Vector3(1,.95f,0),new Vector3(2,.1f,.1f));
        }
        private void Tree(Vector3 p,float size,int tint)
        {
            Stamp(PrimitiveType.Cylinder,FarmSurface.Bark,p+Vector3.up*size,new Vector3(.35f,1.1f,.35f)*size);
            for(int k=0;k<5;k++)
            {
                float a=k*Mathf.PI*.4f;
                Stamp(PrimitiveType.Sphere,(k+tint)%3==0?FarmSurface.LeafLight:FarmSurface.Leaf,p+new Vector3(Mathf.Cos(a)*1.05f,3+(k%2)*.6f,Mathf.Sin(a)*1.05f)*size,new Vector3(2.6f,2.5f,2.6f)*size);
            }
            for(int k=0;k<4;k++) { float a=k*1.7f; Stamp(PrimitiveType.Sphere,FarmSurface.Coral,p+new Vector3(Mathf.Cos(a)*1.8f,2.7f,Mathf.Sin(a)*1.8f)*size,Vector3.one*.23f*size); }
        }
        private void Barn(Vector3 p)
        {
            Stamp(PrimitiveType.Cube,FarmSurface.Coral,p+Vector3.up*2.4f,new Vector3(8,4.8f,7));
            for(int s=-1;s<=1;s+=2)
            {
                Stamp(PrimitiveType.Cube,FarmSurface.Roof,p+new Vector3(s*2.15f,5.25f,0),new Vector3(4.9f,.25f,8),new Vector3(0,0,-s*32));
                for(int z=-1;z<=1;z+=2) Stamp(PrimitiveType.Cube,FarmSurface.Cream,p+new Vector3(s*3.9f,2.4f,z*3.45f),new Vector3(.2f,4.8f,.2f));
            }
            Stamp(PrimitiveType.Cube,FarmSurface.Wood,p+new Vector3(0,1.5f,-3.56f),new Vector3(2.4f,3,.15f));
            Stamp(PrimitiveType.Cube,FarmSurface.Cream,p+new Vector3(0,1.5f,-3.68f),new Vector3(.12f,3.1f,.08f));
            for(int s=-1;s<=1;s+=2)
            {
                Stamp(PrimitiveType.Cube,FarmSurface.Cream,p+new Vector3(s*2.65f,2.8f,-3.57f),new Vector3(1.3f,1.4f,.13f));
                Stamp(PrimitiveType.Cube,FarmSurface.Glass,p+new Vector3(s*2.65f,2.8f,-3.66f),new Vector3(1.05f,1.1f,.08f));
                Stamp(PrimitiveType.Cube,FarmSurface.Cream,p+new Vector3(s*2.65f,2.8f,-3.72f),new Vector3(.08f,1.2f,.04f));
            }
            for(int i=0;i<12;i++) Stamp(PrimitiveType.Cube,FarmSurface.Wood,p+new Vector3(-3.7f+i*.68f,2.3f,-3.51f),new Vector3(.035f,4.4f,.04f));
            for(int i=0;i<3;i++) Stamp(PrimitiveType.Cylinder,FarmSurface.Gold,p+new Vector3(5+i*.7f,.65f,-1),new Vector3(1.2f,.65f,1.2f),new Vector3(0,0,90));
            Stamp(PrimitiveType.Cylinder,FarmSurface.Metal,p+new Vector3(0,7,2),new Vector3(.7f,1.6f,.7f));
        }
        private void Windmill(Vector3 p)
        {
            Stamp(PrimitiveType.Cylinder,FarmSurface.Cream,p+Vector3.up*3,new Vector3(2.5f,3,2.5f));
            Stamp(PrimitiveType.Sphere,FarmSurface.Roof,p+Vector3.up*6.1f,new Vector3(3.2f,2,3.2f));
            var rotor=Root("Windmill rotor",transform,p+new Vector3(0,4.8f,-1.6f)); rotor.gameObject.AddComponent<FarmRotor>();
            for(int k=0;k<4;k++) Piece("Sail",rotor,PrimitiveType.Cube,FarmSurface.Wood,new Vector3(Mathf.Sin(k*Mathf.PI/2)*2,Mathf.Cos(k*Mathf.PI/2)*2,0),new Vector3(.55f,4,.12f),new Vector3(0,0,-k*90));
        }
        private void Pond(Vector3 p)
        {
            Stamp(PrimitiveType.Cylinder,FarmSurface.Water,p,new Vector3(17,.08f,23));
            for(int i=0;i<36;i++) { float a=i*Mathf.PI*2/36; Stamp(PrimitiveType.Sphere,FarmSurface.Stone,p+new Vector3(Mathf.Cos(a)*8.5f,.15f,Mathf.Sin(a)*11.5f),new Vector3(1.1f,.7f,1.3f),new Vector3(0,i*31,0)); }
        }
        private void Terrain()
        {
            var vertices=new List<Vector3>(); var triangles=new List<int>(); const int count=32; const float step=14;
            for(int z=0;z<=count;z++) for(int x=0;x<=count;x++)
            {
                float px=x*step-180,pz=z*step-100;
                float edge=Mathf.Max(Mathf.Max(-px,px-64),Mathf.Max(-pz,pz-64));
                float height=Mathf.Max(0,edge-12)*.22f*(.5f+Mathf.PerlinNoise(px*.012f+20,pz*.012f+20));
                vertices.Add(new Vector3(px,height-.45f,pz));
            }
            for(int z=0;z<count;z++) for(int x=0;x<count;x++) { int a=z*(count+1)+x; triangles.AddRange(new[]{a,a+count+1,a+1,a+1,a+count+1,a+count+2}); }
            var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh);
            instances.Add(mesh,library.grass,Matrix4x4.TRS(grid.transform.position,Quaternion.identity,Vector3.one));
            for(int i=0;i<9;i++)
            {
                Vector3 p=new Vector3(-110+i*36,0,148+(i%3)*25);
                float radius=30+(i%3)*9,height=35+(i%4)*9;
                var mountain=Mountain(radius,height); owned.Add(mountain);
                instances.Add(mountain,Mat(FarmSurface.Mountain),Matrix4x4.TRS(p+grid.transform.position,Quaternion.identity,Vector3.one));
                var snow=Mountain(radius*.302f,height*.3f); owned.Add(snow);
                instances.Add(snow,Mat(FarmSurface.Snow),Matrix4x4.TRS(p+new Vector3(0,height*.7f+.015f,0)+grid.transform.position,Quaternion.identity,Vector3.one));
            }
        }
        private static Mesh Mountain(float radius,float height)
        {
            var v=new List<Vector3>(); var t=new List<int>();
            for(int i=0;i<9;i++) { float a=i*Mathf.PI*2/8,b=(i+1)*Mathf.PI*2/8; int n=v.Count;
                v.Add(new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); v.Add(new Vector3(0,height,0)); v.Add(new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius)); t.AddRange(new[]{n,n+1,n+2}); }
            var mesh=new Mesh(); mesh.SetVertices(v); mesh.SetTriangles(t,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private void OnDestroy() { foreach(var mesh in owned) if(mesh!=null) Destroy(mesh); }
    }
}
```

## Assets/Farm/Runtime/FarmSettings.cs

```csharp
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace NextGenFarm
{
    [Serializable]
    public sealed class FarmSettingsData
    {
        public float master = .8f, music = .45f, sfx = .8f;
        public int quality = 2, fps = 60;
        public void Validate()
        {
            master = Level(master); music = Level(music); sfx = Level(sfx);
            quality = Mathf.Clamp(quality, 0, 3);
            if (fps != 30 && fps != 45 && fps != 60 && fps != -1) fps = 60;
        }
        private static float Level(float value) => float.IsNaN(value) || float.IsInfinity(value) ? .5f : Mathf.Clamp01(value);
    }

    // Explicit HDRP camera/light overrides: built-in shadowDistance alone does not configure HDRP.
    public sealed class FarmSettings : MonoBehaviour
    {
        public FarmSettingsData Data { get; private set; } = new FarmSettingsData();
        public float DrawDistance { get; private set; } = 600;
        public string StoragePath { get; private set; }
        public event Action Changed;
        private Camera view;
        private HDAdditionalLightData[] lights;
        private float targetDistance;
        private VolumeProfile qualityProfile;
        public void Initialize(Camera camera)
        {
            view = camera;
            lights = FindObjectsByType<HDAdditionalLightData>(FindObjectsSortMode.None);
            var volume = new GameObject("Farm quality overrides").AddComponent<Volume>();
            volume.transform.SetParent(transform, false); volume.isGlobal = true; volume.priority = 30;
            qualityProfile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = qualityProfile;
            qualityProfile.Add<HDShadowSettings>(true); qualityProfile.Add<Bloom>(true); qualityProfile.Add<ScreenSpaceAmbientOcclusion>(true);
            string directory = Environment.GetEnvironmentVariable("NEXTGEN_FARM_SAVE_DIR");
            if (string.IsNullOrWhiteSpace(directory)) directory = Application.persistentDataPath;
            StoragePath = Path.Combine(directory, "settings-v1.json");
            try { if (File.Exists(StoragePath)) Data = JsonUtility.FromJson<FarmSettingsData>(File.ReadAllText(StoragePath)) ?? new FarmSettingsData(); }
            catch (Exception ex) { Debug.LogWarning("Settings fallback: " + ex.Message); }
            Apply(); DrawDistance = targetDistance; view.farClipPlane = DrawDistance;
        }
        public void SetVolumes(float master, float music, float sfx)
        { Data.master = master; Data.music = music; Data.sfx = sfx; Data.Validate(); Changed?.Invoke(); }
        public void SetQuality(int quality) { Data.quality = quality; Apply(); }
        public void SetFPS(int fps) { Data.fps = fps; Apply(); }
        public void Apply()
        {
            Data.Validate(); int q = Data.quality;
            targetDistance = new[] { 180f, 320f, 600f, 1000f }[q];
            qualityProfile.TryGet<HDShadowSettings>(out var shadows); shadows.maxShadowDistance.Override(new[] { 30f, 60f, 110f, 180f }[q]);
            qualityProfile.TryGet<Bloom>(out var bloom); bloom.intensity.Override(q == 0 ? 0 : q == 1 ? .08f : .15f);
            qualityProfile.TryGet<ScreenSpaceAmbientOcclusion>(out var ao); ao.intensity.Override(q < 2 ? 0 : .65f);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Data.fps;
            QualitySettings.lodBias = new[] { .6f, 1f, 1.5f, 2f }[q];
            QualitySettings.globalTextureMipmapLimit = q == 0 ? 1 : 0;
            QualitySettings.anisotropicFiltering = q < 2 ? AnisotropicFiltering.Enable : AnisotropicFiltering.ForceEnable;
            var hd = view.GetComponent<HDAdditionalCameraData>();
            if (hd != null)
            {
                hd.customRenderingSettings = true;
                // Keep exposure/tonemapping enabled at every level for a consistent HDRP image.
                SetFrame(hd, FrameSettingsField.Postprocess, true);
                SetFrame(hd, FrameSettingsField.Bloom, q >= 1);
                SetFrame(hd, FrameSettingsField.SSAO, q >= 2);
                SetFrame(hd, FrameSettingsField.Volumetrics, q >= 2);
                hd.antialiasing = q == 0 ? HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing : HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            }
            int resolution = new[] { 512, 1024, 2048, 4096 }[q];
            foreach (var light in lights)
            {
                if (light == null) continue;
                light.SetShadowResolutionOverride(true); light.SetShadowResolution(resolution);
                light.SetShadowFadeDistance(new[] { 30f, 60f, 110f, 180f }[q]);
            }
            Changed?.Invoke();
        }
        private static void SetFrame(HDAdditionalCameraData hd, FrameSettingsField field, bool enabled)
        {
            hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
            hd.renderingPathCustomFrameSettings.SetEnabled(field, enabled);
        }
        private void Update()
        {
            if (view == null) return;
            DrawDistance = Mathf.Lerp(DrawDistance, targetDistance, 1 - Mathf.Exp(-5 * Time.unscaledDeltaTime));
            view.farClipPlane = DrawDistance;
        }
        public bool Save()
        {
            try
            {
                Data.Validate(); Directory.CreateDirectory(Path.GetDirectoryName(StoragePath));
                string temporary = StoragePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(Data, true));
                if (File.Exists(StoragePath)) File.Replace(temporary, StoragePath, null);
                else File.Move(temporary, StoragePath);
                return true;
            }
            catch (Exception ex) { Debug.LogWarning("Cannot save settings: " + ex.Message); return false; }
        }
        private void OnApplicationQuit() { if (StoragePath != null) Save(); }
        private void OnDestroy()
        {
            if (qualityProfile == null) return;
            foreach (var component in qualityProfile.components) Destroy(component);
            Destroy(qualityProfile);
        }
    }
}
```

## Assets/Farm/Runtime/FarmVisualLibrary.cs

```csharp
using System;
using UnityEngine;

namespace NextGenFarm
{
    [Serializable]
    public sealed class CropVisual
    {
        public string cropId = "carrot";
        public Mesh[] stages = new Mesh[3];
        public Material[] materials = new Material[3];
        public Vector3 scale = Vector3.one;
        public Material[] accentMaterials = new Material[3];
    }
    [CreateAssetMenu(menuName = "NextGen Farm/Visual Library")]
    public sealed class FarmVisualLibrary : ScriptableObject
    {
        public Mesh groundMesh;
        public Material grass, drySoil, wetSoil;
        public Material precipitationMaterial;
        public Material[] demoPalette = new Material[0];
        public Material[] artMaterials;
        public Mesh detailGrassMesh;
        public Material cliffMaterial, pathMaterial;
        public CropVisual[] crops = new CropVisual[0];
        public GameObject farmerPrefab, tractorPrefab, cowPrefab;
    }
}
```

## Assets/Farm/Runtime/SaveLoadSystem.cs

```csharp
using System;
using System.IO;
using NextGenFarm.Core;
using UnityEngine;

namespace NextGenFarm
{
    public sealed class SaveLoadSystem : MonoBehaviour
    {
        public string Status { get; private set; } = "Ready";
        public string SavePath => Path.Combine(Environment.GetEnvironmentVariable("NEXTGEN_FARM_SAVE_DIR") ??
            Application.persistentDataPath, "nextgen-farm-v1.json");
        private FarmGridManager grid;
        private AnimalPenSystem animals;
        private float nextSave;
        private FarmSaveStore Store => store ?? (store = new FarmSaveStore(SavePath));
        private FarmSaveStore store;
        public void Initialize(FarmGridManager farm, AnimalPenSystem pens)
        {
            grid = farm; animals = pens; nextSave = Time.unscaledTime + 30;
        }
        public bool TryLoad(CropDefinition[] crops, AnimalDefinition[] definitions,
            out FarmWorld world, out AnimalWorld animalWorld, out double savedUtc, out float cellSize)
        {
            bool loaded = Store.TryLoad(JsonUtility.FromJson<FarmSnapshot>, crops, definitions,
                out world, out animalWorld, out savedUtc, out cellSize);
            Status = Store.Status;
            return loaded;
        }
        public bool Save()
        {
            if (grid == null || grid.World == null || animals == null) return false;
            try
            {
                var world = grid.World;
                var data = new FarmSnapshot { width = world.Width, depth = world.Depth, tiles = world.Tiles,
                    epochUtc = world.EpochUtc, savedUtc = grid.Clock.Now, wallet = world.Wallet,
                    pens = animals.World.Pens, cellSize = grid.CellSize };
                Store.Write(data, value => JsonUtility.ToJson(value));
                Status = "Saved " + DateTime.Now.ToString("HH:mm:ss");
                return true;
            }
            catch (Exception ex) { Status = "Save failed: " + ex.Message; Debug.LogError(Status); return false; }
        }
        private void Update()
        {
            if (Time.unscaledTime < nextSave) return;
            nextSave = Time.unscaledTime + 30; Save();
        }
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() { Save(); }
    }
}
```

## Assets/Farm/Runtime/SeasonTimeManager.cs

```csharp
using System;
using NextGenFarm.Core;
using UnityEngine;

namespace NextGenFarm
{
    public sealed class SeasonTimeManager : MonoBehaviour
    {
        public const double SeasonDuration = ClockMath.SeasonSeconds;
        public double EpochUtc { get; private set; }
        public double Now { get; private set; }
        public Season CurrentSeason => ClockMath.SeasonAt(Now, EpochUtc);
        public double SecondsRemaining => ClockMath.SeasonRemaining(Now, EpochUtc);
        public float DayFraction => (float)((Math.Max(0, Now - EpochUtc) / 600 + 0.35) % 1);
        public event Action<Season> SeasonChanged;
        private double anchorUtc, anchorRealtime;
        private Season previous;
        public static double UtcNow => (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        public void Initialize(double epoch, double minimumUtc = 0)
        {
            EpochUtc = epoch;
            anchorUtc = Math.Max(UtcNow, minimumUtc);
            anchorRealtime = Time.realtimeSinceStartupAsDouble;
            Now = anchorUtc; previous = CurrentSeason;
        }
        private void Update()
        {
            // Monotonic during a session; changing the OS clock cannot rewind crops.
            Now = anchorUtc + Math.Max(0, Time.realtimeSinceStartupAsDouble - anchorRealtime);
            if (previous != CurrentSeason) { previous = CurrentSeason; SeasonChanged?.Invoke(previous); }
        }
        private void OnApplicationPause(bool paused)
        {
            if (!paused && EpochUtc > 0) Initialize(EpochUtc, Now);
        }
    }
}
```

## Assets/Farm/Runtime/TractorController.cs

```csharp
using System.Collections.Generic;
using NextGenFarm.Core;
using UnityEngine;

namespace NextGenFarm
{
    public sealed class TractorController : MonoBehaviour
    {
        public FarmAction SelectedAction { get; set; }
        public string SelectedCrop { get; set; } = "carrot";
        public bool Mounted { get; private set; }
        public bool Automatic { get; private set; }
        public int LastAffected { get; private set; }
        public string NavigationStatus { get; private set; } = "Manual";
        public Vector2Int TargetCell => Automatic ? grid.WorldToCell(transform.position) :
            grid.WorldToCell(transform.position + transform.forward * grid.CellSize);
        private FarmGridManager grid;
        private GameObject walkingVisual, tractorVisual;
        private List<Cell> route, path;
        private int routeIndex, pathIndex;
        private float nextAction;
        public void Initialize(FarmGridManager farm, GameObject person, GameObject tractor)
        {
            grid = farm; walkingVisual = person; tractorVisual = tractor;
            SetMounted(false);
        }
        public void SetMounted(bool mounted)
        {
            StopAuto(); Mounted = mounted;
            walkingVisual.SetActive(!mounted); tractorVisual.SetActive(mounted);
        }
        public void StopAuto() { Automatic = false; path = null; NavigationStatus = "Manual"; }
        public void UseTool()
        {
            if (Time.unscaledTime < nextAction) return;
            if (Automatic) StopAuto();
            nextAction = Time.unscaledTime + 0.2f;
            LastAffected = grid.Act(TargetCell, SelectedAction, SelectedCrop, Mounted);
        }
        public void ToggleAuto()
        {
            if (Automatic) { StopAuto(); return; }
            if (!Mounted) SetMounted(true);
            route = GridNavigation.Serpentine(grid.World.Width, grid.World.Depth);
            routeIndex = pathIndex = 0; path = null; Automatic = true;
            NavigationStatus = "Auto: scanning rows";
        }
        private void Update()
        {
            if (grid == null || !FarmGameFlow.GameplayAllowed) return;
            if (FarmInput.Press('1')) SelectedAction = FarmAction.Plow;
            if (FarmInput.Press('2')) SelectedAction = FarmAction.Sow;
            if (FarmInput.Press('3')) SelectedAction = FarmAction.Water;
            if (FarmInput.Press('4')) SelectedAction = FarmAction.Harvest;
            if (FarmInput.Press('t')) SetMounted(!Mounted);
            if (FarmInput.Press('r')) ToggleAuto();
            Vector2 input = Vector2.ClampMagnitude(FarmInput.Move, 1);
            if (input.sqrMagnitude > 0.01f)
            {
                StopAuto();
                // Project camera forward onto the ground; movement follows the free-look view.
                Vector3 forward=Camera.main!=null?Camera.main.transform.forward:Vector3.forward;forward.y=0;forward.Normalize();
                Vector3 move = forward*input.y + Vector3.Cross(Vector3.up,forward)*input.x;
                Vector3 desired = transform.position + move * ((Mounted ? 7 : 4) * Time.deltaTime);
                var from = grid.WorldToCell(transform.position); var to = grid.WorldToCell(desired);
                // Substeps prevent tunnelling over reserved cells at low frame rates.
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(transform.position, desired) / (grid.CellSize * 0.25f)));
                Vector3 origin = transform.position;
                for (int s = 1; s <= steps; s++)
                {
                    Vector3 candidate = Vector3.Lerp(origin, desired, (float)s / steps);
                    to = grid.WorldToCell(candidate); from = grid.WorldToCell(transform.position);
                    if (!GridNavigation.Passable(grid.World, new Cell(from.x, from.y), new Cell(to.x, to.y), 0.75f)) break;
                    // Avoid diagonal corner cutting.
                    if (from.x != to.x && from.y != to.y &&
                        (!grid.CanDrive(to.x, from.y) || !grid.CanDrive(from.x, to.y))) break;
                    candidate.y = grid.CellCenter(to.x, to.y).y; transform.position = candidate;
                }
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 12 * Time.deltaTime);
            }
            if (Automatic) TickAuto();
            else if (FarmInput.ActionHeld && Time.unscaledTime >= nextAction)
                UseTool();
        }
        private void TickAuto()
        {
            if (routeIndex >= route.Count) { StopAuto(); NavigationStatus = "Auto complete"; return; }
            if (path == null)
            {
                var from = grid.WorldToCell(transform.position);
                path = GridNavigation.FindPath(grid.World, new Cell(from.x, from.y), route[routeIndex]);
                pathIndex = 0;
                if (path == null) { routeIndex++; NavigationStatus = "Skipped unreachable row"; return; }
                // Include start center to align footprint even if the path has zero steps.
                path.Insert(0, new Cell(from.x, from.y));
            }
            if (pathIndex < path.Count)
            {
                var target = path[pathIndex];
                var current = grid.WorldToCell(transform.position);
                if (!GridNavigation.Passable(grid.World, new Cell(current.x, current.y), target, 0.75f))
                { path = null; routeIndex++; return; }
                Vector3 destination = grid.CellCenter(target.x, target.z);
                Vector3 delta = destination - transform.position;
                Vector3 horizontal = new Vector3(delta.x, 0, delta.z);
                if (horizontal.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(horizontal), 8 * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, destination, 7 * Time.deltaTime);
                if ((transform.position - destination).sqrMagnitude < 0.0001f) pathIndex++;
                return;
            }
            if (Time.unscaledTime < nextAction) return;
            nextAction = Time.unscaledTime + 0.2f;
            LastAffected = grid.Act(new Vector2Int(route[routeIndex].x, route[routeIndex].z), SelectedAction, SelectedCrop, true);
            NavigationStatus = "Auto " + (routeIndex + 1) + "/" + route.Count;
            routeIndex++; path = null;
        }
        private void OnDrawGizmosSelected()
        {
            if (grid == null) return;
            var c = TargetCell;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(grid.CellCenter(c.x, c.y) + Vector3.up * 0.1f,
                new Vector3(grid.CellSize, 0.1f, grid.CellSize) * (Mounted ? 3 : 1));
        }
    }
}
```
