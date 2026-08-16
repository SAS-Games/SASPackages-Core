using System;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ActionNodeMenuAttribute : Attribute
{
    public string Path { get; }
    public string Description { get; }

    public ActionNodeMenuAttribute(string path, string description = null)
    {
        Path = path;
        Description = description;
    }
}
