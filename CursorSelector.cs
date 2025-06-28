using UnityEngine;
using UnityEngine.InputSystem;

public class CursorSelector: MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private float _distance;
    [SerializeField] private LayerMask _layer;

    public bool TrySelectRaycast(out Transform target)
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _camera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

        if (Physics.Raycast(ray, out RaycastHit hitInfo, _distance, _layer))
        {
            target = hitInfo.transform;
            return true;
        }
        target = null;
        return false;
    }
}