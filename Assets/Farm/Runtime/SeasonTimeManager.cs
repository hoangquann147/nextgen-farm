using System;
using NextGenFarm.Core;
using UnityEngine;

namespace NextGenFarm
{
    public sealed class SeasonTimeManager : MonoBehaviour
    {
        public const double SeasonDuration = ClockMath.SeasonSeconds;
        public double EpochUtc { get; private set; }
        public double Now { get; private set; }
        public Season CurrentSeason => ClockMath.SeasonAt(Now, EpochUtc);
        public double SecondsRemaining => ClockMath.SeasonRemaining(Now, EpochUtc);
        public float DayFraction => (float)((Math.Max(0, Now - EpochUtc) / 600 + 0.35) % 1);
        public event Action<Season> SeasonChanged;
        private double anchorUtc, anchorRealtime;
        private Season previous;
        public static double UtcNow => (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        public void Initialize(double epoch, double minimumUtc = 0)
        {
            EpochUtc = epoch;
            anchorUtc = Math.Max(UtcNow, minimumUtc);
            anchorRealtime = Time.realtimeSinceStartupAsDouble;
            Now = anchorUtc; previous = CurrentSeason;
        }
        private void Update()
        {
            // Monotonic during a session; changing the OS clock cannot rewind crops.
            Now = anchorUtc + Math.Max(0, Time.realtimeSinceStartupAsDouble - anchorRealtime);
            if (previous != CurrentSeason) { previous = CurrentSeason; SeasonChanged?.Invoke(previous); }
        }
        private void OnApplicationPause(bool paused)
        {
            if (!paused && EpochUtc > 0) Initialize(EpochUtc, Now);
        }
    }
}
