using UnityEngine;

namespace SAS.Core.TagSystem
{
    public class TagDropdownAttribute : PropertyAttribute
    {
        public string SourceFieldName { get; private set; }

        public TagDropdownAttribute() { }

        public TagDropdownAttribute(string sourceFieldName)
        {
            this.SourceFieldName = sourceFieldName;
        }
    }

}