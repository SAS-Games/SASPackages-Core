using System;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class TagDefinitionAttribute : Attribute
{
    public string Name { get; }
    public int Id { get; }

    public TagDefinitionAttribute(string name, int id)
    {
        Name = name;
        Id = id;
    }
}