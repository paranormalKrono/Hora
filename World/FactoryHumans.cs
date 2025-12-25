
using UnityEngine;

namespace NFactoryHumans
{

    public class FactoryHumans: MonoBehaviour
    {
        [SerializeField] SOHumans cfg;

        public Transform Create(int type)
        {
            GameObject prefab = cfg.prefabs[type];
            GameObject inst = Instantiate(prefab);
            return inst.transform;
        }
        
        public void Destroy(Transform tr)
        {
            Destroy(tr.gameObject);
        }
    }

}

// public class Baker: Baker<FactoryHumansAuthoring>
// {
//     public override void Bake(FactoryHumansAuthoring authoring)
//     {
//         Entity entity = GetEntity(TransformUsageFlags.None);
// 
//         SOHumans cfg = authoring.config;
//         int count = SOHumans.HumansTypesCount;
//         DynamicBuffer<Entity> prefabs = new();
//         prefabs.EnsureCapacity(count);
//         for (int i = 0; i < count; ++i)
//         {
//             prefabs[i] = GetEntity(cfg.prefabs[i], TransformUsageFlags.Dynamic);
//         }
//         AddComponent(entity, new FactoryHumans
//         {
//             prefabs = prefabs,
//         });
//     }
// }

// public struct FactoryHumans: IComponentData
// {
//     public DynamicBuffer<Entity> prefabs;
// }
// 
// public struct FactoryHumansActivate
// {
//     public int toCreate;
//     public HumanType humanType;
// }

// public partial class SystemFactoryHumans: SystemBase
// {
//     Entity[] entities;
// 
//     protected override void OnCreate()
//     {
//         RequireForUpdate()
//     }
// 
//     protected override void OnUpdate()
//     {
//         foreach (var ob = SystemAPI.Query(FactoryHumans, FactoryHumansActivate))
//             FactoryHumans factoryHumans = SystemAPI.GetSingleton<FactoryHumans>();
// 
//         EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
// 
//         entities = new Entity[SOHumans.HumansTypesCount];
//         for (int i = 0; i < entities.Length; i++)
//         {
//             Entity toSpawn = factoryHumans.prefabs[i];
//             Entity e = entityCommandBuffer.Instantiate(toSpawn);
// 
//             entityCommandBuffer.SetComponent(e, new Human());
//         }
//     }
// }