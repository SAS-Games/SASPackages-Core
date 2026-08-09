using System;
using System.Threading;
using System.Threading.Tasks;

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
    public SubGraphNode(ActionDataProvider<SubGraphData> dataProvider) : base(dataProvider)
    {
    }

    public override async Task ExecuteAsync(ActionContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var data = _selector.GetNext();
        if (data == null || data.graph == null || data.graph.root == null)
            return;

        var graph = new ExecutionGraph(data.graph.root);
        graph.Initialize(context);
        await graph.ExecuteAsync(context, token);
    }
}

