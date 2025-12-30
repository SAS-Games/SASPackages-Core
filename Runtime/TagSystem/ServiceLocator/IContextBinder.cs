using System;
using System.Collections.Generic;

namespace SAS.Core.TagSystem
{
    
    public enum Scope
    {
        ProjectLevel,
        SceneLevel,
        ObjectLevel,
    }
    
    public interface IContextBinder
    {
        Scope BinderScope { get; }
        object GetOrCreate(Type type, Tag tag = default);
        bool TryGet(Type type, out object instance, Tag tag = default);
        bool TryGet<T>(out T instance, Tag tag = default);

        void Add(Type type, object instance, Tag tag = default);
        IReadOnlyDictionary<Key, object> GetAll();
    }
}
