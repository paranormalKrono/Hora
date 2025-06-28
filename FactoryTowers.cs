using System.Collections.Generic;
using UnityEngine;

namespace NamespaceFactoryTowers
{
    public enum TowerType
    {
        Center,
        Statue,
        MinistryLove,
        MinistryTruth,
    }

    public struct TowerData
    {
        public TowerType type;
        public int level;

        public TowerData(TowerType type, int level)
        {
            this.type = type;
            this.level = level;
        }
    }

    public class FactoryTowers: ScriptableObject
    {
        private const int TypesCount = 4;

        [System.Serializable]
        public struct TowerProto
        {
            [SerializeField] public GameObject _prefab;
            [SerializeField] public float _explosionRange;
        }
         
        [System.Serializable]
        public struct TowerProtoMinistryLoveCfg
        {
            [SerializeField] public int humansCount;
            [SerializeField] public float pointMin;
            [SerializeField] public float pointMax;
        }

        public TowerProto[] _protos = new TowerProto[TypesCount];
        public TowerProtoMinistryLoveCfg[] _protoMinistryLoveLevels;

        public Transform CreateTower(TowerType type, Vector3 pos, Quaternion rot)
        {
            int protoID = (int)type;
            TowerProto proto = _protos[protoID];
            GameObject inst = Instantiate(proto._prefab, pos, rot);

            return inst.transform;
        }

    }
}