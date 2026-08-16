using System;
using System.Threading;
using UnityEngine;

[Serializable]
public class AnimatorSetBoolData
{
    public string parameterName = "Attack";
    public bool value;
}

[NodeBinding(typeof(AnimatorSetBoolNode))]
[Serializable]
public class AnimatorSetBoolProvider : ActionDataProvider<AnimatorSetBoolData>
{
}

[ActionNodeMenu("Animation/Set Bool")]
public class AnimatorSetBoolNode : ActionNode<AnimatorSetBoolData>
{
    public AnimatorSetBoolNode(ActionDataProvider<AnimatorSetBoolData> dataProvider) : base(dataProvider)
    {
    }

    public override async Awaitable ExecuteAsync(ActionContext context, CancellationToken token)
    {
        await Awaitable.MainThreadAsync();
        token.ThrowIfCancellationRequested();

        AnimatorSetBoolData data = _selector.GetNext();
        if (data == null || string.IsNullOrEmpty(data.parameterName) || context.Owner == null)
            return;

        Animator animator = context.Owner.GetComponentInParent<Animator>();
        if (animator == null)
            animator = context.Owner.GetComponentInChildren<Animator>();

        if (animator != null)
            animator.SetBool(data.parameterName, data.value);

    }
}
