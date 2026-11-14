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
