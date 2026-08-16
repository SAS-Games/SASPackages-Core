using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class ActionGraphWindow : EditorWindow
{
    private const int MaxDebugTraceEntries = 200;

    private ActionGraphAsset _config;
    private ActionGraphView _graphView;
    private ObjectField _configField;
    private ToolbarButton _createRootButton;
    private ToolbarButton _addNodeButton;
    private ToolbarButton _backButton;
    private VisualElement _breadcrumbContainer;
    private ToolbarToggle _liveDebugToggle;
    private Label _debugStatusLabel;
    private VisualElement _debugPanel;
    private ScrollView _debugTrace;

    private void OnEnable()
    {
        ActionGraphDebug.NodeStateChanged += OnNodeDebugStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        ActionGraphDebug.NodeStateChanged -= OnNodeDebugStateChanged;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    [MenuItem("Assets/Open Action Graph", false, 1200)]
    private static void OpenSelectedAsset()
    {
        OpenWithConfig(Selection.activeObject as ActionGraphAsset);
    }

    [MenuItem("Assets/Open Action Graph", true)]
    private static bool CanOpenSelectedAsset()
    {
        return Selection.activeObject is ActionGraphAsset;
    }

    public static void OpenWithConfig(ActionGraphAsset config)
    {
        var wnd = GetWindow<ActionGraphWindow>();
        wnd.titleContent = new GUIContent("Action Graph");

        if (config != null)
            wnd.LoadConfig(config);
    }

    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)
    {
#if UNITY_6000_3_OR_NEWER
        if (EditorUtility.EntityIdToObject(instanceID) is ActionGraphAsset config)  
#else
        if (EditorUtility.InstanceIDToObject(instanceID) is ActionGraphAsset config)
#endif
        {
            OpenWithConfig(config);
            return true;
        }

        return false;
    }

    public void CreateGUI()
    {
        rootVisualElement.style.flexDirection = FlexDirection.Column;

        var toolbar = new Toolbar();

        _configField = new ObjectField("Graph")
        {
            objectType = typeof(ActionGraphAsset),
            allowSceneObjects = false
        };
        _configField.RegisterValueChangedCallback(evt => LoadConfig(evt.newValue as ActionGraphAsset));

        toolbar.Add(_configField);
        _createRootButton = new ToolbarButton(ShowCreateRootMenu)
        {
            text = "Create Root",
            tooltip = "Create the required root Sequence or Parallel node for an empty graph"
        };
        toolbar.Add(_createRootButton);
        toolbar.Add(new ToolbarButton(ResetLayout) { text = "Reset Layout" });
        toolbar.Add(new ToolbarButton(() => { _graphView?.FrameAll(); }) { text = "Frame All" });

        _addNodeButton = new ToolbarButton(() => _graphView?.AddNodeToCurrentGroup())
        {
            text = "Add Node",
            tooltip = "Add a child to the currently open Sequence or Parallel node"
        };
        _backButton = new ToolbarButton(() => _graphView?.NavigateBack())
        {
            text = "← Parent",
            tooltip = "Return to the parent Sequence or Parallel node"
        };
        _breadcrumbContainer = new VisualElement();
        _breadcrumbContainer.style.flexDirection = FlexDirection.Row;
        _breadcrumbContainer.style.alignItems = Align.Center;
        _breadcrumbContainer.style.marginLeft = 6f;
        toolbar.Add(_addNodeButton);
        toolbar.Add(_backButton);
        toolbar.Add(_breadcrumbContainer);

        _liveDebugToggle = new ToolbarToggle
        {
            text = "Live Debug",
            tooltip = "Highlight runtime execution while the Editor is in Play Mode",
            value = true
        };
        _liveDebugToggle.RegisterValueChangedCallback(evt =>
        {
            _debugPanel.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            if (!evt.newValue)
                ClearDebugTrace();
        });
        toolbar.Add(_liveDebugToggle);
        toolbar.Add(new ToolbarButton(ClearDebugTrace)
        {
            text = "Clear Trace",
            tooltip = "Clear node highlights and the execution trace"
        });

        _graphView = new ActionGraphView();
        _graphView.NavigationChanged += UpdateNavigationToolbar;

        rootVisualElement.Add(toolbar);
        rootVisualElement.Add(_graphView);
        CreateDebugPanel();
        rootVisualElement.Add(_debugPanel);

        if (_config == null && Selection.activeObject is ActionGraphAsset selectedConfig)
            LoadConfig(selectedConfig);

        UpdateNavigationToolbar();
    }

    private void LoadConfig(ActionGraphAsset config)
    {
        _config = config;
        ClearDebugTrace();

        if (_configField != null && _configField.value != config)
            _configField.SetValueWithoutNotify(config);

        _graphView?.Load(config);
        UpdateNavigationToolbar();
    }

    private void CreateDebugPanel()
    {
        _debugPanel = new VisualElement();
        _debugPanel.style.height = 150f;
        _debugPanel.style.flexShrink = 0f;
        _debugPanel.style.borderTopWidth = 1f;
        _debugPanel.style.borderTopColor = new Color(0.25f, 0.25f, 0.25f);
        _debugPanel.style.paddingLeft = 6f;
        _debugPanel.style.paddingRight = 6f;
        _debugPanel.style.paddingTop = 4f;
        _debugPanel.style.paddingBottom = 4f;

        _debugStatusLabel = new Label("Live Debug: enter Play Mode to capture execution");
        _debugStatusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _debugPanel.Add(_debugStatusLabel);

        _debugTrace = new ScrollView(ScrollViewMode.Vertical)
        {
            horizontalScrollerVisibility = ScrollerVisibility.Hidden,
            verticalScrollerVisibility = ScrollerVisibility.Auto
        };
        _debugTrace.style.flexGrow = 1f;
        _debugPanel.Add(_debugTrace);
    }

    private void OnNodeDebugStateChanged(ActionGraphDebugEvent debugEvent)
    {
        if (_liveDebugToggle?.value != true || _graphView == null || !_graphView.ContainsConfig(debugEvent.Node))
            return;

        _graphView.SetDebugState(debugEvent.Node, debugEvent.State);

        string nodeTitle = _graphView.GetDebugTitle(debugEvent.Node);
        _debugStatusLabel.text = $"{debugEvent.State}: {nodeTitle}";

        string message = $"{debugEvent.Timestamp:F3}  {debugEvent.State,-9}  {nodeTitle}";
        if (debugEvent.Exception != null)
            message += $" — {debugEvent.Exception.Message}";

        var entry = new Label(message);
        entry.style.color = GetDebugColor(debugEvent.State);
        _debugTrace.Add(entry);

        while (_debugTrace.contentContainer.childCount > MaxDebugTraceEntries)
            _debugTrace.contentContainer.RemoveAt(0);

        _debugTrace.ScrollTo(entry);
        Repaint();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            ClearDebugTrace();
            if (_debugStatusLabel != null)
                _debugStatusLabel.text = "Live Debug: waiting for graph execution";
            return;
        }

        if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            ClearDebugTrace();
    }

    private void ClearDebugTrace()
    {
        _graphView?.ClearDebugStates();
        _debugTrace?.Clear();

        if (_debugStatusLabel != null)
        {
            _debugStatusLabel.text = Application.isPlaying
                ? "Live Debug: waiting for graph execution"
                : "Live Debug: enter Play Mode to capture execution";
        }
    }

    private static Color GetDebugColor(ActionGraphDebugState state)
    {
        return state switch
        {
            ActionGraphDebugState.Started => new Color(1f, 0.72f, 0.12f),
            ActionGraphDebugState.Completed => new Color(0.20f, 0.78f, 0.42f),
            ActionGraphDebugState.Cancelled => new Color(0.62f, 0.66f, 0.72f),
            ActionGraphDebugState.Failed => new Color(0.95f, 0.22f, 0.20f),
            _ => Color.white
        };
    }

    private void UpdateNavigationToolbar()
    {
        if (_createRootButton == null || _addNodeButton == null || _backButton == null || _breadcrumbContainer == null)
            return;

        bool needsRoot = _config != null && _config.root == null;
        _createRootButton.style.display = needsRoot ? DisplayStyle.Flex : DisplayStyle.None;
        _addNodeButton.SetEnabled(_graphView?.CanAddToCurrentGroup == true);
        _backButton.SetEnabled(_graphView?.CanNavigateBack == true);

        RebuildBreadcrumbs();
    }

    private void RebuildBreadcrumbs()
    {
        _breadcrumbContainer.Clear();
        _breadcrumbContainer.Add(new Label(_config != null ? _config.name : "No Graph"));

        if (_graphView == null)
            return;

        for (int i = 0; i < _graphView.NavigationDepth; i++)
        {
            _breadcrumbContainer.Add(new Label("›"));

            int level = i;
            var breadcrumb = new ToolbarButton(() => _graphView.NavigateToLevel(level))
            {
                text = _graphView.GetNavigationTitle(level),
                tooltip = level == _graphView.NavigationDepth - 1
                    ? "Current node"
                    : "Return directly to this node"
            };
            breadcrumb.SetEnabled(level < _graphView.NavigationDepth - 1);
            _breadcrumbContainer.Add(breadcrumb);
        }
    }

    private void ShowCreateRootMenu()
    {
        if (_config == null || _config.root != null)
            return;

        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Sequence"), false, () => CreateRoot(FlowNodeType.Sequence));
        menu.AddItem(new GUIContent("Parallel"), false, () => CreateRoot(FlowNodeType.Parallel));
        menu.ShowAsContext();
    }

    private void CreateRoot(FlowNodeType type)
    {
        if (_config == null)
            return;

        if (_config.root != null)
        {
            EditorUtility.DisplayDialog("Action Graph", "This graph already has a root node. Delete the root first if you want to replace it.", "OK");
            return;
        }

        Undo.RecordObject(_config, "Create Action Graph Root");
        _config.root = new FlowNodeConfig
        {
            type = type,
            editorPosition = new Vector2(80f, 120f)
        };

        EditorUtility.SetDirty(_config);
        _graphView.Load(_config);
    }

    private void ResetLayout()
    {
        _graphView?.ResetLayout();
    }
}

