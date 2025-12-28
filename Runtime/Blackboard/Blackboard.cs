using System;
using System.Collections.Generic;

namespace SAS.Core.BlackboardSystem
{
    [Serializable]
    public readonly struct BlackboardKey : IEquatable<BlackboardKey>
    {
        readonly string name;
        readonly int hashedKey;

        public BlackboardKey(string name)
        {
            this.name = name;
            hashedKey = name.ComputeFNV1aHash();
        }

        public bool Equals(BlackboardKey other) => hashedKey == other.hashedKey;

        public override bool Equals(object obj) => obj is BlackboardKey other && Equals(other);
        public override int GetHashCode() => hashedKey;
        public override string ToString() => name;

        public static bool operator ==(BlackboardKey lhs, BlackboardKey rhs) => lhs.hashedKey == rhs.hashedKey;
        public static bool operator !=(BlackboardKey lhs, BlackboardKey rhs) => !(lhs == rhs);
    }


    [Serializable]
    public class Blackboard
    {
        Dictionary<string, BlackboardKey> keyRegistry = new();
        Dictionary<BlackboardKey, object> entries = new();

        public List<Action> PassedActions { get; } = new();

        public void AddAction(Action action)
        {
            PassedActions.Add(action);
        }

        public void ClearActions() => PassedActions.Clear();

        public void Debug()
        {
            foreach (var entry in entries)
            {
                var entryType = entry.Value.GetType();

                if (entryType.IsGenericType && entryType.GetGenericTypeDefinition() == typeof(BlackboardEntry<>))
                {
                    var valueProperty = entryType.GetProperty("Value");
                    if (valueProperty == null) continue;
                    var value = valueProperty.GetValue(entry.Value);
                    UnityEngine.Debug.Log($"Key: {entry.Key}, Value: {value}");
                }
            }
        }

        public bool TryGetValue<T>(BlackboardKey key, out T value)
        {
            value = GetValue<T>(key);
            return value != null;
        }

        public T GetValue<T>(BlackboardKey key)
        {
            if (entries.TryGetValue(key, out var entry))
            {

                if (entry is BlackboardEntry<T> castedEntry)
                    return castedEntry.Value;
            }
            return default;
        }

        public void SetValue<T>(BlackboardKey key, T value)
        {
            if (entries.TryGetValue(key, out var existingEntry))
            {
                var castedEntry = existingEntry as BlackboardEntry<T>;
                ////todo: do we really need this 
                //if (EqualityComparer<T>.Default.Equals(castedEntry.Value, value))
                //    return; // Value hasn't changed, no need to update

                castedEntry.SetValue(value);
            }
            else
                entries[key] = new BlackboardEntry<T>(key, value); // Create new entry if it doesn�t exist
        }

        internal void SetValue<T>(BlackboardKey key, T value, bool readyOnly)
        {
            if (!readyOnly)
                entries[key] = new BlackboardEntry<T>(key, value);
            else
                entries[key] = new BlackboardReadOnlyEntry<T>(key, value);
        }

        public BlackboardKey GetOrRegisterKey(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                return default;

            if (!keyRegistry.TryGetValue(keyName, out BlackboardKey key))
            {
                key = new BlackboardKey(keyName);
                keyRegistry[keyName] = key;
            }

            return key;
        }

        public bool ContainsKey(BlackboardKey key) => entries.ContainsKey(key);

        public void Remove(BlackboardKey key) => entries.Remove(key);

        [Serializable]
        public class BlackboardEntry<T>
        {
            public BlackboardKey Key { get; }
            public T Value { get; private set; }
            public Type ValueType { get; }

            public BlackboardEntry(BlackboardKey key, T value)
            {
                Key = key;
                Value = value;
                ValueType = typeof(T);
            }

            public virtual void SetValue(T value)
            {
                Value = value;
            }

            public override bool Equals(object obj) => obj is BlackboardEntry<T> other && other.Key == Key;
            public override int GetHashCode() => Key.GetHashCode();
        }

        [Serializable]
        public class BlackboardReadOnlyEntry<T> : BlackboardEntry<T>
        {
            public BlackboardReadOnlyEntry(BlackboardKey key, T value) : base(key, value) { }

            public override void SetValue(T value)
            {
                throw new InvalidOperationException($"Cannot modify a read-only blackboard entry for: {Key}.");
            }
        }
    }
}