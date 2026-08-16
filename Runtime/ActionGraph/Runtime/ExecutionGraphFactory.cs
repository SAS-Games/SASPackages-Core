using System;
using System.Linq;
#if UNITY_EDITOR
using System.Threading;
using UnityEngine;
#endif

public static class ExecutionGraphFactory
{
    public static IActionNode Build(NodeConfig config)
    {
        IActionNode node;

        switch (config)
        {
            case FlowNodeConfig flow:
                node = BuildFlow(flow);
                break;

            case ActionNodeConfig action:
                node = ActionNodeFactory.Create(action.dataProvider);
                break;
            
            case ConditionNodeConfig cond:
                node = BuildCondition(cond);
                break;
            
            case RepeatNodeConfig repeat:
                node = BuildRepeat(repeat);
                break;

            case LoopNodeConfig loop:
                node = BuildLoop(loop);
                break;

            case RandomNodeConfig random:
                node = BuildRandom(random);
                break;
            
            default:
                throw new Exception("Unknown node config");
        }

#if UNITY_EDITOR
        return new ActionGraphDebugNode(config, node);
#else
        return node;
#endif
    }

    private static IActionNode BuildFlow(FlowNodeConfig flow)
    {
        var children = flow.children
            .Select(Build)
            .ToList();

        return flow.type switch
        {
            FlowNodeType.Sequence => new SequenceNode(children),
            FlowNodeType.Parallel => new ParallelNode(children),
            _ => throw new Exception("Unknown flow type")
        };
    }
    
    private static IActionNode BuildCondition(ConditionNodeConfig cond)
    {
        var trueNode = cond.trueNode != null ? Build(cond.trueNode) : null;
        var falseNode = cond.falseNode != null ? Build(cond.falseNode) : null;

        return new IfNode(cond.condition, trueNode, falseNode);
    }
    
    private static IActionNode BuildRepeat(RepeatNodeConfig repeat)
    {
        var child = repeat.child != null ? Build(repeat.child) : null;

        return new RepeatNode(child, repeat.count);
    }

    private static IActionNode BuildLoop(LoopNodeConfig loop)
    {
        var child = loop.child != null ? Build(loop.child) : null;

        return new LoopNode(child, loop.condition, loop.maxIterations, loop.conditionTiming);
    }
    
    private static IActionNode BuildRandom(RandomNodeConfig random)
    {
        var children = random.children
            .Select(Build)
            .ToList();

        return new RandomNode(children);
    }
}

#if UNITY_EDITOR
public enum ActionGraphDebugState
{
    Started,
    Completed,
    Cancelled,
    Failed
}

public readonly struct ActionGraphDebugEvent
{
    public readonly NodeConfig Node;
    public readonly ActionGraphDebugState State;
    public readonly double Timestamp;
    public readonly Exception Exception;

    public ActionGraphDebugEvent(
        NodeConfig node,
        ActionGraphDebugState state,
        Exception exception = null)
    {
        Node = node;
        State = state;
        Timestamp = Time.realtimeSinceStartupAsDouble;
        Exception = exception;
    }
}

public static class ActionGraphDebug
{
    public static event Action<ActionGraphDebugEvent> NodeStateChanged;

    internal static bool HasSubscribers => NodeStateChanged != null;

    internal static void Notify(NodeConfig node, ActionGraphDebugState state, Exception exception = null)
    {
        NodeStateChanged?.Invoke(new ActionGraphDebugEvent(node, state, exception));
    }
}

internal sealed class ActionGraphDebugNode : IActionNode
{
    private readonly NodeConfig _config;
    private readonly IActionNode _inner;

    public ActionGraphDebugNode(NodeConfig config, IActionNode inner)
    {
        _config = config;
        _inner = inner;
    }

    public void Init(ActionContext context)
    {
        _inner.Init(context);
    }

    public Awaitable ExecuteAsync(ActionContext context, CancellationToken token)
    {
        return ActionGraphDebug.HasSubscribers
            ? ExecuteWithDebugAsync(context, token)
            : _inner.ExecuteAsync(context, token);
    }

    public void Reset()
    {
        _inner.Reset();
    }

    private async Awaitable ExecuteWithDebugAsync(ActionContext context, CancellationToken token)
    {
        ActionGraphDebug.Notify(_config, ActionGraphDebugState.Started);

        try
        {
            await _inner.ExecuteAsync(context, token);
            ActionGraphDebug.Notify(_config, ActionGraphDebugState.Completed);
        }
        catch (OperationCanceledException)
        {
            ActionGraphDebug.Notify(_config, ActionGraphDebugState.Cancelled);
            throw;
        }
        catch (Exception ex)
        {
            ActionGraphDebug.Notify(_config, ActionGraphDebugState.Failed, ex);
            throw;
        }
    }
}
#endif
