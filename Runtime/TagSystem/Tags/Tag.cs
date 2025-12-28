using System;
using UnityEngine;

namespace SAS.Core.TagSystem
{
    [Serializable]
    public partial struct Tag : IEquatable<Tag>
    {
        [SerializeField, ReadOnly] private int guid; // identity
        public int Id => guid;
        public bool IsValid => guid != 0;

        public override string ToString()
        {
#if UNITY_EDITOR
#pragma warning disable CS0618 // Type or member is obsolete
            return Name;
#pragma warning restore CS0618 // Type or member is obsolete
#else
            return guid.ToString();
#endif
        }

        public bool Equals(Tag other) => guid == other.guid;
        public override bool Equals(object obj) => obj is Tag other && Equals(other);
        public override int GetHashCode() => guid;

        public static bool operator ==(Tag a, Tag b) => a.guid == b.guid;
        public static bool operator !=(Tag a, Tag b) => a.guid != b.guid;

        public static Tag FromId(int id)
        {
            return new Tag { guid = id };
        }

        public static implicit operator Tag(int id)
        {
            return FromId(id);
        }

        public static explicit operator int(Tag tag)
        {
            return tag.Id;
        }
    }
}