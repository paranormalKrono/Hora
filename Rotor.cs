using UnityEngine;

public class Rotor : MonoBehaviour
{
    [SerializeField] private Transform trToRotate;
    [SerializeField] private Transform tiltToRotate;
    [SerializeField] private Transform tilt1;
    [SerializeField] private Transform tilt2;
    [SerializeField] private float rotationForceMin = 1f;
    [SerializeField] private float rotationForceMax = 1f;
    [SerializeField] private float maxSpeed = 6f;
    [SerializeField] private float tiltSpeed = 0.05f;

    private float currentForce = 1f;
    private float currentSpeed = 0f;
    private float targetDir = 1f;

    private float dirSide = 1f;
    private bool isTiltSide = true;

    private void Start()
    {
        if (UnityEngine.Random.Range(-1f, 1f) > 0f)
        {
            dirSide = -1f;
        }
        else
        {
            dirSide = 1f;
        }
        isTiltSide = UnityEngine.Random.Range(-1f, 1f) > 0f;

        currentForce = UnityEngine.Random.Range(rotationForceMin, rotationForceMax);
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        Vector3 v3 = transform.up;
        trToRotate.rotation *= Quaternion.Euler(currentSpeed * delta * v3);

        if (currentSpeed * dirSide < maxSpeed)
        {
            currentSpeed += dirSide * currentForce * delta;
        }

        if ((maxSpeed - currentSpeed * dirSide) < 0.1f)
        {
            currentForce = UnityEngine.Random.Range(rotationForceMin, rotationForceMax);
            dirSide = -dirSide; 
        }

        if (isTiltSide)
        {
            tiltToRotate.rotation = Quaternion.Lerp(tiltToRotate.rotation, tilt1.rotation, delta * tiltSpeed);
            if (Quaternion.Angle(tiltToRotate.rotation, tilt1.rotation) < 2f)
            {
                isTiltSide = false;
            }
        }

        if (!isTiltSide)
        {
            tiltToRotate.rotation = Quaternion.Lerp(tiltToRotate.rotation, tilt2.rotation, delta * tiltSpeed);
            if (Quaternion.Angle(tiltToRotate.rotation, tilt2.rotation) < 2f)
            {
                isTiltSide = true;
            }
        }

    }
}
