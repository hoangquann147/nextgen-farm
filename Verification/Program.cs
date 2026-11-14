using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using NextGenFarm.Core;

public static class Program
{
    private const double T = 1700000000;
    private static int assertions;
    private static CropDefinition[] Crops() => new[] { new CropDefinition(),
        new CropDefinition { id = "spring", seasonMask = 1 } };
    private static AnimalDefinition[] Animals() => new[] { new AnimalDefinition() };
    private static FarmWorld World(int w = 8, int d = 8) => new FarmWorld(w, d, Crops(), T);
    private static void Check(bool condition, string name)
    { if (!condition) throw new Exception("FAIL: " + name); assertions++; Console.WriteLine("PASS " + name); }
    private static void Reject(Action action, string name)
    {
        bool failed = false;
        try { action(); } catch (FormatException) { failed = true; }
        Check(failed, name);
    }
    private static FarmSnapshot Snapshot(FarmWorld w, AnimalWorld a)
        => new FarmSnapshot { width = w.Width, depth = w.Depth, epochUtc = w.EpochUtc,
            savedUtc = T + 20, tiles = w.Tiles, wallet = w.Wallet, pens = a.Pens };
    public static int Main()
    {
        try
        {
            Check(ClockMath.SeasonAt(T, T) == Season.Spring, "season starts at spring");
            Check(ClockMath.SeasonAt(T + 1019.999, T) == Season.Spring, "17 minute boundary before");
            Check(ClockMath.SeasonAt(T + 1020, T) == Season.Summer, "17 minute boundary exact");
            Check(ClockMath.SeasonAt(T + 4080, T) == Season.Spring, "four season wrap");
            Check(ClockMath.SeasonRemaining(T + 1020, T) == 1020, "new season remaining");
            var w = World();
            Check(!w.Apply(-1, 0, FarmAction.Plow, "carrot", T), "out of bounds rejected");
            Check(!w.Apply(0, 0, FarmAction.Sow, "carrot", T), "cannot sow grass");
            Check(w.Apply(0, 0, FarmAction.Plow, "carrot", T), "plow");
            Check(w.Apply(0, 0, FarmAction.Sow, "carrot", T), "sow");
            Check(w.Wallet.coins == 498, "seed price charged once");
            Check(!w.Apply(0, 0, FarmAction.Sow, "carrot", T), "duplicate sow rejected");
            Check(w.Apply(0, 0, FarmAction.Water, "carrot", T + 10), "water");
            Check(w.Get(0, 0).readyUtc == T + 90, "watering reduces absolute deadline");
            Check(!w.Apply(0, 0, FarmAction.Water, "carrot", T + 60), "cannot stack watering bonus after drying");
            Check(w.Get(0, 0).Soil(T + 60) == SoilState.Dry, "wet soil expires while offline");
            Check(!w.Apply(0, 0, FarmAction.Harvest, "carrot", T + 89), "early harvest rejected");
            Check(w.Apply(0, 0, FarmAction.Harvest, "carrot", T + 10000000), "long offline crop never dies");
            Check(w.Wallet.inventory[0].count == 3, "harvest product yield");
            Check(!w.Apply(0, 0, FarmAction.Harvest, "carrot", T + 10000000), "harvest cannot duplicate");
            Check(w.Wallet.Sell("carrot", 4) == 12 && w.Wallet.coins == 510, "town sale inventory to coins");
            Check(w.Wallet.Sell("carrot", 4) == 0, "sale cannot duplicate");
            Check(w.Apply(0, 0, FarmAction.Water, null, T + 10000001) &&
                w.Apply(0, 0, FarmAction.Sow, "carrot", T + 10000002) && w.Get(0, 0).wateredThisCycle,
                "prewatered soil applies one crop bonus");
            var edge = World();
            Check(edge.ApplySquare(0, 0, 1, FarmAction.Plow, "carrot", T) == 4, "3x3 clips at farm edge");
            Check(edge.ApplySquare(4, 4, 1, FarmAction.Plow, "carrot", T) == 9, "tractor affects nine cells");
            edge.Wallet.coins = 5;
            Check(edge.ApplySquare(4, 4, 1, FarmAction.Sow, "carrot", T) == 2 && edge.Wallet.coins == 1,
                "partial 3x3 sow respects budget deterministically");
            Check(edge.Apply(0, 0, FarmAction.Sow, "spring", T + 1020) == false, "season restriction");
            Check(!edge.Apply(0, 0, FarmAction.Sow, "missing", T), "unknown content rejected");
            Check(!edge.Apply(0, 0, FarmAction.Water, null, double.NaN), "nonfinite command rejected");
            var aw = World(); var animals = new AnimalWorld(aw, Animals());
            Check(animals.Place("p1", "cow", 3, 3), "place pen");
            Check(!animals.Place("p2", "cow", 4, 4), "overlapping pen rejected");
            Check(!aw.Apply(3, 3, FarmAction.Plow, null, T), "reserved pen blocks farming");
            Check(!aw.SetHeight(3, 3, 1), "reserved pen blocks terraforming");
            Check(animals.Feed("p1", T) && aw.Wallet.coins == 485, "feed cost per animal");
            Check(!animals.Feed("p1", T + 1), "double feed rejected");
            Check(!animals.Harvest("p1", T + 179), "animal harvest deadline enforced");
            Check(animals.Harvest("p1", T + 100000), "animals progress offline without dying");
            Check(!animals.Harvest("p1", T + 100001), "animal harvest cannot duplicate");
            Check(aw.Wallet.inventory[0].id == "milk" && aw.Wallet.inventory[0].count == 3, "animal yield");
            Check(animals.Feed("p1", T + 100002), "new feed cycle after harvest");
            var snap = Snapshot(aw, animals);
            var restored = snap.Restore(Crops(), Animals(), out var restoredAnimals);
            Check(restoredAnimals.Pens[0].fed && restoredAnimals.Pens[0].Ready(T + 200000), "save restores animal offline deadline");
            Check(restored.Get(3, 3).blocked && restored.Wallet.coins == aw.Wallet.coins, "save restores reservation and money");
            restored.Wallet.inventory[0].count++;
            Check(aw.Wallet.inventory[0].count == 3, "restore deep copies inventory");
            restored.Get(0, 0).height = 2;
            Check(aw.Get(0, 0).height == 0, "restore deep copies tiles");
            snap.version = 99; Reject(() => snap.Restore(Crops(), Animals(), out _), "future save rejected"); snap.version = 1;
            snap.tiles[0].readyUtc = double.NaN;
            Reject(() => snap.Restore(Crops(), Animals(), out _), "nonfinite timestamp rejected"); snap.tiles[0].readyUtc = 0;
            snap.pens.Add(snap.pens[0]); Reject(() => snap.Restore(Crops(), Animals(), out _), "duplicate pen rejected"); snap.pens.RemoveAt(1);
            snap.wallet.inventory.Add(new InventoryEntry("milk", 1));
            Reject(() => snap.Restore(Crops(), Animals(), out _), "duplicate inventory rejected"); snap.wallet.inventory.RemoveAt(1);
            var cw = World(); cw.Apply(1, 1, FarmAction.Plow, null, T); cw.Apply(1, 1, FarmAction.Sow, "carrot", T);
            cw.Apply(1, 1, FarmAction.Water, null, T + 5);
            var cs = Snapshot(cw, new AnimalWorld(cw, Animals()));
            var cr = cs.Restore(Crops(), Animals(), out _);
            Check(cr.Get(1, 1).Ready(T + 100000) && cr.Get(1, 1).wateredThisCycle, "crop deadline and water bonus survive save");
            Check(cr.Apply(1, 1, FarmAction.Harvest, null, T + 100000), "offline restored crop harvestable");
            RoadAndCare(); Navigation(); FileRoundTrip(snap);
            Console.WriteLine("ALL " + assertions + " ASSERTIONS PASSED");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Navigation()
    {
        var w = World();
        for (int z = 0; z < 7; z++) w.Get(3, z).blocked = true;
        var path = GridNavigation.FindPath(w, new Cell(1, 1), new Cell(5, 1));
        Check(path != null && path.Count > 4, "auto routes around obstacles");
        var last = new Cell(1, 1);
        foreach (var c in path)
        {
            if (Math.Abs(c.x - last.x) + Math.Abs(c.z - last.z) != 1 || w.Get(c.x, c.z).blocked)
                throw new Exception("Invalid route");
            last = c;
        }
        Check(last.x == 5 && last.z == 1, "auto route reaches goal through adjacent cells");
        w.Get(3, 7).blocked = true;
        Check(GridNavigation.FindPath(w, new Cell(1, 1), new Cell(5, 1)) == null, "unreachable rows skipped");
        var stepWorld = World(); stepWorld.SetHeight(1, 0, 2);
        Check(!GridNavigation.Passable(stepWorld, new Cell(0, 0), new Cell(1, 0), 0.75f), "tractor cannot climb steep step");
        for (int width = 1; width <= 17; width++)
            for (int depth = 1; depth <= 17; depth++)
            {
                bool[] covered = new bool[width * depth];
                foreach (var c in GridNavigation.Serpentine(width, depth))
                    for (int z = Math.Max(0, c.z - 1); z <= Math.Min(depth - 1, c.z + 1); z++)
                        for (int x = Math.Max(0, c.x - 1); x <= Math.Min(width - 1, c.x + 1); x++) covered[z * width + x] = true;
                foreach (bool cell in covered) if (!cell) throw new Exception("Missed edge in serpentine route");
            }
        Check(true, "3x3 serpentine covers all cells for 289 rectangular map sizes");
    }
    private static void RoadAndCare()
    {
        var w=World();w.Get(0,0).road=true;
        Check(!w.Apply(0,0,FarmAction.Plow,null,T),"road blocks farming");
        Check(GridNavigation.Passable(w,new Cell(0,1),new Cell(0,0),.75f),"road remains walkable");
        Check(!w.SetHeight(0,0,2),"road cannot be terraformed under path");
        var animals=new AnimalWorld(w,Animals());Check(!animals.Place("road-pen","cow",0,0),"pen cannot overlap road");
        w.ApplySquare(3,3,1,FarmAction.Plow,"carrot",T);w.ApplySquare(3,3,1,FarmAction.Sow,"carrot",T);w.ApplySquare(3,3,1,FarmAction.Water,"carrot",T);
        Check(w.Wallet.watered==9&&w.ClaimCareReward(true)&&w.Wallet.diamonds==5,"watering quest grants actual diamonds");
        Check(!w.ClaimCareReward(true)&&w.Wallet.diamonds==5,"watering reward cannot be claimed twice");
        w.ApplySquare(3,3,1,FarmAction.Harvest,"carrot",T+1000);
        Check(w.Wallet.harvested==9&&w.ClaimCareReward(false)&&w.Wallet.medals==1,"harvesting quest grants medal");
        var restored=Snapshot(w,animals).Restore(Crops(),Animals(),out _);
        Check(restored.Get(0,0).road&&restored.Wallet.waterRewardClaimed&&!restored.ClaimCareReward(true),"path and claimed quest survive restore");
    }
    private static void FileRoundTrip(FarmSnapshot snap)
    {
        string directory = Path.Combine(Path.GetTempPath(), "NextGenFarmTests-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "farm.json");
        var store = new FarmSaveStore(path);
        var json = new JavaScriptSerializer { MaxJsonLength = 64 * 1024 * 1024 };
        try
        {
            store.Write(snap, json.Serialize);
            Check(store.TryLoad(json.Deserialize<FarmSnapshot>, Crops(), Animals(), out var loaded, out _, out _, out _) &&
                loaded.Wallet.coins == snap.wallet.coins, "JSON file roundtrip");
            long oldCoins = snap.wallet.coins; snap.wallet.coins = 123;
            store.Write(snap, json.Serialize);
            Check(File.Exists(path + ".bak"), "atomic replacement creates backup");
            File.WriteAllText(path, "broken json");
            Check(store.TryLoad(json.Deserialize<FarmSnapshot>, Crops(), Animals(), out loaded, out _, out _, out _) &&
                loaded.Wallet.coins == oldCoins && store.Status == "Recovered backup", "corrupt primary recovers valid backup");
            File.WriteAllText(path + ".bak", "broken json");
            Check(!store.TryLoad(json.Deserialize<FarmSnapshot>, Crops(), Animals(), out loaded, out _, out _, out _) &&
                loaded == null, "two corrupt saves return no partial world");
        }
        finally
        {
            // Delete only known test files; never recurse through a computed user path.
            foreach (string suffix in new[] { "", ".bak", ".tmp" }) if (File.Exists(path + suffix)) File.Delete(path + suffix);
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }
    }
}
