using System;
using System.Collections.Generic;

namespace SAS.Core.TagSystem
{
    public interface IServiceLocator : IBindable
    {
        void Add<T>(object service, Tag tag = default);
        void Add(Type type, object service, Tag tag = default);
        T Get<T>(Tag tag = default);
        bool TryGet<T>(out T service, Tag tag = default);
        bool TryGet(Type type, out object service, Tag tag = default);
        IEnumerable<T> GetAll<T>(Tag tag = default);
        T GetOrCreate<T>(Tag tag = default);
    }
}