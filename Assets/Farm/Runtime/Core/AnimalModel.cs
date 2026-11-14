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
