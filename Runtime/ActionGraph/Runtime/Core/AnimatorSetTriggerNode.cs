using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class AnimatorSetTriggerData
{
    public string parameterName = "Attack";
    [Tooltip("Optional triggers to clear before setting this trigger.")]
    public string[] resetBeforeSet;
}

[NodeBinding(typeof(AnimatorSetTriggerNode))]
[Serializable]
public class AnimatorSetTriggerProvider : ActionDataProvider<AnimatorSetTriggerData>
{
}

[ActionNodeMenu("Animation/Set Trigger")]
public class AnimatorSetTriggerNode : ActionNode<AnimatorSetTriggerData>
{
    public AnimatorSetTriggerNode(ActionDataProvider<AnimatorSetTriggerData> dataProvider) : base(dataProvider)
    {
    }

    public override Task ExecuteAsync(ActionContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        AnimatorSetTriggerData data = _selector.GetNext();
        if (data == null || string.IsNullOrEmpty(data.parameterName) || context.Owner == null)
            return Task.CompletedTask;

        Animator animator = context.Owner.GetComponentInParent<Animator>();
        if (animator == null)
            animator = context.Owner.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            if (data.resetBeforeSet != null)
            {
                foreach (string triggerName in data.resetBeforeSet)
                {
                    if (!string.IsNullOrEmpty(triggerName))
                        animator.ResetTrigger(triggerName);
                }
            }

            animator.SetTrigger(data.parameterName);
        }

        return Task.CompletedTask;
    }
}
