using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoisyDotsGenerator : MonoBehaviour {
    public GameObject dotPrefab;
    public int dotsPerUnit = 10;
    public float threshold = 0.2f;
    private float prevThreshold;

    List<GameObject> cachedObjects = new List<GameObject>();

    // Start is called before the first frame update
    void Start() {
        prevThreshold = threshold;
    }

    // Update is called once per frame
    void Update() {
        if (threshold!=prevThreshold) {
            prevThreshold = threshold;
            DrawDots();
        }
    }


    void DrawDots() {
        Vector3 dotsPerAxis = transform.localScale * dotsPerUnit;
        Vector3 dotDistance = new Vector3(transform.localScale.x / (dotsPerAxis.x - 1), transform.localScale.y / (dotsPerAxis.y - 1), transform.localScale.z / (dotsPerAxis.z - 1));
        Vector3 cubeCornerOffset = transform.position - transform.localScale / 2;

        Stack<GameObject> availableObjects=new Stack<GameObject>(cachedObjects);

        for (int z = 0; z < dotsPerAxis.z; z++) {
            for (int y = 0; y < dotsPerAxis.y; y++) {
                for (int x = 0; x < dotsPerAxis.x; x++) {
                    Vector3 pos = new Vector3(x, y, z);
                    pos.Scale(dotDistance);
                    pos += cubeCornerOffset;

                    float scaleValue = Perlin.Noise(pos.x, pos.y, pos.z) / 5;
                    if (scaleValue < threshold) continue;

                    GameObject newDot = null;
                    if (availableObjects.Count != 0) {
                        newDot = availableObjects.Pop();
                    } else {
                        newDot = Instantiate(dotPrefab, pos, Quaternion.identity);
                        cachedObjects.Add(newDot);
                    }

                    newDot.transform.position = pos;
                    newDot.transform.localScale = Vector3.one * scaleValue;
                }
            }
        }
    }
}
