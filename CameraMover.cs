using System.Collections;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    [SerializeField] private Transform _cameraTr;
    [SerializeField] private Transform _gameCenter;

    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _rotationSpeed = 15f;
    [SerializeField] private float _sphereSpeed = 25f;

    private bool _isMoving = false;
    private IEnumerator _IMove;

    public delegate void Callback();

    public void CameraSphereLook(Vector2 axis)
    {
        Vector3 right = _cameraTr.up * axis.x;
        Vector3 left = _cameraTr.right * axis.y;
        _cameraTr.RotateAround(_gameCenter.position, left + right, _sphereSpeed);
    }

    public void MoveCameraTo(Transform target, Callback OnMoved = null)
    {
        if (_isMoving)
        {
            StopCoroutine(_IMove);
        }
        _IMove = IMove(target.position, target.rotation, OnMoved);
        StartCoroutine(_IMove);
    }

    private IEnumerator IMove(Vector3 point, Quaternion rotation, Callback OnMoved)
    {
        _isMoving = true;

        Transform tr = _cameraTr;
        while (Vector3.Distance(tr.position, point) > 0.1f || Quaternion.Angle(tr.rotation, rotation) > 1f)
        {
            float delta = Time.deltaTime;
            tr.SetPositionAndRotation(Vector3.MoveTowards(tr.position, point, delta * _moveSpeed), 
                Quaternion.RotateTowards(tr.rotation, rotation, delta * _rotationSpeed));
            yield return null;
        }

        OnMoved?.Invoke();
        _isMoving = false;
    }
}
