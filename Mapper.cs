
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Mapper : MonoBehaviour
{
    [SerializeField] private Main _main;
    [SerializeField] private MenuUI _MenuUI;
    [SerializeField] private GameObject _mainMenuLocation;
    [SerializeField] private GameObject _settingsLocation;
    [SerializeField] private GameObject _gameLocation;
    [SerializeField] private GameObject _cameraGamePlace;

    [SerializeField] private Transform _gameCenter;
    [SerializeField] private GameObject _humanPrefab;
    [SerializeField] private GameObject _redPrefab;
    [SerializeField] private GameObject _bombPrefab;

    [SerializeField] private float _areaRadius = 5f;

    [SerializeField] private int _humanCount = 50;
    [SerializeField] private int _redCount = 15;
    [SerializeField] private int _humanCountChange = 6;
    [SerializeField] private int _redCountChange = 2;
    [SerializeField] private int _humanWave = 3;
    [SerializeField] private int _redWave = 3;
    [SerializeField] private float _humanSpeed = 3f;
    [SerializeField] private float _redSpeed = 6f;
    [SerializeField] private float _humanSpeedVary = 2f;
    [SerializeField] private float _redSpeedVary = 4f;
    [SerializeField] private float _chanceForHumanToChangeDirection = 0.2f;
    [SerializeField] private float _chanceForRedToChangeDirection = 0.35f;
    [SerializeField] private float _chanceForHumanToChangeDirectionVary = 0.1f;
    [SerializeField] private float _chanceForRedToChangeDirectionVary = 0.1f;
    [SerializeField] private float _longUpdateTime = 0.2f;
    [SerializeField] private float _timeBetweenChecks = 0.03f;

    private class HumanS
    {
        public Vector3 moveDirection;
        public Transform tr;
        public readonly float chance;

        public HumanS(Vector3 dir, Transform t, float ch)
        {
            moveDirection = dir;
            tr = t;
            chance = ch;
        }

        public float GetStrength()
        {
            return chance * moveDirection.magnitude;
        }
    }

    // Domain Driven Design
    private List<HumanS> _humanList = new List<HumanS>();
    private List<HumanS> _redList = new List<HumanS>();

    private bool _isGameBuilt = false;
    private bool _isGame = false;
    private int _curHumanWave = 3;
    private int _curRedWave = 3;

    private IEnumerator _ILongUpdate;
    private IEnumerator _ILongUpdateList;

    private void Start()
    {
        _areaRadius = Vector3.Distance(_gameCenter.transform.position, _cameraGamePlace.transform.position);

        _MenuUI._OnHoraUp += () =>
        {
            _settingsLocation.SetActive(false);
            _mainMenuLocation.SetActive(false);
            _gameLocation.SetActive(true);
            BuildGame();
            ContinueGame();
        };

        _MenuUI._OnMainMenu += () =>
        {
            _settingsLocation.SetActive(true);
            _mainMenuLocation.SetActive(true);
            _gameLocation.SetActive(false);
        };

        _MenuUI._OnSwitchGameToMenu += () => StopGame();

        _main.OnGameStateChanged += (Main.GameState gameState) =>
        {
            if (gameState == Main.GameState.Defeat)
            {
                StopGame();
            }
        };
    }

    private void Update()
    {
        if (_isGame)
        {
            Move(_humanList);
            Move(_redList);
        }
    }

    public void GetAreaStrength(Vector3 point, float r, ref float humanStrength, ref float redStrength)
    {
        humanStrength = GetAreaStrength(_humanList, point, r);
        redStrength = GetAreaStrength(_redList, point, r);
    }

    public void Reset()
    {
        StopGame();
        _isGameBuilt = false;
        for (int i = 0; i < _humanList.Count; ++i)
        {
            Destroy(_humanList[i].tr.gameObject);
        }
        for (int i = 0; i < _redList.Count; ++i)
        {
            Destroy(_redList[i].tr.gameObject);
        }
        _humanList.Clear();
        _redList.Clear();
    }

    private float GetAreaStrength(List<HumanS> humans, Vector3 point, float r)
    {
        float v = 0f;
        for (int i = humans.Count - 1; i > 0; i--)
        {
            HumanS humanS = humans[i];
            if (Vector3.Distance(humanS.tr.position, point) < r)
            {
                v += humanS.GetStrength();
                humans.RemoveAt(i);
                Destroy(humanS.tr.gameObject);
            }
        }
        return v;
    }

    private void BuildGame()
    {
        if (!_isGameBuilt)
        {
            _isGameBuilt = true;
            Spawn(_humanList, _humanCount, _humanPrefab, _chanceForHumanToChangeDirection, _chanceForHumanToChangeDirectionVary, _humanSpeed, _humanSpeedVary);
            Spawn(_redList, _redCount, _redPrefab, _chanceForRedToChangeDirection, _chanceForRedToChangeDirectionVary, _redSpeed, _redSpeedVary);
        }
    }

    private void ContinueGame()
    {
        if (!_isGame)
        {
            _isGame = true;
            _ILongUpdate = ILongUpdate();
            StartCoroutine(_ILongUpdate);
        }
    }

    private void StopGame()
    {
        if (_isGame)
        {
            _isGame = false;
            StopCoroutine(_ILongUpdate);
            StopCoroutine(_ILongUpdateList);
        }
    }

    private IEnumerator ILongUpdate()
    {
        while (_isGame)
        {
            _ILongUpdateList = ILongUpdateList(_redList, _redSpeed, _redSpeedVary);
            yield return StartCoroutine(_ILongUpdateList);
            _ILongUpdateList = ILongUpdateList(_humanList, _humanSpeed, _humanSpeedVary);
            yield return StartCoroutine(_ILongUpdateList);

            SpawnHumansWave();
            SpawnRedWave();

            yield return new WaitForSeconds(_longUpdateTime);
        }
    }

    private void SpawnRedWave()
    {
        if (_redCount > _redList.Count)
        {
            _curRedWave -= 1;
            if (_curRedWave <= 0)
            {
                _curHumanWave -= 1;

                _redCountChange *= 2;
                _redCount *= 2;
                _redWave *= 2;

                _curRedWave = _redWave;
                SpawnHumansWave();

                Spawn(_redList, _redCountChange, _redPrefab, _chanceForRedToChangeDirection, _chanceForRedToChangeDirectionVary, _redSpeed, _redSpeedVary);
            }
        }
    }

    private void SpawnHumansWave()
    {
        if (_humanCount > _humanList.Count)
        {
            _curHumanWave -= 1;
            if (_curHumanWave <= 0)
            {
                _humanCountChange *= 2;
                _humanCount *= 2;
                _humanWave *= 2;

                _curHumanWave = _humanWave;
                Spawn(_humanList, _humanCountChange, _humanPrefab, _chanceForHumanToChangeDirection, _chanceForHumanToChangeDirectionVary, _humanSpeed, _humanSpeedVary);
            }
        }
    }

    private IEnumerator ILongUpdateList(List<HumanS> list, float speed, float vary)
    {
        int l = list.Count;
        float[] floats = new float[l];
        for (int i = 0; i < l; i++)
        {
            floats[i] = UnityEngine.Random.Range(0f, 1f);
        }
        float r = _areaRadius;
        Vector3 c = _gameCenter.position;
        for (int i = 0; i < list.Count; i++)
        {
            HumanS human = list[i];
            if (Vector3.Distance(human.tr.position, c) > r)
            {
                // Someone wrote it here. What a mess...
                // human.tr.position -= human.moveDirection.normalized * r;
                // we can delete him though... or no... i'm going insane after 12 hours.
                PlaceHumanRandom(human);
                human.moveDirection = GetMoveDirection(speed, vary);
            }
            else if (human.chance > floats[i])
            {
                human.moveDirection = GetMoveDirection(speed, vary);
            }
            yield return new WaitForSeconds(_timeBetweenChecks);
        }
    }

    private void Spawn(List<HumanS> humanS, int newcount, GameObject prefab, float ch, float chv, float sp, float spv)
    {
        float r = _areaRadius - 1;
        Quaternion q = Quaternion.identity;
        Vector3 c = _gameCenter.transform.position;
        for (int i = 0; i < newcount; i++)
        {
            Transform h = Instantiate(prefab, c + r * UnityEngine.Random.onUnitSphere, q).transform;

            // Give additional qualities.

            float cht = ch + UnityEngine.Random.Range(-chv, chv);
            humanS.Add(new HumanS(GetMoveDirection(sp, spv), h, cht));
        }
    }

    private void PlaceHumanRandom(HumanS humanS)
    {
        Vector3 c = _gameCenter.transform.position;
        float r = _areaRadius - 1;
        humanS.tr.position = c + r * UnityEngine.Random.onUnitSphere;
    }

    private void Move(List<HumanS> list)
    {
        int c = list.Count;
        for (int i = 0; i < c; ++i)
        {
            HumanS human = list[i];
            human.tr.position += human.moveDirection * Time.deltaTime;
        }
    }

    private Vector3 GetMoveDirection(float speed, float speedVary)
    {
        return UnityEngine.Random.onUnitSphere * (speed + UnityEngine.Random.Range(-speedVary, speedVary));
    }
}
