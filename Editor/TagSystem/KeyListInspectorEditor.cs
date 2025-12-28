using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SAS.Core.TagSystem.Editor
{
    [CustomEditor(typeof(KeyList), true)]
    public class KeyListInspectorEditor : UnityEditor.Editor
    {
		private ReorderableList _keyList;
		private SerializedProperty _values; 

		private void OnEnable()
		{
			_values = serializedObject.FindProperty("values");
			CreateReorderableList();
		}

		private void CreateReorderableList()
		{
			_keyList = new ReorderableList(serializedObject, _values, true, true, true, true);
			_keyList.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, target.name);
			};

			_keyList.onAddCallback = (list) =>
			{
				_values.InsertArrayElementAtIndex(_values.arraySize);
				var currValue = _values.GetArrayElementAtIndex(_values.arraySize - 1);
				var newValue = GetUniqueName("New Value", (target as KeyList).values);
				currValue.stringValue = newValue;
				serializedObject.ApplyModifiedProperties();
			};

			_keyList.onRemoveCallback = (list) =>
			{
				if (_values.GetArrayElementAtIndex(list.index) != null)
					_values.DeleteArrayElementAtIndex(list.index);
				serializedObject.ApplyModifiedProperties();
			};

			_keyList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				if (_values.arraySize > 0)
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorGUI.TextField(new Rect(rect.x + 5, rect.y, rect.width / 2, rect.height), "Value " + index);
					EditorGUI.EndDisabledGroup();
					var currValue = _values.GetArrayElementAtIndex(index);
					var newValue = EditorGUI.DelayedTextField(new Rect(rect.width / 2 + 40, rect.y, rect.width / 2 - 20, rect.height), currValue.stringValue);
					if (newValue != currValue.stringValue)
					{
						newValue = GetUniqueName(newValue, (target as KeyList).values);
						currValue.stringValue = newValue;
						serializedObject.ApplyModifiedProperties();
					}
				}
			};
		}

		public override void OnInspectorGUI()
        {
			_keyList.DoLayoutList();
		}

		private string GetUniqueName(string nameBase, string[] usedNames)
		{
			string name = nameBase;
			int counter = 1;
			while (usedNames.Contains(name.Trim()))
			{
				name = nameBase + " " + counter;
				counter++;
			}
			return name;
		}
	}
}
