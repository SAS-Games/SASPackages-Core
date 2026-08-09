using System;
using SAS.Core.BlackboardSystem;

public static class ActionGraphBlackboardUtility
{
    public static Blackboard RequireBlackboard(ActionContext context)
    {
        var blackboard = context != null ? context.ResolveBlackboard() : null;
        if (blackboard == null)
            throw new InvalidOperationException("ActionGraph requires a Blackboard, but none was found on the context or owner.");

        return blackboard;
    }

    public static bool TryGet<T>(ActionContext context, string key, out T value)
    {
        value = default(T);

        if (string.IsNullOrEmpty(key))
            return false;

        var blackboard = context != null ? context.ResolveBlackboard() : null;
        if (blackboard == null || !blackboard.TryGetValue(key, out object rawValue) || rawValue == null)
            return false;

        if (rawValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        try
        {
            value = (T)Convert.ChangeType(rawValue, typeof(T));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryGetNumber(ActionContext context, string key, out float value)
    {
        value = 0f;

        if (!TryGet<object>(context, key, out object rawValue) || rawValue == null)
            return false;

        switch (rawValue)
        {
            case int intValue:
                value = intValue;
                return true;
            case float floatValue:
                value = floatValue;
                return true;
            case double doubleValue:
                value = (float)doubleValue;
                return true;
            case long longValue:
                value = longValue;
                return true;
            default:
                try
                {
                    value = Convert.ToSingle(rawValue);
                    return true;
                }
                catch
                {
                    return false;
                }
        }
    }
}
