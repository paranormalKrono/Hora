using NamespaceOcTree;
using NamespaceWorld;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;
using static NamespaceFactoryTowers.FactoryTowers;

namespace NamespaceJobsHuman
{

    [BurstCompile]
    public struct JobWhite: IJobParallelForTransform
    {
        [ReadOnly] public uint threadID;
        [ReadOnly] public NativeList<HumanData> humanDatas;

        [ReadOnly] public NativeReference<Random> randomBase;

        [ReadOnly] public float deltaTime;
        [ReadOnly] public float3 center;
        [ReadOnly] public float radius;

        [ReadOnly] public WhiteCfg whiteCfg;

        public NativeList<float3> moveDirs;
        public NativeList<float> timers;

        public void Execute(int index, TransformAccess transform)
        {
            Random random = randomBase.Value;
            random.InitState(random.state + threadID);

            HumanData humanData = humanDatas[index];
            float curTimer = timers[index] - deltaTime;
            if (curTimer < 0f)
            {
                WhiteCfg cfg = whiteCfg;
                float curChanceToChangeDirection = humanData.Scalar(cfg.chanceToChangeDirection);
                if (curChanceToChangeDirection > random.NextFloat(0f, 1f))
                {
                    float speed = humanData.Scalar(cfg.speed);
                    float3 move = random.NextFloat3Direction() * speed;
                    moveDirs[index] = (1f + random.NextFloat(-1f, 1f) * humanData.Scalar(cfg.speedVary)) * move;

                    curTimer = humanData.Scalar(cfg.timerChangeDirection);
                    curTimer *= 1f + random.NextFloat(-1f, 1f) * humanData.Scalar(cfg.timerChangeDirectionVary);
                }
            }
            timers[index] = curTimer;

            float3 pos = transform.position;
            if (math.distance(pos, center) > radius)
            {
                pos = center + radius * random.NextFloat3Direction();
            }
            transform.position = pos + moveDirs[index] * deltaTime;
        }
    }

    public struct OcCheckClosestDatas
    {
        public int index;
        public TransformAccessArray closeTargets;
        public int closestID;

        public OcCheckClosestDatas(int index, TransformAccessArray closeTargets, int closestID)
        {
            this.index = index;
            this.closeTargets = closeTargets;
            this.closestID = closestID;
        }
    }

    [BurstCompile]
    public struct JobBlueWait: IJobParallelForTransform
    {
        [ReadOnly] public uint threadID;
        [ReadOnly] public float delta;

        [ReadOnly] public Random randomBase;

        [ReadOnly] public NativeOcTree ocTreeReds;

        [ReadOnly] public BlueCfg blueCfg;

        [ReadOnly] public NativeList<HumanData> humanDatas;
        [ReadOnly] public float3 towerPosition;
        [ReadOnly] public TowerProtoMinistryLoveCfg towerCfg;

        public NativeList<float3> moveDirs;
        public NativeList<float> timers;

        [WriteOnly] public NativeList<OcCheckClosestDatas> seenReds;

        public void Execute(int index, TransformAccess transform)
        {
            Random random = randomBase;
            random.InitState(randomBase.state + threadID);

            HumanData humanData = humanDatas[index];
            BlueCfg cfg = blueCfg;
            float3 pos = transform.position;
            float3 moveDir = moveDirs[index];

            // Check vision
            float visionRange = humanData.Scalar(cfg.vision);
            TransformAccessArray closeReds = ocTreeReds.FindNeighbors(pos, visionRange);
            if (closeReds.length > 0)
            {
                int closestID = -1;
                float min = float.MaxValue;
                float d;
                for (int i = 0; i < closeReds.length; i++)
                {
                    d = math.distance(closeReds[i].position, pos);
                    if (d < min)
                    {
                        min = d;
                        closestID = i;
                    }
                }
                seenReds[index] = new OcCheckClosestDatas(index, closeReds, closestID);

                // Setting speed for moveDir
                float speed = humanData.Scalar(cfg.speed);
                float speedVaried = (1f + random.NextFloat(-1f, 1f) * humanData.Scalar(cfg.speedVary)) * speed;
                float3 dir = new(1f,0f,0f);
                moveDirs[index] = speedVaried * math.normalize(dir);
            }
            else
            {
                closeReds.Dispose();
                seenReds[index] = new OcCheckClosestDatas { index = -1 };

                // Check if point needs update
                float timer = timers[index] - delta;
                if (timer < 0)
                {
                    TowerProtoMinistryLoveCfg towerCfg = this.towerCfg;
                    float3 point = towerPosition + random.NextFloat3Direction() * random.NextFloat(towerCfg.pointMin, towerCfg.pointMax);
                    float3 diff = point - pos;
                    float length = math.length(diff);
                    float speed = humanData.Scalar(cfg.speed);
                    float speedVaried = (1f + random.NextFloat(-1f, 1f) * humanData.Scalar(cfg.speedVary)) * speed;
                    moveDir = speedVaried / length * diff;
                    timer = length / speedVaried;
                    moveDirs[index] = moveDir;
                }
                timers[index] = timer;
            }

            pos += delta * moveDir;
            transform.position = pos;
        }
    }

