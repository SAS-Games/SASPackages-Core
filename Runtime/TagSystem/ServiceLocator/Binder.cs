using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAS.Core.TagSystem
{
    public interface IBindable
    {
        void OnInstanceCreated();
    }

    [Serializable, CreateAssetMenu(menuName = "SAS/Binder")]
    public class Binder : ScriptableObject
    {
        [Serializable]
        public class Binding
        {
            [SerializeField] private string m_Interface;
            [SerializeField] private string m_Type;
            [SerializeField] private Tag m_Tag;
            public Type InterfaceType => Type.GetType(m_Interface);
            public Tag Tag => m_Tag;
            public object CreateInstance(IContextBinder contextBinder)
            {
                object instance = default;
                Type type = Type.GetType(m_Type);

                if (type.IsSubclassOf(typeof(MonoBehaviour)))
                {
                    var results = GameObject.FindObjectsOfType(type);
                    foreach (var result in results)
                    {

                        instance = ((Component)result).GetComponent(type, Tag);
                        if (instance != null)
                            break;
                    }
                    if (instance == null)
                        Debug.LogError($"No GameObject having component attached of the type:  {m_Type} with  tag: {m_Tag} found");
                }
                else
                    instance = Activator.CreateInstance(Type.GetType(m_Type), new[] { contextBinder });


                InvokeInjectionEvent((IBindable)instance);
                return instance;
            }

            private void InvokeInjectionEvent(IBindable bindable)
            {
                bindable.OnInstanceCreated();
            }
        }



        [SerializeField] private Binding[] m_Bindings;
        private Dictionary<Key, object> _cachedBindings = new Dictionary<Key, object>();
        internal IReadOnlyDictionary<Key, object> CachedBindings => _cachedBindings;

        private Key GetKey(Type type, Tag tag)
        {
            return new Key { type = type, tag = tag };
        }

        internal T GetOrCreate<T>(IContextBinder contextBinder, Tag tag = default)
        {
            return (T)GetOrCreate(contextBinder, typeof(T), tag);
        }

        internal object GetOrCreate(IContextBinder contextBinder, Type type, Tag tag = default)
        {
            var key = GetKey(type, tag);
            if (!_cachedBindings.TryGetValue(key, out var value))
            {
                var instance = CreateInstance(contextBinder, type, tag);
                if (instance != null)
                    Add(type, instance, tag);
                return instance;
            }

            return value;
        }

        internal bool TryGet(Type type, out object instance, Tag tag = default)
        {
            var key = GetKey(type, tag);
            if (!_cachedBindings.TryGetValue(key, out instance))
            {
                instance = null;
                Debug.LogError($"Required service of type {type.Name} with tag {tag} is not found");
                return false;
            }

            return true;
        }

        internal void Add(Type type, object instance, Tag tag = default)
        {
            var key = GetKey(type, tag);
            if (!_cachedBindings.TryGetValue(key, out object cachedInstance))
                _cachedBindings.Add(key, instance);

            var baseTypes = type.GetInterfaces();
            if (type.BaseType != null)
                baseTypes = baseTypes.Prepend(type.BaseType).ToArray();

            foreach (var baseType in baseTypes)
                Add(baseType, instance, tag);
        }

        internal void CreateAllInstance(IContextBinder contextBinder)
        {
            foreach (var binding in m_Bindings)
                GetOrCreate(contextBinder, binding.InterfaceType, binding.Tag);
        }

        private object CreateInstance(IContextBinder contextBinder, Type type, Tag tag)
        {
            var binding = Array.Find(m_Bindings, ele => ele.InterfaceType.Equals(type) && ele.Tag == tag);
            return binding?.CreateInstance(contextBinder);
        }

        internal void Clear()
        {
            _cachedBindings.Clear();
        }
    }
}

