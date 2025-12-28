using System;
using System.Collections.Generic;

namespace SAS.Core.TagSystem
{
    public interface IServiceLocator : IBindable
    {
        void Add<T>(object service, string tag = "");
        void Add(Type type, object service, string tag = "");
        T Get<T>(string tag = "");
        bool TryGet<T>(out T service, string tag = "");
        bool TryGet(Type type, out object service, string tag = "");
        IEnumerable<T> GetAll<T>(string tag = "");
        T GetOrCreate<T>(string tag = "");
    }
}
