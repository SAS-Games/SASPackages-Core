#if UniRxEnabled
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using StateEvent = UniRx.Triggers.ObservableStateMachineTrigger.OnStateInfo;

public static class AnimatorExtensions
{
    public static IObservable<Unit> WhenStateEnter(this Animator animator, string stateName)
    {
        TaggedObservableStateMachineTrigger[] triggers = GetTriggers(animator, stateName);
        return MergeTriggerObservables(triggers, trigger => trigger.OnStateEnterAsObservable())
            .First()
            .AsUnitObservable();
    }

    public static IObservable<Unit> WhenStateExit(this Animator animator, string stateName)
    {
        TaggedObservableStateMachineTrigger[] triggers = GetTriggers(animator, stateName);
        return MergeTriggerObservables(triggers, trigger => trigger.OnStateExitAsObservable())
            .First()
            .AsUnitObservable();
    }

    /// <summary>Completes when the next matching state reaches its configured completion threshold.</summary>
    public static IObservable<Unit> WhenStateCompleted(this Animator animator, string stateName)
    {
        return animator.OnStateCompletedAsObservable(stateName)
            .First()
            .AsUnitObservable();
    }

    /// <summary>Completes when the next matching state exits before reaching its configured completion threshold.</summary>
    public static IObservable<Unit> WhenStateInterrupted(this Animator animator, string stateName)
    {
        return animator.OnStateInterruptedAsObservable(stateName)
            .First()
            .AsUnitObservable();
    }

    /// <summary>Observes every completion from matching tagged state triggers.</summary>
    public static IObservable<StateEvent> OnStateCompletedAsObservable(this Animator animator, string stateName)
    {
        TaggedObservableStateMachineTrigger[] triggers = GetTriggers(animator, stateName);
        return MergeTriggerObservables(triggers, trigger => trigger.OnCompletedAsObservable());
    }

    /// <summary>Observes every early or interrupted exit from matching tagged state triggers.</summary>
    public static IObservable<StateEvent> OnStateInterruptedAsObservable(this Animator animator, string stateName)
    {
        TaggedObservableStateMachineTrigger[] triggers = GetTriggers(animator, stateName);
        return MergeTriggerObservables(triggers, trigger => trigger.OnInterruptedAsObservable());
    }

    public static IObservable<Unit> WhenStateExit(this Animator animator, string stateName, float completionPercent, int layerIndex = 0)
    {
        if (completionPercent < 0f || completionPercent > 1f)
            throw new ArgumentOutOfRangeException(nameof(completionPercent), "Completion percent must be between 0 and 1.");

        return Observable.EveryUpdate()
            .Select(_ => animator.GetCurrentAnimatorStateInfo(layerIndex))
            .Where(stateInfo => stateInfo.IsName(stateName) && (stateInfo.normalizedTime % 1f) >= completionPercent)
            .First()
            .AsUnitObservable();
    }

    private static TaggedObservableStateMachineTrigger[] GetTriggers(Animator animator, string stateName)
    {
        if (animator == null)
            throw new ArgumentNullException(nameof(animator));

        if (string.IsNullOrWhiteSpace(stateName))
            throw new ArgumentException("A tagged Animator state name is required.", nameof(stateName));

        TaggedObservableStateMachineTrigger[] triggers = animator.FindTriggers(stateName);
        if (triggers.Length == 0)
            throw new InvalidOperationException($"Missing '{nameof(TaggedObservableStateMachineTrigger)}' or state '{stateName}' was not found.");

        return triggers;
    }

    private static TaggedObservableStateMachineTrigger[] FindTriggers(this Animator animator, string stateName)
    {
        return animator.GetBehaviours<TaggedObservableStateMachineTrigger>()
            .Where(trigger => string.Equals(trigger.stateName, stateName, StringComparison.Ordinal))
            .ToArray();
    }

    private static IObservable<T> MergeTriggerObservables<T>(IReadOnlyList<TaggedObservableStateMachineTrigger> triggers, Func<TaggedObservableStateMachineTrigger, IObservable<T>> observableSelector)
    {
        IObservable<T> observable = observableSelector(triggers[0]);

        for (int i = 1; i < triggers.Count; i++)
            observable = observable.Merge(observableSelector(triggers[i]));

        return observable;
    }
}
#endif
