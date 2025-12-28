#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SAS.Core.TagSystem.Editor
{
    [CustomEditor(typeof(TagDatabase))]
    internal sealed class TagDatabaseEditor : UnityEditor.Editor
    {
        private SerializedProperty _entries;

        private int _renamingIndex = -1;
        private string _renameBuffer;

        private GUIStyle _centeredLabel;
        private GUIStyle _centeredTextField;

        private void OnEnable()
        {
            _entries = serializedObject.FindProperty("entries");

            _centeredLabel = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter
            };

            _centeredTextField = new GUIStyle(EditorStyles.textField)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Tags are identified by GUIDs.\n" +
                "Renaming only changes the display name and is safe.\n" +
                "Deleting a tag will remove it from the database and may break intent.",
                MessageType.Info
            );

            serializedObject.Update();
            EditorGUI.indentLevel++;

            for (int i = 0; i < _entries.arraySize; i++)
            {
                var entry = _entries.GetArrayElementAtIndex(i);
                var nameProp = entry.FindPropertyRelative("name");
                var guidProp = entry.FindPropertyRelative("guid");

                bool isRenaming = _renamingIndex == i;
                int guid = guidProp.intValue;
                string currentName = nameProp.stringValue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                // ---- Left : Tag index ----
                EditorGUILayout.LabelField($"Tag {i}", GUILayout.Width(50));

                // ---- Center : Name / Rename ----
                if (isRenaming)
                {
                    EditorGUI.BeginChangeCheck();

                    string newName = EditorGUILayout.DelayedTextField(
                        _renameBuffer,
                        _centeredTextField,
                        GUILayout.ExpandWidth(true)
                    );

                    if (EditorGUI.EndChangeCheck())
                    {
                        TryRename(i, guid, newName);
                    }
                }
                else
                {
                    var content = new GUIContent(
                        currentName,
                        $"GUID: {guid}"
                    );

                    EditorGUILayout.LabelField(
                        content,
                        _centeredLabel,
                        GUILayout.ExpandWidth(true)
                    );
                }

                // ---- Rename button ----
                using (new EditorGUI.DisabledScope(guid == 0))
                {
                    if (GUILayout.Button("✎", GUILayout.Width(24)))
                    {
                        _renamingIndex = i;
                        _renameBuffer = currentName;
                    }
                }

                // ---- Delete button ----
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    var usages = TagUsageFinder.FindUsages(guid);

                    if (usages.Count > 0)
                    {
                        string message =
                            $"Tag '{currentName}' is used in the following locations:\n\n" +
                            string.Join("\n", usages.Take(8));

                        if (usages.Count > 8)
                            message += "\n...";

                        message += "\n\nDeleting it may break references.";

                        bool confirm = EditorUtility.DisplayDialog(
                            "Tag is in use",
                            message,
                            "Delete Anyway",
                            "Cancel"
                        );

                        if (!confirm)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            break;
                        }
                    }

                    Undo.RecordObject(target, "Delete Tag");
                    _entries.DeleteArrayElementAtIndex(i);
                    _renamingIndex = -1;

                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                    AssetDatabase.SaveAssets();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
        }

        private void TryRename(int index, int guid, string newName)
        {
            var db = (TagDatabase)target;

            if (string.IsNullOrWhiteSpace(newName))
            {
                EditorUtility.DisplayDialog("Invalid Name", "Tag name cannot be empty.", "OK");
                return;
            }

            if (db.Entries.Any(e => e.name == newName && e.guid != guid))
            {
                EditorUtility.DisplayDialog("Duplicate Tag", $"A tag named '{newName}' already exists.", "OK");
                return;
            }
            
            string message =
                $"Rename tag?\n\n" + $"{db.Entries[index]}  →  {newName}\n\n" +
                "This only changes the display name.\n" +
                "All references are GUID-based and remain intact.";

            bool confirm = EditorUtility.DisplayDialog("Rename Tag", message, "Rename", "Cancel");

            if (!confirm)
                return;

            Undo.RecordObject(db, "Rename Tag");

            var entry = db.Entries[index];
            entry.name = newName;

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            _renamingIndex = -1;
        }
    }
}
#endif