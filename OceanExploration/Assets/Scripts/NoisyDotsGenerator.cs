using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoisyDotsGenerator : MonoBehaviour {
    public GameObject dotPrefab;
    public int dotsPerUnit = 10;
    public Vector3 scale = new Vector3(1, 1, 1);

    private float threshold = 0.5f;
    private float prevThreshold;

    Mesh mesh;
    List<GameObject> cachedObjects = new List<GameObject>();




    // Start is called before the first frame update
    void Start() {
        mesh = new Mesh();
        prevThreshold = threshold;
        GetComponent<MeshFilter>().mesh = mesh;

        Random.InitState(10);


        UpdateMesh();
        //DrawDots();
    }

    // Update is called once per frame
    void Update() {
        if (threshold != prevThreshold) {
            prevThreshold = threshold;

            //ThresholdDots();
        }
    }

    void UpdateMesh() {
        Vector3Int dotsPerAxis = Vector3Int.RoundToInt(scale * dotsPerUnit);
        Vector3 dotDistance = new Vector3(scale.x / (dotsPerAxis.x - 1), scale.y / (dotsPerAxis.y - 1), scale.z / (dotsPerAxis.z - 1));
        Vector3 cubeCornerOffset = transform.position - scale / 2;

        Stack<GameObject> availableObjects = new Stack<GameObject>(cachedObjects);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int z = 0; z < dotsPerAxis.z - 1; z++) {
            for (int y = 0; y < dotsPerAxis.y - 1; y++) {
                for (int x = 0; x < dotsPerAxis.x - 1; x++) {
                    Vector3 pos = new Vector3(x, y, z);

                    //GameObject newDot = null;
                    //Vector3 worldPos = GetPointPosition(pos + new Vector3(0, 0, 1), dotDistance, cubeCornerOffset);
                    //if (availableObjects.Count != 0) {
                    //    newDot = availableObjects.Pop();
                    //} else {
                    //    newDot = Instantiate(dotPrefab, worldPos, Quaternion.identity);
                    //    cachedObjects.Add(newDot);
                    //}
                    //newDot.transform.position = worldPos;
                    //newDot.transform.localScale = Vector3.one/2;
                    //if (GetPixelValue(worldPos) > 0) {
                    //    newDot.SetActive(true);
                    //} else {
                    //    newDot.SetActive(true);
                    //}

                    MarchingCubes.GRIDCELL gridCell;
                    gridCell.cornerPositions = new Vector3[8];
                    gridCell.cornerValues = new float[8];
                    gridCell.surfaceValue = threshold;


                    gridCell.cornerPositions[0] = GetPointPosition(pos + new Vector3(0, 0, 1), dotDistance, cubeCornerOffset);
                    gridCell.cornerPositions[1] = GetPointPosition(pos + new Vector3(1, 0, 1), dotDistance, cubeCornerOffset);
                    gridCell.cornerPositions[2] = GetPointPosition(pos + new Vector3(1, 0, 0), dotDistance, cubeCornerOffset);
                    gridCell.cornerPositions[3] = GetPointPosition(pos + new Vector3(0, 0, 0), dotDistance, cubeCornerOffset);

                    gridCell.cornerPositions[4] = GetPointPosition(pos + new Vector3(0, 1, 1), dotDistance, cubeCornerOffset);
                    gridCell.cornerPositions[5] = GetPointPosition(pos + new Vector3(1, 1, 1), dotDistance, cubeCornerOffset);
                    gridCell.cornerPositions[6] = GetPointPosition(pos + new Vector3(1, 1, 0), dotDistance, cubeCornerOffset);
                    gridCell.cornerPositions[7] = GetPointPosition(pos + new Vector3(0, 1, 0), dotDistance, cubeCornerOffset);

                    for (int i = 0; i < 8; i++) {
                        //if (i == 3 || i==2 || i==0) gridCell.cornerValues[i] = 1;
                        //else 
                            gridCell.cornerValues[i] = GetPixelValue(gridCell.cornerPositions[i]);
                    }

                    var surfaceTriangles = MarchingCubes.GetSurface(gridCell);
                    for (int i = 0; i < surfaceTriangles.Count; i++) {
                        int startingOffset = vertices.Count;

                        vertices.Add(surfaceTriangles[i].corners[0]);
                        vertices.Add(surfaceTriangles[i].corners[1]);
                        vertices.Add(surfaceTriangles[i].corners[2]);

                        triangles.Add(startingOffset);
                        triangles.Add(startingOffset + 2);
                        triangles.Add(startingOffset + 1);
                    }
                }
            }
        }

        //Vector3[] vertices1 = new Vector3[] {
        //    new Vector3(0,0,0),
        //    new Vector3(0,0,1),
        //    new Vector3(1,0,0),
        //};

        //int[] triangles1 = new int[] {
        //    0,1,2,
        //};

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    Vector3 GetPointPosition(Vector3 integerIndex, Vector3 dotDistance, Vector3 cubeCornerOffset) {
        integerIndex.Scale(dotDistance);
        integerIndex += cubeCornerOffset;
        return integerIndex;
    }

    // Get noise in 3D position
    float GetPixelValue(Vector3 pos) {
        //if (pos.y < 0) return threshold+Random.value/1;
        //return 0;
        return Perlin.Noise(pos.x, pos.y, pos.z) / 2 + 0.5f;
    }


    void OnDrawGizmosSelected() {
        // Draw a semitransparent blue cube at the transforms position
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawCube(transform.position, scale);
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
