using UnityEditor;
using UnityEngine;
using System;

namespace SAS.Core.TagSystem.Editor
{
    internal class TagNamePromptWindow : EditorWindow
    {
        private string _name;
        private Action<string> _onConfirm;

        public static void Show(string title, string defaultName, Action<string> onConfirm)
        {
            var window = CreateInstance<TagNamePromptWindow>();
            window.titleContent = new GUIContent(title);
            window._name = defaultName;
            window._onConfirm = onConfirm;

            window.position = new Rect(
                Screen.width / 2f,
                Screen.height / 2f,
                300,
                70);

            window.ShowUtility();
        }

        private void OnGUI()
        {
            GUI.SetNextControlName("TagNameField");
            _name = EditorGUILayout.TextField("Tag Name", _name);

            GUILayout.Space(8);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_name)))
            {
                if (GUILayout.Button("Create"))
                {
                    _onConfirm?.Invoke(_name.Trim());
                    Close();
                }
            }

            // Auto-focus
            EditorGUI.FocusTextInControl("TagNameField");
        }
    }
}