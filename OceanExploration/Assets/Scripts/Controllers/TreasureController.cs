using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureController : MonoBehaviour {
    private int startAnimation = 0;
    public float rotationSpeed = 360.0f;
    public float scalingSpeed = -1f;
    public float minimumSize = 0.5f;

    public float rotationDuration = 3;
    public float rotationStartValue = 0;
    public float rotationEndValue = 360;
    float animatedRotation=0;

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        if (startAnimation == 1) {
            StartCoroutine(Animation());
            startAnimation = 2;
        }
        if (startAnimation == 2) {
            Vector3 localUp = transform.InverseTransformDirection(Vector3.up);
            transform.Rotate(localUp, animatedRotation);

            Vector3 scale = transform.localScale;
            scale += Vector3.one * scalingSpeed * Time.deltaTime;
            transform.localScale = scale;

            if (scale.magnitude < minimumSize) {
                gameObject.SetActive(false);
            }
        }
    }

    IEnumerator Animation() {
        float timeElapsed = 0;

        while (timeElapsed < rotationDuration) {
            animatedRotation = Mathf.Lerp(rotationStartValue, rotationEndValue, timeElapsed / rotationDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        animatedRotation = rotationEndValue;
    }

    private void OnTriggerEnter(Collider other) {
        if (startAnimation == 0) startAnimation = 1;
    }
}
