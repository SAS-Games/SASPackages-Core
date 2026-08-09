using System;
using System.Linq;

public static class ExecutionGraphFactory
{
    public static IActionNode Build(NodeConfig config)
    {
        switch (config)
        {
            case FlowNodeConfig flow:
                return BuildFlow(flow);

            case ActionNodeConfig action:
                return ActionNodeFactory.Create(action.dataProvider);
            
            case ConditionNodeConfig cond:
                return BuildCondition(cond);
            
            case RepeatNodeConfig repeat:
                return BuildRepeat(repeat);

            case LoopNodeConfig loop:
                return BuildLoop(loop);

            case RandomNodeConfig random:
                return BuildRandom(random);
            
            default:
                throw new Exception("Unknown node config");
        }
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
