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
