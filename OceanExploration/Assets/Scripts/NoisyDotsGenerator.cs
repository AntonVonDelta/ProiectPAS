using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoisyDotsGenerator : MonoBehaviour {
    public int dotsPerUnit = 1;
    public Vector3 scale = new Vector3(1, 1, 1);
    public bool showGizmo = false;
    public float threshold = 0.5f;
    public float perlinNoiseScale = 0.26f;
    public bool refresh = false;

    public float groundHeight = 0.2f;

    private float prevThreshold;
    private float prevPerlinNoiseScale;
    Mesh mesh;




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
        // If we got 1 dots per unit and a scale of 2 then
        // 0   1   (2)  we actualy only got dots 0 and 1 while 2 is part of the next chunk
        // ALso the distance between them is really 1/dotsPerUnit
        Vector3Int dotsPerAxis = Vector3Int.RoundToInt(scale * dotsPerUnit);
        float dotDistance = (float)1 / dotsPerUnit;
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
                    gridCell.cornerPositions[0] = positions[0];
                    gridCell.cornerPositions[1] = positions[1];
                    gridCell.cornerPositions[2] = positions[2];
                    gridCell.cornerPositions[3] = positions[3];

                    gridCell.cornerPositions[4] = positions[4];
                    gridCell.cornerPositions[5] = positions[5];
                    gridCell.cornerPositions[6] = positions[6];
                    gridCell.cornerPositions[7] = positions[7];

                    for (int i = 0; i < 8; i++) {
                        // Check if the point lies on the cube itself and seal it off
                        //if (isPointOnCube(positions[i], dotsPerAxis)) gridCell.cornerValues[i] = 0;
                        //else
                            gridCell.cornerValues[i] = GetPixelValue(gridCell.cornerPositions[i]);
                    }

                    Vector3 originPointPos = GetPointPosition(indexes, dotDistance, cubeCornerOffset);
                    var surfaceTriangles = MarchingCubes.GetSurface(gridCell);
                    for (int i = 0; i < surfaceTriangles.Count; i++) {
                        int startingOffset = vertices.Count;

                        vertices.Add(cubeCornerOffset + surfaceTriangles[i].corners[0] * dotDistance);
                        vertices.Add(cubeCornerOffset + surfaceTriangles[i].corners[1] * dotDistance);
                        vertices.Add(cubeCornerOffset + surfaceTriangles[i].corners[2] * dotDistance);

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
    Vector3 GetPointPosition(Vector3 integerIndex, float dotDistance, Vector3 cubeCornerOffset) {
        integerIndex *= dotDistance;
        integerIndex += cubeCornerOffset;
        return integerIndex;
    }

    // Get noise in 3D position
    float GetPixelValue(Vector3 pos) {
        //return map(  (int)Mathf.Abs( pos.x * pos.z * 227)<<16 + (int)Mathf.Abs(pos.y * pos.y * 1213)<<8 + (int)Mathf.Abs(pos.z* 727),0, int.MaxValue,0, 1);

        //if (pos.y < transform.position.y - scale.y + groundHeight) return 1;
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


}
