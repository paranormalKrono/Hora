
using HumanComponents;
using NFactoryHumans;
using OcTreeData;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using static SOHumans;

namespace NWorld
{
    [System.Serializable]
    public struct WorldSpace
    {
        public float radius;
        public float radiusAdditional;
        public float radiusError;
        public float3 center;
    }

    public struct WorldExplosionData
    {
        public float whiteValue;
        public float redValue;
    }

    public class World
    {
        private SOWorld _cfg;
        private FactoryHumans _factoryHumans;

        private NativeReference<Unity.Mathematics.Random> _random = new(Allocator.Persistent);
        private WorldSpace _space;

        // private Dictionary<Transform, (HumanType, int)> _humansKV = new();

        private TransformAccessArray _whiteTransforms;
        private OcTree _whiteOcTree;
        private NativeList<Human> _whiteDatas;
        private NativeList<HMove> _whiteMoves;
        private NativeList<HMove> _whiteMovesUpdated;

        private TransformAccessArray _redTransforms;
        private OcTree _redOcTree;
        private NativeList<Human> _redDatas;
        private NativeList<HMove> _redMoves;
        private NativeList<HMove> _redMovesUpdated;

        private float _redAddTimer;
        private float _whiteAddTimer;

        private JobHandle _mainJob;

        public OcTree WhiteOcTree { get => _whiteOcTree; }
        public OcTree RedOcTree { get => _redOcTree; }

        public World(SOWorld cfg, FactoryHumans factoryHumans, uint seed)
        {
            _cfg = cfg;
            _factoryHumans = factoryHumans;

            // It should be here, because it will leak otherway
            _whiteTransforms = new(new Transform[0]);
            _whiteDatas = new(Allocator.Persistent);
            _whiteMoves = new(Allocator.Persistent);
            _whiteMovesUpdated = new(Allocator.Persistent);

            _redTransforms = new(new Transform[0]);
            _redDatas = new(Allocator.Persistent);
            _redMoves = new(Allocator.Persistent);
            _redMovesUpdated = new(Allocator.Persistent);

            WorldSpace space = cfg.space;
            Rectangle rect = new(space.center, space.radius + space.radiusError);

            _whiteOcTree = new OcTree(cfg.ocTreeCfg, _whiteTransforms, rect);
            _redOcTree = new OcTree(cfg.ocTreeCfg, _redTransforms, rect);

            _space = space;

            _random.Value = new Unity.Mathematics.Random(seed);
        }

        public void Destroy()
        {
            _mainJob.Complete();
            for (int i = 0; i < _whiteTransforms.length; ++i)
            {
                DestroyHuman(_whiteTransforms[i]);
            }
            for (int i = 0; i < _redTransforms.length; ++i)
            {
                DestroyHuman(_redTransforms[i]);
            }
            Dispose();
        }

        public void Dispose()
        {
            _mainJob.Complete();

            _whiteTransforms.Dispose();
            _whiteDatas.Dispose();
            _whiteMoves.Dispose();
            _whiteMovesUpdated.Dispose();
            _whiteOcTree.Dispose();

            _redTransforms.Dispose();
            _redDatas.Dispose();
            _redMoves.Dispose();
            _redMovesUpdated.Dispose();
            _redOcTree.Dispose();

            _random.Dispose();
        }

