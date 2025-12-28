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
            if (guidProp == null)
            {
                EditorGUI.HelpBox(position, "Tag property missing 'guid' field.", MessageType.Error);
                return false;
            }

            var database = Resources.Load<TagDatabase>(DatabasePath);
            if (database == null)
            {
                EditorGUI.HelpBox(position, "TagDatabase not found.", MessageType.Error);
                return false;
            }

            int currentId = guidProp.intValue;
            var entries = database.Entries;

            string[] options = new string[entries.Count + 1];
            options[0] = "<None>";

            int selectedIndex = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                options[i + 1] = entries[i].name;
                if (entries[i].guid == currentId)
                    selectedIndex = i + 1;
            }

            EditorGUI.BeginProperty(position, label, tagProperty);

            int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, options);

            if (newIndex != selectedIndex)
            {
                guidProp.intValue = newIndex == 0 ? 0 : entries[newIndex - 1].guid;
                EditorGUI.EndProperty();
                return true;
            }

            EditorGUI.EndProperty();
            return false;
        }
    }
}