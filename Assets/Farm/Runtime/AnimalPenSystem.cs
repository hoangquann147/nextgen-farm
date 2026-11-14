using NextGenFarm.Core;
using UnityEngine;

namespace NextGenFarm
{
    public sealed class AnimalPenSystem : MonoBehaviour
    {
        public AnimalWorld World { get; private set; }
        private SeasonTimeManager clock;
        public void Initialize(AnimalWorld world, SeasonTimeManager time) { World = world; clock = time; }
        public int FeedAll()
        {
            int count = 0;
            foreach (var pen in World.Pens) if (World.Feed(pen.id, clock.Now)) count++;
            return count;
        }
        public int HarvestAll()
        {
            int count = 0;
            foreach (var pen in World.Pens) if (World.Harvest(pen.id, clock.Now)) count++;
            return count;
        }
    }
}
