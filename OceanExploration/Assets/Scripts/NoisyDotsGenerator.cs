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

    public float terraceHeight = 4;
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

        // Now this offset is calculated from the ZERO/ORIGIN position
        // It is half of the scale in order to align the mesh with a cube which has the center in the origin
        // Any position transformation done in the editor actually adds that offset to every vertex in the mesh
        // So we MUST NOT add that offset ourselves and this is not what this variable is for.
        Vector3 cubeCornerOffset = -scale / 2;

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
                    Vector3[] positions = {
                        indexes + new Vector3Int(0, 0, 1),
                        indexes + new Vector3Int(1, 0, 1),
                        indexes + new Vector3Int(1, 0, 0),
                        indexes + new Vector3Int(0, 0, 0),
                        indexes + new Vector3Int(0, 1, 1),
                        indexes + new Vector3Int(1, 1, 1),
                        indexes + new Vector3Int(1, 1, 0),
                        indexes + new Vector3Int(0, 1, 0)
                    };
                    gridCell.cornerPositions[0] = positions[0] * dotDistance;
                    gridCell.cornerPositions[1] = positions[1] * dotDistance;
                    gridCell.cornerPositions[2] = positions[2] * dotDistance;
                    gridCell.cornerPositions[3] = positions[3] * dotDistance;

                    gridCell.cornerPositions[4] = positions[4] * dotDistance;
                    gridCell.cornerPositions[5] = positions[5] * dotDistance;
                    gridCell.cornerPositions[6] = positions[6] * dotDistance;
                    gridCell.cornerPositions[7] = positions[7] * dotDistance;

                    for (int i = 0; i < 8; i++) {
                        // Check if the point lies on the cube itself and seal it off
                        if (isPointOnCube(Vector3Int.FloorToInt(positions[i]), dotsPerAxis)) gridCell.cornerValues[i] = threshold - 1;
                        else {
                            // Sample the noise using the dots resolution aka their position
                            // More dots will lower 'dotDistance' and thus will increase the 
                            // number of samples taken for the same unit of noise
                            gridCell.cornerValues[i] = GetPixelValue(gridCell.cornerPositions[i], dotDistance, dotsPerAxis);
                        }
                    }

                    var surfaceTriangles = MarchingCubes.GetSurface(gridCell);
                    for (int i = 0; i < surfaceTriangles.Count; i++) {
                        int startingOffset = vertices.Count;

                        vertices.Add(cubeCornerOffset + surfaceTriangles[i].corners[0]);
                        vertices.Add(cubeCornerOffset + surfaceTriangles[i].corners[1]);
                        vertices.Add(cubeCornerOffset + surfaceTriangles[i].corners[2]);

                        // Reverse order here for culling to work
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

    Vector3 GetPixelPosition(Vector3 index, float dotDistance) {
        return transform.position - scale / 2 + index * dotDistance;
    }
    // Get noise in 3D position
    float GetPixelValue(Vector3 pos, float dotDistance, Vector3Int dotsPerAxis) {
        if (pos.y <= groundHeight) return threshold + 1;

        float noise = Perlin.Noise(pos.x * perlinNoiseScale, pos.y * perlinNoiseScale, pos.z * perlinNoiseScale) / 2 + 0.5f;
        float squishFactor = map(pos.y, 0, dotsPerAxis.y * dotDistance, 1f, 0);

        //float noiseWeight = dotsPerAxis.y * dotDistance;
        //return ((pos.y % terraceHeight) - pos.y)*0.8f + noise* noiseWeight;

        return noise * squishFactor;
    }

    float GetCavesPixelValue(Vector3 pos, float dotDistance, Vector3Int dotsPerAxis) {
        // Creates solid blocks with walkable caves inside on multiple levels
        // The levels are created by the terraceHeight
        // Tests params:
        //  Scale 20 20 20
        //  Thresh 0.5
        //  Perlin Scale 0.26
        //  Terrace height 4
        float noise = Perlin.Noise(pos.x * perlinNoiseScale, pos.y * perlinNoiseScale, pos.z * perlinNoiseScale) / 2 + 0.5f;

        return (1f + (pos.y % terraceHeight) / terraceHeight) * noise;
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
