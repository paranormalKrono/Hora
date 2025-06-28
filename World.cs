using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;
using NamespaceFactoryTowers;
using NamespaceFactoryHumans;
using NamespaceJobsHuman;
using NamespaceOcTree;
using static NamespaceFactoryTowers.FactoryTowers;
using UnityEngine.Events;

// This code was written with blood
// I had many problems to solve when I was writing it
// I deleted all the code several times
namespace NamespaceWorld
{
    [System.Serializable]
    public class WorldCfg
    {
        public WorldTowers worldTowers;
        public WorldScore worldScore;

        public Unity.Mathematics.Random random;
        public int interloopBatchCount;
        public WhiteCfg whiteCfg;
        public BlueCfg blueCfg;
        public RedCfg redCfg;
        public Vector3 center;
        public float radius;
    }

    public class World
    {
        public NativeReference<Unity.Mathematics.Random> _random;
        public int _interloopBatchCount;
        public WhiteCfg _whiteCfg;
        public BlueCfg _blueCfg;
        public RedCfg _redCfg;

        public WorldTowers _towers;
        public WorldScore _score;

        private FactoryHumans _factoryHumans;
        private FactoryTowers _factoryTowers;

        private HumanGroup _groupWhite;

        private NativeList<HumanGroupBlue> _groupsBlue;
        private TransformAccessArray _groupsBlueTowers;

        private NativeList<HumanGroupRed> _groupsRed;
        private TransformAccessArray _groupsRedTargets;

        private NativeOcTree _ocTreeWhites;
        private NativeOcTree _ocTreeBlues;
        private NativeOcTree _ocTreeReds;

        private enum KVTI
        {
            Arrays,
            Groups,
            SubGroups,
            TowersBlues,
            TowersReds,
        }

        // "One Transform <-> few TransformAccessArrays <-> Index" relation,
        // insert, remove and find work at O(1),
        // because of RemoveSwapBack in TransformAccessArray
        private Dictionary<(KVTI, Transform), int> _kvTransformIndex;
        private Dictionary<HumanType, NativeOcTree> _kvHumanOcTree;

        private float3 _center;
        private float _radius;

        private UnityAction<float> OnExplosionScore;

        public World(WorldCfg cfg, FactoryHumans factoryHumans, FactoryTowers factoryTowers) 
        {
            _random = new(Allocator.Persistent);
            _random.Value = cfg.random;
            _interloopBatchCount = cfg.interloopBatchCount;
            _whiteCfg = cfg.whiteCfg;
            _blueCfg = cfg.blueCfg;
            _redCfg = cfg.redCfg;
            _center = cfg.center;
            _radius = cfg.radius;

            _towers = cfg.worldTowers;
            _score = cfg.worldScore;

            _factoryHumans = factoryHumans;
            _factoryTowers = factoryTowers;

            Rectangle rect = new(_center, _radius);

            _groupWhite = new(HumanType.White);
            _ocTreeWhites = new(rect);

            _groupsBlue = new(Allocator.Persistent);
            _groupsBlueTowers = new();
            _ocTreeBlues = new(rect);

            _groupsRed = new(Allocator.Persistent);
            _groupsRedTargets = new();
            _ocTreeReds = new(rect);

            _kvTransformIndex = new();
            _kvHumanOcTree = new Dictionary<HumanType, NativeOcTree> 
            { 
                { HumanType.White, _ocTreeWhites },
                { HumanType.Blue, _ocTreeBlues },
                { HumanType.Red, _ocTreeReds }
            };
        }

