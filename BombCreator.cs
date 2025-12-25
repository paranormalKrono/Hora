using UnityEngine;

public class BombCreator : MonoBehaviour
{
    [SerializeField] private AudioSource _bombSound;
    [SerializeField] private GameObject _bombPrefab;

    [SerializeField] private Transform _gameCenter;
    [SerializeField] private Transform _cameraTr;

    [SerializeField] private float _bombMoveSpeed = 2.0f;
    [SerializeField] private float _bombScrollSpeed = 12.0f;
    [SerializeField] private float _bombRadius = 3f;

    public struct ExplosionData
    {
        public Vector3 position;
        public float radius;
    }

    private Transform bomb;
    private bool isBombCreated = false;

    public bool IsBombCreated { get { return isBombCreated; } }

    public void CreateBomb()
    {
        if (!isBombCreated)
        {
            isBombCreated = true;
            bomb = Instantiate(_bombPrefab, _gameCenter).transform;
        }
    }

    public void MoveBomb(Vector2 axis)
    {
        if (isBombCreated)
        {
            bomb.position += _bombMoveSpeed * Time.deltaTime * (_cameraTr.right * axis.x + _cameraTr.up * axis.y);
        }
    }

    public void MoveBombScroll(float value)
    {
        if (isBombCreated)
        {
            bomb.position += value * Time.deltaTime * _bombScrollSpeed * _cameraTr.forward;
        }
    }

    public ExplosionData Explode()
    {
        if (isBombCreated)
        {
            Vector3 pos = bomb.position;
            Explode(pos);
            return new ExplosionData 
            {
                position = pos, 
                radius = _bombRadius 
            };
        }
        return new ExplosionData();
    }

    public void RemoveBomb()
    {
        if (isBombCreated)
        {
            isBombCreated = false;
            Destroy(bomb.gameObject);
        }
    }

    private void Explode(Vector3 p)
    {
        _bombSound.transform.position = bomb.position;
        _bombSound.Play();
        Destroy(bomb.gameObject);
        isBombCreated = false;
    }
}
