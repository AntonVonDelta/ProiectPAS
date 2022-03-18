using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoisyDotsGenerator : MonoBehaviour {
    public GameObject dotPrefab;
    public int dotsPerUnit = 10;
    public Vector3 scale = new Vector3(1, 1, 1);
    public bool addMarginsInside = true;
    public bool showGizmo = false;
    public float threshold = 0.5f;
    public float perlinNoiseScale = 1;

    public bool refresh = false;

    private float prevThreshold;
    private float prevPerlinNoiseScale;
    Mesh mesh;
    List<GameObject> cachedObjects = new List<GameObject>();




    // Start is called before the first frame update
    void Start() {
        mesh = new Mesh();
        prevThreshold = threshold;
        prevPerlinNoiseScale = perlinNoiseScale;

        GetComponent<MeshFilter>().mesh = mesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        UpdateMesh();
    }

    // Update is called once per frame
    void Update() {
        if (threshold != prevThreshold || perlinNoiseScale != prevPerlinNoiseScale || refresh) {
            prevThreshold = threshold;
            prevPerlinNoiseScale = perlinNoiseScale;
            refresh = false;

            UpdateMesh();
        }
    }

    void UpdateMesh() {
        Vector3Int dotsPerAxis = Vector3Int.RoundToInt(scale * dotsPerUnit);
        Vector3 dotDistance = new Vector3(scale.x / (dotsPerAxis.x - 1), scale.y / (dotsPerAxis.y - 1), scale.z / (dotsPerAxis.z - 1));
        Vector3 cubeCornerOffset = transform.position - scale / 2;


        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for (int z = 0; z < dotsPerAxis.z - 1; z++) {
            for (int y = 0; y < dotsPerAxis.y - 1; y++) {
                for (int x = 0; x < dotsPerAxis.x - 1; x++) {
                    Vector3Int indexes = new Vector3Int(x, y, z);

                    MarchingCubes.GRIDCELL gridCell;
                    gridCell.cornerPositions = new Vector3[8];
                    gridCell.cornerValues = new float[8];
                    gridCell.surfaceValue = threshold;

                    // Cube position shifts
                    // This is necessary because the values: edges and vertexes stored in MArchingCube
                    // are created using a cube with the 0 indexed vertex starting in the bottom-left-further point
                    // Here's the data and the cube: http://paulbourke.net/geometry/polygonise/
                    Vector3Int[] positions = {
                        indexes + new Vector3Int(0, 0, 1),
                        indexes + new Vector3Int(1, 0, 1),
                        indexes + new Vector3Int(1, 0, 0),
                        indexes + new Vector3Int(0, 0, 0),
                        indexes + new Vector3Int(0, 1, 1),
                        indexes + new Vector3Int(1, 1, 1),
                        indexes + new Vector3Int(1, 1, 0),
                        indexes + new Vector3Int(0, 1, 0)
                    };
                    gridCell.cornerPositions[0] = GetPointPosition(positions[0], dotDistance, cubeCornerOffset);
                    gridCell.cornerPositions[1] = GetPointPosition(positions[1], dotDistance, cubeCornerOffset);
                    gridCell.cornerPositions[2] = GetPointPosition(positions[2], dotDistance, cubeCornerOffset);
                    gridCell.cornerPositions[3] = GetPointPosition(positions[3], dotDistance, cubeCornerOffset);

                    gridCell.cornerPositions[4] = GetPointPosition(positions[4], dotDistance, cubeCornerOffset);
                    gridCell.cornerPositions[5] = GetPointPosition(positions[5], dotDistance, cubeCornerOffset);
                    gridCell.cornerPositions[6] = GetPointPosition(positions[6], dotDistance, cubeCornerOffset);
                    gridCell.cornerPositions[7] = GetPointPosition(positions[7], dotDistance, cubeCornerOffset);

                    for (int i = 0; i < 8; i++) {
                        // Check if the point lies on the cube itself and seal it off
                        if (isPointOnCube(positions[i], dotsPerAxis)) gridCell.cornerValues[i] = 0;
                        else gridCell.cornerValues[i] = GetPixelValue(gridCell.cornerPositions[i]);
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

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    bool isPointOnCube(Vector3Int pos, Vector3Int maxIndexes) {
        if (pos.y == 0) return true;
        if (pos.z == 0) return true;
        if (pos.x == 0) return true;

        if (pos.z == maxIndexes.z - 1) return true;
        if (pos.y == maxIndexes.y - 1) return true;
        if (pos.x == maxIndexes.x - 1) return true;
        return false;
    }
    Vector3 GetPointPosition(Vector3 integerIndex, Vector3 dotDistance, Vector3 cubeCornerOffset) {
        integerIndex.Scale(dotDistance);
        integerIndex += cubeCornerOffset;
        return integerIndex;
    }

    // Get noise in 3D position
    float GetPixelValue(Vector3 pos) {
        //return map(  (int)Mathf.Abs( pos.x * pos.z * 227)<<16 + (int)Mathf.Abs(pos.y * pos.y * 1213)<<8 + (int)Mathf.Abs(pos.z* 727),0, int.MaxValue,0, 1);
        return Perlin.Noise(pos.x * perlinNoiseScale, pos.y * perlinNoiseScale, pos.z * perlinNoiseScale) / 2 + 0.5f;
    }
    public float map(float value, float from1, float to1, float from2, float to2) {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }


    void OnDrawGizmosSelected() {
        // Draw a semitransparent blue cube at the transforms position
        Gizmos.color = new Color(1, 0, 0, 0.5f);

        if (showGizmo) Gizmos.DrawCube(transform.position, scale);
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
