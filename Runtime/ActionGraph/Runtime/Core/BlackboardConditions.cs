using System;

public enum ActionGraphNumberComparison
{
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual
}

public enum ActionGraphStringComparison
{
    Equal,
    NotEqual,
    Contains,
    IsEmpty,
    IsNotEmpty
}

[Serializable]
public class ActionGraphBlackboardExistsCondition : ICondition
{
    public string key;
    public bool expected = true;

    public bool Evaluate(ActionContext context)
    {
        var blackboard = context != null ? context.ResolveBlackboard() : null;
        bool exists = blackboard != null && !string.IsNullOrEmpty(key) && blackboard.Contains(key);
        return exists == expected;
    }
}

[Serializable]
public class ActionGraphBlackboardBoolCondition : ICondition
{
    public string key;
    public bool expected = true;
    public bool resultWhenMissing;

    public bool Evaluate(ActionContext context)
    {
        if (!ActionGraphBlackboardUtility.TryGet(context, key, out bool value))
            return resultWhenMissing;

        return value == expected;
    }
}

[Serializable]
public class ActionGraphBlackboardIntCondition : ICondition
{
    public string key;
    public ActionGraphNumberComparison comparison;
    public int compareValue;
    public bool resultWhenMissing;

    public bool Evaluate(ActionContext context)
    {
        if (!ActionGraphBlackboardUtility.TryGet(context, key, out int value))
            return resultWhenMissing;

        return Compare(value, compareValue, comparison);
    }

    private static bool Compare(int value, int compareValue, ActionGraphNumberComparison comparison)
    {
        switch (comparison)
        {
            case ActionGraphNumberComparison.Equal:
                return value == compareValue;
            case ActionGraphNumberComparison.NotEqual:
                return value != compareValue;
            case ActionGraphNumberComparison.Greater:
                return value > compareValue;
            case ActionGraphNumberComparison.GreaterOrEqual:
                return value >= compareValue;
            case ActionGraphNumberComparison.Less:
                return value < compareValue;
            case ActionGraphNumberComparison.LessOrEqual:
                return value <= compareValue;
            default:
                return false;
        }
    }
}

[Serializable]
public class ActionGraphBlackboardFloatCondition : ICondition
{
    public string key;
    public ActionGraphNumberComparison comparison;
    public float compareValue;
    public float epsilon = 0.0001f;
    public bool resultWhenMissing;

    public bool Evaluate(ActionContext context)
    {
        if (!ActionGraphBlackboardUtility.TryGet(context, key, out float value))
            return resultWhenMissing;

        return Compare(value, compareValue, comparison, epsilon);
    }

    private static bool Compare(float value, float compareValue, ActionGraphNumberComparison comparison, float epsilon)
    {
        switch (comparison)
        {
            case ActionGraphNumberComparison.Equal:
                return Math.Abs(value - compareValue) <= epsilon;
            case ActionGraphNumberComparison.NotEqual:
                return Math.Abs(value - compareValue) > epsilon;
            case ActionGraphNumberComparison.Greater:
                return value > compareValue;
            case ActionGraphNumberComparison.GreaterOrEqual:
                return value >= compareValue;
            case ActionGraphNumberComparison.Less:
                return value < compareValue;
            case ActionGraphNumberComparison.LessOrEqual:
                return value <= compareValue;
            default:
                return false;
        }
    }
}

[Serializable]
public class ActionGraphBlackboardStringCondition : ICondition
{
    public string key;
    public ActionGraphStringComparison comparison;
    public string compareValue;
    public bool ignoreCase = true;
    public bool resultWhenMissing;

    public bool Evaluate(ActionContext context)
    {
        if (!ActionGraphBlackboardUtility.TryGet(context, key, out string value))
            return resultWhenMissing;

        string actual = value ?? string.Empty;
        string expected = compareValue ?? string.Empty;
        StringComparison stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        switch (comparison)
        {
            case ActionGraphStringComparison.Equal:
                return string.Equals(actual, expected, stringComparison);
            case ActionGraphStringComparison.NotEqual:
                return !string.Equals(actual, expected, stringComparison);
            case ActionGraphStringComparison.Contains:
                return actual.IndexOf(expected, stringComparison) >= 0;
            case ActionGraphStringComparison.IsEmpty:
                return string.IsNullOrEmpty(actual);
            case ActionGraphStringComparison.IsNotEmpty:
                return !string.IsNullOrEmpty(actual);
            default:
                return false;
        }
    }
}
