using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using EditorUtility = SAS.Core.Editor.EditorUtility;

namespace SAS.Core.TagSystem.Editor
{
    [CustomEditor(typeof(Binder))]
    public class BinderInspector : UnityEditor.Editor
    {
        private ReorderableList _bindings;
        private Type[] _allInterface;
        private Type[] _allBindableType;

        private void OnEnable()
        {
            _allInterface = AppDomain.CurrentDomain.GetAllInterface<IBindable>().ToArray();
            _allBindableType = AppDomain.CurrentDomain.GetAllDerivedTypes<IBindable>().ToArray();
            _bindings = new ReorderableList(serializedObject, serializedObject.FindProperty("m_Bindings"), true, true,
                true, true);
            DrawReorderableBindingsList(_bindings);
        }

        public override void OnInspectorGUI()
        {
            _bindings.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawReorderableBindingsList(ReorderableList bindings)
        {
            bindings.drawHeaderCallback = (Rect rect) =>
            {
                var style = new GUIStyle(GUI.skin.label)
                    { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                var pos = new Rect(rect.x + 30, rect.y - 2, rect.width / 3, rect.height - 2);
                EditorGUI.LabelField(pos, "Injectable", style);

                pos = new Rect(rect.x + 30 + rect.width / 3, rect.y - 2, rect.width / 3,
                    rect.height -
                    2); //new Rect(rect.width - Mathf.Min(100, rect.width / 3 - 20) - 20, rect.y, width, rect.height);
                EditorGUI.LabelField(pos, "Bind With", style);

                pos = new Rect(rect.x + 30 + 2 * rect.width / 3, rect.y - 2, rect.width / 3 - 30, rect.height - 2);
                EditorGUI.LabelField(pos, "Tag", style);
            };

            bindings.onAddCallback = list =>
            {
                bindings.serializedProperty.InsertArrayElementAtIndex(bindings.serializedProperty.arraySize);
                var injectable = bindings.serializedProperty
                    .GetArrayElementAtIndex(bindings.serializedProperty.arraySize - 1)
                    .FindPropertyRelative("m_Interface");
                if (bindings.serializedProperty.arraySize == 1)
                    injectable.stringValue = _allInterface[0].AssemblyQualifiedName;
            };

            bindings.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var injectableInterface = bindings.serializedProperty.GetArrayElementAtIndex(index)
                    .FindPropertyRelative("m_Interface");
                var typeToBind = bindings.serializedProperty.GetArrayElementAtIndex(index)
                    .FindPropertyRelative("m_Type");
                var tag = bindings.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("m_Tag");

                if (GUI.Button(new Rect(rect.x, rect.y, 30, rect.height - 5), "C#"))
                {
                    var assetsPath = AssetDatabase.GetAllAssetPaths();
                    foreach (var path in assetsPath)
                    {
                        var script = (MonoScript)AssetDatabase.LoadAssetAtPath(path, typeof(MonoScript));
                        if (script != null)
                        {
                            if (script.GetClass()?.AssemblyQualifiedName == typeToBind.stringValue)
                            {
                                AssetDatabase.OpenAsset(script);
                                break;
                            }
                        }
                    }
                }

                rect.y += 2;
                var curActionIndex = Array.FindIndex(_allInterface,
                    ele => ele.AssemblyQualifiedName == injectableInterface.stringValue);
                var pos = new Rect(rect.x + 30, rect.y - 2, rect.width / 3, rect.height - 2);
                int id = GUIUtility.GetControlID("injectableInterface".GetHashCode(), FocusType.Keyboard, pos);
                if (curActionIndex != -1 || string.IsNullOrEmpty(injectableInterface.stringValue))
                    Core.Editor.EditorUtility.DropDown(id, pos, _allInterface.Select(ele => Sanitize(ele.ToString())).ToArray(),
                        curActionIndex, selectedIndex => SetSelectedInterface(injectableInterface, selectedIndex));
                else
                    Core.Editor.EditorUtility.DropDown(id, pos, _allInterface.Select(ele => Sanitize(ele.ToString())).ToArray(),
                        curActionIndex, injectableInterface.stringValue, Color.red,
                        selectedIndex => SetSelectedInterface(injectableInterface, selectedIndex));

                var validTypes = GetAllSuitableTypes(injectableInterface.stringValue);
                curActionIndex =
                    Array.FindIndex(validTypes, ele => ele.AssemblyQualifiedName == typeToBind.stringValue);
                pos = new Rect(rect.x + 30 + rect.width / 3, rect.y - 2, rect.width / 3, rect.height - 2);
                id = GUIUtility.GetControlID("bindable".GetHashCode(), FocusType.Keyboard, pos);
                if (curActionIndex != -1)
                    Core.Editor.EditorUtility.DropDown(id, pos, validTypes.Select(ele => Sanitize(ele.ToString())).ToArray(),
                        curActionIndex, selectedIndex => SetSelectedType(typeToBind, validTypes[selectedIndex]));
                else
                    Core.Editor.EditorUtility.DropDown(id, pos, validTypes.Select(ele => Sanitize(ele.ToString())).ToArray(),
                        curActionIndex, string.IsNullOrEmpty(typeToBind.stringValue) ? "None" : typeToBind.stringValue,
                        Color.red, selectedIndex => SetSelectedType(typeToBind, validTypes[selectedIndex]));

                pos = new Rect(rect.x + 30 + 2 * rect.width / 3, rect.y - 2, rect.width / 3 - 30, rect.height - 2);
                id = GUIUtility.GetControlID("Tag".GetHashCode(), FocusType.Keyboard, pos);
                bool changed = TagEditorUtility.DrawTagPopup(pos, tag, GUIContent.none);

                if (changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    UnityEditor.EditorUtility.SetDirty(target);
                }
            };
        }

        private Type[] GetAllSuitableTypes(string injectableInterface)
        {
            Type interfaceType = Type.GetType(injectableInterface);
            if (interfaceType == null)
                return new Type[] { };
            return Array.FindAll(_allBindableType,
                type => type.IsSubclassOf(interfaceType) || interfaceType.IsAssignableFrom(type));
        }

        private string Sanitize(string typeAsString)
        {
            if (typeAsString.Contains(","))
                typeAsString = typeAsString.Split(',')[0];
            return typeAsString;
        }

        private void SetSelectedInterface(SerializedProperty sp, int index)
        {
            if (index != -1)
                sp.stringValue = _allInterface[index].AssemblyQualifiedName;
            serializedObject.ApplyModifiedProperties();
        }

        private void SetSelectedType(SerializedProperty sp, Type selectedType)
        {
            var index = Array.IndexOf(_allBindableType, selectedType);
            if (index != -1)
                sp.stringValue = _allBindableType[index].AssemblyQualifiedName;
            serializedObject.ApplyModifiedProperties();
        }
    }
}