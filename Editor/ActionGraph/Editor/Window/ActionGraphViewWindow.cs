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
    private ActionGraphAsset _config;
    private ActionGraphView _graphView;
    private ObjectField _configField;
    

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
        toolbar.Add(new ToolbarButton(UseSelection) { text = "Use Selection" });
        toolbar.Add(new ToolbarButton(CreateRoot) { text = "Create Root" });
        toolbar.Add(new ToolbarButton(ResetLayout) { text = "Reset Layout" });
        toolbar.Add(new ToolbarButton(() => { _graphView?.FrameAll(); }) { text = "Frame All" });

        _graphView = new ActionGraphView();

        rootVisualElement.Add(toolbar);
        rootVisualElement.Add(_graphView);

        if (_config == null && Selection.activeObject is ActionGraphAsset selectedConfig)
            LoadConfig(selectedConfig);
    }

    private void LoadConfig(ActionGraphAsset config)
    {
        _config = config;

        if (_configField != null && _configField.value != config)
            _configField.SetValueWithoutNotify(config);

        _graphView?.Load(config);
    }

    private void UseSelection()
    {
        if (Selection.activeObject is ActionGraphAsset selectedConfig)
            LoadConfig(selectedConfig);
    }

    private void CreateRoot()
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
            type = FlowNodeType.Sequence,
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
    private ActionGraphNodeSearchProvider _searchProvider;
    private ActionGraphAsset _config;
    private Port _pendingConnectionPort;
    private double _pendingConnectionStartTime;
    private NodeConfig _lastSearchParent;
    private OutputSlot _lastSearchSlot;
    private double _lastSearchTime;
    private bool _rebuildQueued;

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
        Rebuild();
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
            evt.menu.AppendAction("Create Root/Sequence", _ => CreateRoot(FlowNodeType.Sequence));

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
        foreach (var edge in edges.ToList())
            RemoveElement(edge);

        foreach (var node in nodes.ToList())
            RemoveElement(node);

        _nodeViews.Clear();

        if (_config?.root == null)
            return;

        EnsureMissingPositions();
        AddNodeTree(_config.root, true);
        AddEdges(_config.root);
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

    private void AddNodeTree(NodeConfig config, bool isRoot)
    {
        var view = new ActionGraphNodeView(
            config,
            GetNodeTitle(config),
            isRoot,
            () => DeleteNode(config),
            SupportsBranchCollapse(config),
            config.editorCollapsed,
            () => ToggleBranchCollapse(config),
            () => DrawNodeInspector(config),
            OnNodeGeometryChanged);

        view.SetPosition(new Rect(config.editorPosition, GetNodeSize(config)));
        view.titleContainer.style.backgroundColor = GetNodeColor(config);

        ConfigureOutputPorts(view, config);
        AddElement(view);
        _nodeViews[config] = view;

        foreach (var child in GetVisibleChildren(config))
            AddNodeTree(child.node, false);
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


