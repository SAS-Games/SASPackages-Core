using System;
using UnityEditor;
using UnityEngine;

namespace SAS.Core.Editor
{
    public static class EditorUtility
    {
        public static void DropDown(int id, Rect position, string[] options, int selectedIndex, string defaultText, Color color, Action<int> onSelect, Action onAddItemClicked = null)
        {
            DrawDropDown(id, position, position, options, selectedIndex, defaultText, color, onSelect, onAddItemClicked);
        }

        public static void DropDown(int id, Rect position, Rect popupPos, string[] options, int selectedIndex, string defaultText, Color color, Action<int> onSelect, Action onAddItemClicked = null)
        {
            DrawDropDown(id, position, popupPos, options, selectedIndex, defaultText, color, onSelect, onAddItemClicked);
        }

        private static void DrawDropDown(int id, Rect position, Rect popupPos, string[] options, int selectedIndex, string defaultText, Color color, Action<int> onSelect, Action onAddItemClicked = null)
        {
            int controlID = GUIUtility.GetControlID(id, FocusType.Keyboard, position);
            int result = selectedIndex;
            var previousColor = GUI.contentColor;
            GUI.contentColor = color;
            if (DropdownButton(controlID, position, new GUIContent(selectedIndex == -1 ? defaultText : options[selectedIndex])))
            {
                SearchablePopup.Show(popupPos, options, selectedIndex, onSelect, onAddItemClicked);
            }
            GUI.contentColor = previousColor;
        }

        public static void DropDown(int id, Rect position, string[] options, int selectedIndex, Action<int> onSelect, Action onAddItemClicked = null)
        {
            DropDown(id, position, options, selectedIndex, "None", Color.white, onSelect, onAddItemClicked);
        }

        public static void DropDown(int id, Rect position, Rect popupPos, string[] options, int selectedIndex, Action<int> onSelect, Action onAddItemClicked = null)
        {
            DropDown(id, position, popupPos, options, selectedIndex, "None", Color.white, onSelect, onAddItemClicked);
        }

        private static bool DropdownButton(int id, Rect position, GUIContent content)
        {
            Event current = Event.current;
            switch (current.type)
            {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && current.button == 0)
                    {
                        Event.current.Use();
                        return true;
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id && current.character == '\n')
                    {
                        Event.current.Use();
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
