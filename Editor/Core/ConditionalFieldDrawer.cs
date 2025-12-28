using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ConditionalFieldAttribute))]
public class ConditionalFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShouldShow(property))
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return ShouldShow(property) ? EditorGUI.GetPropertyHeight(property, label, true) : 0f;
    }

    private bool ShouldShow(SerializedProperty property)
    {
        ConditionalFieldAttribute showIf = (ConditionalFieldAttribute)attribute;
        SerializedProperty conditionProperty = property.serializedObject.FindProperty(showIf.ConditionFieldName);

        if (conditionProperty == null)
        {
            Debug.LogWarning($"ShowIf: Property '{showIf.ConditionFieldName}' not found.");
            return true;
        }

        switch (conditionProperty.propertyType)
        {
            case SerializedPropertyType.Enum:
                return conditionProperty.enumValueIndex == (int)showIf.CompareValue;
            case SerializedPropertyType.Boolean:
                return conditionProperty.boolValue.Equals(showIf.CompareValue);
            default:
                Debug.LogWarning($"ShowIf: Unsupported property type '{conditionProperty.propertyType}'.");
                return true;
        }
    }
}
