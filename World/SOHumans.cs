
using UnityEngine;

[CreateAssetMenu(fileName = "SOHumans", menuName = "Scriptable Objects/SOHumans")]
public class SOHumans : ScriptableObject
{

    public const int HumansTypesCount = 2;
    public enum HumanType
    {
        White,
        Red,
    }

    [SerializeField] public GameObject[] prefabs = new GameObject[HumansTypesCount];

}