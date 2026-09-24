#if UniRxEnabled
using System;
using UniRx;
using UnityEngine;
using UniRx.Triggers;

[Serializable]
public sealed class NormalizedAnimationCue
{
    public string cueName;

    [Range(0f, 1f)]
    public float normalizedTime = 0.5f;
}

public readonly struct AnimationCueStateInfo
{
    public readonly string CueName;
    public readonly ObservableStateMachineTrigger.OnStateInfo State;

    public AnimationCueStateInfo(string cueName, ObservableStateMachineTrigger.OnStateInfo state)
    {
        CueName = cueName;
        State = state;
    }
}

public sealed class CombatAnimationCueStateMachineTrigger : TaggedObservableStateMachineTrigger
{
    private Subject<AnimationCueStateInfo> cuePublished;
    private bool[] publishedCues;

    [Tooltip("Named graph-facing cues emitted once when this state reaches each normalized time.")]
    public NormalizedAnimationCue[] cues = Array.Empty<NormalizedAnimationCue>();

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ResetCues();
        base.OnStateEnter(animator, stateInfo, layerIndex);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        PublishReachedCues(animator, stateInfo, layerIndex);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PublishReachedCues(animator, stateInfo, layerIndex);
        base.OnStateExit(animator, stateInfo, layerIndex);
    }

    public IObservable<AnimationCueStateInfo> OnCueAsObservable(string cueName)
    {
        if (string.IsNullOrWhiteSpace(cueName))
            throw new ArgumentException("An animation cue name is required.", nameof(cueName));

        return (cuePublished ??= new Subject<AnimationCueStateInfo>())
            .Where(cue => string.Equals(cue.CueName, cueName, StringComparison.Ordinal));
    }

    private void ResetCues()
    {
        if (publishedCues == null || publishedCues.Length != cues.Length)
            publishedCues = new bool[cues.Length];
        else
            Array.Clear(publishedCues, 0, publishedCues.Length);
    }

    private void PublishReachedCues(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (cues == null || cues.Length == 0)
            return;

        if (publishedCues == null || publishedCues.Length != cues.Length)
            ResetCues();

        float normalizedTime = stateInfo.normalizedTime;
        for (int i = 0; i < cues.Length; i++)
        {
            NormalizedAnimationCue cue = cues[i];
            if (publishedCues[i] || cue == null || string.IsNullOrWhiteSpace(cue.cueName) ||
                normalizedTime < Mathf.Clamp01(cue.normalizedTime))
            {
                continue;
            }

            publishedCues[i] = true;
            cuePublished?.OnNext(new AnimationCueStateInfo(
                cue.cueName,
                new ObservableStateMachineTrigger.OnStateInfo(animator, stateInfo, layerIndex)));
        }
    }
}
#endif
