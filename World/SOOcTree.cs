
using UnityEngine;

[CreateAssetMenu(fileName = "SOOcTree", menuName = "Scriptable Objects/SOOcTree")]
public class SOOcTree: ScriptableObject
{
    [SerializeField] public int batchCount = 64;

    [Header("Boundaries")]
    [SerializeField] public int nodesPoolLength = 256;
    [SerializeField] public int updateArrayLength = 512;

    [Header("Buffer sizes")]
    [SerializeField] public int updateBufferLength = 64;
}