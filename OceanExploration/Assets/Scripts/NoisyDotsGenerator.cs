using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoisyDotsGenerator : MonoBehaviour {
    public GameObject dotPrefab;
    public int dotsPerUnit = 10;
    public float threshold = 0.2f;

    [SerializeField]
    private Slider slider;
    private bool valueChanged = true;

    List<GameObject> cachedObjects = new List<GameObject>();

    // Start is called before the first frame update
    void Start() {
        slider.onValueChanged.AddListener((a) => { valueChanged = true; });
    }

    // Update is called once per frame
    void Update() {
        if (valueChanged) {
            valueChanged = false;

            DrawDots();

        }
    }


    void DrawDots() {
        Vector3 dotsPerAxis = transform.localScale * dotsPerUnit;
        Vector3 dotDistance = new Vector3(transform.localScale.x / (dotsPerAxis.x - 1), transform.localScale.y / (dotsPerAxis.y - 1), transform.localScale.z / (dotsPerAxis.z - 1));
        Vector3 cubeCornerOffset = transform.position - transform.localScale / 2;

        for (int z = 0; z < dotsPerAxis.z; z++) {
            for (int y = 0; y < dotsPerAxis.y; y++) {
                for (int x = 0; x < dotsPerAxis.x; x++) {
                    Vector3 pos = new Vector3(x, y, z);
                    pos.Scale(dotDistance);
                    pos += cubeCornerOffset;

                    float scaleValue = Perlin.Noise(pos.x, pos.y, pos.z) / 5;
                    if (scaleValue < threshold) continue;

                    GameObject newDot = Instantiate(dotPrefab, pos, Quaternion.identity);
                    newDot.transform.localScale = Vector3.one * scaleValue;

                    cachedObjects.Add(newDot);
                }
            }
        }
    }
}
