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
