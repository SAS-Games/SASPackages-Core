using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SAS.Core.TagSystem.Editor
{
    [CreateAssetMenu(fileName = "Key List", menuName = "SAS/Key List")]
    [System.Serializable]
    public class KeyList : ScriptableObject
    {
        public string[] values = new string[] { };

        const string assetName = "Key List";
        public static KeyList Instance()
        {
            var basePath = "Assets/Editor Default Resources/KeyList/";
            var filePath = $"{basePath}/{assetName}.asset";

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            var asset = AssetDatabase.LoadAssetAtPath<KeyList>(filePath);
            if (asset == null)
            {
                asset = CreateInstance<KeyList>();
                AssetDatabase.CreateAsset(asset, $"{filePath}");
                AssetDatabase.SaveAssets();
            }

            var keyListAssets = EditorGUIUtility.Load(filePath) as KeyList;
            return keyListAssets;
        }

        public static string[] GetList()
        {
            return Instance().values;
        }

        public void Add(string value)
        {
            if (Array.IndexOf(values, value) == -1)
                values = values.Concat(new string[] { value }).ToArray();
        }

        public void Remove(int index)
        {
            values = values.Where(e => e != values[index]).ToArray();
        }

        public void AddRange(List<string> values)
        {
            this.values = this.values.Concat(values).ToArray();
            this.values = this.values.Distinct().ToArray();
            EditorUtility.SetDirty(this);
        }
    }
}
