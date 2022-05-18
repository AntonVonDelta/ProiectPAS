using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


public class PlayerController : MonoBehaviour {
    public Camera playerCamera;
    public GameObject motorParticleSystem;
    public float moveForceMagnitude = 5f;
    public float moveForceAcceleratedMultiplier = 1.5f;

    public float rotateAmount = 2.5f;
    public float mouseMultiplier = 2f;
    public float smoothTime = 0.3f;
    public float lengthFromCenterToBack = 1;
    public float oceanSurface = 20;

    private Rigidbody rb;
    private Vector3 localCameraPosition;
    private Vector3 localMotorParticlePosition;
    private Vector3 cameraMovementVelocity = Vector3.zero;


    void Start() {
        rb = GetComponent<Rigidbody>();

        localCameraPosition = transform.InverseTransformPoint(playerCamera.transform.position);
        localMotorParticlePosition = transform.InverseTransformPoint(motorParticleSystem.transform.position);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void FixedUpdate() {
        // Move particle system
        //motorParticleSystem.transform.position = transform.TransformPoint(localMotorParticlePosition);
        //motorParticleSystem.transform.rotation = transform.rotation;

        // Move camera to follow vehicle
        Vector3 cameraWorldPos = transform.TransformPoint(localCameraPosition);
        playerCamera.transform.position = Vector3.SmoothDamp(playerCamera.transform.position, cameraWorldPos, ref cameraMovementVelocity, smoothTime);

        // Move camera in the direction the vehicle is pointing
        //Vector3 topPosition = transform.position + transform.TransformVector(forwardOfVehiclerReference * distanceReference);
        //playerCamera.transform.position = topPosition;
        //playerCamera.transform.rotation = Quaternion.LookRotation(transform.TransformDirection(forwardOfVehiclerReference), transform.TransformDirection(upwardsOfVehicleReference));

        // Rotate camera
        playerCamera.transform.LookAt(transform);

        if (IsMouseOverGameWindow()) {
            Vector3 positionDelta = new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseMultiplier;
            Vector3 viewportPositionDelta = playerCamera.ScreenToViewportPoint(positionDelta);

            float deltaAngleX = Mathf.Rad2Deg * Mathf.Atan2(viewportPositionDelta.x, playerCamera.nearClipPlane);
            // We inverse the Y value because Unity has the viewport inversed in comparison to the screen space
            float deltaAngleY = Mathf.Rad2Deg * Mathf.Atan2(-viewportPositionDelta.y, playerCamera.nearClipPlane);

            // Act with a force on the tail of the vehicler
            // We have to negate viewportPositionDelta because we are acting on the tail and thus pulling up will lower the nose
            Vector3 localMovementToGlobal = transform.TransformVector(-viewportPositionDelta);
            rb.AddForceAtPosition(localMovementToGlobal * moveForceMagnitude,
                transform.position - transform.forward * lengthFromCenterToBack * transform.localScale.z, ForceMode.VelocityChange);

            // Restrict X axis angle
            Vector3 localEulerAngles = transform.localRotation.eulerAngles;
            localEulerAngles.x = Mathf.Clamp(ChangeAngleInterval(localEulerAngles.x), -60, 54);
            localEulerAngles.z = 0; // Do not roll on Z axis
            transform.localRotation = Quaternion.Euler(localEulerAngles);
        }

        // Apply a forward force but until velocity is reached
        // Disallow upward movement if outside of water
        Vector3 forwardDirection = transform.forward;
        float moveMultiplier = 1;
        if (transform.position.y >= oceanSurface) {
            Vector3 worldForward = transform.TransformDirection(transform.forward);
            worldForward.y = 0;
            forwardDirection = transform.InverseTransformDirection(worldForward);
        }
        if (Input.GetKey(KeyCode.Space)) {
            moveMultiplier = moveForceAcceleratedMultiplier;
        }
        ApplyForceToReachVelocity(rb, Input.GetAxis("Vertical") * forwardDirection * moveMultiplier * moveForceMagnitude);

        // Apply force to keep vehicle underwater
        if (transform.position.y >= oceanSurface) {
            // Do not go above water
            Vector3 downDirection = transform.InverseTransformDirection(Vector3.down) * Mathf.Lerp(1f / 2, 1, (transform.position.y - oceanSurface) / 4) * moveMultiplier * moveForceMagnitude;
            rb.AddForce(downDirection);
        }

        // Rotate from keyboard
        transform.Rotate(Vector3.up, Input.GetAxis("Horizontal") * rotateAmount, Space.World);
    }


    private void OnDrawGizmos() {
        Gizmos.color = Color.red;

        if (rb != null) Gizmos.DrawSphere(transform.position + rb.centerOfMass, 0.4f);
        Gizmos.DrawWireSphere(transform.position - transform.forward * lengthFromCenterToBack * transform.localScale.z, 0.2f);
    }

    /// <summary>
    /// Changes the angle interval from [0-360) to (-180, 180]
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    float ChangeAngleInterval(float angle) {
        return (angle > 180) ? angle - 360 : angle;
    }

    /// <summary>
    /// Counter clockwise test
    /// </summary>
    bool IsAngleBetween(float angle, float min, float max) {
        if (min < max) {
            if (min < angle && angle < max) return true;
        } else {
            if (angle > min) return true;
            if (angle < max) return true;
        }
        return false;
    }

    bool IsMouseOverGameWindow() {
        var viewport = playerCamera.ScreenToViewportPoint(Input.mousePosition);
        if (viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) return false;
        return true;
    }


    //https://github.com/ditzel/UnityOceanWavesAndShip/blob/master/Waves/Assets/PhysicsHelper.cs
    public static void ApplyForceToReachVelocity(Rigidbody rigidbody, Vector3 velocity, float force = 1, ForceMode mode = ForceMode.Force) {
        if (force == 0 || velocity.magnitude == 0)
            return;

        velocity = velocity + velocity.normalized * 0.2f * rigidbody.drag;

        //force = 1 => need 1 s to reach velocity (if mass is 1) => force can be max 1 / Time.fixedDeltaTime
        force = Mathf.Clamp(force, -rigidbody.mass / Time.fixedDeltaTime, rigidbody.mass / Time.fixedDeltaTime);

        //dot product is a projection from rhs to lhs with a length of result / lhs.magnitude https://www.youtube.com/watch?v=h0NJK4mEIJU
        if (rigidbody.velocity.magnitude == 0) {
            rigidbody.AddForce(velocity * force, mode);
        } else {
            var velocityProjectedToTarget = (velocity.normalized * Vector3.Dot(velocity, rigidbody.velocity) / velocity.magnitude);
            rigidbody.AddForce((velocity - velocityProjectedToTarget) * force, mode);
        }
    }
}
