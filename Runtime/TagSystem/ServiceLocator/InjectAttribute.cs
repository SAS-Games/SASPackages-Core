using UnityEngine.Scripting;

namespace SAS.Core.TagSystem
{
    [Preserve]
    public class InjectAttribute : BaseRequiresAttribute 
    {
        public bool optional;
        public InjectAttribute(int tag = default, bool optional = false)
        {
            this.optional = optional;
            this.tag = tag;
        }
    }
}