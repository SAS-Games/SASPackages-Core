using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAS.Core.TagSystem
{
    /// <summary>
    /// Editor / Development-time tag name database.
    /// Not intended for runtime gameplay logic.
    /// </summary>
    [CreateAssetMenu(menuName = "SAS/TagDatabase List")]
    public class TagDatabase : ScriptableObject
    {
        public static string NAME = "Tag Database";
        [Serializable]
        public class Entry
        {
            public string name;
            [ReadOnly, SerializeField] public int guid;
        }

        [SerializeField] private List<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => entries;
        private static Dictionary<int, string> _lookup;

        private void OnEnable()
        {
            BuildLookup();
        }


        private void BuildLookup()
        {
            _lookup ??= new Dictionary<int, string>(entries.Count);
            _lookup.Clear();

            foreach (var e in entries)
            {
                if (e.guid == 0)
                    continue;
                _lookup[e.guid] = e.name;
            }
        }
        
        public string GetNameByGuid(int guid)
        {
            if (guid == 0 || _lookup == null)
                return null;

            return _lookup.GetValueOrDefault(guid);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            BuildLookup();
        }
        
        public void AddEntry(string name)
        {
            entries.Add(new Entry
            {
                guid = GenerateId(),
                name = name
            });

            BuildLookup();
        }

        private static int GenerateId()
        {
            // Editor-only ID generation
            return Guid.NewGuid().GetHashCode();
        }
#endif
    }
}
