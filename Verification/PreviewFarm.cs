using System;
using System.IO;
using System.Web.Script.Serialization;
using NextGenFarm.Core;

// Reproducible visual fixture, isolated from the user's main farm.
public static class PreviewFarm
{
    public static int Main(string[] args)
    {
        double now = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        var crops = new[] { new CropDefinition(), new CropDefinition { id = "wheat", growSeconds = 300 },
            new CropDefinition { id="corn",growSeconds=600,waterReductionSeconds=90,seedCost=3,yield=4,sellPrice=9 } };
        var definitions = new[] { new AnimalDefinition() };
        bool night = args.Length > 1 && args[1] == "--night";
        var farm = new FarmWorld(32, 32, crops, now - (night ? 390 : 1242));
        var animals = new AnimalWorld(farm, definitions);
        FarmLandscapePlan.Seed(farm,animals,now);
        farm.Wallet.coins=2500;
        var snapshot = new FarmSnapshot { width = 32, depth = 32, cellSize = 2,
            epochUtc = farm.EpochUtc, savedUtc = now, tiles = farm.Tiles, pens = animals.Pens, wallet = farm.Wallet };
        snapshot.Restore(crops, definitions, out _);
        string destination = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.WriteAllText(destination, new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 }.Serialize(snapshot));
        Console.WriteLine("Visual demo fixture written: " + destination);
        return 0;
    }
}