public partial class ActionGraphView : GraphView
{
    private enum OutputSlot
    {
        Children,
        Child,
        True,
        False
    }

    private const float NodeWidth = 320f;
    private const float NodeHeight = 180f;
    private const float MinNodeWidth = 220f;
    private const float MinNodeHeight = 120f;
    private const float ColumnWidth = 360f;
    private const float RowHeight = 230f;

    private readonly Dictionary<NodeConfig, ActionGraphNodeView> _nodeViews = new();
    private readonly Dictionary<NodeConfig, ActionGraphDebugState> _debugStates = new();
    private readonly List<NodeConfig> _navigationStack = new();
    private ActionGraphNodeSearchProvider _searchProvider;
    private ActionGraphAsset _config;
    private Port _pendingConnectionPort;
    private double _pendingConnectionStartTime;
    private NodeConfig _lastSearchParent;
    private OutputSlot _lastSearchSlot;
    private double _lastSearchTime;
    private bool _rebuildQueued;

    public event System.Action NavigationChanged;
    public bool CanNavigateBack => _navigationStack.Count > 1;
    public bool CanAddToCurrentGroup => CurrentScope is FlowNodeConfig;
    public int NavigationDepth => _navigationStack.Count;

    private NodeConfig CurrentScope => _navigationStack.Count > 0 ? _navigationStack[^1] : null;

