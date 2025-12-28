using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SAS.Core.TagSystem.Editor
{
    [CustomEditor(typeof(Tagger), true)]
    public class TaggerEditor : UnityEditor.Editor
    {
        private ReorderableList _componentTagList;

        private void OnEnable()
        {
            _componentTagList = new ReorderableList(serializedObject, serializedObject.FindProperty("m_Tags"), true, true, false, true);

            _componentTagList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Tagged Component List");
            };

            _componentTagList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var componentTag = serializedObject.FindProperty("m_Tags");
                if (componentTag.arraySize > 0)
                {
                    var component = componentTag.GetArrayElementAtIndex(index).FindPropertyRelative("m_Component");
                    var tag = componentTag.GetArrayElementAtIndex(index).FindPropertyRelative("m_Value");
                 
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.ObjectField(new Rect(rect.x + 5, rect.y, rect.width / 2, rect.height), component.objectReferenceValue, typeof(Component), false);

                    EditorGUI.EndDisabledGroup();

                    Rect pos = new Rect(rect.width / 2 + 60, rect.y, rect.width / 2 - 20, rect.height);
                    int id = GUIUtility.GetControlID("SearchableStringDrawer".GetHashCode(), FocusType.Keyboard, pos);
                    
                    Rect tagRect = new Rect(
                        rect.x + rect.width / 2 + 10,
                        rect.y,
                        rect.width / 2 - 15,
                        rect.height);
                    
                    bool changed = TagEditorUtility.DrawTagPopup(
                        tagRect,
                        tag,
                        GUIContent.none);

                    if (changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        UnityEditor.EditorUtility.SetDirty(target);
                    }
                }
            };
        }

        public override void OnInspectorGUI()
        {
            _componentTagList.DoLayoutList();
        }
    }
}
