using NextGenFarm.Core;
using UnityEngine;

namespace NextGenFarm
{
    public sealed class FarmGridManager : MonoBehaviour
    {
        public event System.Action<Vector2Int, FarmAction, int> ActionApplied;
        public FarmWorld World { get; private set; }
        public SeasonTimeManager Clock { get; private set; }
        public float CellSize { get; private set; } = 2;
        public void Initialize(FarmWorld world, SeasonTimeManager clock, float cellSize)
        {
            World = world; Clock = clock; CellSize = cellSize;
        }
        public Vector2Int WorldToCell(Vector3 position)
        {
            var p = position - transform.position;
            return new Vector2Int(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.z / CellSize));
        }
        public Vector3 CellCenter(int x, int z)
        {
            var tile = World.Get(x, z);
            return transform.position + new Vector3((x + 0.5f) * CellSize, tile?.height ?? 0, (z + 0.5f) * CellSize);
        }
        public int Act(Vector2Int center, FarmAction action, string cropId, bool tractor)
        {
            int count = World.ApplySquare(center.x, center.y, tractor ? 1 : 0, action, cropId, Clock.Now);
            if (count > 0) ActionApplied?.Invoke(center, action, count);
            return count;
        }
        public bool CanDrive(int x, int z) => World.Contains(x, z) && !World.Get(x, z).blocked;
    }
}