    public ActionGraphView()
    {
        style.flexGrow = 1f;

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        graphViewChanged = OnGraphViewChanged;
        nodeCreationRequest = OnNodeCreationRequest;
    }

    public void Load(ActionGraphAsset config)
    {
        _config = config;
        _debugStates.Clear();
        _navigationStack.Clear();
        if (_config?.root is FlowNodeConfig rootGroup)
            _navigationStack.Add(rootGroup);

        Rebuild();
        NavigationChanged?.Invoke();
    }

    public bool ContainsConfig(NodeConfig config)
    {
        return config != null && ContainsNode(_config?.root, config);
    }

    public string GetDebugTitle(NodeConfig config)
    {
        return GetNodeTitle(config);
    }

    public void SetDebugState(NodeConfig config, ActionGraphDebugState state)
    {
        if (config == null)
            return;

        _debugStates[config] = state;
        if (_nodeViews.TryGetValue(config, out ActionGraphNodeView view))
            view.SetDebugState(state, GetNodeColor(config));
    }

    public void ClearDebugStates()
    {
        _debugStates.Clear();
        foreach (var pair in _nodeViews)
            pair.Value.SetDebugState(null, GetNodeColor(pair.Key));
    }

    public void AddNodeToCurrentGroup()
    {
        if (CurrentScope is not FlowNodeConfig group)
            return;

        ShowCreateNodeSearch(group, OutputSlot.Children, GetElementScreenPosition(this));
    }

