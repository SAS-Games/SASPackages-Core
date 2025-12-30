using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAS.Core.TagSystem
{
    [DefaultExecutionOrder(-100)]
    public class BaseContextBinder : MonoBase, IContextBinder
    {
        [SerializeField] public bool m_EarlyBinding = false;

        [Tooltip(
            "If Scope.SceneLevel, this GameObject will be marked as DontDestroyOnLoad. Make Sure Only one context is there for which Scope is SceneLevel")]
        [SerializeField]
        private Scope m_Scope = Scope.SceneLevel;

        [SerializeField] private Binder m_Binder; 
        public bool IsCrossContextBinder => m_Scope == Scope.ProjectLevel;
        Scope IContextBinder.BinderScope => m_Scope;

        protected override void Awake()
        {
            ++m_Binder.refCount;
            if (m_Scope == Scope.ObjectLevel)
                m_Binder = Instantiate(m_Binder);

            if (m_Scope == Scope.ProjectLevel)
            {
                if (!ComponentExtensions._cachedContext.TryGetValue("DontDestroyOnLoad", out var context))
                {
                    DontDestroyOnLoad(gameObject);
                    ComponentExtensions._cachedContext.Add("DontDestroyOnLoad", this);
                }
                else
                    Debug.LogWarning($"There is already an CrossContextBinder wit the name {context.GetType().Name} ");
            }
            else if (m_Scope == Scope.SceneLevel)
            {
                if (!ComponentExtensions._cachedContext.ContainsKey(this.gameObject.scene.name))
                    ComponentExtensions._cachedContext.Add(this.gameObject.scene.name, this);
            }

            if (m_EarlyBinding)
                m_Binder.CreateAllInstance(this);
        }

        object IContextBinder.GetOrCreate(Type type, Tag tag)
        {
            return m_Binder.GetOrCreate(this, type, tag);
        }


        bool IContextBinder.TryGet(Type type, out object instance, Tag tag)
        {
            return m_Binder.TryGet(type, out instance, tag);
        }

        bool IContextBinder.TryGet<T>(out T instance, Tag tag)
        {
            instance = default;

            if ((this as IContextBinder).TryGet(typeof(T), out object result, tag))
            {
                instance = (T)result;
                return true;
            }

            return false;
        }

        void IContextBinder.Add(Type type, object instance, Tag tag)
        {
            m_Binder.Add(type, instance, tag);
        }

        IReadOnlyDictionary<Key, object> IContextBinder.GetAll()
        {
            return m_Binder.CachedBindings;
        }

        protected override void OnDestroy()
        {
            if (gameObject != null && gameObject.scene != null && !string.IsNullOrEmpty(gameObject.scene.name))
                ComponentExtensions._cachedContext.Remove(gameObject?.scene.name);
            if (m_Binder != null)
            {
                --m_Binder.refCount;
                if (m_Binder.refCount==0)
                    m_Binder.Clear();
            }

            base.OnDestroy();
        }
    }
}
