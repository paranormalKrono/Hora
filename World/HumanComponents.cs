

using Unity.Mathematics;

namespace HumanComponents
{

    [System.Serializable]
    public struct Human
    {
        // Temperament
        public float soul;
        public float fear;
        public float rebel;
        public float smart;

        // Physics
        public float health;
        public float tired;

        public float Scalar(Human h)
        {
            return soul * h.soul +
                fear * h.fear +
                rebel * h.rebel +
                smart * h.smart +

                health * h.health +
                tired * h.tired;
        }
    }

    [System.Serializable]
    public struct HumanCharacter
    {
        public float baseValue;
        public float baseUpdateTime;
        public float baseSpeed;

        public Human value;
        public Human updateTime;
        public Human speed;
    }


    public struct HMove
    {
        public float updateTimer;
        public float3 moveDirection;
    }
}