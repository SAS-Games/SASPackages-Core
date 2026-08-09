using System.Threading;
using System.Threading.Tasks;

public interface IActionNode
{
    void Init(ActionContext context);
    Task ExecuteAsync(ActionContext context, CancellationToken token);
     void Reset();
}

public interface IPresenter<in T>
{
    void Init(T data);
}