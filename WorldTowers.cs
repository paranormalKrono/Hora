using System.Collections.Generic;
using UnityEngine;
using NamespaceFactoryTowers;
using Unity.Collections;
using UnityEngine.Jobs;
using NamespaceOcTree;
using UnityEngine.Events;

namespace NamespaceWorld 
{

    [System.Serializable]
    public class WorldTowersCfg
    {
        public Vector3 center;
        public float radius;
        public float speedMove;
        public float speedScroll;
        public Vector3[] towersPositions;
        public Quaternion[] towersRotations;
        public TowerData[] towersData;
    }

    public class WorldTowers: MonoBehaviour
    {
        private Vector3 _center;
        private float _radius;
        private float _speedMove;
        private float _speedScroll;

        private TransformAccessArray _transforms;
        private NativeList<TowerData> _datas;
        private Dictionary<Transform, int> _kv;
        private NativeOcTree _ocTree;

        private FactoryTowers _factory;
        private Transform _cameraTr;

        private Transform _selectedTower;
        private TowerData _selectedTowerData;

        private UnityAction<Transform> OnTowerRemoved;

        public int TowersCount { get => _transforms.length; }
        public NativeOcTree OcTree { get =>  _ocTree; }

        public enum SelectionState
        {
            Nothing,
            TowerSelected
        }

        private SelectionState _state;

        public SelectionState State { get => _state; }

        public WorldTowers(WorldTowersCfg cfg, FactoryTowers factory, Transform cameraTr)
        {
            _center = cfg.center;
            _radius = cfg.radius;
            _speedMove = cfg.speedMove;
            _speedScroll = cfg.speedScroll;

            var poss = cfg.towersPositions;
            var data = cfg.towersData;
            for (int i = 0; i < poss.Length; ++i)
            {
                var pos = poss[i];
                var d = data[i];
                TowerCreateSelect(d.type);
            }

            _transforms = new();
            _datas = new();
            _kv = new();
            Rectangle rect = new(_center, _radius);
            _ocTree = new(rect);

            _factory = factory;
            _cameraTr = cameraTr;
        }

        public void DestroyDispose()
        {
            var trs = _transforms;
            for (int i = 0; i < trs.length; ++i)
            {
                TowerDestroy(trs[i]);
            }
            trs.Dispose();

            if (_state == SelectionState.TowerSelected)
            {
                TowerSelectedDestroy();
            }
            _state = SelectionState.Nothing;

            _datas.Dispose();
            _ocTree.Dispose();
        }

        public Transform TowerGet(int index) => _transforms[index];

        public TowerData TowerDataGet(Transform tr)
        {
            return _datas[_kv[tr]];
        }

        public void TowerCreateSelect(TowerType towerType)
        {
            if (_state == SelectionState.Nothing)
            {
                _state = SelectionState.TowerSelected;

                Quaternion rot = Quaternion.Euler(0, UnityEngine.Random.Range(-180f, 180f), 0);
                Transform tr = _factory.CreateTower(towerType, _center, rot);

                _selectedTowerData = new TowerData(towerType, 0);
                _selectedTower = tr;
            }
        }

        public void TowerSelectedMove(Vector2 axis)
        {
            if (_state == SelectionState.TowerSelected)
            {
                // Update tower in ocTree
                _ocTree.UpdateConcrete(_selectedTower);
                transform.position += (_cameraTr.right * axis.x + _cameraTr.up * axis.y) * Time.deltaTime * _speedMove;
            }
        }

        public void TowerSelectedMoveScroll(float value)
        {
            if (_state == SelectionState.TowerSelected)
            {
                transform.position -= value * Time.deltaTime * _speedScroll * _cameraTr.forward;
            }
        }

        public void TowerSelectedPlace()
        {
            if (_state == SelectionState.TowerSelected)
            {
                _state = SelectionState.Nothing;

                Transform tr = _selectedTower;
                TowerData data = _selectedTowerData;

                _transforms.Add(tr);
                _kv.Add(tr, _transforms.length - 1);
                _datas.Add(data);
                _ocTree.Insert(tr);
            }
        }

        public void TowerSelect(Transform tr)
        {
            if (_state == SelectionState.Nothing)
            {
                _state = SelectionState.TowerSelected;

                int index = _kv[tr];

                _selectedTower = tr;
                _selectedTowerData = _datas[index];

                Transform prevLast = _transforms[index];
                _kv[prevLast] = index;
                _transforms.RemoveAtSwapBack(index);
                _kv.Remove(tr);
                _datas.RemoveAtSwapBack(index);
                _ocTree.RemoveConcrete(tr);

            }
        }

        public void TowerSelectedDestroy()
        {
            if (_state == SelectionState.TowerSelected)
            {
                _state = SelectionState.Nothing;
                Destroy(_selectedTower.gameObject);
            }
        }


        public void TowerDestroy(Transform tr)
        {
            Destroy(tr.gameObject);
        }
    }
}