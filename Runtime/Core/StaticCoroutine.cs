using System.Collections;
using UnityEngine;

namespace SAS.Core
{
    public class StaticCoroutine : AutoInstantiateSingleton<StaticCoroutine>
    {
        protected override void Awake()
        {
            _PersistentOnSceneChange = true;
            base.Awake();
        }

        public static Coroutine Start(IEnumerator coroutine)
        {
           return Instance.StartCoroutine(coroutine);
        }

        public static void Stop(Coroutine coroutine)
        {
            Instance.StopCoroutine(coroutine);
        }

        public static void Stop(string coroutine)
        {
            Instance.StopCoroutine(coroutine);
        }
    }
}
