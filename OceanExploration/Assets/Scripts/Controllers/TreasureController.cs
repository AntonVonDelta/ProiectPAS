using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureController : MonoBehaviour {
    private bool startAnimation = false;

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        if (startAnimation) {

        }
    }

    private void OnTriggerEnter(Collider other) {
        startAnimation = true;
    }
}
