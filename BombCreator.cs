using UnityEngine;

public class BombCreator : MonoBehaviour
{
    [SerializeField] private AudioSource _bombSound;
    [SerializeField] private GameObject _bombPrefab;
    [SerializeField] private Mapper _mapper;
    [SerializeField] private Main _main;
    [SerializeField] private Transform _gameCenter;
    [SerializeField] private Transform _cameraTr;
    [SerializeField] private float bombMoveSpeed = 2.0f;

    [SerializeField] private float _bombRadius = 3f;

    private GameObject curBomb;
    private bool isBombCreated = false;

    public bool IsBombCreated { get { return isBombCreated; } }

    private void Start()
    {
        _main.OnGameStateChanged += (Main.GameState gameState) =>
        {
            if (gameState != Main.GameState.Game)
            {
                RemoveBomb();
            }
        };
    }

    public void CreateBomb()
    {
        if (!isBombCreated)
        {
            isBombCreated = true;
            curBomb = Instantiate(_bombPrefab, _gameCenter);
        }
    }

    public void MoveBomb(Vector2 axis)
    {
        if (isBombCreated)
        {
            curBomb.transform.position += (_cameraTr.right * axis.x + _cameraTr.up * axis.y) * Time.deltaTime * bombMoveSpeed;
        }
    }

    public void MoveBombScroll(float value)
    {
        if (isBombCreated)
        {
            curBomb.transform.position += value * Time.deltaTime * bombMoveSpeed * _cameraTr.forward;
        }
    }

    public void Explode()
    {
        if (isBombCreated)
        {
            Explode(curBomb.transform.position);
        }
    }

    public void RemoveBomb()
    {
        if (isBombCreated)
        {
            isBombCreated = false;
            Destroy(curBomb);
        }
    }

    private void Explode(Vector3 p)
    {
        _bombSound.transform.position = curBomb.transform.position;
        _bombSound.Play();
        Destroy(curBomb);
        float humStr = 0f;
        float redStr = 0f;
        _mapper.GetAreaStrength(p, _bombRadius, ref humStr, ref redStr);
        _main.Explode(humStr, redStr);
        isBombCreated = false;
    }
}
