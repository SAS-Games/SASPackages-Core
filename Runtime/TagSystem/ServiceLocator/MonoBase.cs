using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAS.Core.TagSystem
{
    public class MonoBase : MonoBehaviour
    {
        private List<MonoBase> _children = new List<MonoBase>();
        public IReadOnlyList<MonoBase> Children => _children;
        private MonoBase _parent;

        protected virtual void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene == gameObject.scene)
            {
                Init();
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        protected virtual void Init()
        {
            this.Initialize();
            Debug.Log($"Scene Loaded {gameObject.scene.name}");
        }

        protected virtual void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            // TODO: Use refection set sett all the properties null;
            foreach (var child in _children)
            {
                Destroy(child.gameObject);
            }
        }

        public void SetParent(MonoBase parent)
        {
            _parent = parent;
            parent?.AddChild(this);
        }

        private void AddChild(MonoBase monoBase)
        {
            _children.Add(monoBase);
        }

        public void Unparent()
        {
            _parent?._children?.Remove(this);
            _parent = null;
        }
    }
}