        public void Update(float delta)
        {
            var r = _random.Value;
            var random1 = new Unity.Mathematics.Random(r.NextUInt());
            var random2 = new Unity.Mathematics.Random(r.NextUInt());
            _random.Value = r;

            _whiteAddTimer -= delta;
            if (_whiteAddTimer < 0)
            {
                _whiteAddTimer = _cfg.whiteTimeToAdd;

                HumanCreate(HumanType.White);
            }

            _redAddTimer -= delta;
            if (_redAddTimer < 0)
            {
                _redAddTimer = _cfg.redTimeToAdd;

                HumanCreate(HumanType.Red);
            }

            _whiteOcTree.Update();
            _redOcTree.Update();

            JobHandle jw = new();
            if (_whiteTransforms.length > 0)
            {
                jw = new JobHuman()
                {
                    character = _cfg.whiteCharacter,
                    delta = delta,
                    humans = _whiteDatas,
                    moveDatas = _whiteMoves,
                    moveDatasNew = _whiteMovesUpdated,
                    random = random1,
                    space = _space
                }.Schedule(_whiteTransforms, _mainJob);

                var c = _whiteMoves;
                _whiteMoves = _whiteMovesUpdated;
                _whiteMovesUpdated = c;
            }

            JobHandle jr = new();
            if (_redTransforms.length > 0)
            {
                jr = new JobHuman()
                {
                    character = _cfg.redCharacter,
                    delta = delta,
                    humans = _redDatas,
                    moveDatas = _redMoves,
                    moveDatasNew = _redMovesUpdated,
                    random = random2,
                    space = _space
                }.Schedule(_redTransforms, _mainJob);

                var c = _redMoves;
                _redMoves = _redMovesUpdated;
                _redMovesUpdated = c;
            }

            _mainJob = JobHandle.CombineDependencies(jw, jr);
            // jw.Complete();
            // jr.Complete();
            // _mainJob.Complete();
        }

        // The slowest...
        public WorldExplosionData Explosion(Vector3 explosionPosition, float explosionRadius)
        {
            WorldExplosionData res = new();

            Rectangle explosionRect = new Rectangle(explosionPosition, explosionRadius);
            NativeList<int> whites = new NativeList<int>(Allocator.Temp);
            NativeList<int> reds = new NativeList<int>(Allocator.Temp);
            _whiteOcTree.Query(explosionRect, whites);
            _redOcTree.Query(explosionRect, reds);

            // Still, we have to remove distant ones
            TransformAccessArray whiteTrs = _whiteTransforms;
            for (int i = 0; i < whites.Length; ++i)
            {
                int id = whites[i];
                float distance = Vector3.Distance(whiteTrs[id].position, explosionPosition);
                if (distance > explosionRadius)
                {
                    whites.RemoveAtSwapBack(i);
                }
            }
            TransformAccessArray redTrs = _redTransforms;
            for (int i = 0; i < reds.Length; ++i)
            {
                int id = reds[i];
                float distance = Vector3.Distance(redTrs[id].position, explosionPosition);
                if (distance > explosionRadius)
                {
                    reds.RemoveAtSwapBack(i);
                }
            }

            for (int i = 0; i < whites.Length; i++)
            {
                res.whiteValue += CalculateHumanValue(HumanType.White, whites[i]);
            }
            for (int i = 0; i < reds.Length; i++)
            {
                res.redValue += CalculateHumanValue(HumanType.Red, reds[i]);
            }

            // When we remove human from the lists, current indexes become invalid
            // but we can prevent it by removing last ids first
            whites.Sort();
            reds.Sort();

            for (int i = whites.Length - 1; i >= 0; i--)
            {
                HumanRemove(HumanType.White, whites[i]);
            }
            for (int i = reds.Length - 1; i >= 0; i--)
            {
                HumanRemove(HumanType.Red, reds[i]);
            }

            whites.Dispose();
            reds.Dispose();

            return res;
        }


        private float CalculateHumanValue(HumanType type, int id)
        {
            float res = 0f;

            Human h = new();
            HumanCharacter character = new();
            if (type == HumanType.White)
            {
                h = _whiteDatas[id];
                character = _cfg.whiteCharacter;
            }
            else if (type == HumanType.Red)
            {
                h = _redDatas[id];
                character = _cfg.redCharacter;
            }

            res = character.baseValue + h.Scalar(character.value);

            return res;
        }

