using System;
using UnityEngine;

[Serializable]
public class RepeatNodeConfig : NodeConfig
{
    public int count = 1;

    [SerializeReference]
    public NodeConfig child;
}