        public void Update()
        {
            float delta = Time.deltaTime;
            int interloopBatchCount = _interloopBatchCount;

            FactoryTowers factoryTowers = _factoryTowers;
            NativeReference<Unity.Mathematics.Random> randomNative = _random;
            Unity.Mathematics.Random random = randomNative.Value;
            uint threadID = 0;
            BlueCfg blueCfg = _blueCfg;
            RedCfg redCfg = _redCfg;

            ref HumanGroup groupWhite = ref _groupWhite;
            NativeList<HumanGroupBlue> groupsBlue = _groupsBlue;
            NativeList<HumanGroupRed> groupsRed = _groupsRed;

            NativeOcTree ocTreeWhites = _ocTreeWhites;
            NativeOcTree ocTreeBlues = _ocTreeBlues;
            NativeOcTree ocTreeReds = _ocTreeReds;

            var kvTI = _kvTransformIndex;

            // White update
            JobHandle whiteUpdate = new JobWhite
            {
                threadID = threadID++,
                deltaTime = delta,
                randomBase = randomNative,
                humanDatas = groupWhite.humanDatas,
                moveDirs = groupWhite.moveDirections,
                timers = groupWhite.timers,
                center = _center,
                radius = _radius,
                whiteCfg = _whiteCfg,
            }.Schedule(groupWhite.transforms);

            // Blue update
            JobHandle[] jobHandlesBlueWait = new JobHandle[groupsBlue.Length];
            JobHandle[] jobHandlesBlueFollow = new JobHandle[groupsBlue.Length];
            for (int i = 0; i < groupsBlue.Length; ++i, threadID += 2)
            {
                HumanGroupBlue groupBlue = groupsBlue[i];
                ref HumanGroup groupFollowing = ref groupBlue.groupFollowing;
                ref HumanGroup groupWaiting = ref groupBlue.groupWaiting;

                jobHandlesBlueWait[i] = new JobBlueWait
                {
                    threadID = threadID,
                    delta = delta,
                    randomBase = randomNative.Value,
                    ocTreeReds = ocTreeReds,
                    blueCfg = blueCfg,
                    humanDatas = groupWaiting.humanDatas,
                    towerPosition = groupBlue.towerPosition,
                    towerCfg = groupBlue.towerCfg,
                    moveDirs = groupWaiting.moveDirections,
                    timers = groupWaiting.timers,
                }.Schedule(groupWaiting.transforms);

                jobHandlesBlueFollow[i] = new JobBlueFollow
                {
                    threadID = threadID + 1,
                    delta = delta,
                    randomBase = randomNative.Value,
                    ocTreeReds = ocTreeReds,
                    blueCfg = blueCfg,
                    humanDatas = groupFollowing.humanDatas,
                    targets = groupBlue.followTargets,
                    moveDirs = groupFollowing.moveDirections,
                    timers = groupFollowing.timers,
                    state = groupBlue.followState,
                }.Schedule(groupBlue.groupFollowing.transforms);
            }

            // Red update
            JobHandle[] jobHandlesRed = new JobHandle[groupsRed.Length];
            for (int i = 0; i < groupsRed.Length; ++i, ++threadID)
            {
                HumanGroupRed groupRed = groupsRed[i];
                ref HumanGroup group = ref groupRed.group;

                jobHandlesRed[i] = new JobRed 
                { 
                    delta = delta, 
                    humanDatas = group.humanDatas, 
                    moveDirs = group.moveDirections, 
                    randomBase = random, 
                    redCfg = redCfg, 
                    states = groupRed.states, 
                    targets = _groupsRedTargets, 
                    threadID = threadID, 
                    timers = group.timers
                }.Schedule(group.transforms);
                
            }

            // Process red data
            for (int k = 0; k < groupsRed.Length; ++k)
            {
                HumanGroupRed groupRed = groupsRed[k];
                NativeList<float> states = groupRed.states;

                for (int j = 0; j < states.Length; ++j)
                {
                    float st = states[j];
                    if (st > 0f) // Not so often
                    {
                        states[j] = -1f;

                        ref HumanGroup group = ref groupRed.group;
                        Vector3 pos = group.transforms[j].position;
                        group.HumanRemove(j);
                        Explosion(pos, states[j]);
                    }
                }
            }

            // Process blue data
            for (int k = 0; k < groupsBlue.Length; ++k)
            {
                jobHandlesBlueWait[k].Complete();
                jobHandlesBlueFollow[k].Complete();

                HumanGroupBlue groupBlue = groupsBlue[k];
                ref HumanGroup groupFollowing = ref groupBlue.groupFollowing;
                ref HumanGroup groupWaiting = ref groupBlue.groupWaiting;

                NativeList<OcCheckClosestDatas> ocCheckClosestDatas = groupBlue.waitOcCheck;
                TransformAccessArray targets = groupBlue.followTargets;
                NativeList<int> state = groupBlue.followState;

                // FOLLOW THE INDICES OF THE MIND!

                // Change blue from wait to follow if red is in vision range
                for (int i = ocCheckClosestDatas.Length; i > 0; --i)
                {
                    OcCheckClosestDatas ocCheck = ocCheckClosestDatas[i];
                    if (ocCheck.index != -1)
                    {
                        int id = ocCheck.index;
                        groupBlue.HumanAddFollow(groupWaiting.humanDatas[id], ocCheck.closeTargets[ocCheck.closestID]);
                        groupWaiting.HumanRemove(id);
                        ocCheck.closeTargets.Dispose();
                    }
                }

                // Change blue from follow to wait if lost or caught red
                for (int i = state.Length; i > 0; --i)
                {
                    if (state[i] == 2) // Caught target
                    {
                        Transform redTr = groupBlue.followTargets[i];
                        int groupIndex = kvTI[(KVTI.Groups, redTr)];
                        int index = kvTI[(KVTI.Arrays, redTr)];

                        HumanGroupRed groupRed = groupsRed[groupIndex];
                        HumanGroup group = groupRed.group;
                        HumanChange(ref group, ref _groupWhite, index);
                        redTr.position = _center + _radius * random.NextFloat3Direction();
                        groupRed.group = group;
                        groupsRed[groupIndex] = groupRed;
                    }
                    if (state[i] == 1) // Target lost
                    {
                        groupWaiting.HumanAdd(groupFollowing.humanDatas[i], groupFollowing.transforms[i]);
                        groupBlue.HumanRemoveFollow(i);
                    }
                }
                groupsBlue[k] = groupBlue;
            }

            // Process white data
            whiteUpdate.Complete();
        }

