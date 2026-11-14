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
