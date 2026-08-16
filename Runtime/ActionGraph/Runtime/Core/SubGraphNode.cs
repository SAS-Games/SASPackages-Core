using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;

[Serializable]
public class SubGraphData
{
    public ActionGraphAsset graph;
}

[NodeBinding(typeof(SubGraphNode))]
[Serializable]
public class SubGraphNodeProvider : ActionDataProvider<SubGraphData>
{
}

public class SubGraphNode : ActionNode<SubGraphData>
{
    [ThreadStatic] private static List<ActionGraphAsset> _initializationStack;

    private readonly Dictionary<ActionGraphAsset, ExecutionGraph> _graphs = new();

    public SubGraphNode(ActionDataProvider<SubGraphData> dataProvider) : base(dataProvider)
    {
    }

    public override void Init(ActionContext context)
    {
        base.Init(context);
        _graphs.Clear();

        SubGraphData[] allData = _dataProvider.GetAllData();
        if (allData == null)
            return;

        for (int i = 0; i < allData.Length; i++)
        {
            ActionGraphAsset asset = allData[i]?.graph;
            if (asset != null && asset.root != null)
                GetOrBuildGraph(asset, context);
        }
    }

    public override async Awaitable ExecuteAsync(ActionContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        SubGraphData data = _selector.GetNext();
        if (data == null || data.graph == null || data.graph.root == null)
            return;

        ExecutionGraph graph = GetOrBuildGraph(data.graph, context);
        graph.Reset();
        await graph.ExecuteAsync(context, token);
    }

    public override void Reset()
    {
        base.Reset();

        foreach (ExecutionGraph graph in _graphs.Values)
            graph.Reset();
    }

    private ExecutionGraph GetOrBuildGraph(ActionGraphAsset asset, ActionContext context)
    {
        if (_graphs.TryGetValue(asset, out ExecutionGraph graph))
            return graph;

        graph = BuildGraph(asset, context);
        _graphs.Add(asset, graph);
        return graph;
    }

    private static ExecutionGraph BuildGraph(ActionGraphAsset asset, ActionContext context)
    {
        List<ActionGraphAsset> stack = _initializationStack ??= new List<ActionGraphAsset>();
        int recursiveIndex = stack.IndexOf(asset);
        if (recursiveIndex >= 0)
            throw CreateRecursiveReferenceException(stack, recursiveIndex, asset);

        stack.Add(asset);
        try
        {
            var graph = new ExecutionGraph(asset.root);
            graph.Initialize(context);
            return graph;
        }
        finally
        {
            stack.RemoveAt(stack.Count - 1);
            if (stack.Count == 0)
                _initializationStack = null;
        }
    }

    private static InvalidOperationException CreateRecursiveReferenceException(
        List<ActionGraphAsset> stack,
        int recursiveIndex,
        ActionGraphAsset repeatedAsset)
    {
        var path = new StringBuilder();
        for (int i = recursiveIndex; i < stack.Count; i++)
        {
            if (path.Length > 0)
                path.Append(" -> ");

            path.Append(GetAssetName(stack[i]));
        }

        if (path.Length > 0)
            path.Append(" -> ");

        path.Append(GetAssetName(repeatedAsset));
        return new InvalidOperationException($"Recursive ActionGraph SubGraph reference detected: {path}");
    }

    private static string GetAssetName(ActionGraphAsset asset)
    {
        return asset != null && !string.IsNullOrEmpty(asset.name)
            ? asset.name
            : "Unnamed ActionGraph";
    }
}
