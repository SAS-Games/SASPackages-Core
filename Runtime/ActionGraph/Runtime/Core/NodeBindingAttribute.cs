using System;

[AttributeUsage(AttributeTargets.Class)]
public class NodeBindingAttribute : Attribute
{
    public Type NodeType { get; }

    public NodeBindingAttribute(Type nodeType)
    {
        NodeType = nodeType;
    }
}