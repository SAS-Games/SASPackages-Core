using System;
using UnityEditor;
using UnityEngine;

namespace SAS.Core.Editor
{
    public static class EditorUtility
    {
        public static void DropDown(int id, Rect position, string[] options, int selectedIndex, Action<int> onSelect, Action onAddItemClicked = null)
        {
            DropDown(id, position, position, options, null, selectedIndex, "None", Color.white, onSelect, onAddItemClicked);
        }

        public static void DropDown(int id, Rect position, Rect popupPos, string[] options, int selectedIndex, Action<int> onSelect, Action onAddItemClicked = null)
        {
            DropDown(id, position, popupPos, options, null, selectedIndex, "None", Color.white, onSelect, onAddItemClicked);
        }

        public static void DropDown(int id, Rect position, string[] options, int selectedIndex, string defaultText, Color color, Action<int> onSelect, Action onAddItemClicked = null)
        {
            DropDown(id, position, position, options, null, selectedIndex, defaultText, color, onSelect, onAddItemClicked);
        }

        public static void DropDown(int id, Rect position, Rect popupPos, string[] options, int selectedIndex, string defaultText, Color color, Action<int> onSelect, Action onAddItemClicked = null)
        {
            DropDown(id, position, popupPos, options, null, selectedIndex, defaultText, color, onSelect, onAddItemClicked);
        }

        public static void DropDown(int id, Rect position, Rect popupPos, string[] popupOptions, string[] displayOptions, int selectedIndex, string defaultText, Color color, Action<int> onSelect, Action onAddItemClicked = null)
        {
            DrawDropDown(id, position, popupPos, popupOptions, displayOptions, selectedIndex, defaultText, color, onSelect, onAddItemClicked);
        }

        private static void DrawDropDown(int id, Rect position, Rect popupPos, string[] popupOptions, string[] displayOptions, int selectedIndex, string defaultText, Color color, Action<int> onSelect, Action onAddItemClicked)
        {
            int controlID = GUIUtility.GetControlID(id, FocusType.Keyboard, position);

            var prevColor = GUI.contentColor;
            GUI.contentColor = color;

            bool hasSelection =
                selectedIndex >= 0 &&
                selectedIndex < popupOptions.Length;

            // ---- Button label ----
            string label =
                hasSelection
                    ? (displayOptions != null && selectedIndex < displayOptions.Length
                        ? displayOptions[selectedIndex]
                        : popupOptions[selectedIndex])
                    : defaultText;

            // ---- Tooltip ----
            string tooltip =
                hasSelection
                    ? popupOptions[selectedIndex]
                    : defaultText;

            var content = new GUIContent(label, tooltip);

            if (DropdownButton(controlID, position, content))
            {
                // Popup always uses FULL names
                SearchablePopup.Show(
                    popupPos,
                    popupOptions,
                    selectedIndex,
                    onSelect,
                    onAddItemClicked
                );
            }

            GUI.contentColor = prevColor;
        }

        private static bool DropdownButton(int id, Rect position, GUIContent content)
        {
            Event current = Event.current;

            switch (current.type)
            {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && current.button == 0)
                    {
                        current.Use();
                        return true;
                    }

                    break;

                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id && current.character == '\n')
                    {
                        current.Use();
                        return true;
                    }

                    break;

                case EventType.Repaint:
                    EditorStyles.popup.Draw(position, content, id, false);
                    break;
            }

            return false;
        }
    }
}