using System;
using System.Threading;
using UnityEngine;

[Serializable]
public class WaitSecondsData
{
    public float duration = 0.1f;
}

[NodeBinding(typeof(WaitSecondsNode))]
[Serializable]
public class WaitSecondsNodeProvider : ActionDataProvider<WaitSecondsData>
{
}

public class WaitSecondsNode : ActionNode<WaitSecondsData>
{
    public WaitSecondsNode(ActionDataProvider<WaitSecondsData> dataProvider) : base(dataProvider)
    {
    }

    public override async Awaitable ExecuteAsync(ActionContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var data = _selector.GetNext();
        float duration = data != null ? Math.Max(0f, data.duration) : 0f;

        if (duration <= 0f)
            return;

        await Awaitable.WaitForSecondsAsync(duration, token);
    }
}
