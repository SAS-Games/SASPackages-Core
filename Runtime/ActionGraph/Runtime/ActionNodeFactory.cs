using System;

public static class ActionNodeFactory
{
    public static IActionNode Create(ActionDataProvider dataProvider)
    {
        var dsType = dataProvider.GetType();

        var attr = (NodeBindingAttribute)Attribute.GetCustomAttribute(dsType, typeof(NodeBindingAttribute));

        if (attr == null)
            throw new Exception($"No NodeBinding found for {dsType}");

        return (IActionNode)Activator.CreateInstance(attr.NodeType, dataProvider);
    }
}