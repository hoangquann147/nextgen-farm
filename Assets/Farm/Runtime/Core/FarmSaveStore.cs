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
