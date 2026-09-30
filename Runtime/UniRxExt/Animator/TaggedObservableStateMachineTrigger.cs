#if UniRxEnabled
using System;
using UniRx;
using UnityEngine;
using UniRx.Triggers;

public class TaggedObservableStateMachineTrigger : ObservableStateMachineTrigger
{
    private Subject<OnStateInfo> completed;
    private Subject<OnStateInfo> interrupted;
    private bool completionPublished;

    [Tooltip("Stable application-facing name used to find this Animator state trigger.")]
    public string stateName;

    [Tooltip("When enabled, reaching the threshold completes the state; exiting earlier is treated as an interruption.")]
    public bool requireCompletionThreshold = true;

    [Tooltip("Minimum normalized state time required for an exit to count as completion.")]
    [Range(0f, 1f)]
    public float minimumNormalizedTime = 0.9f;

    /// <summary>
    /// Returns whether the Animator state has reached its configured completion threshold.
    /// </summary>
    public bool IsCompleted(AnimatorStateInfo stateInfo)
    {
        if (!requireCompletionThreshold)
            return true;

        return stateInfo.normalizedTime >= Mathf.Clamp01(minimumNormalizedTime);
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        completionPublished = false;
        base.OnStateEnter(animator, stateInfo, layerIndex);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);

        if (requireCompletionThreshold && IsCompleted(stateInfo))
            PublishCompletion(animator, stateInfo, layerIndex);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);

        if (completionPublished)
            return;

        if (IsCompleted(stateInfo))
        {
            PublishCompletion(animator, stateInfo, layerIndex);
            return;
        }

        interrupted?.OnNext(new OnStateInfo(animator, stateInfo, layerIndex));
    }

    /// <summary>
    /// Observes completion when the state first reaches its configured threshold. If an
    /// update skips across the threshold, completion is emitted as the state exits.
    /// </summary>
    public IObservable<OnStateInfo> OnCompletedAsObservable()
    {
        return completed ??= new Subject<OnStateInfo>();
    }

    /// <summary>Observes exits that occur before the configured completion threshold.</summary>
    public IObservable<OnStateInfo> OnInterruptedAsObservable()
    {
        return interrupted ??= new Subject<OnStateInfo>();
    }

    private void PublishCompletion(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (completionPublished)
            return;

        completionPublished = true;
        completed?.OnNext(new OnStateInfo(animator, stateInfo, layerIndex));
    }
}
#endif
