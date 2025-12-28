using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SAS.Core.TagSystem.Editor
{
    [CustomPropertyDrawer(typeof(Tag))]
    public class TagDrawer : PropertyDrawer
    {
        private bool isRenaming;
        private string renameBuffer;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var guidProp = property.FindPropertyRelative("guid");
            var resolvedNameProp = property.FindPropertyRelative("resolvedName");
            var sourceOptionsProp = property.FindPropertyRelative("sourceOptions");

#if UNITY_EDITOR
            var lastKnownNameProp = property.FindPropertyRelative("lastKnownName");
#endif

            var targetObject = property.serializedObject.targetObject;
            TagDropdownAttribute attr = fieldInfo.GetCustomAttribute<TagDropdownAttribute>();

            TagDatabase stringOptions = null;

            if (attr != null && !string.IsNullOrEmpty(attr.SourceFieldName))
            {
                var sourceFieldInfo = targetObject.GetType().GetField(
                    attr.SourceFieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (sourceFieldInfo != null)
                    stringOptions = sourceFieldInfo.GetValue(targetObject) as TagDatabase;
                else
                {
                    string resourcePath = attr.SourceFieldName;
                    if (resourcePath.EndsWith(".asset"))
                        resourcePath = resourcePath[..^6];

                    stringOptions = Resources.Load<TagDatabase>(resourcePath);
                }
            }

            if (stringOptions == null && sourceOptionsProp.objectReferenceValue != null)
                stringOptions = sourceOptionsProp.objectReferenceValue as TagDatabase;

            if (stringOptions == null)
                stringOptions = Resources.Load<TagDatabase>($"TagDatabase/{TagDatabase.NAME}");

            if (stringOptions == null)
            {
                EditorGUI.HelpBox(position, "Missing TagDatabase asset.", MessageType.Error);
                return;
            }

            int currentGuid = guidProp.intValue;
            string currentName = stringOptions.GetNameByGuid(currentGuid);

#if UNITY_EDITOR
            if (string.IsNullOrEmpty(currentName))
                currentName = lastKnownNameProp.stringValue;
#endif

            // --- Layout ---
            Rect dropdownRect = new(position.x, position.y, position.width - 50, position.height);
            Rect renameRect = new(position.x + position.width - 48, position.y, 22, position.height);
            Rect addRect = new(position.x + position.width - 24, position.y, 22, position.height);

            if (isRenaming && currentGuid != 0)
            {
                EditorGUI.BeginChangeCheck();
                renameBuffer = EditorGUI.DelayedTextField(dropdownRect, label.text, renameBuffer);

                if (EditorGUI.EndChangeCheck())
                {
                    if (!TagRenameUtility.CanRename(stringOptions, currentGuid, renameBuffer, out var error))
                    {
                        EditorUtility.DisplayDialog("Invalid Tag Name", error, "OK");
                        return;
                    }

                    string message =
                        $"Rename tag?\n\n" + $"{currentName}  →  {renameBuffer}\n\n" + "This only changes the display name.\n" +
                        "All references are GUID-based and remain intact.";

                    bool confirm = EditorUtility.DisplayDialog("Rename Tag", message, "Rename", "Cancel");

                    if (!confirm)
                        return;

                    Undo.RecordObject(stringOptions, "Rename Tag");

                    var entry = stringOptions.Entries.First(e => e.guid == currentGuid);
                    entry.name = renameBuffer;

                    EditorUtility.SetDirty(stringOptions);
                    AssetDatabase.SaveAssets();

                    isRenaming = false;
                }
            }
            else
            {
                List<string> displayList = new() { "<None>" };
                int selectedIndex = 0;

                for (int i = 0; i < stringOptions.Entries.Count; i++)
                {
                    var entry = stringOptions.Entries[i];
                    displayList.Add(entry.name);

                    if (entry.guid == currentGuid)
                        selectedIndex = i + 1;
                }

                // Missing entry visualization
                if (selectedIndex == 0 && currentGuid != 0)
                {
                    displayList.Insert(1, $"❌ {currentName ?? "Missing"}");
                    selectedIndex = 1;
                }

                int newIndex = EditorGUI.Popup(
                    dropdownRect,
                    label.text,
                    selectedIndex,
                    displayList.ToArray());

                if (newIndex != selectedIndex)
                {
                    if (newIndex == 0)
                    {
                        guidProp.intValue = 0;
                        resolvedNameProp.stringValue = "";
                        sourceOptionsProp.objectReferenceValue = stringOptions;
                    }
                    else
                    {
                        var entry = stringOptions.Entries[newIndex - 1];
                        guidProp.intValue = entry.guid;
                        resolvedNameProp.stringValue = entry.name;
                        sourceOptionsProp.objectReferenceValue = stringOptions;

#if UNITY_EDITOR
                        lastKnownNameProp.stringValue = entry.name;
#endif
                    }
                }
            }

            using (new EditorGUI.DisabledScope(currentGuid == 0))
            {
                if (GUI.Button(renameRect, "✎"))
                {
                    isRenaming = !isRenaming;
                    renameBuffer = currentName;
                }
            }

            if (GUI.Button(addRect, "+"))
            {
                string newKeyName = ObjectNames.GetUniqueName(
                    stringOptions.Entries.Select(e => e.name).ToArray(), "NewKey");

                Undo.RecordObject(stringOptions, "Add String Option");
                stringOptions.AddEntry(newKeyName);
                EditorUtility.SetDirty(stringOptions);

                var newEntry = stringOptions.Entries.Last();

                guidProp.intValue = newEntry.guid;
                resolvedNameProp.stringValue = newEntry.name;
                sourceOptionsProp.objectReferenceValue = stringOptions;

#if UNITY_EDITOR
                lastKnownNameProp.stringValue = newEntry.name;
#endif
            }
        }
    }
}