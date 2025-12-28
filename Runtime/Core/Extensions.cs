using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace SAS.Core
{
    [Serializable]
    public struct floatRange
    {
        public float min;
        public float max;

        public floatRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    [Serializable]
    public struct intRange
    {
        public int min;
        public int max;

        public intRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }
    }

    public static class Extensions
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
