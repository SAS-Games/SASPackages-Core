using UnityEngine;

[CreateAssetMenu(fileName = "New Action Graph", menuName = "Action Graph/Action Graph Asset")]
public class ActionGraphAsset : ScriptableObject
{
    [SerializeReference]
    public NodeConfig root = new FlowNodeConfig();
}

