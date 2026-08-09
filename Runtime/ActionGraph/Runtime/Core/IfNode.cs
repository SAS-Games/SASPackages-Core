using System;
using System.Threading;
using System.Threading.Tasks;

public interface ICondition
{
    bool Evaluate(ActionContext context);
}

public class IfNode : IActionNode
{
    private readonly ICondition _condition;
    private readonly IActionNode _trueNode;
    private readonly IActionNode _falseNode;

    public IfNode(ICondition condition, IActionNode trueNode, IActionNode falseNode = null)
    {
        _condition = condition;
        _trueNode = trueNode;
        _falseNode = falseNode;
    }

    public void Init(ActionContext context)
    {
        (_trueNode as IActionNode)?.Init(context);
        (_falseNode as IActionNode)?.Init(context);
    }

    public async Task ExecuteAsync(ActionContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        if (_condition == null)
            throw new InvalidOperationException("IfNode cannot execute without a condition.");

        if (_condition.Evaluate(context))
        {
            if (_trueNode != null)
                await _trueNode.ExecuteAsync(context, token);
        }
        else
        {
            if (_falseNode != null)
                await _falseNode.ExecuteAsync(context, token);
        }
    }

    public void Reset()
    {
        _trueNode?.Reset();
        _falseNode?.Reset();
    }
}
