using System;
using System.Collections.Generic;

namespace NextGenFarm.Core
{
    public struct Cell
    {
        public int x, z;
        public Cell(int x, int z) { this.x = x; this.z = z; }
    }
    public static class GridNavigation
    {
        public static bool Passable(FarmWorld world, Cell a, Cell b, float maxStep)
        {
            var from = world.Get(a.x, a.z); var to = world.Get(b.x, b.z);
            return from != null && to != null && !to.blocked && Math.Abs(from.height - to.height) <= maxStep;
        }
        public static List<Cell> FindPath(FarmWorld world, Cell start, Cell goal, float maxStep = 0.75f)
        {
            if (!world.Contains(start.x, start.z) || !world.Contains(goal.x, goal.z) ||
                world.Get(goal.x, goal.z).blocked) return null;
            int[] previous = new int[world.Tiles.Length];
            for (int i = 0; i < previous.Length; i++) previous[i] = -1;
            int startIndex = world.Index(start.x, start.z), goalIndex = world.Index(goal.x, goal.z);
            var queue = new Queue<int>(); queue.Enqueue(startIndex); previous[startIndex] = startIndex;
            int[] dx = { 0, 1, 0, -1 }, dz = { 1, 0, -1, 0 };
            while (queue.Count > 0 && previous[goalIndex] < 0)
            {
                int index = queue.Dequeue(); var a = new Cell(index % world.Width, index / world.Width);
                for (int d = 0; d < 4; d++)
                {
                    var b = new Cell(a.x + dx[d], a.z + dz[d]);
                    if (!world.Contains(b.x, b.z) || !Passable(world, a, b, maxStep)) continue;
                    int next = world.Index(b.x, b.z);
                    if (previous[next] >= 0) continue;
                    previous[next] = index; queue.Enqueue(next);
                }
            }
            if (previous[goalIndex] < 0) return null;
            var path = new List<Cell>();
            for (int i = goalIndex; i != startIndex; i = previous[i]) path.Add(new Cell(i % world.Width, i / world.Width));
            path.Reverse(); return path;
        }
        public static List<Cell> Serpentine(int width, int depth)
        {
            var route = new List<Cell>();
            bool reverse = false;
            for (int z = 0; z < depth; z += 3)
            {
                var row = new List<Cell>();
                for (int x = 0; x < width; x += 3) row.Add(new Cell(Math.Min(x + 1, width - 1), Math.Min(z + 1, depth - 1)));
                if (reverse) row.Reverse();
                route.AddRange(row); reverse = !reverse;
            }
            return route;
        }
    }
}