    public void NavigateBack()
    {
        if (!CanNavigateBack)
            return;

        NavigateToLevel(_navigationStack.Count - 2);
    }

    public string GetNavigationTitle(int level)
    {
        return level >= 0 && level < _navigationStack.Count
            ? GetNodeTitle(_navigationStack[level])
            : string.Empty;
    }

    public void NavigateToLevel(int level)
    {
        if (level < 0 || level >= _navigationStack.Count)
            return;

        int removeCount = _navigationStack.Count - level - 1;
        if (removeCount == 0)
            return;

        _navigationStack.RemoveRange(level + 1, removeCount);
        Rebuild();
        NavigationChanged?.Invoke();
        schedule.Execute(_ => FrameAll()).StartingIn(25);
    }

    private void EnterGroup(FlowNodeConfig group)
    {
        if (group == null || ReferenceEquals(group, CurrentScope))
            return;

        _navigationStack.Add(group);
        Rebuild();
        NavigationChanged?.Invoke();
        schedule.Execute(_ => FrameAll()).StartingIn(25);
    }

    public void ResetLayout()
    {
        if (_config?.root == null)
            return;

        Undo.RecordObject(_config, "Reset Action Graph Layout");

        int row = 0;
        AssignLayout(_config.root, 0, ref row);

        EditorUtility.SetDirty(_config);
        Rebuild();
        schedule.Execute(_ => FrameAll()).StartingIn(50);
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);

        if (_config == null)
            return;

        if (_config.root == null)
        {
            evt.menu.AppendAction("Create Root/Sequence", _ => CreateRoot(FlowNodeType.Sequence));
            evt.menu.AppendAction("Create Root/Parallel", _ => CreateRoot(FlowNodeType.Parallel));
        }

        evt.menu.AppendAction("Reset Layout", _ => ResetLayout(), _config.root != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        evt.menu.AppendAction("Frame All", _ => FrameAll(), _config.root != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        bool rebuild = false;

        if (change.movedElements != null)
        {
            foreach (var moved in change.movedElements.OfType<ActionGraphNodeView>())
            {
                PersistNodeRect(moved);
            }
        }

        if (change.elementsToRemove != null)
        {
            foreach (var edge in change.elementsToRemove.OfType<Edge>())
            {
                if (edge.output?.node is ActionGraphNodeView parent &&
                    edge.input?.node is ActionGraphNodeView child &&
                    edge.output.userData is OutputSlot slot)
                {
                    Undo.RecordObject(_config, "Disconnect Action Graph Node");
                    Disconnect(parent.Config, child.Config, slot);
                    MarkDirty();
                    rebuild = true;
                }
            }

            foreach (var node in change.elementsToRemove.OfType<ActionGraphNodeView>())
            {
                Undo.RecordObject(_config, "Delete Action Graph Node");
                RemoveNode(node.Config);
                MarkDirty();
                rebuild = true;
            }
        }

        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
            {
                if (edge.output?.node is ActionGraphNodeView parent &&
                    edge.input?.node is ActionGraphNodeView child &&
                    edge.output.userData is OutputSlot slot)
                {
                    Undo.RecordObject(_config, "Connect Action Graph Node");
                    Connect(parent.Config, child.Config, slot);
                    MarkDirty();
                    rebuild = true;
                }
            }
        }

        if (rebuild)
            QueueRebuild();

        return change;
    }

    private void Rebuild()
    {
        EnsureValidNavigationScope();

        foreach (var edge in edges.ToList())
            RemoveElement(edge);

        foreach (var node in nodes.ToList())
            RemoveElement(node);

        _nodeViews.Clear();

        if (_config?.root == null)
            return;

        EnsureMissingPositions();

        if (CurrentScope is FlowNodeConfig group)
        {
            var children = GetVisibleChildren(CurrentScope).ToList();
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                string titlePrefix = group.type == FlowNodeType.Sequence ? $"{i + 1}. " : string.Empty;
                AddNodeTree(child.node, true, titlePrefix);
                if (group.type == FlowNodeType.Sequence && _nodeViews.TryGetValue(child.node, out var childView))
                {
                    NodeConfig childNode = child.node;
                    childView.AddExecutionOrderControls(
                        i > 0 ? () => MoveSequenceChild(group, childNode, -1) : null,
                        i < children.Count - 1 ? () => MoveSequenceChild(group, childNode, 1) : null);
                }
                AddEdges(child.node);
            }
        }
        else
        {
            AddNodeTree(_config.root, true);
            AddEdges(_config.root);
        }

