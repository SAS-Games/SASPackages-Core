using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAS.Core.TagSystem
{
    public class ServiceLocator : IServiceLocator
    {
        public interface IService { }

        private Dictionary<string, List<object>> _services = new Dictionary<string, List<object>>();

        public void Add<T>(object service, string tag = "")
        {
            Add(typeof(T), service, tag);
        }

        public void Add(Type type, object service, string tag = "")
        {
            var key = GetKey(type, tag);
            if (!_services.TryGetValue(key, out var serviceList))
            {
                serviceList = new List<object>();
                _services.Add(key, serviceList);
            }

            if (!serviceList.Contains(service))
                serviceList.Add(service);

            var baseTypes = type.GetInterfaces();
            if (type.BaseType != null)
                baseTypes = baseTypes.Prepend(type.BaseType).ToArray();

            foreach (var baseType in baseTypes)
                Add(baseType, service, tag);
        }

        private string GetKey(Type type, string tag)
        {
            return $"{type.Name}{tag}";
        }

        public T Get<T>(string tag = "")
        {
            TryGet<T>(out var service, tag);
            return service;
        }

        public bool TryGet<T>(out T service, string tag = "")
        {
            bool result = TryGet(typeof(T), out object serviceObj, tag);
            service = (T)serviceObj; 
            return result;
        }

        public bool TryGet(Type type, out object service, string tag = "")
        {
            var key = GetKey(type, tag);
            if (!_services.TryGetValue(key, out var services))
            {
                service = null;
                Debug.LogError($"Required service of type {type.Name} with tag {tag} is not found");
                return false;
            }

            if (services.Count > 1)
                Debug.LogError($"There is more than one IService that implements {type.Name}");

            service = services[0];
            return true;
        }

        public IEnumerable<T> GetAll<T>(string tag = "")
        {
            return GetAll(typeof(T), tag).Cast<T>();
        }

        public IEnumerable<object> GetAll(Type type, string tag = "")
        {
            if (_services.TryGetValue(GetKey(type, tag), out var value))
                return value;
            else
                return Array.Empty<object>();
        }

        public T GetOrCreate<T>(string tag = "")
        {
            return (T)GetOrCreate(typeof(T), tag);
        }

        public object GetOrCreate(Type type, string tag = "")
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

        public bool Remove<T>(string tag = "")
        {
            return Remove(typeof(T), tag);
        }

        public bool Remove(Type type, string tag = "")
        {
            var key = GetKey(type, tag);
            return !_services.Remove(key);
        }

        public void OnInstanceCreated()
        {
            Debug.Log("Service Loactor has been injected for very first time");
        }
    }
}
