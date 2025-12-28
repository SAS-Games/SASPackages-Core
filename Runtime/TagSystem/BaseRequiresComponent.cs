using System;

namespace SAS.Core.TagSystem
{
    public abstract class BaseRequiresComponent : BaseRequiresAttribute
    {
        public bool includeInactive;
    }
    public abstract class BaseRequiresAttribute : Attribute
    {
        public Tag tag;
    }
}
