using System;
using System.Reflection;
using UnityEngine;

[Serializable]
public abstract class ActionDataProvider
{
    public virtual bool HasConfigurableData => true;

    public abstract IDataSelector CreateSelector();

    public virtual bool EnsureDefaultData()
    {
        return false;
    }
}

public interface IIndexedActionDataProvider
{
}

[Serializable]
public abstract class ActionDataProvider<T> : ActionDataProvider
{
    [SerializeField] private bool useSingleValue;
    [SerializeField] private T[] data;

    public bool UseSingleValue => useSingleValue;

    public override bool HasConfigurableData => HasSerializableDataFields();

    public override IDataSelector CreateSelector()
    {
        EnsureDefaultData();

        if (!HasConfigurableData)
            return new FixedSelector<T>(default(T));

        if (data == null || data.Length == 0)
            throw new Exception($"{GetType().Name} has no data");

        if (useSingleValue)
            return new FixedSelector<T>(data[0]);

        return new SequentialSelector<T>(data);
    }

    public IDataSelector<T> CreateTypedSelector()
    {
        return (IDataSelector<T>)CreateSelector();
    }

    public T[] GetAllData() => data;

    /// <summary>
    /// Populates provider data when an ActionGraph is assembled from code.
    /// Each ActionNode execution consumes one value; multiple values are selector
    /// variants, not a batch of actions.
    /// </summary>
    public void SetData(T[] values, bool singleValue = true)
    {
        data = values ?? Array.Empty<T>();
        useSingleValue = singleValue;
    }

    public override bool EnsureDefaultData()
    {
        if (!HasConfigurableData)
        {
            if (data == null || data.Length == 0)
                return false;

            data = Array.Empty<T>();
            useSingleValue = true;
            return true;
        }

        if (!CanCreateDefaultData())
            return false;

        bool changed = false;

        if (data == null || data.Length == 0)
        {
            data = new[] { CreateDefaultData() };
            useSingleValue = true;
            return true;
        }

        if (!typeof(T).IsValueType)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != null)
                    continue;

                data[i] = CreateDefaultData();
                changed = true;
            }
        }

        return changed;
    }

    private static bool CanCreateDefaultData()
    {
        return typeof(T).IsValueType || typeof(T).GetConstructor(Type.EmptyTypes) != null;
    }

    private static bool HasSerializableDataFields()
    {
        Type dataType = typeof(T);
        if (dataType.IsValueType || dataType == typeof(string) || typeof(UnityEngine.Object).IsAssignableFrom(dataType))
            return true;

        var fields = dataType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.IsStatic || field.IsNotSerialized)
                continue;

            if (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField)))
                return true;
        }

        return false;
    }

    private static T CreateDefaultData()
    {
        return (T)Activator.CreateInstance(typeof(T));
    }
}
