using System;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ActionNodeMenuAttribute : Attribute
{
    public string Path { get; }

    public ActionNodeMenuAttribute(string path)
    {
        Path = path;
    }
}
