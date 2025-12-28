using System;

namespace SAS.Core.TagSystem
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FieldRequiresInSceneAttribute : BaseRequiresComponent
    {
        public FieldRequiresInSceneAttribute(int tag = 0, bool includeInactive = false)
        {
            this.includeInactive = includeInactive;
            this.tag = tag;
        }
    }
}
