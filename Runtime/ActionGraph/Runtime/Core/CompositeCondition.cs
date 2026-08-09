using System;
using UnityEngine;

public enum ActionGraphConditionMode
{
    Any,
    All,
    None
}

[Serializable]
public class CompositeCondition : ICondition
{
    public ActionGraphConditionMode mode = ActionGraphConditionMode.Any;
    [SerializeReference] public ICondition[] conditions;

    public bool Evaluate(ActionContext context)
    {
        if (conditions == null || conditions.Length == 0)
            return false;

        switch (mode)
        {
            case ActionGraphConditionMode.Any:
                foreach (var condition in conditions)
                {
                    if (condition != null && condition.Evaluate(context))
                        return true;
                }
                return false;
            case ActionGraphConditionMode.All:
                foreach (var condition in conditions)
                {
                    if (condition == null || !condition.Evaluate(context))
                        return false;
                }
                return true;
            case ActionGraphConditionMode.None:
                foreach (var condition in conditions)
                {
                    if (condition != null && condition.Evaluate(context))
                        return false;
                }
                return true;
            default:
                return false;
        }
    }
}
