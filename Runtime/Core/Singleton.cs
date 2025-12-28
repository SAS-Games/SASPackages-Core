using UnityEngine;

namespace SAS.Core
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _Instance = null;

        public bool _PersistentOnSceneChange = false;

        protected Singleton() { }

        public static T Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = FindFirstObjectByType<T>();

                return _Instance;
            }
        }

        protected virtual void Awake()
        {
            if (_PersistentOnSceneChange)
                DontDestroyOnLoad(gameObject);
        }

        protected virtual void Start()
        {
            T[] instance = FindObjectsByType<T>(FindObjectsSortMode.None);
            if (instance.Length > 1)
            {
                Debug.Log(gameObject.name + " has been destroyed because another object already has the same component.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (this == _Instance)
                _Instance = null;
        }
    }
}