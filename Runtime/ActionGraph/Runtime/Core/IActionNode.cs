using System.Threading;
using UnityEngine;

public interface IActionNode
{
    void Init(ActionContext context);
    Awaitable ExecuteAsync(ActionContext context, CancellationToken token);
     void Reset();
}

public interface IPresenter<in T>
{
    void Init(T data);
}
