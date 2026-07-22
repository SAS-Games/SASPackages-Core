using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SAS.Core.TagSystem.Editor
{
    public static class TagEditorUtility
    {
        private static TagDatabase s_CachedDatabase;

        public static bool DrawTagPopup(Rect position, SerializedProperty tagProperty, GUIContent label)
        {
            var guidProp = tagProperty.FindPropertyRelative("guid");
            var resolvedNameProp = tagProperty.FindPropertyRelative("resolvedName");
            var sourceOptionsProp = tagProperty.FindPropertyRelative("sourceOptions");
            var lastKnownNameProp = tagProperty.FindPropertyRelative("lastKnownName");

            if (guidProp == null || resolvedNameProp == null || sourceOptionsProp == null || lastKnownNameProp == null)
            {
                EditorGUI.HelpBox(position, "Invalid Tag property.", MessageType.Error);
                return false;
            }

            TagDatabase database = GetTagDatabase();

            if (database == null)
            {
                EditorGUI.HelpBox(position, "TagDatabase not found.", MessageType.Error);
                return false;
            }

            var entries = database.Entries;

            // <None> + entries + <Add New>
            string[] options = new string[entries.Count + 2];
            options[0] = "<None>";

            int selectedIndex = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                options[i + 1] = entries[i].name;

                if (entries[i].guid == guidProp.intValue)
                    selectedIndex = i + 1;
            }

            int addNewIndex = options.Length - 1;
            options[addNewIndex] = "➕ Add New Tag…";

            EditorGUI.BeginProperty(position, label, tagProperty);

            int newIndex = EditorGUI.Popup(
                position,
                label.text,
                selectedIndex,
                options);

            bool changed = false;

            // Add New Tag selected
            if (newIndex == addNewIndex)
            {
                string defaultName = ObjectNames.GetUniqueName(entries.Select(entry => entry.name).ToArray(), "NewTag");

                TagNamePromptWindow.Show("Create Tag", defaultName, newName =>
                    {
                        CreateAndAssignTag(
                            database,
                            tagProperty,
                            guidProp,
                            resolvedNameProp,
                            sourceOptionsProp,
                            lastKnownNameProp,
                            newName);
                    });

                EditorGUI.EndProperty();
                return false;
            }

            // Normal selection
            if (newIndex != selectedIndex)
            {
                if (newIndex == 0)
                {
                    guidProp.intValue = 0;
                    resolvedNameProp.stringValue = string.Empty;
                    sourceOptionsProp.objectReferenceValue = null;
                    lastKnownNameProp.stringValue = string.Empty;
                }
                else
                {
                    var entry = entries[newIndex - 1];

                    guidProp.intValue = entry.guid;
                    resolvedNameProp.stringValue = entry.name;
                    sourceOptionsProp.objectReferenceValue = database;
                    lastKnownNameProp.stringValue = entry.name;
                }

                changed = true;
            }

            EditorGUI.EndProperty();
            return changed;
        }

        public static bool DrawCreateTagButton(Rect rect, TagDatabase database, SerializedProperty guidProp, SerializedProperty resolvedNameProp, SerializedProperty sourceOptionsProp, SerializedProperty lastKnownNameProp = null)
        {
            if (!GUI.Button(rect, "+"))
                return false;

            if (database == null)
            {
                Debug.LogError("[TagSystem] Cannot create a tag because TagDatabase is null.");
                return false;
            }

            string newName = ObjectNames.GetUniqueName(
                database.Entries.Select(entry => entry.name).ToArray(),
                "NewTag");

            Undo.RecordObject(database, "Add Tag");

            database.AddEntry(newName);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssetIfDirty(database);

            var newEntry = database.Entries.Last();

            guidProp.intValue = newEntry.guid;
            resolvedNameProp.stringValue = newEntry.name;
            sourceOptionsProp.objectReferenceValue = database;

            if (lastKnownNameProp != null)
                lastKnownNameProp.stringValue = newEntry.name;

            return true;
        }

        /// <summary>
        /// Finds the TagDatabase anywhere under Assets.
        /// The asset can be renamed or moved to another folder.
        /// </summary>
        public static TagDatabase GetTagDatabase()
        {
            if (s_CachedDatabase != null)
                return s_CachedDatabase;

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(TagDatabase)}", new[] { "Assets" });

            if (guids.Length == 0)
                return null;

            if (guids.Length > 1)
            {
                Debug.LogWarning($"[TagSystem] Found {guids.Length} TagDatabase assets. " + "Only one TagDatabase should exist. The first valid database will be used.");
            }

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TagDatabase database = AssetDatabase.LoadAssetAtPath<TagDatabase>(assetPath);

                if (database == null)
                    continue;

                s_CachedDatabase = database;
                return s_CachedDatabase;
            }

            return null;
        }

        public static void ClearDatabaseCache()
        {
            s_CachedDatabase = null;
        }

        private static void CreateAndAssignTag(TagDatabase database, SerializedProperty tagProperty, SerializedProperty guidProp, SerializedProperty resolvedNameProp, SerializedProperty sourceOptionsProp, SerializedProperty lastKnownNameProp, string newName)
        {
            newName = newName?.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                EditorUtility.DisplayDialog("Invalid Tag", "Tag name cannot be empty.", "OK");
                return;
            }

            if (database.Entries.Any(entry => string.Equals(entry.name, newName, System.StringComparison.OrdinalIgnoreCase)))
            {
                EditorUtility.DisplayDialog("Duplicate Tag", $"A tag named '{newName}' already exists.", "OK");
                return;
            }

            Undo.RecordObject(database, "Add Tag");

            database.AddEntry(newName);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssetIfDirty(database);

            var newEntry = database.Entries.Last();

            guidProp.intValue = newEntry.guid;
            resolvedNameProp.stringValue = newEntry.name;
            sourceOptionsProp.objectReferenceValue = database;
            lastKnownNameProp.stringValue = newEntry.name;

            tagProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}