using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CameraMover : MonoBehaviour
{

    [SerializeField] private MenuUI _MenuUI;
    [SerializeField] private float _menuMoveSpeed = 10f;
    [SerializeField] private float _menuRotationSpeed = 15f;
    [SerializeField] private float _gameSpeed = 25f;
    [SerializeField] private Transform _menuPlace;
    [SerializeField] private Transform _settingsPlace;
    [SerializeField] private Transform _gamePlace;
    [SerializeField] private Transform _gameCenter;

    private bool _isMoving = false;
    private bool _isGame = false;
    private IEnumerator _IMove;

    public UnityAction OnMoved;

    private void Start()
    {
        _MenuUI._OnSwitchMenuToSettings += () => MoveCameraTo(_settingsPlace);
        _MenuUI._OnSwitchSettingsToMenu += () => MoveCameraTo(_menuPlace);
        _MenuUI._OnSwitchMenuToGame += () => MoveCameraTo(_gamePlace);
        _MenuUI._OnSwitchGameToMenu += () =>
        {
            _isGame = false;
            MoveCameraTo(_menuPlace);
        };

        _MenuUI._OnHoraUp += () => _isGame = true;
    }

    public void CameraLook(Vector2 axis)
    {
        if (_isGame)
        {
            Vector3 right = transform.up * axis.x;
            Vector3 left = transform.right * axis.y;
            transform.RotateAround(_gameCenter.position, left + right, _gameSpeed);
            // Quaternion lookRotation = Quaternion.LookRotation(_gameCenter.position - transform.position, transform.up);
        }
    }

    private void MoveCameraTo(Transform target)
    {
        if (_isMoving)
        {
            StopCoroutine(_IMove);
        }
        _IMove = IMove(target.position, target.rotation);
        StartCoroutine(_IMove);
    }

    private IEnumerator IMove(Vector3 point, Quaternion rotation)
    {
        _isMoving = true;
        while (Vector3.Distance(transform.position, point) > 0.1f || Quaternion.Angle(transform.rotation, rotation) > 1f)
        {
            float delta = Time.deltaTime;
            transform.SetPositionAndRotation(Vector3.MoveTowards(transform.position, point, delta * _menuMoveSpeed), 
                Quaternion.RotateTowards(transform.rotation, rotation, delta * _menuRotationSpeed));
            yield return null;
        }
        if (OnMoved != null)
        {
            OnMoved();
        }
        _isMoving = false;
    }
}
