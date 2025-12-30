using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAS.Core.TagSystem
{
    public class ServiceLocator : IServiceLocator
    {
        public class WeakService
        {
            public WeakReference<object> Reference;

            public WeakService(object service)
            {
                Reference = new WeakReference<object>(service);
            }

            public bool TryGet(out object service)
            {
                return Reference.TryGetTarget(out service);
            }

            public bool IsAlive => Reference.TryGetTarget(out _);
        }

        public interface IService
        {
        }

        private Dictionary<Key, List<WeakService>> _services = new();

        public void Add<T>(object service, Tag tag = default)
        {
            Add(typeof(T), service, tag);
        }

        public void Add(Type type, object service, Tag tag = default)
        {
            var key = GetKey(type, tag);
            if (!_services.TryGetValue(key, out var serviceList))
            {
                serviceList = new List<WeakService>();
                _services.Add(key, serviceList);
            }

            if (!serviceList.Any(ws => ws.TryGet(out var s) && s == service))
                serviceList.Add(new WeakService(service));

            var baseTypes = type.GetInterfaces();
            if (type.BaseType != null)
                baseTypes = baseTypes.Prepend(type.BaseType).ToArray();

            foreach (var baseType in baseTypes)
                Add(baseType, service, tag);
        }


        private Key GetKey(Type type, Tag tag)
        {
            return new Key { type = type, tag = tag };
        }

        public T Get<T>(Tag tag = default)
        {
            TryGet<T>(out var service, tag);
            return service;
        }

        public bool TryGet<T>(out T service, Tag tag = default)
        {
            bool result = TryGet(typeof(T), out object serviceObj, tag);
            service = (T)serviceObj;
            return result;
        }

        public bool TryGet(Type type, out object service, Tag tag = default)
        {
            service = null;
            var key = GetKey(type, tag);

            if (!_services.TryGetValue(key, out var list))
                return false;

            // Clean up dead references
            list.RemoveAll(w => !w.IsAlive);

            if (list.Count == 0)
            {
                _services.Remove(key);
                return false;
            }

            if (list.Count > 1)
                Debug.LogError($"More than one service registered for {type.Name}");

            return list[0].TryGet(out service);
        }


        public IEnumerable<T> GetAll<T>(Tag tag = default)
        {
            return GetAll(typeof(T), tag).Cast<T>();
        }

        public IEnumerable<object> GetAll(Type type, Tag tag = default)
        {
            if (_services.TryGetValue(GetKey(type, tag), out var list))
            {
                list.RemoveAll(ws => !ws.IsAlive);

                foreach (var ws in list)
                {
                    if (ws.Reference.TryGetTarget(out var service))
                        yield return service;
                }
            }
            yield break;
        }


        public T GetOrCreate<T>(Tag tag = default)
        {
            return (T)GetOrCreate(typeof(T), tag);
        }

        public object GetOrCreate(Type type, Tag tag = default)
        {
            var key = GetKey(type, tag);
            if (!_services.TryGetValue(key, out var values))
            {
                var instance = Activator.CreateInstance(type, new[] { this });
                Add(type, instance, tag);
                return instance;
            }

            return values[0];
        }

        public bool Remove<T>(Tag tag = default)
        {
            return Remove(typeof(T), tag);
        }

        public bool Remove(Type type, Tag tag = default)
        {
            var key = GetKey(type, tag);
            return _services.Remove(key);
        }

        public void OnInstanceCreated()
        {
            Debug.Log("Service Loactor has been injected for very first time");
        }
    }
}