        public void DestroyDispose()
        {
            _towers.DestroyDispose();

            FactoryHumans factoryHumans = _factoryHumans;
            FactoryTowers factoryTowers = _factoryTowers;

            HumanGroup groupWhite = _groupWhite;
            groupWhite.Destroy(factoryHumans);
            groupWhite.Dispose();

            _ocTreeWhites.Dispose();

            NativeList<HumanGroupBlue> groupsBlue = _groupsBlue;
            for (int i = 0; i < groupsBlue.Length; ++i)
            {
                HumanGroupBlue h = groupsBlue[i];
                h.Destroy(factoryHumans);
                h.Dispose();
            }
            _groupsBlue.Dispose();
            _groupsBlueTowers.Dispose();
            _ocTreeBlues.Dispose();

            NativeList<HumanGroupRed> groupsRed = _groupsRed;
            for (int i = 0; i < groupsRed.Length; ++i)
            {
                HumanGroupRed red = groupsRed[i];
                red.Destroy(factoryHumans);
                red.Dispose();
            }
            _groupsRed.Dispose();
            _groupsRedTargets.Dispose();
            _ocTreeReds.Dispose();
        }

        public void TowerAdd(TowerData tower, Transform tr)
        {
            FactoryTowers factoryTowers = _factoryTowers;
            var kvTI = _kvTransformIndex;

            if (tower.type == TowerType.MinistryLove)
            {
                ref TransformAccessArray towersBlue = ref _groupsBlueTowers;

                towersBlue.Add(tr);
                kvTI.Add((KVTI.TowersBlues, tr), towersBlue.length - 1);

                TowerProtoMinistryLoveCfg towerCfg = factoryTowers._protoMinistryLoveLevels[tower.level];
                int humansCount = towerCfg.humansCount;
                HumanGroupBlue group = new();
                ref HumanGroup groupWait = ref group.groupWaiting;
                HumansChange(ref _groupWhite, ref groupWait, humansCount);
                group.towerCfg = towerCfg;
                group.towerPosition = tr.position;
                _groupsBlue.Add(group);
            }
        }

