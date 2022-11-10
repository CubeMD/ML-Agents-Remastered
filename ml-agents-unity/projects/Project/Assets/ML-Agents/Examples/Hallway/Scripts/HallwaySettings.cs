using UnityEngine;


[CreateAssetMenu(menuName = "Create HallwaySettings", fileName = "HallwaySettings", order = 0)]
public class HallwaySettings : ScriptableObject
{
    public float agentRunSpeed;
    public float agentRotationSpeed;
    public float agentSensorRadius;
    public Material goalScoredMaterial;
    public Material failMaterial;

}
