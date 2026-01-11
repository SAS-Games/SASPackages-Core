using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SAS.Core.TagSystem.Editor
{
    public static class TagEditorUtility
    {
        private static string DatabasePath => $"TagDatabase/{TagDatabase.NAME}";

        public static bool DrawTagPopup(Rect position, SerializedProperty tagProperty, GUIContent label)
        {
            var guidProp = tagProperty.FindPropertyRelative("guid");
            var resolvedNameProp = tagProperty.FindPropertyRelative("resolvedName");
            var sourceOptionsProp = tagProperty.FindPropertyRelative("sourceOptions");
            var lastKnownNameProp = tagProperty.FindPropertyRelative("lastKnownName");

            if (guidProp == null)
            {
                EditorGUI.HelpBox(position, "Invalid Tag property.", MessageType.Error);
                return false;
            }

            var database = Resources.Load<TagDatabase>(DatabasePath);
            if (database == null)
            {
                EditorGUI.HelpBox(position, "TagDatabase not found.", MessageType.Error);
                return false;
            }

            var entries = database.Entries;

            // +2 → <None> + <Add New>
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

            int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, options);

            bool changed = false;

            // --- Add New Tag selected ---
            if (newIndex == addNewIndex)
            {
                string defaultName = ObjectNames.GetUniqueName(
                    entries.Select(e => e.name).ToArray(),
                    "NewTag");

                TagNamePromptWindow.Show("Create Tag", defaultName, newName =>
                {
                    // Validation
                    if (entries.Any(e => e.name == newName))
                    {
                        EditorUtility.DisplayDialog(
                            "Duplicate Tag",
                            $"A tag named '{newName}' already exists.",
                            "OK");
                        return;
                    }

                    Undo.RecordObject(database, "Add Tag");
                    database.AddEntry(newName);
                    EditorUtility.SetDirty(database);

                    var newEntry = database.Entries.Last();

                    guidProp.intValue = newEntry.guid;
                    resolvedNameProp.stringValue = newEntry.name;
                    sourceOptionsProp.objectReferenceValue = database;
                    lastKnownNameProp.stringValue = newEntry.name;

                    tagProperty.serializedObject.ApplyModifiedProperties();
                });

                EditorGUI.EndProperty();
                return false;
            }

            // --- Normal selection ---
            if (newIndex != selectedIndex)
            {
                if (newIndex == 0)
                {
                    guidProp.intValue = 0;
                    resolvedNameProp.stringValue = "";
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

        public static bool DrawCreateTagButton(Rect rect, TagDatabase database, SerializedProperty guidProp,
            SerializedProperty resolvedNameProp, SerializedProperty sourceOptionsProp,
            SerializedProperty lastKnownNameProp = null)
        {
            if (!GUI.Button(rect, "+"))
                return false;

            string newName = ObjectNames.GetUniqueName(database.Entries.Select(e => e.name).ToArray(), "NewTag");

            Undo.RecordObject(database, "Add Tag");
            database.AddEntry(newName);
            EditorUtility.SetDirty(database);

            var newEntry = database.Entries.Last();

            guidProp.intValue = newEntry.guid;
            resolvedNameProp.stringValue = newEntry.name;
            sourceOptionsProp.objectReferenceValue = database;

            if (lastKnownNameProp != null)
                lastKnownNameProp.stringValue = newEntry.name;

            return true;
        }
    }
}