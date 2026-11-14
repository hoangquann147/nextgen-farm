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
