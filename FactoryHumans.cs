using UnityEngine;

namespace NamespaceFactoryHumans
{
    public enum HumanType
    {
        White,
        Red,
        Blue
    }

    public class FactoryHumans: ScriptableObject
    {

        [SerializeField] public GameObject _prefab;

        [System.Serializable]
        private struct HumanProto
        {
            [SerializeField] public Material _style;
        }

        public const int HumansTypesCount = 4;
        private HumanProto[] _protos = new HumanProto[HumansTypesCount];

        public Transform Create(Vector3 pos, Quaternion rot)
        {
            GameObject inst = Instantiate(_prefab, pos, rot);
            return inst.transform;
        }

        public void ChangeStyle(Transform tr, HumanType type)
        {
            tr.GetComponent<MeshRenderer>().material = _protos[(int)type]._style;
        }

        public void Destroy(Transform tr)
        {
            Destroy(tr.gameObject);
        }
    }
}