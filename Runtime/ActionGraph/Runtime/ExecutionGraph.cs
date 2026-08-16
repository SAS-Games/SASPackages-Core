using System.Threading;
using UnityEngine;

public class ExecutionGraph
{
    private readonly IActionNode _root;

    public ExecutionGraph(NodeConfig rootConfig)
    {
        _root = ExecutionGraphFactory.Build(rootConfig);
    }
    
    public void Initialize(ActionContext context)
    {
        _root.Init(context);
    }

    public async Awaitable ExecuteAsync(ActionContext context, CancellationToken token)
    {
        await _root.ExecuteAsync(context, token);
    }

    public void Reset()
    {
        _root.Reset();
    }
}
