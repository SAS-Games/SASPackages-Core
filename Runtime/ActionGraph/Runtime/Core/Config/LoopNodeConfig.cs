using System;
using UnityEngine;

public enum LoopConditionTiming
{
    BeforeChild,
    AfterChild
}

[Serializable]
public class LoopNodeConfig : NodeConfig
{
    public int maxIterations = 1;
    public LoopConditionTiming conditionTiming = LoopConditionTiming.AfterChild;

    [SerializeReference] public ICondition condition;

    [SerializeReference]
    public NodeConfig child;
}
