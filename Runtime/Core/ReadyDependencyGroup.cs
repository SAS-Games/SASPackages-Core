using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

public interface IReady
{
    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// A composite readiness source that waits for all configured dependencies
/// to finish initializing.
/// </summary>
public sealed class ReadyDependencyGroup : MonoBehaviour, IReady
{
    [SerializeField] private List<SerializableInterface<IReady>> m_Dependencies = new();

    private readonly List<IReady> _dependencies = new();
    private readonly CancellationTokenSource _destroyCancellation = new();
    private bool _isInitialized;
    private bool _isConfigurationValid = true;

    private void Awake()
    {
        CacheDependencies();
        _isInitialized = true;
    }

    private void CacheDependencies()
    {
        _dependencies.Clear();

        for (var i = 0; i < m_Dependencies.Count; i++)
        {
            var dependency = m_Dependencies[i].Value;

            if (IsMissing(dependency))
            {
                Debug.LogError($"{nameof(ReadyDependencyGroup)} on '{name}' has a missing dependency at index {i}.", this);
                _isConfigurationValid = false;
                continue;
            }

            if (ReferenceEquals(dependency, this))
            {
                Debug.LogError($"{nameof(ReadyDependencyGroup)} on '{name}' cannot depend on itself.", this);
                _isConfigurationValid = false;
                continue;
            }

            if (_dependencies.Contains(dependency))
            {
                Debug.LogWarning($"{nameof(ReadyDependencyGroup)} on '{name}' contains a duplicate dependency at index {i}.", this);
                continue;
            }

            _dependencies.Add(dependency);
        }
    }

    public async Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized) throw new InvalidOperationException($"{nameof(ReadyDependencyGroup)} on '{name}' has not been initialized.");
        if (!_isConfigurationValid) throw new InvalidOperationException($"{nameof(ReadyDependencyGroup)} on '{name}' has invalid dependencies.");

        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _destroyCancellation.Token);

        var waitTasks = new Task[_dependencies.Count];
        for (var i = 0; i < _dependencies.Count; i++)
        {
            var dependency = _dependencies[i];
            if (IsMissing(dependency))
                throw new InvalidOperationException($"{nameof(ReadyDependencyGroup)} on '{name}' lost dependency at index {i} while waiting.");

            waitTasks[i] = dependency.WaitUntilReadyAsync(linkedCancellation.Token);
        }

        await Task.WhenAll(waitTasks);
    }

    private void OnDestroy()
    {
        _destroyCancellation.Cancel();
        _destroyCancellation.Dispose();
    }

    private static bool IsMissing(IReady dependency)
    {
        return dependency == null || dependency is Object unityObject && unityObject == null;
    }
}

public static class ReadinessTask
{
    public static async Task WaitAsync(Task task, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            await task;
            return;
        }

        var cancellationSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        using (cancellationToken.Register(() => cancellationSource.TrySetCanceled()))
        {
            var completedTask = await Task.WhenAny(task, cancellationSource.Task);
            await completedTask;
        }
    }
}