        NavigationChanged?.Invoke();
    }

    private void QueueRebuild()
    {
        if (_rebuildQueued)
            return;

        _rebuildQueued = true;
        schedule.Execute(_ =>
        {
            _rebuildQueued = false;
            Rebuild();
        }).StartingIn(0);
    }

    private void AddNodeTree(NodeConfig config, bool isRoot, string titlePrefix = null)
    {
        var view = new ActionGraphNodeView(
            config,
            titlePrefix + GetNodeTitle(config),
            GetNodeDescription(config),
            config is FlowNodeConfig,
            config is FlowNodeConfig group && !ReferenceEquals(config, CurrentScope)
                ? () => EnterGroup(group)
                : null,
            isRoot,
            () => DeleteNode(config),
            SupportsBranchCollapse(config),
            config.editorCollapsed,
            () => ToggleBranchCollapse(config),
            () => DrawNodeInspector(config),
            OnNodeGeometryChanged);

        view.SetPosition(new Rect(config.editorPosition, GetNodeSize(config)));
        view.titleContainer.style.backgroundColor = GetNodeColor(config);
        if (_debugStates.TryGetValue(config, out ActionGraphDebugState debugState))
            view.SetDebugState(debugState, GetNodeColor(config));

        ConfigureOutputPorts(view, config);
        AddElement(view);
        _nodeViews[config] = view;

        foreach (var child in GetVisibleChildren(config))
            AddNodeTree(child.node, false);
    }

    private void EnsureValidNavigationScope()
    {
        while (_navigationStack.Count > 0 &&
               (_config?.root == null || !ContainsNode(_config.root, _navigationStack[^1])))
        {
            _navigationStack.RemoveAt(_navigationStack.Count - 1);
        }

        if (_navigationStack.Count == 0 && _config?.root is FlowNodeConfig rootGroup)
            _navigationStack.Add(rootGroup);
    }

    private void MoveSequenceChild(FlowNodeConfig sequence, NodeConfig child, int offset)
    {
        if (sequence?.children == null || child == null || offset == 0)
            return;

        int oldIndex = sequence.children.IndexOf(child);
        int newIndex = oldIndex + offset;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= sequence.children.Count)
            return;

        Undo.RecordObject(_config, "Reorder Sequence Child");
        (sequence.children[oldIndex], sequence.children[newIndex]) =
            (sequence.children[newIndex], sequence.children[oldIndex]);
        MarkDirty();
        Rebuild();
    }

    private void ToggleBranchCollapse(NodeConfig config)
    {
        if (_config == null || config == null)
            return;

        Undo.RecordObject(_config, config.editorCollapsed ? "Expand Action Graph Branch" : "Collapse Action Graph Branch");
        config.editorCollapsed = !config.editorCollapsed;
        MarkDirty();
        QueueRebuild();
    }

    private void OnNodeGeometryChanged(ActionGraphNodeView view)
    {
        PersistNodeRect(view);
    }

    private void PersistNodeRect(ActionGraphNodeView view)
    {
        if (view?.Config == null)
            return;

        Rect rect = view.GetPosition();
        Vector2 size = ClampNodeSize(rect.size);

        bool changed = view.Config.editorPosition != rect.position ||
                       view.Config.editorSize != size;

        if (!changed)
            return;

        view.Config.editorPosition = rect.position;
        view.Config.editorSize = size;
        MarkDirty();
    }

    private static Vector2 GetNodeSize(NodeConfig config)
    {
        return config != null && config.editorSize != Vector2.zero
            ? ClampNodeSize(config.editorSize)
            : new Vector2(NodeWidth, NodeHeight);
    }

    private static Vector2 ClampNodeSize(Vector2 size)
    {
        return new Vector2(
            Mathf.Max(MinNodeWidth, size.x),
            Mathf.Max(MinNodeHeight, size.y));
    }
}