        public void TowerRemove(TowerData tower, Transform tr)
        {
            var kvTI = _kvTransformIndex;

            NativeList<HumanGroupRed> groupsRed = _groupsRed;

            if (tower.type == TowerType.MinistryLove)
            {
                // Remove tower from array
                ref TransformAccessArray blueTowersTrs = ref _groupsBlueTowers;
                int indexBlueTeam = kvTI[(KVTI.TowersBlues, tr)];
                kvTI.Remove((KVTI.TowersBlues, tr));
                kvTI[(KVTI.TowersBlues, blueTowersTrs[blueTowersTrs.length - 1])] = indexBlueTeam;
                blueTowersTrs.RemoveAtSwapBack(indexBlueTeam);

                // Remove blue team
                HumanGroupBlue group = _groupsBlue[indexBlueTeam];
                ref HumanGroup wait = ref group.groupWaiting;
                ref HumanGroup follow = ref group.groupFollowing;
                HumansChange(ref wait, ref _groupWhite, wait.HumansCount);
                HumansChange(ref follow, ref _groupWhite, follow.HumansCount);
                group.Dispose();
                _groupsBlue.RemoveAtSwapBack(indexBlueTeam);
            }

            // Check if red team exists
            if (kvTI.TryGetValue((KVTI.TowersReds, tr), out int index))
            {
                Unity.Mathematics.Random random = _random.Value;
                HumanGroupRed groupRed = groupsRed[index];
                ref HumanGroup group = ref groupRed.group;

                // We have to rearrange all reds from this team to other red teams
                int count = group.HumansCount;
                int[] ids = new int[count];
                for (int i = 0; i < count; i++)
                {
                    ids[i] = i;
                }

                // Shuffle
                for (int i = count - 1; i > 0; i--)
                {
                    int k = random.NextInt(i - 1);
                    (ids[i], ids[k]) = (ids[k], ids[i]);
                }

                // Dividing into segments
                int segmentsCount = _towers.TowersCount;
                int[] segmentsEnds = new int[segmentsCount];
                segmentsEnds[^1] = count;
                for (int i = 0; i < segmentsCount - 1; i++)
                {
                    segmentsEnds[i] = random.NextInt(segmentsEnds[i-1], count);
                }

                // Rearrange
                int l;
                int r = 0;
                for (int i = 0; i < segmentsEnds.Length; i++)
                {
                    l = r; // first is 0, last is segmentsEnds[^2]
                    r = segmentsEnds[i]; // first is segmentsEnds[0], last is segments[^1] == count
                    if (l != r)
                    {
                        int curTower = i;
                        Transform towerTr = _towers.TowerGet(curTower);

                        int indexGroup;
                        HumanGroupRed groupSelected;

                        // Check if group exists
                        if (kvTI.TryGetValue((KVTI.TowersReds, towerTr), out indexGroup))
                        {
                            groupSelected = groupsRed[indexGroup];
                        }
                        else
                        {
                            groupSelected = new HumanGroupRed(towerTr.position);
                        }

                        for (int j = l; j < r; j++)
                        {
                            int curID = ids[j];
                            // F... does it change ocTrees? And other stuff!? Yeah!? Yes!
                            HumanChange(ref group, ref groupSelected.group, curID);
                        }

                        if (groupsRed.Length >= indexGroup)
                        {
                            groupsRed[indexGroup] = groupSelected;
                        }
                        else
                        {
                            groupsRed.Add(groupSelected);
                        }
                    }
                }

                groupsRed[index] = groupRed;
            }
        }

        public void TowerPositionUpdate(TowerData tower, Transform tr)
        {
            var kvTI = _kvTransformIndex;

            // Update tower position for reds
            int groupIndex = kvTI[(KVTI.TowersReds, tr)];
            HumanGroupRed groupRed = _groupsRed[groupIndex];
            groupRed.targetPosition = tr.position;
            _groupsRed[groupIndex] = groupRed;

            // Update tower position for blues
            if (tower.type == TowerType.MinistryLove)
            {
                NativeList<HumanGroupBlue> groupsBlue = _groupsBlue;
                int groupID = _kvTransformIndex[(KVTI.Groups, tr)];
                HumanGroupBlue group = groupsBlue[groupID];
                group.towerPosition = tr.position;
                groupsBlue[groupID] = group;
            }
        }