        private void HumanCreate(HumanType type)
        {
            var r = _random.Value;

            Transform tr = _factoryHumans.Create((int)type);

            // Additional properties

            float distance = r.NextFloat(0, _space.radius);
            float3 direction = r.NextFloat3Direction();
            tr.position = direction * distance + _space.center;

            Human h = new Human
            {
                fear = r.NextFloat(0.1f, 1f),
                health = r.NextFloat(0.1f, 1f),
                rebel = r.NextFloat(0.1f, 1f),
                smart = r.NextFloat(0.1f, 1f),
                soul = r.NextFloat(0.1f, 1f),
                tired = r.NextFloat(0.1f, 1f),
            };
            HMove hm = new HMove();

            int id = -1;
            if (type == HumanType.White)
            {
                id = _whiteTransforms.length;
                _whiteTransforms.Add(tr);
                _whiteOcTree.Insert(id); // Depends on transforms

                _whiteDatas.Add(h);
                _whiteMoves.Add(hm);
                _whiteMovesUpdated.Add(hm);
            }
            else if (type == HumanType.Red)
            {
                id = _redTransforms.length;
                _redTransforms.Add(tr);
                _redOcTree.Insert(id); // Depends on transforms

                _redDatas.Add(h);
                _redMoves.Add(hm);
                _redMovesUpdated.Add(hm);
            }

            // _humansKV.Add(tr, (type, id));

            // We need to save random state
            _random.Value = r;
        }

        // private void HumanRemove(Transform tr)
        // {
        //     (HumanType type, int id) = _humansKV[tr];
        //     HumanRemove(type, id);
        // }

        // Works with swapback, last id will become invalid!
        private void HumanRemove(HumanType type, int id)
        {
            Transform tr = null;
            int changed_id = -1;

            if (type == HumanType.White)
            {
                tr = _whiteTransforms[id];
                changed_id = _whiteTransforms.length - 1;

                _whiteOcTree.RemoveSwapBack(id, changed_id);
                _whiteTransforms.RemoveAtSwapBack(id);

                _whiteDatas.RemoveAtSwapBack(id);
                _whiteMoves.RemoveAtSwapBack(id);
                _whiteMovesUpdated.RemoveAtSwapBack(id);

            }
            else if (type == HumanType.Red)
            {
                tr = _redTransforms[id];
                changed_id = _redTransforms.length - 1;

                _redOcTree.RemoveSwapBack(id, changed_id);
                _redTransforms.RemoveAtSwapBack(id);

                _redDatas.RemoveAtSwapBack(id);
                _redMoves.RemoveAtSwapBack(id);
                _redMovesUpdated.RemoveAtSwapBack(id);
                
            }
            DestroyHumanWithEffects(tr);
        }

        private void DestroyHumanWithEffects(Transform tr)
        {
            // Add effects and sounds

            DestroyHuman(tr);
        }
        private void DestroyHuman(Transform tr)
        {
            _factoryHumans.Destroy(tr);
        }


    }

    [BurstCompile]
    public struct JobHuman: IJobParallelForTransform
    {
        [ReadOnly] public Unity.Mathematics.Random random;
        [ReadOnly] public float delta;

        [ReadOnly] public WorldSpace space;
        [ReadOnly] public NativeList<Human> humans;
        [ReadOnly] public NativeList<HMove> moveDatas;
        [ReadOnly] public HumanCharacter character;

        // I know what I do
        [WriteOnly, NativeDisableParallelForRestriction] public NativeList<HMove> moveDatasNew;

        public void Execute(int index, TransformAccess transform)
        {
            // Move
            HMove md = moveDatas[index];
            transform.position += (Vector3)(delta * md.moveDirection);

            // Update
            float t = md.updateTimer - delta;
            if (t < 0)
            {
                HMove new_md = new HMove();
                // We must have a different random for every index
                Unity.Mathematics.Random r = new(random.NextUInt() + (uint)index);
                Human h = humans[index];
                HumanCharacter ch = character;
                new_md.updateTimer = ch.baseUpdateTime + ch.updateTime.Scalar(h);

                float speed = ch.baseSpeed + ch.speed.Scalar(h);
                new_md.moveDirection = r.NextFloat3Direction() * speed;
                moveDatasNew[index] = new_md;
            }
            else
            {
                moveDatasNew[index] = new HMove
                {
                    updateTimer = t,
                    moveDirection = md.moveDirection
                };
            }

            // If off sphere limits
            WorldSpace sp = space;
            float distanceToCenter = Vector3.Distance(transform.position, sp.center);
            if (distanceToCenter > sp.radius + sp.radiusAdditional)
            {
                // We place him inside the sphere in the opposite direction from the center
                float3 dir = math.normalize((float3)transform.position - sp.center);
                transform.position = sp.center - dir * (sp.radius - 1);
            }
        }
    }

}