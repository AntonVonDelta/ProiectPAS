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
        DrawDots();
    }

    // Update is called once per frame
    void Update() {
        if (threshold != prevThreshold) {
            prevThreshold = threshold;

            ThresholdDots();
        }
    }


    void DrawDots() {
        Vector3 dotsPerAxis = transform.localScale * dotsPerUnit;
        Vector3 dotDistance = new Vector3(transform.localScale.x / (dotsPerAxis.x - 1), transform.localScale.y / (dotsPerAxis.y - 1), transform.localScale.z / (dotsPerAxis.z - 1));
        Vector3 cubeCornerOffset = transform.position - transform.localScale / 2;

        Stack<GameObject> availableObjects = new Stack<GameObject>(cachedObjects);

        for (int z = 0; z < dotsPerAxis.z; z++) {
            for (int y = 0; y < dotsPerAxis.y; y++) {
                for (int x = 0; x < dotsPerAxis.x; x++) {
                    Vector3 pos = new Vector3(x, y, z);
                    pos.Scale(dotDistance);
                    pos += cubeCornerOffset;

                    //float scaleValue = (float)Random.Range(0,10000)/10000/5;
                    float scaleValue = Perlin.Noise(pos.x * 2, pos.y * 2, pos.z * 2) / 2 + 0.5f;   // map from -1,1 to 0,1

                    GameObject newDot = null;
                    if (availableObjects.Count != 0) {
                        newDot = availableObjects.Pop();
                    } else {
                        newDot = Instantiate(dotPrefab, pos, Quaternion.identity);
                        cachedObjects.Add(newDot);
                    }

                    newDot.transform.position = pos;
                    newDot.transform.localScale = Vector3.one * scaleValue;
                    newDot.SetActive(true);
                }
            }
        }
    }

    void ThresholdDots() {
        Vector3 dotsPerAxis = transform.localScale * dotsPerUnit;
        Vector3 dotDistance = new Vector3(transform.localScale.x / (dotsPerAxis.x - 1), transform.localScale.y / (dotsPerAxis.y - 1), transform.localScale.z / (dotsPerAxis.z - 1));
        Vector3 cubeCornerOffset = transform.position - transform.localScale / 2;

        Stack<GameObject> availableObjects = new Stack<GameObject>(cachedObjects);

        foreach (GameObject obj in cachedObjects) {
            if (obj.transform.localScale.x < threshold) obj.SetActive(false);
            else obj.SetActive(true);
        }

    }
}
