using System.Threading;
using UnityEngine;

public class LoopNode : IActionNode
{
    private readonly IActionNode _child;
    private readonly ICondition _condition;
    private readonly int _maxIterations;
    private readonly LoopConditionTiming _conditionTiming;

    public LoopNode(IActionNode child, ICondition condition, int maxIterations, LoopConditionTiming conditionTiming)
    {
        _child = child;
        _condition = condition;
        _maxIterations = maxIterations;
        _conditionTiming = conditionTiming;
    }

    public void Init(ActionContext context)
    {
        _child?.Init(context);
    }

    public async Awaitable ExecuteAsync(ActionContext context, CancellationToken token)
    {
        for (int i = 0; i < _maxIterations; i++)
        {
            token.ThrowIfCancellationRequested();

            if (_conditionTiming == LoopConditionTiming.BeforeChild && !Evaluate(context))
                return;

            if (_child != null)
                await _child.ExecuteAsync(context, token);

            if (_conditionTiming == LoopConditionTiming.AfterChild && !Evaluate(context))
                return;
        }
    }

    public void Reset()
    {
        _child?.Reset();
    }

    private bool Evaluate(ActionContext context)
    {
        return _condition != null && _condition.Evaluate(context);
    }
}
