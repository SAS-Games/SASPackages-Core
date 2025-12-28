using System.Collections.Generic;
using UnityEngine;

public static class EventBus<T> where T : IEvent
{
    static readonly SortedSet<IEventBinding<T>> bindings = new SortedSet<IEventBinding<T>>(new EventBindingComparer<T>());

    public static void Register(EventBinding<T> binding)
    {
        binding.IncrementRegistrationOrder();
        bindings.Add(binding);
    }
    public static void Deregister(EventBinding<T> binding) => bindings.Remove(binding);

    public static void Raise(T @event)
    {
        foreach (var binding in bindings)
        {
            binding.OnEvent.Invoke(@event);
            binding.OnEventNoArgs.Invoke();
        }
    }

    static void Clear()
    {
        Debug.Log($"Clearing {typeof(T).Name} bindings");
        bindings.Clear();
    }
}