        public void TowerUpdate(TowerData tower, Transform tr)
        {
            FactoryTowers factoryTowers = _factoryTowers;
            var kvTI = _kvTransformIndex;

            if (tower.type == TowerType.MinistryLove)
            {
                NativeList<HumanGroupBlue> groupsBlue = _groupsBlue;
                int index = kvTI[(KVTI.TowersBlues, tr)];
                HumanGroupBlue group = _groupsBlue[index];
                TowerProtoMinistryLoveCfg towerCfg = factoryTowers._protoMinistryLoveLevels[tower.level];

                // Change count of blues
                int humansDiff = towerCfg.humansCount - group.HumansCount;
                if (humansDiff > 0)
                {
                    HumansChange(ref _groupWhite, ref group.groupWaiting, humansDiff);
                }
                else if (humansDiff < 0)
                {
                    ref HumanGroup wait = ref group.groupWaiting;
                    if (-humansDiff <= wait.HumansCount)
                    {
                        HumansChange(ref wait, ref _groupWhite, -humansDiff);
                    }
                    else
                    {
                        HumansChange(ref wait, ref _groupWhite, group.groupWaiting.HumansCount);
                    }
                }

                // Also paths can be changed, but I didn't test this code for a month so...

                _groupsBlue[index] = group;
            }
        }

        public void HumansCreateWhite(int newCount)
        {
            Unity.Mathematics.Random random = _random.Value;
            FactoryHumans factoryHumans = _factoryHumans;
            var kvTI = _kvTransformIndex;

            HumanGroup group = _groupWhite; // without ref, because it's normal to copy this struct with few pointers
            NativeOcTree ocTree = _ocTreeWhites;
            int prevCount = group.HumansCount;

            Vector3 v3z = Vector3.zero;
            Quaternion qi = Quaternion.identity;
            float r = _radius;
            Vector3 c = _center;

            for (int i = prevCount; i < prevCount + newCount; ++i)
            {
                HumanData hd = new (random.NextFloat(0f, 1f), random.NextFloat(0f, 1f), random.NextFloat(0f, 1f), random.NextFloat(0f, 1f), random.NextFloat(0.5f, 1f), random.NextFloat(0f, 0.2f));
                v3z = (Vector3)random.NextFloat3Direction() * r + c;
                Transform tr = factoryHumans.Create(v3z, qi);

                kvTI.Add((KVTI.Arrays, tr), i);
                ocTree.Insert(tr);
                group.HumanAdd(hd, tr);
            }

            _groupWhite = group;
        }

        private void HumanChange(ref HumanGroup group, ref HumanGroup toGroup, int index)
        {
            Transform tr = group.transforms[index];
            group.HumanMove(ref toGroup, index);

            var kvTI = _kvTransformIndex;
            kvTI[(KVTI.Arrays, tr)] = toGroup.HumansCount - 1;

            var kvOC = _kvHumanOcTree;
            kvOC[group.type].RemoveConcrete(tr);
            kvOC[toGroup.type].Insert(tr);

            FactoryHumans factoryHumans = _factoryHumans;
            factoryHumans.ChangeStyle(tr, toGroup.type);
        }

        private void HumansChange(ref HumanGroup group, ref HumanGroup toGroup, int count)
        {
            Unity.Mathematics.Random random = _random.Value;

            HumanType type = group.type;
            HumanType toType = toGroup.type;

            var kvTI = _kvTransformIndex;
            var kvOC = _kvHumanOcTree;

            int toOldLength = toGroup.HumansCount;
            int toNewLength = toGroup.HumansCount + count;

            if (count == group.HumansCount)
            {
                group.HumansMoveWhole(ref toGroup);
                TransformAccessArray trs = toGroup.transforms;
                for (int i = toOldLength; i < toNewLength; ++i)
                {
                    Transform tr = trs[i];
                    kvTI[(KVTI.Arrays, tr)] = i;
                    kvOC[type].RemoveConcrete(tr);
                    kvOC[toType].Insert(tr);
                }
            }
            else
            {
                // Manual random move, because we need to update kv and maintain order after every remove
                ref NativeList<HumanData> hds = ref group.humanDatas;
                ref TransformAccessArray trs = ref group.transforms;

                ref var newHds = ref toGroup.humanDatas;
                ref var newTrs = ref toGroup.transforms;
                ref var newTimers = ref toGroup.timers;
                ref var newMovedirs = ref toGroup.moveDirections;

                toGroup.Resize(toNewLength);
                for (int i = toOldLength; i < toNewLength; ++i)
                {
                    int choice = random.NextInt(hds.Length);
                    Transform tr = trs[choice];

                    newHds[i] = hds[choice];
                    newTrs[i] = tr;
                    newTimers[i] = 0f;
                    newMovedirs[i] = 0f;

                    kvTI[(KVTI.Arrays, tr)] = i;
                    kvTI[(KVTI.Arrays, trs[trs.length - 1])] = choice;
                    kvOC[type].RemoveConcrete(tr);
                    kvOC[toType].Insert(tr);

                    group.HumanRemove(choice);
                }
            }

            FactoryHumans factoryHumans = _factoryHumans;
            TransformAccessArray humansTrs = toGroup.transforms;
            for (int i = toOldLength; i < toNewLength; ++i)
            {
                factoryHumans.ChangeStyle(humansTrs[i].transform, toGroup.type);
            }
        }

