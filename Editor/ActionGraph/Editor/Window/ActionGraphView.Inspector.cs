using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public partial class ActionGraphView
{
    private void DrawNodeInspector(NodeConfig config)
    {
        EditorGUIUtility.labelWidth = 88f;

        switch (config)
        {
            case FlowNodeConfig flow:
                DrawFlowInspector(flow);
                break;
            case RandomNodeConfig random:
                DrawOrderedChildren(random, random.children, "Choices");
                break;
            case RepeatNodeConfig repeat:
                DrawRepeatInspector(repeat);
                break;
            case LoopNodeConfig loop:
                DrawLoopInspector(loop);
                break;
            case ConditionNodeConfig condition:
                DrawConditionInspector(condition);
                break;
            case ActionNodeConfig action:
                DrawActionInspector(action);
                break;
        }
    }

    private void DrawFlowInspector(FlowNodeConfig flow)
    {
        var newType = (FlowNodeType)EditorGUILayout.EnumPopup("Type", flow.type);
        if (newType != flow.type)
        {
            Undo.RecordObject(_config, "Change Flow Type");
            flow.type = newType;
            MarkDirty();
            QueueRebuild();
        }

        DrawOrderedChildren(flow, flow.children, "Children");
    }

    private void DrawRepeatInspector(RepeatNodeConfig repeat)
    {
        int newCount = Mathf.Max(0, EditorGUILayout.IntField("Count", repeat.count));
        if (newCount != repeat.count)
        {
            Undo.RecordObject(_config, "Change Repeat Count");
            repeat.count = newCount;
            MarkDirty();
        }

        if (repeat.child == null)
            EditorGUILayout.HelpBox("Connect or add one child node.", MessageType.Info);
    }

    private void DrawLoopInspector(LoopNodeConfig loop)
    {
        int newMaxIterations = Mathf.Max(0, EditorGUILayout.IntField("Max Iterations", loop.maxIterations));
        if (newMaxIterations != loop.maxIterations)
        {
            Undo.RecordObject(_config, "Change Loop Max Iterations");
            loop.maxIterations = newMaxIterations;
            MarkDirty();
        }

        var newTiming = (LoopConditionTiming)EditorGUILayout.EnumPopup("Condition Timing", loop.conditionTiming);
        if (newTiming != loop.conditionTiming)
        {
            Undo.RecordObject(_config, "Change Loop Condition Timing");
            loop.conditionTiming = newTiming;
            MarkDirty();
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(loop.condition?.GetType().Name ?? "Select Condition"))
            ShowLoopConditionMenu(loop);

        using (new EditorGUI.DisabledScope(loop.condition == null))
        {
            if (GUILayout.Button("Clear", GUILayout.Width(48f)))
            {
                Undo.RecordObject(_config, "Clear Loop Condition");
                loop.condition = null;
                MarkDirty();
            }
        }

        EditorGUILayout.EndHorizontal();

        if (loop.condition == null)
        {
            EditorGUILayout.HelpBox("Condition is required before this loop can continue.", MessageType.Warning);
        }
        else
        {
            DrawObjectFields(loop.condition, "Edit Loop Condition");
        }

        if (loop.child == null)
            EditorGUILayout.HelpBox("Connect or add one child node.", MessageType.Info);
    }

    private void DrawActionInspector(ActionNodeConfig action)
    {
        EditorGUILayout.BeginHorizontal();

        string label = GetActionNodeDisplayName(action) ?? "Select Action";
        if (GUILayout.Button(label))
            ShowActionMenu(action);

        using (new EditorGUI.DisabledScope(action.dataProvider == null))
        {
            if (GUILayout.Button("Clear", GUILayout.Width(48f)))
            {
                Undo.RecordObject(_config, "Clear Action Node");
                action.dataProvider = null;
                MarkDirty();
                QueueRebuild();
            }
        }

        EditorGUILayout.EndHorizontal();

        if (action.dataProvider == null)
        {
            EditorGUILayout.HelpBox("This action has no data provider.", MessageType.Warning);
            return;
        }

        DrawProviderFields(action.dataProvider);
        DrawProviderWarnings(action.dataProvider);
    }

    private void DrawConditionInspector(ConditionNodeConfig condition)
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(condition.condition?.GetType().Name ?? "Select Condition"))
            ShowConditionMenu(condition);

        using (new EditorGUI.DisabledScope(condition.condition == null))
        {
            if (GUILayout.Button("Clear", GUILayout.Width(48f)))
            {
                Undo.RecordObject(_config, "Clear Condition");
                condition.condition = null;
                MarkDirty();
                QueueRebuild();
            }
        }

        EditorGUILayout.EndHorizontal();

        if (condition.condition == null)
        {
            EditorGUILayout.HelpBox("Condition is required before this node can run.", MessageType.Warning);
            return;
        }

        DrawObjectFields(condition.condition, "Edit Condition");

        if (condition.trueNode == null)
            EditorGUILayout.HelpBox("True branch is empty.", MessageType.Info);

        if (condition.falseNode == null)
            EditorGUILayout.HelpBox("False branch is empty.", MessageType.Info);
    }

    private void DrawOrderedChildren(NodeConfig owner, List<NodeConfig> children, string label)
    {
        EditorGUILayout.Space(4f);
        int childCount = children != null ? children.Count(child => child != null) : 0;
        bool expanded = owner == null || !owner.editorChildrenListCollapsed;
        bool newExpanded = EditorGUILayout.Foldout(expanded, $"{label} ({childCount})", true);

        if (newExpanded != expanded && owner != null)
        {
            Undo.RecordObject(_config, newExpanded ? $"Expand {label}" : $"Collapse {label}");
            owner.editorChildrenListCollapsed = !newExpanded;
            MarkDirty();
        }

        if (!newExpanded)
            return;

        if (children == null || childCount == 0)
        {
            EditorGUILayout.HelpBox($"{label} list is empty.", MessageType.Info);
            return;
        }

        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child == null)
                continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{i + 1}. {GetNodeTitle(child)}", GUILayout.MinWidth(130f));

            using (new EditorGUI.DisabledScope(i == 0))
            {
                if (GUILayout.Button("Up", GUILayout.Width(34f)))
                {
                    Undo.RecordObject(_config, "Reorder Action Graph Node");
                    (children[i - 1], children[i]) = (children[i], children[i - 1]);
                    MarkDirty();
                    QueueRebuild();
                }
            }

            using (new EditorGUI.DisabledScope(i == children.Count - 1))
            {
                if (GUILayout.Button("Down", GUILayout.Width(50f)))
                {
                    Undo.RecordObject(_config, "Reorder Action Graph Node");
                    (children[i + 1], children[i]) = (children[i], children[i + 1]);
                    MarkDirty();
                    QueueRebuild();
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawProviderFields(ActionDataProvider dataProvider)
    {
        if (dataProvider.EnsureDefaultData())
            MarkDirty();

        var serializedConfig = new SerializedObject(_config);
        serializedConfig.Update();

        var providerProperty = FindManagedReferenceProperty(serializedConfig, dataProvider);
        if (providerProperty == null)
        {
            EditorGUILayout.HelpBox("Unable to draw provider fields.", MessageType.Warning);
            return;
        }

        if (!dataProvider.HasConfigurableData)
        {
            EditorGUILayout.HelpBox("This action has no configurable data.", MessageType.Info);
            return;
        }

        EditorGUI.BeginChangeCheck();
        if (!DrawActionDataProviderFields(providerProperty, dataProvider is IIndexedActionDataProvider))
            EditorGUILayout.PropertyField(providerProperty, true);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_config, "Edit Action Node Data");
            if (serializedConfig.ApplyModifiedProperties())
                MarkDirty();
        }
    }

    private static bool DrawActionDataProviderFields(SerializedProperty providerProperty, bool usesIndexedData)
    {
        SerializedProperty useSingleValueProperty = providerProperty.FindPropertyRelative("useSingleValue");
        SerializedProperty dataProperty = providerProperty.FindPropertyRelative("data");

        if (useSingleValueProperty == null || dataProperty == null || !dataProperty.isArray)
            return false;

        if (dataProperty.arraySize == 0)
            dataProperty.arraySize = 1;

        dataProperty.isExpanded = true;
        EditorGUILayout.PropertyField(dataProperty, new GUIContent("Data"), true);

        var useFirstLabel = new GUIContent(
            "Use First Data Only",
            usesIndexedData
                ? "On: always use Data[0]. Off: use CurrentAttackIndex and clamp to the last data item."
                : "On: always use Data[0]. Off: advance through the data array with the generic selector.");
        EditorGUILayout.PropertyField(useSingleValueProperty, useFirstLabel);

        string selectionMode = useSingleValueProperty.boolValue
            ? "Data[0]"
            : usesIndexedData ? "Current Attack Index" : "Sequential";
        EditorGUILayout.LabelField("Selection", selectionMode, EditorStyles.miniLabel);

        return true;
    }

    private static SerializedProperty FindManagedReferenceProperty(SerializedObject serializedObject, object value)
    {
        var iterator = serializedObject.GetIterator();

        while (iterator.Next(true))
        {
            if (iterator.propertyType == SerializedPropertyType.ManagedReference &&
                ReferenceEquals(iterator.managedReferenceValue, value))
            {
                return iterator.Copy();
            }
        }

        return null;
    }

    private void DrawProviderWarnings(ActionDataProvider dataProvider)
    {
        if (!dataProvider.HasConfigurableData)
            return;

        var method = dataProvider.GetType().GetMethod("GetAllData", BindingFlags.Public | BindingFlags.Instance);
        if (method?.Invoke(dataProvider, null) is Array data && data.Length == 0)
            EditorGUILayout.HelpBox("Provider data is empty. This action will throw when executed.", MessageType.Warning);
    }

    private void DrawObjectFields(object target, string undoName)
    {
        var fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.IsPrivate && field.GetCustomAttributes(typeof(SerializeField), true).Length == 0)
                continue;

            object oldValue = field.GetValue(target);
            object newValue = DrawField(field, oldValue);

            if (!Equals(oldValue, newValue))
            {
                Undo.RecordObject(_config, undoName);
                field.SetValue(target, newValue);
                MarkDirty();
            }
        }
    }

    private static object DrawField(FieldInfo field, object value)
    {
        Type fieldType = field.FieldType;
        string label = ObjectNames.NicifyVariableName(field.Name);

        if (fieldType == typeof(int))
            return EditorGUILayout.IntField(label, value != null ? (int)value : 0);

        if (fieldType == typeof(float))
            return EditorGUILayout.FloatField(label, value != null ? (float)value : 0f);

        if (fieldType == typeof(bool))
            return EditorGUILayout.Toggle(label, value != null && (bool)value);

        if (fieldType == typeof(string))
            return EditorGUILayout.TextField(label, value != null ? (string)value : string.Empty);

        if (fieldType.IsEnum)
            return EditorGUILayout.EnumPopup(label, value as Enum ?? (Enum)Activator.CreateInstance(fieldType));

        if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            return EditorGUILayout.ObjectField(label, value as UnityEngine.Object, fieldType, true);

        EditorGUILayout.LabelField(label, $"Unsupported: {fieldType.Name}");
        return value;
    }
}

