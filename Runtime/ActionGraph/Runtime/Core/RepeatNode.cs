using System.Threading;
using UnityEngine;

public class RepeatNode : IActionNode
{
    private readonly IActionNode _child;
    private readonly int _count;

    public RepeatNode(IActionNode child, int count)
    {
        _child = child;
        _count = count;
    }
    
    public void Init(ActionContext context)
    {
        _child?.Init(context);
    }

    public async Awaitable ExecuteAsync(ActionContext context, CancellationToken token)
    {
        for (int i = 0; i < _count; i++)
        {
            token.ThrowIfCancellationRequested();

            if (_child != null)
                await _child.ExecuteAsync(context, token);
        }
    }

    public void Reset()
    {
        _child?.Reset();
    }
}