        private void Explosion(Vector3 pos, float distance)
        {
            Unity.Mathematics.Random random = _random.Value;
            var kvTI = _kvTransformIndex;
            NativeList<HumanGroupBlue> groupsBlue = _groupsBlue;
            HumanGroup groupWhite = _groupWhite;
            WorldTowers towers = _towers;
            WorldScore score = _score;

            TransformAccessArray towersToExplode = _towers.OcTree.FindNeighbors(pos, distance);
            TransformAccessArray whitesToExplode = _ocTreeWhites.FindNeighbors(pos, distance);
            TransformAccessArray bluesToExplode = _ocTreeBlues.FindNeighbors(pos, distance);

            for (int i = 0; i < towersToExplode.length; ++i)
            {
                Transform tr = towersToExplode[i];
                score.ScoreAddTower(towers.TowerDataGet(tr).type);

                towers.TowerDestroy(tr);
            }
            for (int i = 0; i < whitesToExplode.length; ++i)
            {
                int index = kvTI[(KVTI.Arrays, whitesToExplode[i])];
                score.ScoreAddHuman(HumanType.White, groupWhite.humanDatas[index]);

                HumanData hd = new(random.NextFloat(0f, 1f), random.NextFloat(0f, 1f), random.NextFloat(0f, 1f), random.NextFloat(0f, 1f), random.NextFloat(0.5f, 1f), random.NextFloat(0f, 0.2f));
                Vector3 v3z = (Vector3)random.NextFloat3Direction() * _radius + (Vector3)_center;
                groupWhite.humanDatas[index] = hd;
                groupWhite.transforms[index].position = v3z;
            }
            for (int i = 0; i < bluesToExplode.length; ++i)
            {
                Transform tr = bluesToExplode[i];
                int indexGroup = kvTI[(KVTI.Groups, tr)];
                int indexSubGroup = kvTI[(KVTI.SubGroups, tr)];
                int index = kvTI[(KVTI.Arrays, tr)];

                // Hell no!!!! I have to check Waiters also.
                // Assumption about Followers isn't right, because explosion distance can be more than seen distance!!!
                // BUG!!!
                // And I don't know how to fix it
                // It's almost the end, but it's a hard problem, because of the current architecture
                HumanGroupBlue groupBlue = groupsBlue[indexGroup];
                ref 
                if (indexSubGroup == 1)
                {

                }
                ref HumanGroup group = indexSubGroup == 0 ? ref groupBlue.groupFollowing : ref ;
                score.ScoreAddHuman(HumanType.Blue, groupBlue.groupFollowing.humanDatas[index]);
                HumanChange(ref groupBlue.groupFollowing, ref groupWhite, index);

                HumanData hd = new(random.NextFloat(0f, 1f), random.NextFloat(0f, 1f), random.NextFloat(0f, 1f), random.NextFloat(0f, 1f), random.NextFloat(0.5f, 1f), random.NextFloat(0f, 0.2f));
                Vector3 v3z = (Vector3)random.NextFloat3Direction() * _radius + (Vector3)_center;

                int newIndex = groupWhite.HumansCount - 1;
                groupWhite.humanDatas[newIndex] = hd;
                groupWhite.transforms[newIndex].position = v3z;

                groupsBlue[indexGroup] = groupBlue;
            }
        }
    }
}