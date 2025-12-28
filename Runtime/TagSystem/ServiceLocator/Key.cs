using System;
using System.Collections.Generic;

namespace SAS.Core.TagSystem
{
    public class Key : IEquatable<Key>
    {
        public Type type;
        public Tag tag;

        public bool Equals(Key other)
        {
            return other != null && type == other.type && tag == other.tag;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Key);
        }

        public override int GetHashCode()
        {
            var hashCode = -1744019480;
            hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(type);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(tag.ToString());
            return hashCode;
        }
    }
}
