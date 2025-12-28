using System;
using System.Collections.Generic;

namespace SAS.Core.TagSystem.Editor
{
    public static class BinderExtentions
    {
        public static Type[] GetAllInterface<T>(this AppDomain appDomain)
        {
            var result = new List<Type>();
            var assemblies = appDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.IsInterface && typeof(T).IsAssignableFrom(type) && !type.IsAssignableFrom(typeof(T)))
                        result.Add(type);
                }
            }
            return result.ToArray();
        }

        public static Type[] GetAllDerivedTypes<T>(this AppDomain appDomain) where T : class
        {
            var result = new List<Type>();
            var assemblies = appDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (!type.IsAbstract && (type.IsSubclassOf(typeof(T)) || typeof(T).IsAssignableFrom(type)))
                        result.Add(type);
                }
            }
            return result.ToArray();
        }
    }
}
