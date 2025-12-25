
using HumanComponents;
using NWorld;
using UnityEngine;

[CreateAssetMenu(fileName = "SOWorld", menuName = "Scriptable Objects/SOWorld")]
public class SOWorld: ScriptableObject
{
    public bool isDebugMode = false;

    public int interloopBatchCount = 64;
    public float ocTreeUpdateTime = 1.0f;
    public float humanSpeedDiff = 0.1f;
    
    
    public WorldSpace space = new()
    {
        radius = 15f,
        radiusAdditional = 3f
    };

    public SOOcTree ocTreeCfg;

    [Header("White")]
    public int whiteStartCount = 40;
    public float whiteTimeToAdd = 0.2f;
    public HumanCharacter whiteCharacter = new()
    {
        baseValue = 1.0f,
        baseUpdateTime = 3.0f,
        baseSpeed = 0.8f,

        value = new Human()
        {
            soul = 1.5f,
            fear = 0.5f,
            rebel = 2.0f,
            smart = 1.5f,
            health = 1.0f,
            tired = 1.2f
        },
        updateTime = new Human()
        {
            soul = 0.2f,
            fear = 0.001f,
            rebel = 2.0f,
            smart = 1.5f,
            health = 0.01f,
            tired = 0.2f
        },
        speed = new Human()
        {
            soul = 1.5f,
            fear = 0.5f,
            rebel = 2.0f,
            smart = 1.5f,
            health = 1.0f,
            tired = 1.2f
        }
    };

    [Header("Red")]
    public int redCount = 10;
    public float redTimeToAdd = 0.5f;
    public HumanCharacter redCharacter = new()
    {
        baseValue = 2.0f,
        baseUpdateTime = 2.0f,
        baseSpeed = 1.0f,

        value = new Human()
        {
            soul = 0.5f,
            fear = 0.6f,
            rebel = 3.0f,
            smart = 2.0f,
            health = 2.0f,
            tired = 0.8f
        },
        updateTime = new Human()
        {
            soul = 1.5f,
            fear = 3.0f,
            rebel = 1.0f,
            smart = 0.1f,
            health = 0.01f,
            tired = 0.1f
        },
        speed = new Human()
        {
            soul = 0.5f,
            fear = 0.6f,
            rebel = 3.0f,
            smart = 2.0f,
            health = 2.0f,
            tired = 0.8f
        },
    };
}