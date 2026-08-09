public interface IActionGraphExecutionController
{
    bool IsExecuting { get; }
    void CancelExecution();
}
