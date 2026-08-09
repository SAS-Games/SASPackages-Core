using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public enum ActionGraphBlackboardValueType
{
    Bool,
    Int,
    Float,
    String,
    UnityObject
}

public enum ActionGraphBlackboardNumberType
{
    Int,
    Float
}

public enum ActionGraphBlackboardNumberOperation
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Set
}

[Serializable]
public class ActionGraphBlackboardSetValueData
{
    public string key;
    public ActionGraphBlackboardValueType valueType;
    public bool boolValue;
    public int intValue;
    public float floatValue;
    public string stringValue;
    public UnityEngine.Object objectValue;

    public object GetValue()
    {
        switch (valueType)
        {
            case ActionGraphBlackboardValueType.Bool:
                return boolValue;
            case ActionGraphBlackboardValueType.Int:
                return intValue;
            case ActionGraphBlackboardValueType.Float:
                return floatValue;
            case ActionGraphBlackboardValueType.String:
                return stringValue;
            case ActionGraphBlackboardValueType.UnityObject:
                return objectValue;
            default:
                return null;
        }
    }
}

[NodeBinding(typeof(ActionGraphSetBlackboardValueNode))]
[Serializable]
public class ActionGraphSetBlackboardValueProvider : ActionDataProvider<ActionGraphBlackboardSetValueData>
{
}

public class ActionGraphSetBlackboardValueNode : ActionNode<ActionGraphBlackboardSetValueData>
{
    public ActionGraphSetBlackboardValueNode(ActionDataProvider<ActionGraphBlackboardSetValueData> dataProvider) : base(dataProvider)
    {
    }

    public override Task ExecuteAsync(ActionContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var data = _selector.GetNext();
        if (data == null || string.IsNullOrEmpty(data.key))
            return Task.CompletedTask;

        var blackboard = ActionGraphBlackboardUtility.RequireBlackboard(context);
        blackboard.SetValue(data.key, data.GetValue());

        return Task.CompletedTask;
    }
}

[Serializable]
public class ActionGraphBlackboardRemoveValueData
{
    public string key;
}

[NodeBinding(typeof(ActionGraphRemoveBlackboardValueNode))]
[Serializable]
public class ActionGraphRemoveBlackboardValueProvider : ActionDataProvider<ActionGraphBlackboardRemoveValueData>
{
}

public class ActionGraphRemoveBlackboardValueNode : ActionNode<ActionGraphBlackboardRemoveValueData>
{
    public ActionGraphRemoveBlackboardValueNode(ActionDataProvider<ActionGraphBlackboardRemoveValueData> dataProvider) : base(dataProvider)
    {
    }

    public override Task ExecuteAsync(ActionContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var data = _selector.GetNext();
        if (data == null || string.IsNullOrEmpty(data.key))
            return Task.CompletedTask;

        var blackboard = ActionGraphBlackboardUtility.RequireBlackboard(context);
        blackboard.Remove(data.key);

        return Task.CompletedTask;
    }
}

[Serializable]
public class ActionGraphBlackboardNumberData
{
    public string key;
    public ActionGraphBlackboardNumberType numberType;
    public ActionGraphBlackboardNumberOperation operation;
    public float value = 1f;
    public bool createIfMissing = true;
}

[NodeBinding(typeof(ActionGraphModifyBlackboardNumberNode))]
[Serializable]
public class ActionGraphModifyBlackboardNumberProvider : ActionDataProvider<ActionGraphBlackboardNumberData>
{
}

public class ActionGraphModifyBlackboardNumberNode : ActionNode<ActionGraphBlackboardNumberData>
{
    public ActionGraphModifyBlackboardNumberNode(ActionDataProvider<ActionGraphBlackboardNumberData> dataProvider) : base(dataProvider)
    {
    }

    public override Task ExecuteAsync(ActionContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var data = _selector.GetNext();
        if (data == null || string.IsNullOrEmpty(data.key))
            return Task.CompletedTask;

        var blackboard = ActionGraphBlackboardUtility.RequireBlackboard(context);
        bool hasCurrent = ActionGraphBlackboardUtility.TryGetNumber(context, data.key, out float currentValue);

        if (!hasCurrent && !data.createIfMissing)
            return Task.CompletedTask;

        float nextValue = ApplyOperation(hasCurrent ? currentValue : 0f, data.operation, data.value);

        if (data.numberType == ActionGraphBlackboardNumberType.Int)
            blackboard.SetValue(data.key, Mathf.RoundToInt(nextValue));
        else
            blackboard.SetValue(data.key, nextValue);

        return Task.CompletedTask;
    }

    private static float ApplyOperation(float currentValue, ActionGraphBlackboardNumberOperation operation, float value)
    {
        switch (operation)
        {
            case ActionGraphBlackboardNumberOperation.Add:
                return currentValue + value;
            case ActionGraphBlackboardNumberOperation.Subtract:
                return currentValue - value;
            case ActionGraphBlackboardNumberOperation.Multiply:
                return currentValue * value;
            case ActionGraphBlackboardNumberOperation.Divide:
                return Mathf.Approximately(value, 0f) ? currentValue : currentValue / value;
            case ActionGraphBlackboardNumberOperation.Set:
                return value;
            default:
                return currentValue;
        }
    }
}
