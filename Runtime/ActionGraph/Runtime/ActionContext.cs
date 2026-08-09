using SAS.Core.BlackboardSystem;
using UnityEngine;

public class ActionContext
{
    public GameObject Owner;
    public Blackboard Blackboard;

    public Blackboard ResolveBlackboard()
    {
        if (Blackboard != null)
            return Blackboard;

        ActionGraphBlackboardComponent component = Owner != null
            ? Owner.GetComponentInParent<ActionGraphBlackboardComponent>()
            : null;
        return component != null ? component.Blackboard : null;
    }
}
