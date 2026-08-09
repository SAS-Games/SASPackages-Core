using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class RandomNode : IActionNode
{
    private readonly List<IActionNode> _children;

    public RandomNode(List<IActionNode> children)
    {
        _children = children;
    }
    
    public void Init(ActionContext context)
    {
        if (_children == null) return;

        for (int i = 0; i < _children.Count; i++)
            _children[i]?.Init(context);
    }
    
    public async Task ExecuteAsync(ActionContext context, CancellationToken token)
    {
        if (_children == null || _children.Count == 0)
            return;

        int index = Random.Range(0, _children.Count);

        await _children[index].ExecuteAsync(context, token);
    }

    public void Reset()
    {
        foreach (var c in _children)
            c.Reset();
    }
}