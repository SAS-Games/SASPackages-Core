using System.Collections.Generic;
using System;

public class EventBindingComparer<T> : IComparer<IEventBinding<T>> where T : IEvent
{
    public int Compare(IEventBinding<T> x, IEventBinding<T> y)
    {
        // First, compare the bindings by priority (higher priority comes first)
        int priorityComparison = y.Priority.CompareTo(x.Priority);
        if (priorityComparison != 0)
            return priorityComparison;

        // If priorities are the same, maintain the registration order
        return x.RegistrationOrder.CompareTo(y.RegistrationOrder);
    }
}
