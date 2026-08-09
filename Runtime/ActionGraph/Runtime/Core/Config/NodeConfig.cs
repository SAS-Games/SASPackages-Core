using System;
using UnityEngine;

[Serializable]
public abstract class NodeConfig
{
    public Vector2 editorPosition;
    public Vector2 editorSize;
    public bool editorCollapsed;
    public bool editorChildrenListCollapsed;
}
