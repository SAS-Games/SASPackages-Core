using System;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class SerializableInterface<TInterface, TObject> where TObject : Object where TInterface : class
{
    [SerializeField, HideInInspector] private TObject _value;

    public TInterface Value
    {
        get => _value switch
        {
            null => null,
            TInterface @interface => @interface,
            _ => throw new InvalidOperationException($"{_value} needs to implement an interface {nameof(TInterface)}.")
        };
        set => _value = value switch
        {
            null => null,
            TObject newValue => newValue,
            _ => throw new ArgumentException($"{value} needs to be of type {typeof(TObject)}.", string.Empty)
        };
    }

    public SerializableInterface()
    {
    }

    public SerializableInterface(TObject target) => _value = target;

    public SerializableInterface(TInterface @interface) => _value = @interface as TObject;

    public static implicit operator TInterface(SerializableInterface<TInterface, TObject> obj) => obj.Value;
}

[Serializable]
public class SerializableInterface<TInterface> : SerializableInterface<TInterface, Object> where TInterface : class
{
}