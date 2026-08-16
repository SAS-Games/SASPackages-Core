using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using UnityEngine;

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

    public async Awaitable ExecuteAsync(ActionContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        int count = _nodes.Count;
        if (count == 0) return;

        Awaitable[] executions = ArrayPool<Awaitable>.Shared.Rent(count);
        int startedCount = 0;
        Exception firstException = null;

        try
        {
            for (int i = 0; i < count; i++)
            {
                try
                {
                    Awaitable execution = _nodes[i].ExecuteAsync(context, token);
                    executions[startedCount] = execution;
                    startedCount++;
                }
                catch (Exception ex)
                {
                    firstException ??= ex;
                }
            }

            // All branches start before the first await. Await every pooled
            // handle exactly once, even if another branch fails or is cancelled.
            for (int i = 0; i < startedCount; i++)
            {
                try
                {
                    await executions[i];
                }
                catch (Exception ex)
                {
                    firstException ??= ex;
                }
            }

            if (firstException != null)
                ExceptionDispatchInfo.Capture(firstException).Throw();
        }
        finally
        {
            // Awaitable instances are pooled by Unity. Do not retain references
            // to consumed handles inside the shared array pool.
            ArrayPool<Awaitable>.Shared.Return(executions, clearArray: true);
        }
    }

    public void Reset()
    {
        for (int i = 0; i < _nodes.Count; i++)
            _nodes[i].Reset();
    }
}
