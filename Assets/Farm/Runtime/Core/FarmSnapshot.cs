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
