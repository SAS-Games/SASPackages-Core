using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class SequenceNode : IActionNode
{
    private List<IActionNode> _nodes;

    public SequenceNode(List<IActionNode> nodes)
    {
        _nodes = nodes;
    }
    
    public void Init(ActionContext context)
    {
        foreach (var c in _nodes)
            c.Init(context);
    }

    public async Task ExecuteAsync(ActionContext context, CancellationToken token)
    {
        for (int i = 0; i < _nodes.Count; i++)
        {
            token.ThrowIfCancellationRequested();

            var node = _nodes[i];
            await node.ExecuteAsync(context, token);
        }
    }
    
    public void Reset()
    {
        foreach (var child in _nodes)
            child.Reset();
    }
}
