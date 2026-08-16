using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ActionGraphView
{
    private sealed class ActionGraphNodeView : Node
    {
        private readonly Label _descriptionLabel;

        public readonly NodeConfig Config;
        public readonly Dictionary<OutputSlot, Port> OutputPorts = new();
        public Port InputPort { get; }

        public ActionGraphNodeView(
            NodeConfig config,
            string titleText,
            string descriptionText,
            bool isGroup,
            Action openGroup,
            bool isRoot,
            Action delete,
            bool canCollapseBranch,
            bool isBranchCollapsed,
            Action toggleBranchCollapse,
            Action drawInspector,
            Action<ActionGraphNodeView> persistGeometry)
        {
            Config = config;
            title = isBranchCollapsed ? $"{titleText} (collapsed)" : titleText;
            viewDataKey = config.GetHashCode().ToString();
            expanded = true;

            capabilities |= Capabilities.Movable | Capabilities.Deletable | Capabilities.Selectable | Capabilities.Resizable;
            style.minWidth = MinNodeWidth;
            style.minHeight = MinNodeHeight;
            mainContainer.style.flexGrow = 1f;
            mainContainer.style.minHeight = 0f;
            tooltip = descriptionText;

            if (isGroup && openGroup != null)
            {
                var openButton = new Button(openGroup)
                {
                    text = "›",
                    tooltip = "Enter this Sequence or Parallel node"
                };
                openButton.style.width = 24f;
                openButton.style.height = 18f;
                openButton.style.marginLeft = 4f;
                titleContainer.Add(openButton);

                titleContainer.RegisterCallback<MouseDownEvent>(evt =>
                {
                    var targetElement = evt.target as VisualElement;
                    bool isButton = targetElement is Button ||
                                    targetElement?.GetFirstAncestorOfType<Button>() != null;
                    if (evt.clickCount != 2 || isButton)
                        return;

                    openGroup();
                    evt.StopPropagation();
                }, TrickleDown.TrickleDown);

                RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                        return;

                    openGroup();
                    evt.StopPropagation();
                });
            }

            if (!isRoot)
            {
                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(NodeConfig));
                InputPort.portName = "In";
                inputContainer.Add(InputPort);
            }

            if (canCollapseBranch)
            {
                var collapseButton = new Button(toggleBranchCollapse)
                {
                    text = isBranchCollapsed ? ">" : "v",
                    tooltip = isBranchCollapsed ? "Expand child branch" : "Collapse child branch"
                };
                collapseButton.style.width = 22f;
                collapseButton.style.height = 18f;
                titleContainer.Add(collapseButton);
            }

            var deleteButton = new Button(delete)
            {
                text = "x",
                tooltip = "Delete selected node or edge"
            };
            deleteButton.style.width = 22f;
            deleteButton.style.height = 18f;
            titleContainer.Add(deleteButton);

            extensionContainer.style.flexGrow = 1f;
            extensionContainer.style.minHeight = 0f;

            _descriptionLabel = new Label();
            _descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
            _descriptionLabel.style.marginLeft = 8f;
            _descriptionLabel.style.marginRight = 8f;
            _descriptionLabel.style.marginTop = 4f;
            _descriptionLabel.style.marginBottom = 6f;
            _descriptionLabel.style.color = new Color(0.78f, 0.78f, 0.78f);
            _descriptionLabel.style.fontSize = 11f;
            extensionContainer.Add(_descriptionLabel);
            SetDescription(descriptionText);

            var inspectorScroll = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Auto
            };
            inspectorScroll.style.flexGrow = 1f;
            inspectorScroll.style.minHeight = 0f;

            var inspector = new IMGUIContainer(drawInspector);
            inspector.style.flexGrow = 1f;
            inspectorScroll.Add(inspector);
            extensionContainer.Add(inspectorScroll);

            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.oldRect.size == Vector2.zero || evt.newRect.size == Vector2.zero)
                    return;

                persistGeometry?.Invoke(this);
            });
            RefreshExpandedState();
        }

        public void SetDescription(string descriptionText)
        {
            string value = descriptionText ?? string.Empty;
            _descriptionLabel.text = value;
            tooltip = value;
        }

        public void SetDebugState(ActionGraphDebugState? state, Color baseTitleColor)
        {
            if (!state.HasValue)
            {
                titleContainer.style.backgroundColor = baseTitleColor;
                style.borderLeftWidth = 0f;
                style.borderRightWidth = 0f;
                style.borderTopWidth = 0f;
                style.borderBottomWidth = 0f;
                return;
            }

            Color debugColor = state.Value switch
            {
                ActionGraphDebugState.Started => new Color(1f, 0.72f, 0.12f),
                ActionGraphDebugState.Completed => new Color(0.20f, 0.78f, 0.42f),
                ActionGraphDebugState.Cancelled => new Color(0.62f, 0.66f, 0.72f),
                ActionGraphDebugState.Failed => new Color(0.95f, 0.22f, 0.20f),
                _ => baseTitleColor
            };

            float blend = state.Value == ActionGraphDebugState.Started || state.Value == ActionGraphDebugState.Failed
                ? 0.65f
                : 0.32f;
            titleContainer.style.backgroundColor = Color.Lerp(baseTitleColor, debugColor, blend);

            style.borderLeftWidth = 3f;
            style.borderRightWidth = 3f;
            style.borderTopWidth = 3f;
            style.borderBottomWidth = 3f;
            style.borderLeftColor = debugColor;
            style.borderRightColor = debugColor;
            style.borderTopColor = debugColor;
            style.borderBottomColor = debugColor;
        }

        public void AddExecutionOrderControls(Action moveEarlier, Action moveLater)
        {
            var earlierButton = new Button(moveEarlier ?? (() => { }))
            {
                text = "↑",
                tooltip = "Run this node earlier in the Sequence"
            };
            earlierButton.style.width = 22f;
            earlierButton.style.height = 18f;
            earlierButton.SetEnabled(moveEarlier != null);

            var laterButton = new Button(moveLater ?? (() => { }))
            {
                text = "↓",
                tooltip = "Run this node later in the Sequence"
            };
            laterButton.style.width = 22f;
            laterButton.style.height = 18f;
            laterButton.SetEnabled(moveLater != null);

            int insertIndex = Mathf.Max(0, titleContainer.childCount - 1);
            titleContainer.Insert(insertIndex, earlierButton);
            titleContainer.Insert(insertIndex + 1, laterButton);
        }

        public void AddOutputPort(
            string label,
            OutputSlot slot,
            Port.Capacity capacity,
            bool canAddNode,
            Action<Port> trackConnection,
            Func<Port, Vector2, bool> isOverCompatibleInputPort,
            Action<Vector2> addNode)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, typeof(NodeConfig));
            port.portName = label;
            port.userData = slot;

            Vector2 dragStartPosition = Vector2.zero;
            port.RegisterCallback<MouseDownEvent>(evt =>
            {
                dragStartPosition = evt.mousePosition;
                trackConnection?.Invoke(port);
            }, TrickleDown.TrickleDown);

            port.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (!canAddNode)
                    return;

                if ((evt.mousePosition - dragStartPosition).sqrMagnitude < 16f)
                    return;

                if (isOverCompatibleInputPort?.Invoke(port, evt.mousePosition) == true)
                    return;

                addNode(GetScreenPosition(evt.mousePosition));
            }, TrickleDown.TrickleDown);

            var addButton = new Button(() =>
            {
                addNode(GetScreenPosition(port.worldBound.center));
            })
            {
                text = "+",
                tooltip = $"Add {label.ToLowerInvariant()} node"
            };
            addButton.style.width = 22f;
            addButton.style.height = 18f;
            addButton.SetEnabled(canAddNode);
            port.Add(addButton);

            outputContainer.Add(port);
            OutputPorts[slot] = port;
        }

        private static Vector2 GetScreenPosition(Vector2 panelPosition)
        {
            var window = EditorWindow.focusedWindow;
            if (window != null)
                panelPosition += window.position.position;

            return panelPosition;
        }
    }
}

