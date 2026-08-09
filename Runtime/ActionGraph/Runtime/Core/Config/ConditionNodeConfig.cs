using System;
using UnityEngine;

[Serializable]
public class ConditionNodeConfig : NodeConfig
{
    [SerializeReference] public ICondition condition;

    [SerializeReference] public NodeConfig trueNode;
    [SerializeReference] public NodeConfig falseNode;
}