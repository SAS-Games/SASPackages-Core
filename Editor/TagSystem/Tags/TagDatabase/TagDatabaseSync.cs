#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SAS.Core.TagSystem.Editor
{
    [InitializeOnLoad]
    internal static class TagDatabaseSync
    {
        static TagDatabaseSync()
        {
            // Runs after every domain reload / compilation
            EditorApplication.delayCall += SyncAllDatabases;
        }

        [MenuItem("Tools/Tags/Sync TagDatabase")]
        private static void SyncAllDatabases()
        {
            var databases = AssetDatabase
                .FindAssets("t:TagDatabase")
                .Select(guid => AssetDatabase.LoadAssetAtPath<TagDatabase>(
                    AssetDatabase.GUIDToAssetPath(guid)))
                .Where(db => db != null);

            foreach (var db in databases)
                SyncDatabase(db);
        }

        private static void SyncDatabase(TagDatabase database)
        {
            var tagType = typeof(Tag);

            var idFields = tagType
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(int) && (f.IsLiteral || f.IsInitOnly));
            
            var duplicateGroups = idFields
                .Select(f => new
                {
                    Name = f.Name,
                    Value = f.IsLiteral ? (int)f.GetRawConstantValue() : (int)f.GetValue(null)
                })
                .GroupBy(x => x.Value).Where(g => g.Count() > 1).ToList();

            if (duplicateGroups.Count > 0)
            {
                string message = "Duplicate Tag GUIDs detected!\n\n";

                foreach (var group in duplicateGroups)
                {
                    message += $"GUID: {group.Key}\n";
                    foreach (var item in group)
                    {
                        message += $"  - {item.Name}\n";
                    }
                    message += "\n";
                }

                Debug.LogError("[TagDatabase] Duplicate GUIDs found!\n" + message, database);

                EditorUtility.DisplayDialog("Duplicate Tag GUIDs", message, "OK");

                return;
            }

            var existing = database.Entries.ToDictionary(e => e.guid);

            bool changed = false;

            foreach (var field in idFields)
            {
                int guid = field.IsLiteral ? (int)field.GetRawConstantValue() : (int)field.GetValue(null);
                string name = field.Name;

                if (guid == 0)
                {
                    Debug.LogError($"[TagDatabase] Tag '{name}' has invalid GUID 0", database);
                    continue;
                }

                if (existing.TryGetValue(guid, out var entry))
                {
                    if (entry.name != name)
                    {
                        Debug.LogWarning($"[TagDatabase] GUID {guid} name mismatch. " +
                                         $"Code='{name}', Database='{entry.name}'", database);
                    }
                }
                else
                {
                    database.Entries
                        .GetType()
                        .GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?
                        .Invoke(database.Entries, new object[]
                        {
                            new TagDatabase.Entry
                            {
                                guid = guid,
                                name = name
                            }
                        });

                    Debug.Log(
                        $"[TagDatabase] Added missing tag '{name}' ({guid})",
                        database);

                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
