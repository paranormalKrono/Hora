using UnityEngine;
using UnityEngine.Events;
using NamespaceWorld;
using NamespaceFactoryTowers;
using NamespaceFactoryHumans;

public class Mapper: MonoBehaviour
{
    [SerializeField] private WorldCfg _worldCfg;
    [SerializeField] private FactoryTowers _factoryTowers;
    [SerializeField] private FactoryHumans _factoryHumans;

    [Header("Config")]
    [SerializeField] private int _interloopBatchCount = 64;

    [Header("White")]
    [SerializeField] private int _whiteStartCount = 40;
    [SerializeField] private int _whiteCountChange = 6;
    [SerializeField] private int _whiteCountChangeCoeff = 6;
    [SerializeField] private float _whiteCountChangeTime = 8f;

    [Header("Red")]
    [SerializeField] private int _redCount = 10;
    [SerializeField] private int _redCountChange = 2;

    private bool _isGameBuilt = false;
    public bool _isGame = false;

    private int _curWhiteCountChange;
    private float _curWhiteCountChangeTime;

    public World world;
    public UnityAction<Transform> OnExplosionTower;

    private void Update()
    {
        if (_isGameBuilt && _isGame)
        {
            float delta = Time.deltaTime;

            world.Update();

            float countTimer = _curWhiteCountChangeTime -= delta;
            if (countTimer < 0)
            {
                countTimer = _whiteCountChangeTime;

                world.HumansCreateWhite(_curWhiteCountChange);
                _curWhiteCountChange = (int)(_curWhiteCountChange * _whiteCountChangeCoeff);
            }
            _curWhiteCountChangeTime = countTimer;
        }
    }

    public void SetGame(bool isGame)
    {
        _isGame = isGame;
    }

    public void Build(uint seed)
    {
        if (!_isGameBuilt)
        {
            _isGameBuilt = true;

            world = new(_worldCfg,_factoryHumans, _factoryTowers);

            _curWhiteCountChange = _whiteCountChange;
            _curWhiteCountChangeTime = 0f;

            world.HumansCreateWhite(_whiteStartCount);
        }
    }

    public void Unbuild()
    {
        if (_isGameBuilt)
        {
            _isGameBuilt = false;

            world.DestroyDispose();
        }
    }
}