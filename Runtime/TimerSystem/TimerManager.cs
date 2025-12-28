using System.Collections.Generic;
using UnityEngine;

namespace SAS.TimerSystem
{
    public static class TimerManager
    {
        static readonly HashSet<Timer> timers = new();
        static readonly List<Timer> sweep = new();

       
        public static void RegisterTimer(Timer timer)
        {
            if (timer == null)
            {
                Debug.LogWarning("[TimerManager] Attempted to register a null timer.");
                return;
            }

            if (!timers.Add(timer))
            {
                Debug.LogWarning($"[TimerManager] Timer '{timer}' is already registered.");
                return;
            }
        }

        public static void DeregisterTimer(Timer timer)
        {
            if (timer == null) return;
            timers.Remove(timer);
        }

        public static void UpdateTimers()
        {
            if (timers.Count == 0) return;

            sweep.RefreshWith(timers);
            foreach (var timer in sweep)
            {
                timer.Tick();
            }
        }

        public static void Clear()
        {
            sweep.RefreshWith(timers);
            foreach (var timer in sweep)
            {
                timer.Dispose();
            }

            timers.Clear();
            sweep.Clear();
        }
    }
}