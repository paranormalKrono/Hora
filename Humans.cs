
using NamespaceFactoryHumans;
using NamespaceJobsHuman;
using static NamespaceFactoryTowers.FactoryTowers;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace NamespaceWorld
{
    [System.Serializable]
    public struct HumanData
    {
        // Temperament
        private float soul;
        private float fear;
        private float rebel;
        private float smart;

        // Physics
        private float health;
        private float tired;

        public HumanData(float soul, float fear, float rebel, float smart, float health, float tired)
        {
            this.soul = soul;
            this.fear = fear;
            this.rebel = rebel;
            this.smart = smart;

            this.health = health;
            this.tired = tired;
        }

        public float Scalar(HumanData h)
        {
            return soul * h.soul +
                fear * h.fear +
                rebel * h.rebel +
                smart * h.smart +

                health * h.health +
                tired * h.tired;
        }
    }

    public struct HumanGroup
    {
        public HumanType type;
        public NativeList<HumanData> humanDatas;
        public TransformAccessArray transforms;
        public NativeList<float> timers;
        public NativeList<float3> moveDirections;

        public int HumansCount { get => humanDatas.Length; }

        public HumanGroup(HumanType type)
        {
            this.type = type;
            this.humanDatas = new NativeList<HumanData>();
            this.transforms = new TransformAccessArray();
            this.timers = new NativeList<float>();
            this.moveDirections = new NativeList<float3>();
        }

        public void HumanAdd(HumanData hd, Transform tr)
        {
            humanDatas.Add(hd);
            transforms.Add(tr);
            timers.Add(0f);
            moveDirections.Add(0f);
        }

        public void HumansAdd(NativeList<HumanData> hds, TransformAccessArray trs)
        {
            int prevLength = HumansCount;
            humanDatas.AddRange(hds.AsArray());
            hds.Dispose();
            int length = HumansCount;

            transforms.ResizeArray(length);
            timers.Resize(length, NativeArrayOptions.UninitializedMemory);
            moveDirections.Resize(length, NativeArrayOptions.UninitializedMemory);
            for (int i = prevLength; i < length; ++i)
            {
                transforms[i] = trs[i];
                timers[i] = 0f;
                moveDirections[i] = 0f;
            }
        }

        public void HumanRemove(int index)
        {
            humanDatas.RemoveAtSwapBack(index);
            transforms.RemoveAtSwapBack(index);
            timers.RemoveAtSwapBack(index);
            moveDirections.RemoveAtSwapBack(index);
        }

        public void Resize(int length)
        {
            humanDatas.Resize(length, NativeArrayOptions.UninitializedMemory);
            transforms.ResizeArray(length);
            timers.Resize(length, NativeArrayOptions.UninitializedMemory);
            moveDirections.Resize(length, NativeArrayOptions.UninitializedMemory);
        }

        public void HumanRemoveRangeSwapBack(int index, int count)
        {
            humanDatas.RemoveRangeSwapBack(index, count);
            for (int i = index; i < index + count; ++i)
            {
                transforms.RemoveAtSwapBack(i);
            }
            timers.RemoveRangeSwapBack(index, count);
            moveDirections.RemoveRangeSwapBack(index, count);
        }

        public void HumanMove(ref HumanGroup toGroup, int index)
        {
            toGroup.HumanAdd(humanDatas[index], transforms[index]);
            HumanRemove(index);
        }

        public void HumansMoveWhole(ref HumanGroup toGroup)
        {
            toGroup.HumansAdd(humanDatas, transforms);
        }

        public void HumansMoveGroup(ref HumanGroup toGroup, int index, int count)
        {
            int prevLength = toGroup.HumansCount;
            int length = toGroup.HumansCount + count;
            toGroup.Resize(length);
            for (int i = prevLength; i < length; ++i)
            {
                toGroup.humanDatas[i] = humanDatas[index + i];
                toGroup.transforms[i] = transforms[index + i];
                toGroup.timers[i] = 0f;
                toGroup.moveDirections[i] = 0f;
            }
            HumanRemoveRangeSwapBack(index, count);
        }

        public void Destroy(FactoryHumans factoryHumans)
        {
            TransformAccessArray trs = transforms;
            for (int i = 0; i < trs.length; ++i)
            {
                factoryHumans.Destroy(trs[i]);
            }
        }

        public void Dispose()
        {
            humanDatas.Clear();
            transforms.Dispose();
            timers.Dispose();
            moveDirections.Dispose();
        }
    }

    public struct HumanGroupBlue
    {
        public enum BlueGroupType
        {

        }

        public HumanGroup groupWaiting;
        public HumanGroup groupFollowing;
        public TransformAccessArray followTargets;
        public NativeList<int> followState;
        public NativeList<OcCheckClosestDatas> waitOcCheck;

        public TowerProtoMinistryLoveCfg towerCfg;
        public float3 towerPosition;

        public HumanGroupBlue(TowerProtoMinistryLoveCfg cfg)
        {
            this.groupWaiting = new HumanGroup(HumanType.Blue);
            this.groupFollowing = new HumanGroup(HumanType.Blue);
            this.followTargets = new TransformAccessArray();
            this.followState = new NativeList<int>();
            this.waitOcCheck = new NativeList<OcCheckClosestDatas>();
            this.towerCfg = cfg;
            this.towerPosition = 0f;
        }

        public int HumansCount { get => groupWaiting.humanDatas.Length + groupFollowing.humanDatas.Length; }

        public void HumanAddFollow(HumanData hd, Transform tr)
        {
            groupFollowing.HumanAdd(hd, tr);
            followTargets.Add(tr);
            followState.Add(0);
        }

        public void HumanRemoveFollow(int id)
        {
            groupFollowing.HumanRemove(id);
            followTargets.RemoveAtSwapBack(id);
            followState.RemoveAtSwapBack(id);
        }

        public void Destroy(FactoryHumans factoryHumans)
        {
            groupWaiting.Destroy(factoryHumans);
            groupFollowing.Destroy(factoryHumans);
        }

        public void Dispose()
        {
            groupWaiting.Dispose();
            groupFollowing.Dispose();
            followTargets.Dispose();
            followState.Dispose();
            waitOcCheck.Dispose();
        }
    }


    public struct HumanGroupRed
    {
        public HumanGroup group;
        public NativeList<float> states;
        public float3 targetPosition;

        public HumanGroupRed(float3 targetPosition)
        {
            this.group = new(HumanType.Red);
            this.states = new(Allocator.Persistent);
            this.targetPosition = targetPosition;
        }

        public int HumansCount { get => group.humanDatas.Length; }

        public void HumanAdd(HumanData hd, Transform tr)
        {
            group.HumanAdd(hd, tr);
            states.Add(-1f);
        }

        public void HumanRemove(int id)
        {
            group.HumanRemove(id);
            states.RemoveAtSwapBack(id);
        }

        public void Destroy(FactoryHumans factoryHumans)
        {
            group.Destroy(factoryHumans);
        }

        public void Dispose()
        {
            group.Dispose();
            states.Dispose();
        }
    }

    [System.Serializable]
    public struct WhiteCfg
    {
        public HumanData speed;
        public HumanData speedVary;
        public HumanData chanceToChangeDirection;
        public HumanData timerChangeDirection;
        public HumanData timerChangeDirectionVary;
    }

    [System.Serializable]
    public struct BlueCfg
    {
        public HumanData speed;
        public HumanData speedVary;
        public HumanData vision;
        public HumanData checkTargetTime;
        public HumanData catchTargetDistance;
    }

    [System.Serializable]
    public struct RedCfg
    {
        public HumanData speed;
        public HumanData speedVary;
        public HumanData explosionDistance;
        public HumanData timerUpdateSpeed;
    }
}