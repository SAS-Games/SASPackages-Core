using System;
using System.Threading;
using UnityEngine;

[Serializable]
public sealed class AnimatorSetIntegerData
{
    public string parameterName = "Value";
    public int value;
}

[NodeBinding(typeof(AnimatorSetIntegerNode))]
[Serializable]
public sealed class AnimatorSetIntegerProvider : ActionDataProvider<AnimatorSetIntegerData>
{
}

[ActionNodeMenu("Animation/Set Integer")]
public sealed class AnimatorSetIntegerNode : ActionNode<AnimatorSetIntegerData>
{
    public AnimatorSetIntegerNode(ActionDataProvider<AnimatorSetIntegerData> dataProvider)
        : base(dataProvider)
    {
    }

    public override async Awaitable ExecuteAsync(ActionContext context, CancellationToken token)
    {
        await Awaitable.MainThreadAsync();
        token.ThrowIfCancellationRequested();

        AnimatorSetIntegerData data = _selector.GetNext();
        if (data == null || string.IsNullOrWhiteSpace(data.parameterName) || context?.Owner == null)
            return;

        Animator animator = context.Owner.GetComponentInParent<Animator>();
        if (animator == null)
            animator = context.Owner.GetComponentInChildren<Animator>(true);

        if (animator != null)
            animator.SetInteger(data.parameterName, data.value);
    }
}
