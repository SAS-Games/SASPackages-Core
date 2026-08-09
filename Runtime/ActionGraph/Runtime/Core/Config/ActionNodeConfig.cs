using System;
using UnityEngine;

[Serializable]
public class ActionNodeConfig : NodeConfig
{
    [SerializeReference]
    public ActionDataProvider dataProvider;
}