    [BurstCompile]
    public struct JobBlueFollow: IJobParallelForTransform
    {
        [ReadOnly] public uint threadID;
        [ReadOnly] public float delta;

        [ReadOnly] public Random randomBase;

        [ReadOnly] public NativeOcTree ocTreeReds;

        [ReadOnly] public BlueCfg blueCfg;

        [ReadOnly] public NativeList<HumanData> humanDatas;

        public TransformAccessArray targets;
        public NativeList<float3> moveDirs;
        public NativeList<float> timers;

        [WriteOnly] public NativeList<int> state;

        public void Execute(int index, TransformAccess transform)
        {
            Random random = randomBase;
            random.InitState(randomBase.state + threadID);

            HumanData humanData = humanDatas[index];
            BlueCfg cfg = blueCfg;
            float3 pos = transform.position;
            float timer = timers[index] - delta;

            // Update target
            if (timer < 0f)
            {
                float visionRange = humanData.Scalar(cfg.vision);
                TransformAccessArray closeReds = ocTreeReds.FindNeighbors(pos, visionRange);
                if (closeReds.length > 0)
                {
                    int closestID = -1;
                    float min = float.MaxValue;
                    float d;
                    for (int i = 0; i < closeReds.length; i++)
                    {
                        d = math.distance(closeReds[i].position, pos);
                        if (d < min)
                        {
                            min = d;
                            closestID = i;
                        }
                    }
                    targets[index] = closeReds[closestID];
                }
                else
                {
                    state[index] = 1;
                }
                closeReds.Dispose();

                timer = humanData.Scalar(cfg.checkTargetTime);
            }
            timers[index] = timer;

            // Check near target
            float3 moveDir = moveDirs[index];
            float3 targetPos = targets[index].position;
            float catchDistance = humanData.Scalar(cfg.catchTargetDistance);
            if (math.distance(pos, targetPos) < catchDistance)
            {
                state[index] = 2;
            }
            else
            {
                // Update movedir without changing speed
                float3 diff = targetPos - pos;
                moveDir = math.normalize(diff) * math.length(moveDir);
                moveDirs[index] = moveDir;
            }

            // Move
            pos += delta * moveDir;
            transform.position = pos;
        }
    }

    [BurstCompile]
    public struct JobRed: IJobParallelForTransform
    {
        [ReadOnly] public uint threadID;
        [ReadOnly] public float delta;

        [ReadOnly] public Random randomBase;

        // [ReadOnly] public NativeOcTree ocTreeBlues;

        [ReadOnly] public RedCfg redCfg;

        [ReadOnly] public NativeList<HumanData> humanDatas;

        public TransformAccessArray targets;
        public NativeList<float3> moveDirs;
        public NativeList<float> timers;

        [WriteOnly] public NativeList<float> states; // -1.0 - nothing, >0.0 - explosion distance

        public void Execute(int index, TransformAccess transform)
        {
            Random random = randomBase;
            random.InitState(randomBase.state + threadID);

            float3 targetPos = targets[0].position;
            float3 pos = transform.position;
            HumanData humanData = humanDatas[index];

            RedCfg cfg = redCfg;

            // Check explosion distance
            float explosionDistance = humanData.Scalar(cfg.explosionDistance);
            if (math.distance(targetPos, pos) < explosionDistance)
            {
                states[index] = explosionDistance;
            }

            // UpdateMovementSpeed
            float timer = timers[index] - delta;
            if (timer < 0)
            {
                float speed = humanData.Scalar(cfg.speed);
                float speedVaried = (1f + random.NextFloat(-1f, 1f) * humanData.Scalar(cfg.speedVary)) * speed;
                float3 moveDir = math.normalize(moveDirs[index]);
                moveDirs[index] = speedVaried * moveDir;
                timer = humanData.Scalar(cfg.timerUpdateSpeed);
            }
            timers[index] = timer;

            // Check nearby blues
            // float visionDistance = humanData.Scalar(coeffsVision);
            // TransformAccessArray ocCheck = ocTreeBlues.FindNeighbors(pos, visionDistance);
            // for (int i = 0; i < ocCheck.length; i++) 
            // {
            //     // Avoidance algorithm
            // }
        }
    }
}