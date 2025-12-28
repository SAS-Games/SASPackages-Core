using UnityEngine;
using UnityEngine.Events;
using System.Linq;

[RequireComponent(typeof(Collider))]
public class OnTriggerHandler : MonoBehaviour
{
    [SerializeField] private string[] m_CollisionTags = { "Player" };
    [SerializeField] private UnityEvent<GameObject> m_OnTriggerEnterAction;
    [SerializeField] private UnityEvent<GameObject> m_OnTriggerExitAction;

    public UnityEvent<GameObject> OnTriggerEnterAction => m_OnTriggerEnterAction;
    public UnityEvent<GameObject> OnTriggerExitAction => m_OnTriggerExitAction;

    private void OnTriggerEnter(Collider other)
    {
        if (m_CollisionTags.Contains(other.tag))
        {
            m_OnTriggerEnterAction?.Invoke(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (m_CollisionTags.Contains(other.tag))
        {
            m_OnTriggerExitAction?.Invoke(other.gameObject);
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    private void EnsureTriggerCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            UnityEditor.EditorUtility.SetDirty(col);
        }
    }
#endif
}