using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    public Camera playerCamera;

    private Vector3 forwardOfVehiclerReference;
    private Vector3 upwardsOfVehicleReference;
    private float distanceReference;
    private Vector3 lastMousePos;

    // Start is called before the first frame update
    void Start() {
        forwardOfVehiclerReference = transform.InverseTransformVector(Vector3.forward);
        upwardsOfVehicleReference = transform.InverseTransformVector(Vector3.up);

        distanceReference = (playerCamera.transform.position - transform.position).magnitude;
        
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    // Update is called once per frame
    void Update() {

        // Move camera in the direction the vehicle is pointing
        Vector3 topPosition = transform.position + transform.TransformVector(forwardOfVehiclerReference * distanceReference);
        playerCamera.transform.position = topPosition;
        playerCamera.transform.rotation = Quaternion.LookRotation(transform.TransformDirection(forwardOfVehiclerReference), transform.TransformDirection(upwardsOfVehicleReference));

        if (IsMouseOverGameWindow()) {
            Vector3 positionDelta = Input.mousePosition - lastMousePos;
            if (positionDelta.sqrMagnitude > Mathf.Epsilon) {
                Vector3 viewportPositionDelta = playerCamera.ScreenToViewportPoint(Input.mousePosition) - playerCamera.ScreenToViewportPoint(lastMousePos);

                float deltaAngleX = Mathf.Rad2Deg * Mathf.Atan2(viewportPositionDelta.x, playerCamera.nearClipPlane);

                // We inverse the Y value because Unity has the viewport inversed in comparison to the screen space
                float deltaAngleY = Mathf.Rad2Deg * Mathf.Atan2(-viewportPositionDelta.y, playerCamera.nearClipPlane);

                // x local, y global...can't see it? Well it means you suck at rotations
                transform.Rotate(deltaAngleY, 0, 0, Space.Self);
                transform.Rotate(0, deltaAngleX, 0, Space.World);
                
                // Restrict X axis angle
                Vector3 localEulerAngles = transform.localEulerAngles;
                localEulerAngles.x = Mathf.Clamp(localEulerAngles.x, 90-60, 90+60);
                transform.localEulerAngles = localEulerAngles;

                // Only set last position when the difference is significant otherwise we risk adding small errors to the variable and miss them
                lastMousePos = Input.mousePosition;

            }
        }
        if (Input.GetKey(KeyCode.A)) {

        }
    }

    bool IsMouseOverGameWindow() {
        var viewport = playerCamera.ScreenToViewportPoint(Input.mousePosition);
        if (viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) return false;
        return true;
    }
}
