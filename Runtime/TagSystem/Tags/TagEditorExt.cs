using System;
using UnityEngine;

namespace SAS.Core.TagSystem
{
#if UNITY_EDITOR
    public partial struct Tag
    {
        [SerializeField] private string resolvedName;
        [SerializeField] private TagDatabase sourceOptions;
        private bool _isResolved;
        [SerializeField] private string lastKnownName;

        [Obsolete("Tag.Name is for Editor use only. " + "Do NOT use this in gameplay code. Use Tag.Id instead.", false)]
        public string Name
        {
            get
            {
                if (_isResolved)
                    return resolvedName;

                if (!sourceOptions || guid == 0)
                {
                    resolvedName = lastKnownName;
                    _isResolved = true;
                    return resolvedName;
                }

                resolvedName = sourceOptions.GetNameByGuid(guid);

                if (string.IsNullOrEmpty(resolvedName))
                    resolvedName = lastKnownName;
                _isResolved = true;
                return resolvedName;
            }
        }

        public void Set(int newGuid, string newName, TagDatabase sourceSO)
        {
            if (guid != 0)
                return;

            guid = newGuid;
            resolvedName = newName;
            sourceOptions = sourceSO;
            lastKnownName = newName;
            _isResolved = true;
        }

        public string GetLastKnownName() => lastKnownName;
    }
#endif
}