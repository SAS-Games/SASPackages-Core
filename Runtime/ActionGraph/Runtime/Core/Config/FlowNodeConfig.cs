using System;
using System.Collections.Generic;
using UnityEngine;

public enum FlowNodeType
{
    Sequence,
    Parallel
}

[Serializable]
public class FlowNodeConfig : NodeConfig
{
    public FlowNodeType type;

    [SerializeReference]
    public List<NodeConfig> children = new();
}