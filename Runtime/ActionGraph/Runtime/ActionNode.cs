using System.Threading;
using System.Threading.Tasks;

public abstract class ActionNode<T> : IActionNode
{
    protected readonly ActionDataProvider<T> _dataProvider;
    protected IDataSelector<T> _selector;

    protected ActionNode(ActionDataProvider<T> dataProvider)
    {
        _dataProvider = dataProvider;
        _selector = dataProvider.CreateTypedSelector();
    }
    
    public virtual void Init(ActionContext context)
    {
    }

    public abstract Task ExecuteAsync(ActionContext context, CancellationToken token);
    
    public virtual void Reset()
    {
        _selector?.Reset();
    }
}
