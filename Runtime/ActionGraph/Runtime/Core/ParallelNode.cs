using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class ParallelNode : IActionNode
{
    private readonly List<IActionNode> _nodes;

    public ParallelNode(List<IActionNode> nodes)
    {
        _nodes = nodes;
    }

    public void Init(ActionContext context)
    {
        for (int i = 0; i < _nodes.Count; i++)
            _nodes[i].Init(context);
    }

    public async Task ExecuteAsync(ActionContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        int count = _nodes.Count;
        if (count == 0) return;

        var tasks = new Task[count];

        for (int i = 0; i < count; i++)
            tasks[i] = ExecuteChildAsync(_nodes[i], context, token);

        await Task.WhenAll(tasks);
    }

    private static async Task ExecuteChildAsync(IActionNode node, ActionContext context, CancellationToken token)
    {
        await node.ExecuteAsync(context, token);
    }

    public void Reset()
    {
        for (int i = 0; i < _nodes.Count; i++)
            _nodes[i].Reset();
    }
}
