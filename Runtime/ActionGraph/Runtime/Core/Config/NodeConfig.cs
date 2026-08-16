using System;
using UnityEngine;

[Serializable]
public abstract class NodeConfig
{
    [TextArea]
    public string editorDescription;

    public Vector2 editorPosition;
    public Vector2 editorSize;
    public bool editorCollapsed;
    public bool editorChildrenListCollapsed;
}
