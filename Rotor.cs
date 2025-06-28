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

    private float currentForce;
    private float rotationDirection;
    private bool tiltSide;

    private float currentSpeed = 0f;

    private void Start()
    {
        Unity.Mathematics.Random random = new Unity.Mathematics.Random();
        tiltSide = random.NextFloat(-1f, 1f) > 0f;
        rotationDirection = random.NextFloat(-1f, 1f) > 0f ? -1f : 1f;

        currentForce = random.NextFloat(rotationForceMin, rotationForceMax);
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        // Rotation
        Vector3 v3 = transform.up;
        if (currentSpeed * rotationDirection < maxSpeed)
        {
            currentSpeed += rotationDirection * currentForce * delta;
        }
        if ((maxSpeed - currentSpeed * rotationDirection) < 0.1f)
        {
            currentForce = Random.Range(rotationForceMin, rotationForceMax);
            rotationDirection = -rotationDirection;
        }
        trToRotate.rotation *= Quaternion.Euler(currentSpeed * delta * v3);

        // Tilt
        Quaternion targetTilt = tiltSide ? tilt1.rotation : tilt2.rotation;
        tiltToRotate.rotation = Quaternion.Lerp(tiltToRotate.rotation, targetTilt, delta * tiltSpeed);
        if (Quaternion.Angle(tiltToRotate.rotation, targetTilt) < 2f)
            tiltSide = !tiltSide;
    }
}