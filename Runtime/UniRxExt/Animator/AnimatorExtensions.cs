#if UniRxEnabled
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;
using System;
using UnityEngine.Assertions;

public static class AnimatorExtensions
{
    public static IObservable<Unit> WhenStateEnter(this Animator animator, string stateName)
    {
        var enterTriggers = GetTriggers(animator, stateName, out var firstTrigger);
        var enterObservable = GetTriggerStateEnter(firstTrigger);
        foreach (var trigger in enterTriggers.Skip(1))
        {
            enterObservable = enterObservable.Merge(GetTriggerStateEnter(trigger));
        }

        return enterObservable.First().AsUnitObservable();
    }


    public static IObservable<Unit> WhenStateExit(this Animator animator, string stateName)
    {
        var exitTriggers = GetTriggers(animator, stateName, out var firstTrigger);
        var exitObservable = GetTriggerStateExit(firstTrigger);
        foreach (var trigger in exitTriggers.Skip(1))
        {
            exitObservable = exitObservable.Merge(GetTriggerStateExit(trigger));
        }

        return exitObservable.First().AsUnitObservable();
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


    private static IEnumerable<TaggedObservableStateMachineTrigger> GetTriggers(Animator animator, string stateName, out TaggedObservableStateMachineTrigger firstTrigger)
    {
        var triggers = animator.FindTriggers(stateName);
        firstTrigger = triggers.FirstOrDefault();
        Assert.IsFalse(firstTrigger == null, $"Missing 'TaggedObservableStateMachineTrigger' or state \"{stateName}\" not found.");
        return triggers;
    }
    private static IEnumerable<TaggedObservableStateMachineTrigger> FindTriggers(this Animator animator, string stateName)
    {
        return animator.GetBehaviours<TaggedObservableStateMachineTrigger>()
            .Where(trigger => trigger.stateName == stateName);
    }

    private static IObservable<Unit> GetTriggerStateEnter(TaggedObservableStateMachineTrigger trigger)
    {
        return trigger.OnStateEnterAsObservable().AsUnitObservable();
    }

    private static IObservable<Unit> GetTriggerStateExit(TaggedObservableStateMachineTrigger trigger)
    {
        return trigger.OnStateExitAsObservable().AsUnitObservable();
    }
}
#endif
