using System;
using System.Threading;
using UnityEngine;

[Serializable]
public class WaitFramesData
{
    public int frameCount = 1;
}

[NodeBinding(typeof(WaitFramesNode))]
[Serializable]
public class WaitFramesNodeProvider : ActionDataProvider<WaitFramesData>
{
}

public class WaitFramesNode : ActionNode<WaitFramesData>
{
    public WaitFramesNode(ActionDataProvider<WaitFramesData> dataProvider) : base(dataProvider)
    {
    }

    public override async Awaitable ExecuteAsync(ActionContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var data = _selector.GetNext();
        int frameCount = data != null ? Mathf.Max(0, data.frameCount) : 0;

        for (int i = 0; i < frameCount; i++)
        {
            token.ThrowIfCancellationRequested();
            await Awaitable.NextFrameAsync(token);
        }
    }
}
