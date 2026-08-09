using System;
using System.Collections.Generic;
using UnityEditor;

static class NodeEditorCache
{
    public static Dictionary<Type, Type> GetNodeToDataSetMap()
    {
        var nodeToDataSet = new Dictionary<Type, Type>();

        var dataSetTypes = TypeCache.GetTypesDerivedFrom<ActionDataProvider>();

        foreach (var dsType in dataSetTypes)
        {
            if (dsType.IsAbstract) continue;

            var attr = (NodeBindingAttribute)Attribute.GetCustomAttribute(
                dsType, typeof(NodeBindingAttribute));

            if (attr == null) continue;

            nodeToDataSet[attr.NodeType] = dsType;
        }

        return nodeToDataSet;
    }
}

static class ActionNodeEditorNames
{
    public static string GetDisplayName(Type nodeType)
    {
        string menuPath = GetMenuPath(nodeType);
        int separatorIndex = menuPath.LastIndexOf('/');
        return separatorIndex >= 0 ? menuPath.Substring(separatorIndex + 1) : menuPath;
    }

    public static string GetMenuPath(Type nodeType)
    {
        var attribute = (ActionNodeMenuAttribute)Attribute.GetCustomAttribute(nodeType, typeof(ActionNodeMenuAttribute));
        if (attribute != null && !string.IsNullOrEmpty(attribute.Path))
            return attribute.Path;

        string name = nodeType.Name;
        if (name.EndsWith("Node", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Node".Length);

        return ObjectNames.NicifyVariableName(name);
    }
}
