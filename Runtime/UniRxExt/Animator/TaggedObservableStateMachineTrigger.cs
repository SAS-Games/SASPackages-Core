#if UniRxEnabled
using UnityEngine;
using UniRx.Triggers;

public class TaggedObservableStateMachineTrigger : ObservableStateMachineTrigger
{
    [Tooltip("Stable application-facing name used to find this Animator state trigger.")]
    public string stateName;

    [Tooltip("When enabled, leaving the state before the completion threshold is treated as an interruption rather than completion.")]
    public bool requireCompletionThreshold = true;

    [Tooltip("Minimum normalized state time required for an exit to count as completion.")]
    [Range(0f, 1f)]
    public float minimumNormalizedTime = 0.9f;

    /// <summary>
    /// Returns whether an Animator state exit represents a completed state rather than an
    /// early transition or interruption.
    /// </summary>
    public bool IsCompleted(AnimatorStateInfo stateInfo)
    {
        if (!requireCompletionThreshold)
            return true;

        return stateInfo.normalizedTime >= Mathf.Clamp01(minimumNormalizedTime);
    }
}
#endif
