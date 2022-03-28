using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    public Camera playerCamera;

    private Vector3 frontOfVehicleReference;
    private float distanceReference;
    private Vector3 lastMousePos;

    // Start is called before the first frame update
    void Start() {
        frontOfVehicleReference = transform.InverseTransformVector(Vector3.forward);
        distanceReference = (playerCamera.transform.position - transform.position).magnitude;

        //Cursor.visible = false;
    }

    // Update is called once per frame
    void Update() {
        Vector3 topPosition = transform.position + transform.TransformVector(frontOfVehicleReference * distanceReference);
        playerCamera.transform.position = topPosition;

        if (IsMouseOverGameWindow()) {
            Vector3 positionDelta = Input.mousePosition - lastMousePos;
            if (positionDelta.sqrMagnitude > Mathf.Epsilon) {
                Vector3 viewportPositionDelta = playerCamera.ScreenToViewportPoint(Input.mousePosition) - playerCamera.ScreenToViewportPoint(lastMousePos);

                float deltaAngleX = Mathf.Rad2Deg * Mathf.Atan2(viewportPositionDelta.x, playerCamera.nearClipPlane);

                // We inverse the Y value because Unity has the viewport inversed in comparison to the screen space
                float deltaAngleY = Mathf.Rad2Deg * Mathf.Atan2(-viewportPositionDelta.y, playerCamera.nearClipPlane);
                
                // x local, y global...can't see it? Well it means you suck at rotations
                playerCamera.transform.Rotate(deltaAngleY, 0, 0, Space.Self);
                playerCamera.transform.Rotate(0, deltaAngleX, 0, Space.World);

                // Only set last position when the difference is significant otherwise we risk adding small errors to the variable and miss them
                lastMousePos = Input.mousePosition;
            }
        }
    }

    bool IsMouseOverGameWindow() {
        var viewport = playerCamera.ScreenToViewportPoint(Input.mousePosition);
        if (viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) return false;
        return true;
    }
}
