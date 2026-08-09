using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ActionGraphView
{
    private void ConfigureOutputPorts(ActionGraphNodeView view, NodeConfig config)
    {
        switch (config)
        {
            case FlowNodeConfig:
                view.AddOutputPort("Children", OutputSlot.Children, Port.Capacity.Multi, true, TrackPendingConnection, IsOverCompatibleInputPort, position => ShowCreateNodeSearch(config, OutputSlot.Children, position));
                break;
            case RandomNodeConfig:
                view.AddOutputPort("Choices", OutputSlot.Children, Port.Capacity.Multi, true, TrackPendingConnection, IsOverCompatibleInputPort, position => ShowCreateNodeSearch(config, OutputSlot.Children, position));
                break;
            case RepeatNodeConfig repeat:
                view.AddOutputPort("Child", OutputSlot.Child, Port.Capacity.Single, repeat.child == null, TrackPendingConnection, IsOverCompatibleInputPort, position => ShowCreateNodeSearch(config, OutputSlot.Child, position));
                break;
            case LoopNodeConfig loop:
                view.AddOutputPort("Child", OutputSlot.Child, Port.Capacity.Single, loop.child == null, TrackPendingConnection, IsOverCompatibleInputPort, position => ShowCreateNodeSearch(config, OutputSlot.Child, position));
                break;
            case ConditionNodeConfig condition:
                view.AddOutputPort("True", OutputSlot.True, Port.Capacity.Single, condition.trueNode == null, TrackPendingConnection, IsOverCompatibleInputPort, position => ShowCreateNodeSearch(config, OutputSlot.True, position));
                view.AddOutputPort("False", OutputSlot.False, Port.Capacity.Single, condition.falseNode == null, TrackPendingConnection, IsOverCompatibleInputPort, position => ShowCreateNodeSearch(config, OutputSlot.False, position));
                break;
        }

        view.RefreshPorts();
    }

    private void AddEdges(NodeConfig parent)
    {
        if (!_nodeViews.TryGetValue(parent, out var parentView))
            return;

        foreach (var child in GetVisibleChildren(parent))
        {
            if (_nodeViews.TryGetValue(child.node, out var childView) &&
                parentView.OutputPorts.TryGetValue(child.slot, out var output) &&
                childView.InputPort != null)
            {
                AddElement(output.ConnectTo(childView.InputPort));
            }

            AddEdges(child.node);
        }
    }

    private IEnumerable<(NodeConfig node, OutputSlot slot)> GetVisibleChildren(NodeConfig config)
    {
        if (config != null && config.editorCollapsed)
            yield break;

        foreach (var child in GetChildren(config))
            yield return child;
    }

    private static bool SupportsBranchCollapse(NodeConfig config)
    {
        return config is FlowNodeConfig ||
               config is RandomNodeConfig ||
               config is RepeatNodeConfig ||
               config is LoopNodeConfig ||
               config is ConditionNodeConfig;
    }

    private IEnumerable<(NodeConfig node, OutputSlot slot)> GetChildren(NodeConfig config)
    {
        switch (config)
        {
            case FlowNodeConfig flow:
                if (flow.children == null)
                    yield break;

                foreach (var child in flow.children.Where(child => child != null))
                    yield return (child, OutputSlot.Children);
                break;

            case RandomNodeConfig random:
                if (random.children == null)
                    yield break;

                foreach (var child in random.children.Where(child => child != null))
                    yield return (child, OutputSlot.Children);
                break;

            case RepeatNodeConfig repeat:
                if (repeat.child != null)
                    yield return (repeat.child, OutputSlot.Child);
                break;

            case LoopNodeConfig loop:
                if (loop.child != null)
                    yield return (loop.child, OutputSlot.Child);
                break;

            case ConditionNodeConfig cond:
                if (cond.trueNode != null)
                    yield return (cond.trueNode, OutputSlot.True);
                if (cond.falseNode != null)
                    yield return (cond.falseNode, OutputSlot.False);
                break;
        }
    }

    private void Connect(NodeConfig parent, NodeConfig child, OutputSlot slot)
    {
        if (parent == null || child == null || parent == child || ContainsNode(child, parent))
            return;

        RemoveFromParent(_config.root, child);

        switch (parent)
        {
            case FlowNodeConfig flow when slot == OutputSlot.Children:
                flow.children ??= new List<NodeConfig>();
                if (!flow.children.Contains(child))
                    flow.children.Add(child);
                break;

            case RandomNodeConfig random when slot == OutputSlot.Children:
                random.children ??= new List<NodeConfig>();
                if (!random.children.Contains(child))
                    random.children.Add(child);
                break;

            case RepeatNodeConfig repeat when slot == OutputSlot.Child:
                repeat.child = child;
                break;

            case LoopNodeConfig loop when slot == OutputSlot.Child:
                loop.child = child;
                break;

            case ConditionNodeConfig condition when slot == OutputSlot.True:
                condition.trueNode = child;
                break;

            case ConditionNodeConfig condition when slot == OutputSlot.False:
                condition.falseNode = child;
                break;
        }
    }

    private void Disconnect(NodeConfig parent, NodeConfig child, OutputSlot slot)
    {
        switch (parent)
        {
            case FlowNodeConfig flow when slot == OutputSlot.Children:
                flow.children?.Remove(child);
                break;

            case RandomNodeConfig random when slot == OutputSlot.Children:
                random.children?.Remove(child);
                break;

            case RepeatNodeConfig repeat when slot == OutputSlot.Child && repeat.child == child:
                repeat.child = null;
                break;

            case LoopNodeConfig loop when slot == OutputSlot.Child && loop.child == child:
                loop.child = null;
                break;

            case ConditionNodeConfig condition when slot == OutputSlot.True && condition.trueNode == child:
                condition.trueNode = null;
                break;

            case ConditionNodeConfig condition when slot == OutputSlot.False && condition.falseNode == child:
                condition.falseNode = null;
                break;
        }
    }

    private void RemoveNode(NodeConfig target)
    {
        if (_config?.root == target)
        {
            _config.root = null;
            return;
        }

        RemoveFromParent(_config.root, target);
    }

    private void DeleteNode(NodeConfig target)
    {
        if (_config == null || target == null)
            return;

        Undo.RecordObject(_config, "Delete Action Graph Node");
        RemoveNode(target);
        MarkDirty();
        QueueRebuild();
    }

    private bool RemoveFromParent(NodeConfig current, NodeConfig target)
    {
        if (current == null || target == null)
            return false;

        switch (current)
        {
            case FlowNodeConfig flow:
                if (flow.children == null)
                    return false;

                if (flow.children.Remove(target))
                    return true;
                foreach (var child in flow.children.ToList())
                {
                    if (RemoveFromParent(child, target))
                        return true;
                }
                break;

            case RandomNodeConfig random:
                if (random.children == null)
                    return false;

                if (random.children.Remove(target))
                    return true;
                foreach (var child in random.children.ToList())
                {
                    if (RemoveFromParent(child, target))
                        return true;
                }
                break;

            case RepeatNodeConfig repeat:
                if (repeat.child == target)
                {
                    repeat.child = null;
                    return true;
                }
                return RemoveFromParent(repeat.child, target);

            case LoopNodeConfig loop:
                if (loop.child == target)
                {
                    loop.child = null;
                    return true;
                }
                return RemoveFromParent(loop.child, target);

            case ConditionNodeConfig condition:
                if (condition.trueNode == target)
                {
                    condition.trueNode = null;
                    return true;
                }

                if (condition.falseNode == target)
                {
                    condition.falseNode = null;
                    return true;
                }

                return RemoveFromParent(condition.trueNode, target) ||
                       RemoveFromParent(condition.falseNode, target);
        }

        return false;
    }

    private static bool ContainsNode(NodeConfig root, NodeConfig target)
    {
        if (root == null || target == null)
            return false;

        if (root == target)
            return true;

        return root switch
        {
            FlowNodeConfig flow => flow.children != null && flow.children.Any(child => ContainsNode(child, target)),
            RandomNodeConfig random => random.children != null && random.children.Any(child => ContainsNode(child, target)),
            RepeatNodeConfig repeat => ContainsNode(repeat.child, target),
            LoopNodeConfig loop => ContainsNode(loop.child, target),
            ConditionNodeConfig condition => ContainsNode(condition.trueNode, target) || ContainsNode(condition.falseNode, target),
            _ => false
        };
    }

    private void EnsureMissingPositions()
    {
        int row = 0;
        AssignMissingLayout(_config.root, 0, ref row);
    }

    private void AssignMissingLayout(NodeConfig config, int depth, ref int row)
    {
        if (config == null)
            return;

        if (config.editorPosition == Vector2.zero)
            config.editorPosition = new Vector2(80f + depth * ColumnWidth, 120f + row * RowHeight);

        row++;

        foreach (var child in GetChildren(config))
            AssignMissingLayout(child.node, depth + 1, ref row);
    }

    private void AssignLayout(NodeConfig config, int depth, ref int row)
    {
        if (config == null)
            return;

        config.editorPosition = new Vector2(80f + depth * ColumnWidth, 120f + row * RowHeight);
        row++;

        foreach (var child in GetChildren(config))
            AssignLayout(child.node, depth + 1, ref row);
    }

    private static string GetNodeTitle(NodeConfig config)
    {
        return config switch
        {
            FlowNodeConfig flow => flow.type.ToString(),
            RandomNodeConfig => "Random",
            RepeatNodeConfig repeat => $"Repeat x{repeat.count}",
            LoopNodeConfig loop => $"Loop x{loop.maxIterations}",
            ConditionNodeConfig condition => condition.condition != null ? $"If ({condition.condition.GetType().Name})" : "If",
            ActionNodeConfig action => GetActionNodeDisplayName(action) ?? "Action",
            _ => "Unknown"
        };
    }

    private static Type GetActionNodeType(ActionNodeConfig action)
    {
        if (action?.dataProvider == null)
            return null;

        var attribute = (NodeBindingAttribute)Attribute.GetCustomAttribute(action.dataProvider.GetType(), typeof(NodeBindingAttribute));
        return attribute?.NodeType;
    }

    private static string GetActionNodeDisplayName(ActionNodeConfig action)
    {
        Type nodeType = GetActionNodeType(action);
        return nodeType != null ? ActionNodeEditorNames.GetDisplayName(nodeType) : null;
    }

    private static StyleColor GetNodeColor(NodeConfig config)
    {
        return config switch
        {
            FlowNodeConfig => new Color(0.20f, 0.31f, 0.43f),
            RandomNodeConfig => new Color(0.28f, 0.24f, 0.42f),
            RepeatNodeConfig => new Color(0.39f, 0.28f, 0.17f),
            LoopNodeConfig => new Color(0.34f, 0.27f, 0.18f),
            ConditionNodeConfig => new Color(0.39f, 0.31f, 0.12f),
            ActionNodeConfig => new Color(0.18f, 0.36f, 0.26f),
            _ => new Color(0.24f, 0.24f, 0.24f)
        };
    }

    private void MarkDirty()
    {
        if (_config != null)
            EditorUtility.SetDirty(_config);
    }
}

