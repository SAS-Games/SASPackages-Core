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
        public readonly NodeConfig Config;
        public readonly Dictionary<OutputSlot, Port> OutputPorts = new();
        public Port InputPort { get; }

        public ActionGraphNodeView(
            NodeConfig config,
            string titleText,
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

