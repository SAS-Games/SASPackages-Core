using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RandomNodeConfig : NodeConfig
{
    [SerializeReference]
    public List<NodeConfig> children = new();
}