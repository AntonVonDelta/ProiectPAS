using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoisyDotsGenerator : MonoBehaviour {
    public GameObject dotPrefab;

    List<GameObject> cachedObjects = new List<GameObject>();

    // Start is called before the first frame update
    void Start() {
        int dotsPerUnit = 5;

        Vector3 dotDistance = new Vector3(transform.localScale.x / (dotsPerUnit-1), transform.localScale.y / (dotsPerUnit - 1), transform.localScale.z / (dotsPerUnit - 1));

        for (int z = 0; z < dotsPerUnit * transform.localScale.z; z++) {
            for (int y = 0; y < dotsPerUnit * transform.localScale.y; y++) {
                for (int x = 0; x < dotsPerUnit * transform.localScale.x; x++) {
                    Vector3 pos = new Vector3(x, y, z);
                    pos.Scale(dotDistance) ;
                    pos += transform.position;

                    cachedObjects.Add(Instantiate(dotPrefab, pos, Quaternion.identity));
                }
            }
        }

    }

    // Update is called once per frame
    void Update() {

    }
}
