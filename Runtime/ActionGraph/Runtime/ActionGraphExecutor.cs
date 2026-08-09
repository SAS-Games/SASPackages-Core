using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class ActionGraphExecutor : IActionGraphExecutionController, IDisposable
{
    private ExecutionGraph _graph;
    private CancellationTokenSource _cts;
    private Task _currentExecution;

    public bool HasGraph => _graph != null;
    public bool IsExecuting => _currentExecution != null && !_currentExecution.IsCompleted;

    public bool Build(ActionGraphAsset graphAsset, ActionContext context)
    {
        CancelExecution();

        if (graphAsset == null || graphAsset.root == null)
        {
            _graph = null;
            return false;
        }

        _graph = new ExecutionGraph(graphAsset.root);
        _graph.Initialize(context);
        return true;
    }

    public async Task ExecuteAsync(ActionContext context)
    {
        if (_graph == null)
            throw new InvalidOperationException("Cannot execute Action Graph because no graph has been built.");

        CancelTokenOnly();

        CancellationTokenSource cts = new CancellationTokenSource();
        _cts = cts;

        try
        {
            Task execution = _graph.ExecuteAsync(context, cts.Token);
            _currentExecution = execution;
            await execution;
        }
        finally
        {
            if (ReferenceEquals(_cts, cts))
            {
                _cts.Dispose();
                _cts = null;
                _currentExecution = null;
            }
        }
    }

    public void Reset()
    {
        _graph?.Reset();
    }

    public void CancelExecution()
    {
        CancelTokenOnly();
        _currentExecution = null;
    }

    public void Dispose()
    {
        CancelExecution();
    }

    private void CancelTokenOnly()
    {
        if (_cts == null)
            return;

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }
}
