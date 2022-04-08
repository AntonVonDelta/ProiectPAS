using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator {
    struct GPUTriangle {
        public Vector3 p1;
        public Vector3 p2;
        public Vector3 p3;
    };

    public ComputeShader marchingCubesShader;
    public Vector3 scale = new Vector3(1, 1, 1);

    public bool doInterpolate = true;
    public bool squishTerrain = true;
    public bool closeLateralSurface = true;
    public int dotsPerUnit = 1;
    public float threshold = 0.5f;
    public float perlinNoiseScale = 0.26f;


    Mesh mesh;
    Vector3Int gridIndex;

    public MeshGenerator(GameObject obj, Vector3Int gridIndex) {
        this.gridIndex = gridIndex;

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        obj.GetComponent<MeshFilter>().mesh = mesh;

        // Load the caustics-rendered material
        Material material = Resources.Load("Materials/Caustics", typeof(Material)) as Material;
        if (material) {
            obj.GetComponent<MeshRenderer>().material = material;
        } else {
            Debug.LogWarning("Material Resource Caustics could not be loaded.");
        }
    }


    // Sets the object's mesh to the gnerated one
    public void SetObjectMesh() {
        Vector3Int dotsPerAxis = Vector3Int.RoundToInt(scale * dotsPerUnit);
        float dotDistance = (float)1 / dotsPerUnit;
        Vector3 cubeCornerOffset = -scale / 2;

        // perlin-scale independent offset aka applied before perlin scaling
        Vector3 perlinPositionOffset = new Vector3(gridIndex.x * scale.x, gridIndex.y * scale.y, gridIndex.z * scale.z);


        // We create a compute buffer big enough to hold all possible triangles
        // Max 5 triangles per marched cube
        int structSize = 3 * 3 * sizeof(float);
        ComputeBuffer triangleBuffer = new ComputeBuffer(dotsPerAxis.x * dotsPerAxis.y * dotsPerAxis.z * 5, structSize, ComputeBufferType.Append);
        GPUTriangle[] surfaceTriangles = new GPUTriangle[dotsPerAxis.x * dotsPerAxis.y * dotsPerAxis.z * 5];
        triangleBuffer.SetCounterValue(0);

        marchingCubesShader.SetBuffer(0, "triangleBuffer", triangleBuffer);
        marchingCubesShader.SetBool("doInterpolate", doInterpolate);
        marchingCubesShader.SetBool("squishTerrain", squishTerrain);
        marchingCubesShader.SetBool("closeLateralSurface", closeLateralSurface);

        marchingCubesShader.SetFloat("surfaceValue", threshold);
        marchingCubesShader.SetFloat("perlinNoiseScale", perlinNoiseScale);
        marchingCubesShader.SetFloat("dotDistance", dotDistance);
        marchingCubesShader.SetInts("dotsPerAxis", dotsPerAxis.x, dotsPerAxis.y, dotsPerAxis.z);
        marchingCubesShader.SetFloats("trianglePositionOffset", cubeCornerOffset.x, cubeCornerOffset.y, cubeCornerOffset.z);
        marchingCubesShader.SetFloats("perlinPositionOffset", perlinPositionOffset.x, perlinPositionOffset.y, perlinPositionOffset.z);

        marchingCubesShader.Dispatch(0, 1 + (dotsPerAxis.x / 8), 1 + (dotsPerAxis.y / 8), 1 + (dotsPerAxis.z / 8));

        int triangleCount = GetAppendCount(triangleBuffer);
        triangleBuffer.GetData(surfaceTriangles);
        triangleBuffer.Dispose();

        Dictionary<Vector3, int> uniqueVertexes = new Dictionary<Vector3, int>();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for (int i = 0; i < triangleCount; i++) {
            int vertexIndex;

            // Here order of triangles is changed in order for culling to work
            if (uniqueVertexes.TryGetValue(surfaceTriangles[i].p1, out vertexIndex)) {
                triangles.Add(vertexIndex);
            } else {
                int newVertexIndex = vertices.Count;

                vertices.Add(surfaceTriangles[i].p1);
                triangles.Add(newVertexIndex);
                uniqueVertexes.Add(surfaceTriangles[i].p1, newVertexIndex);
            }

            if (uniqueVertexes.TryGetValue(surfaceTriangles[i].p3, out vertexIndex)) {
                triangles.Add(vertexIndex);
            } else {
                int newVertexIndex = vertices.Count;

                vertices.Add(surfaceTriangles[i].p3);
                triangles.Add(newVertexIndex);
                uniqueVertexes.Add(surfaceTriangles[i].p3, newVertexIndex);
            }

            if (uniqueVertexes.TryGetValue(surfaceTriangles[i].p2, out vertexIndex)) {
                triangles.Add(vertexIndex);
            } else {
                int newVertexIndex = vertices.Count;

                vertices.Add(surfaceTriangles[i].p2);
                triangles.Add(newVertexIndex);
                uniqueVertexes.Add(surfaceTriangles[i].p2, newVertexIndex);
            }
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    //https://sites.google.com/site/aliadevlog/counting-buffers-in-directcompute
    private static int GetAppendCount(ComputeBuffer appendBuffer) {
        ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        ComputeBuffer.CopyCount(appendBuffer, countBuffer, 0);

        int[] counter = new int[1] { 0 };
        countBuffer.GetData(counter);
        countBuffer.Dispose();
        return counter[0];
    }

}
