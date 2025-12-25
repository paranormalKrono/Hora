using UnityEngine;
using NFactoryHumans;
using NWorld;

public class Mapper: MonoBehaviour
{
    [SerializeField] private SOWorld _worldCfg;
    [SerializeField] private FactoryHumans _factoryHumans;

    [SerializeField] private OcTreeVisual.OcTreeVisual _whitesOcTreeVisual;
    [SerializeField] private OcTreeVisual.OcTreeVisual _redsOcTreeVisual;

    public enum State
    {
        None,
        Paused,
        Game
    }

    private State _state;

    private World _world;

    private void Update()
    {
        if (_state == State.Game)
        {
            float delta = Time.deltaTime;

            _world.Update(delta);
        }
    }

    public void SetState(State state)
    {
        State prev = _state;
        if (prev == State.None)
        {
            if (state == State.Paused)
            {
                Build();
            }
        }
        else // prev = Paused|Game
        if (state == State.None)
        {
            Unbuild();
        }
        _state = state;
    }

    public WorldExplosionData Explosion(Vector3 explosionPosition, float explosionRadius)
    {
        return _world.Explosion(explosionPosition, explosionRadius);
    }

    public void Build()
    {
        if (_world != null)
        {
            Unbuild();
        }
        uint seed = (uint)Random.Range(0, int.MaxValue - 1);
        _world = new World(_worldCfg, _factoryHumans, seed);

        if (_worldCfg.isDebugMode)
        {
            var rootWhites = _world.WhiteOcTree.GetRoot();
            _whitesOcTreeVisual.Initialize(rootWhites.nodes, rootWhites.rootBoundary);

            var rootReds = _world.RedOcTree.GetRoot();
            _redsOcTreeVisual.Initialize(rootReds.nodes, rootReds.rootBoundary);
        }
    }

    public void Unbuild()
    {
        if (_world != null)
        {
            _world.Destroy();
            _world = null;

            if (_worldCfg.isDebugMode) 
            {
                _redsOcTreeVisual.Stop();
                _whitesOcTreeVisual.Stop();
            }
        }
    }

    public void OnDestroy()
    {
        if (_world != null)
        {
            _world.Dispose();
        }
    }
}