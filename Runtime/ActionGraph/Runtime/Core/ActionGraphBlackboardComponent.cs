using SAS.Core.BlackboardSystem;
using UnityEngine;

[DisallowMultipleComponent]
public class ActionGraphBlackboardComponent : MonoBehaviour
{
    private readonly Blackboard blackboard = new Blackboard();

    public Blackboard Blackboard => blackboard;
}
