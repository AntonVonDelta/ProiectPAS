using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    public Camera playerCamera;
    public float moveForceMagnitude = 5f;
    public float rotateAmount = 2.5f;
    public float lookSpeed = 2f;
    public float smoothTime = 0.3f;
    public float lengthFromCenterToBack = 1;

    private Rigidbody rb;
    private Vector3 forwardOfVehiclerReference;
    private Vector3 upwardsOfVehicleReference;
    private Vector3 rightOfVehicleReference;
    private Vector3 positionOffset;

    private Vector3 cameraMovementVelocity = Vector3.zero;

    // Start is called before the first frame update
    void Start() {
        rb = GetComponent<Rigidbody>();

        forwardOfVehiclerReference = transform.InverseTransformVector(Vector3.forward).normalized;
        upwardsOfVehicleReference = transform.InverseTransformVector(Vector3.up).normalized;
        rightOfVehicleReference = transform.InverseTransformVector(Vector3.right).normalized;
        positionOffset = transform.InverseTransformPoint(playerCamera.transform.position);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void FixedUpdate() {
        // Move camera to follow vehicle
        Vector3 cameraWorldPos = transform.TransformPoint(positionOffset);
        playerCamera.transform.position = Vector3.SmoothDamp(playerCamera.transform.position, cameraWorldPos, ref cameraMovementVelocity, smoothTime);

        // Move camera in the direction the vehicle is pointing
        //Vector3 topPosition = transform.position + transform.TransformVector(forwardOfVehiclerReference * distanceReference);
        //playerCamera.transform.position = topPosition;
        //playerCamera.transform.rotation = Quaternion.LookRotation(transform.TransformDirection(forwardOfVehiclerReference), transform.TransformDirection(upwardsOfVehicleReference));

        // Rotate camera
        playerCamera.transform.LookAt(transform);

        if (IsMouseOverGameWindow()) {
            Vector3 positionDelta = new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * lookSpeed;
            Vector3 viewportPositionDelta = playerCamera.ScreenToViewportPoint(positionDelta);

            float deltaAngleX = Mathf.Rad2Deg * Mathf.Atan2(viewportPositionDelta.x, playerCamera.nearClipPlane);
            // We inverse the Y value because Unity has the viewport inversed in comparison to the screen space
            float deltaAngleY = Mathf.Rad2Deg * Mathf.Atan2(-viewportPositionDelta.y, playerCamera.nearClipPlane);

            // x local, y global...can't see it? Well it means you suck at rotations
            transform.Rotate(deltaAngleY, 0, 0, Space.Self);
            transform.Rotate(0, deltaAngleX, 0, Space.World);

            // Restrict X axis angle
            Vector3 localEulerAngles = transform.localRotation.eulerAngles;
            localEulerAngles.x = Mathf.Clamp(ChangeAngleInterval(localEulerAngles.x), -60, 60);
            localEulerAngles.z = 0; // Do not roll on Z axis
            transform.localRotation = Quaternion.Euler(localEulerAngles);
        }

        rb.AddForceAtPosition(Input.GetAxis("Vertical") * transform.TransformDirection(forwardOfVehiclerReference) * moveForceMagnitude, transform.position - transform.TransformDirection(forwardOfVehiclerReference) * lengthFromCenterToBack * transform.localScale.z);
        transform.Rotate(Vector3.up, Input.GetAxis("Horizontal") * rotateAmount, Space.World);
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;

        if (rb != null) Gizmos.DrawSphere(transform.position + rb.centerOfMass, 0.4f);
        Gizmos.DrawWireSphere(transform.position - transform.TransformDirection(forwardOfVehiclerReference) * lengthFromCenterToBack * transform.localScale.z, 0.2f);
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
    bool isAngleBetween(float angle, float min, float max) {
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
}
