using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ActionGraphView
{
    private void ShowActionMenu(ActionNodeConfig action)
    {
        var menu = new GenericMenu();
        var map = NodeEditorCache.GetNodeToDataSetMap();

        foreach (var pair in map.OrderBy(pair => ActionNodeEditorNames.GetMenuPath(pair.Key)))
        {
            Type nodeType = pair.Key;
            Type providerType = pair.Value;

            menu.AddItem(new GUIContent(ActionNodeEditorNames.GetMenuPath(nodeType)), false, () =>
            {
                Undo.RecordObject(_config, "Select Action Node");
                action.dataProvider = (ActionDataProvider)Activator.CreateInstance(providerType);
                action.dataProvider.EnsureDefaultData();
                MarkDirty();
                QueueRebuild();
            });
        }

        menu.ShowAsContext();
    }

    private void ShowConditionMenu(ConditionNodeConfig condition)
    {
        var menu = new GenericMenu();
        var conditionTypes = TypeCache.GetTypesDerivedFrom<ICondition>()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .OrderBy(type => type.Name);

        foreach (var type in conditionTypes)
        {
            Type capturedType = type;
            menu.AddItem(new GUIContent(capturedType.Name), false, () =>
            {
                Undo.RecordObject(_config, "Select Condition");
                condition.condition = (ICondition)Activator.CreateInstance(capturedType);
                MarkDirty();
                QueueRebuild();
            });
        }

        menu.ShowAsContext();
    }

    private void ShowLoopConditionMenu(LoopNodeConfig loop)
    {
        var menu = new GenericMenu();
        var conditionTypes = TypeCache.GetTypesDerivedFrom<ICondition>()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .OrderBy(type => type.Name);

        foreach (var type in conditionTypes)
        {
            Type capturedType = type;
            menu.AddItem(new GUIContent(capturedType.Name), false, () =>
            {
                Undo.RecordObject(_config, "Select Loop Condition");
                loop.condition = (ICondition)Activator.CreateInstance(capturedType);
                MarkDirty();
            });
        }

        menu.ShowAsContext();
    }

    private void OnNodeCreationRequest(NodeCreationContext context)
    {
        if (_pendingConnectionPort?.node is ActionGraphNodeView parentView &&
            _pendingConnectionPort.userData is OutputSlot slot &&
            EditorApplication.timeSinceStartup - _pendingConnectionStartTime < 1d)
        {
            ShowCreateNodeSearch(parentView.Config, slot, context.screenMousePosition);
            return;
        }

        ShowCreateNodeSearch(null, OutputSlot.Children, context.screenMousePosition);
    }

    private void TrackPendingConnection(Port port)
    {
        _pendingConnectionPort = port;
        _pendingConnectionStartTime = EditorApplication.timeSinceStartup;
    }

    private void ShowCreateNodeSearch(NodeConfig parent, OutputSlot slot, Vector2 screenPosition)
    {
        if (_config == null)
            return;

        if (parent == null && _config.root != null)
            return;

        if (parent != null && !CanCreateChild(parent, slot))
            return;

        double now = EditorApplication.timeSinceStartup;
        if (_lastSearchParent == parent && _lastSearchSlot == slot && now - _lastSearchTime < 0.1d)
            return;

        _lastSearchParent = parent;
        _lastSearchSlot = slot;
        _lastSearchTime = now;

        if (_searchProvider == null)
        {
            _searchProvider = ScriptableObject.CreateInstance<ActionGraphNodeSearchProvider>();
            _searchProvider.Initialize(this);
        }

        _searchProvider.Configure(parent, slot, GetGraphPositionFromScreen(screenPosition));
        SearchWindow.Open(new SearchWindowContext(screenPosition), _searchProvider);
    }

    private void CreateAndConnectNode(NodeConfig parent, OutputSlot slot, Func<NodeConfig> factory, Vector2 graphPosition)
    {
        if (_config == null || factory == null)
            return;

        NodeConfig node = factory();
        if (node == null)
            return;

        if (parent == null && _config.root != null)
            return;

        Undo.RecordObject(_config, "Create Action Graph Node");

        node.editorPosition = parent != null && graphPosition == Vector2.zero
            ? parent.editorPosition + GetNewChildOffset(parent, slot)
            : graphPosition;

        if (parent == null)
            _config.root = node;
        else
            Connect(parent, node, slot);

        MarkDirty();
        QueueRebuild();
    }

    private Vector2 GetGraphPositionFromScreen(Vector2 screenPosition)
    {
        var window = EditorWindow.focusedWindow;
        Vector2 windowPosition = screenPosition;

        if (window != null)
            windowPosition -= window.position.position;

        return contentViewContainer.WorldToLocal(windowPosition);
    }

    private static Vector2 GetElementScreenPosition(VisualElement element)
    {
        Vector2 screenPosition = element.worldBound.center;
        var window = EditorWindow.focusedWindow;
        if (window != null)
            screenPosition += window.position.position;

        return screenPosition;
    }

    private bool IsOverCompatibleInputPort(Port outputPort, Vector2 panelPosition)
    {
        return GetCompatiblePorts(outputPort, null)
            .Any(port => port.direction == Direction.Input && port.worldBound.Contains(panelPosition));
    }

    private bool CanCreateChild(NodeConfig parent, OutputSlot slot)
    {
        switch (parent)
        {
            case FlowNodeConfig _ when slot == OutputSlot.Children:
            case RandomNodeConfig _ when slot == OutputSlot.Children:
                return true;

            case RepeatNodeConfig repeat when slot == OutputSlot.Child:
                return repeat.child == null;

            case LoopNodeConfig loop when slot == OutputSlot.Child:
                return loop.child == null;

            case ConditionNodeConfig condition when slot == OutputSlot.True:
                return condition.trueNode == null;

            case ConditionNodeConfig condition when slot == OutputSlot.False:
                return condition.falseNode == null;

            default:
                return false;
        }
    }

    private IEnumerable<NodeCreationOption> GetNodeCreationOptions()
    {
        yield return new NodeCreationOption("Flow/Sequence", () => new FlowNodeConfig { type = FlowNodeType.Sequence });
        yield return new NodeCreationOption("Flow/Parallel", () => new FlowNodeConfig { type = FlowNodeType.Parallel });
        yield return new NodeCreationOption("Flow/Repeat", () => new RepeatNodeConfig());
        yield return new NodeCreationOption("Flow/Loop", () => new LoopNodeConfig());
        yield return new NodeCreationOption("Flow/Random", () => new RandomNodeConfig());

        yield return new NodeCreationOption("Action/Empty Action", () => new ActionNodeConfig());

        foreach (var pair in NodeEditorCache.GetNodeToDataSetMap().OrderBy(pair => ActionNodeEditorNames.GetMenuPath(pair.Key)))
        {
            Type nodeType = pair.Key;
            Type providerType = pair.Value;
            yield return new NodeCreationOption("Action/" + ActionNodeEditorNames.GetMenuPath(nodeType), () =>
            {
                var provider = (ActionDataProvider)Activator.CreateInstance(providerType);
                provider.EnsureDefaultData();
                return new ActionNodeConfig { dataProvider = provider };
            });
        }

        yield return new NodeCreationOption("Flow/If", () => new ConditionNodeConfig());

        var conditionTypes = TypeCache.GetTypesDerivedFrom<ICondition>()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .OrderBy(type => type.Name);

        foreach (var conditionType in conditionTypes)
        {
            Type capturedType = conditionType;
            yield return new NodeCreationOption("If/" + capturedType.Name, () => new ConditionNodeConfig
            {
                condition = (ICondition)Activator.CreateInstance(capturedType)
            });
        }
    }

    private void CreateRoot(FlowNodeType type)
    {
        if (_config == null)
            return;

        if (_config.root != null)
            return;

        Undo.RecordObject(_config, "Create Action Graph Root");
        _config.root = new FlowNodeConfig
        {
            type = type,
            editorPosition = new Vector2(80f, 120f)
        };

        MarkDirty();
        Rebuild();
    }

    private Vector2 GetNewChildOffset(NodeConfig parent, OutputSlot slot)
    {
        int siblingCount = GetChildren(parent).Count(child => child.slot == slot);
        float yOffset = slot switch
        {
            OutputSlot.False => RowHeight,
            OutputSlot.True => 0f,
            _ => siblingCount * RowHeight
        };

        return new Vector2(ColumnWidth, yOffset);
    }

    private sealed class NodeCreationOption
    {
        public readonly string Path;
        public readonly Func<NodeConfig> Factory;

        public NodeCreationOption(string path, Func<NodeConfig> factory)
        {
            Path = path;
            Factory = factory;
        }
    }

    private sealed class ActionGraphNodeSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        private ActionGraphView _view;
        private NodeConfig _parent;
        private OutputSlot _slot;
        private Vector2 _graphPosition;

        public void Initialize(ActionGraphView view)
        {
            _view = view;
        }

        public void Configure(NodeConfig parent, OutputSlot slot, Vector2 graphPosition)
        {
            _parent = parent;
            _slot = slot;
            _graphPosition = graphPosition;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0)
            };

            var createdGroups = new HashSet<string>();

            foreach (var option in _view.GetNodeCreationOptions().OrderBy(option => option.Path))
            {
                string[] parts = option.Path.Split('/');
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    string groupPath = string.Join("/", parts.Take(i + 1));
                    if (createdGroups.Add(groupPath))
                        tree.Add(new SearchTreeGroupEntry(new GUIContent(parts[i]), i + 1));
                }

                tree.Add(new SearchTreeEntry(new GUIContent(parts[parts.Length - 1]))
                {
                    level = parts.Length,
                    userData = option
                });
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            var option = entry.userData as NodeCreationOption;
            if (option == null || _view == null)
                return false;

            _view.CreateAndConnectNode(_parent, _slot, option.Factory, _graphPosition);
            return true;
        }
    }
}

