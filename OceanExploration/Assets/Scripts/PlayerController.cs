using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    public Camera playerCamera;
    public float moveForceMagnitude = 5f;
    public float rotateAmount = 2.5f;
    public float lookSpeed = 2f;

    private Rigidbody rb;
    private Vector3 forwardOfVehiclerReference;
    private Vector3 upwardsOfVehicleReference;
    private Vector3 rightOfVehicleReference;
    private float distanceReference;

    // Start is called before the first frame update
    void Start() {
        rb = GetComponent<Rigidbody>();

        forwardOfVehiclerReference = transform.InverseTransformVector(Vector3.forward);
        upwardsOfVehicleReference = transform.InverseTransformVector(Vector3.up);
        rightOfVehicleReference = transform.InverseTransformVector(Vector3.right);
        distanceReference = (playerCamera.transform.position - transform.position).magnitude;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void FixedUpdate() {
        // Move camera in the direction the vehicle is pointing
        Vector3 topPosition = transform.position + transform.TransformVector(forwardOfVehiclerReference * distanceReference);
        playerCamera.transform.position = topPosition;
        playerCamera.transform.rotation = Quaternion.LookRotation(transform.TransformDirection(forwardOfVehiclerReference), transform.TransformDirection(upwardsOfVehicleReference));

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
            Vector3 localEulerAngles = transform.localEulerAngles;
            localEulerAngles.x = Mathf.Clamp(localEulerAngles.x, 90 - 60, 90 + 60);
            transform.localEulerAngles = localEulerAngles;
        }

        rb.AddRelativeForce(Input.GetAxis("Vertical") * forwardOfVehiclerReference * moveForceMagnitude);
        transform.Rotate(Vector3.up, Input.GetAxis("Horizontal") * rotateAmount, Space.World);
    }


    bool IsMouseOverGameWindow() {
        var viewport = playerCamera.ScreenToViewportPoint(Input.mousePosition);
        if (viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) return false;
        return true;
    }
}
