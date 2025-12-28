using UnityEngine;

public class ConditionalFieldAttribute : PropertyAttribute
{
    public string ConditionFieldName { get; }
    public object CompareValue { get; }

    public ConditionalFieldAttribute(string conditionFieldName, object compareValue)
    {
        ConditionFieldName = conditionFieldName;
        CompareValue = compareValue;
    }
}
