using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class WaitUntilConditionData
{
    [SerializeReference] public ICondition condition;
    public float timeoutSeconds = -1f;
    public bool throwOnTimeout;
}

[NodeBinding(typeof(WaitUntilConditionNode))]
[Serializable]
public class WaitUntilConditionNodeProvider : ActionDataProvider<WaitUntilConditionData>
{
}

public class WaitUntilConditionNode : ActionNode<WaitUntilConditionData>
{
    public WaitUntilConditionNode(ActionDataProvider<WaitUntilConditionData> dataProvider) : base(dataProvider)
    {
    }

    public override async Task ExecuteAsync(ActionContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var data = _selector.GetNext();
        if (data == null || data.condition == null)
            throw new InvalidOperationException("WaitUntilConditionNode requires a condition.");

        float elapsed = 0f;

        while (!data.condition.Evaluate(context))
        {
            token.ThrowIfCancellationRequested();

            if (data.timeoutSeconds >= 0f && elapsed >= data.timeoutSeconds)
            {
                if (data.throwOnTimeout)
                    throw new TimeoutException("WaitUntilConditionNode timed out.");

                return;
            }

            await Awaitable.NextFrameAsync();
            elapsed += Time.deltaTime;
        }
    }
}
