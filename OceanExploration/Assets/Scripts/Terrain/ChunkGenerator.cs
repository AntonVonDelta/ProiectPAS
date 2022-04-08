using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour {
    [Header("Chunk parameters")]
    [Tooltip("The size of the base")]
    public int chunkSize = 10;

    [Tooltip("The height of a chunk")]
    public int heightSize = 30;

    [Tooltip("The radius around the player where chunks are loaded")]
    public int loadingRadius = 2;
    public bool refresh = false;
    public bool countTotalVertexes = false;

    [Header("Terrain generation params")]
    public ComputeShader marchingCubesShader;
    public int dotsPerUnit = 1;
    public bool doInterpolate = true;
    public bool squishTerrain = true;
    public float threshold = 0.5f;
    public float perlinNoiseScale = 0.26f;

    [Header("Vegetation generation params")]
    public GameObject plantPrefab;

    struct Chunk {
        public GameObject chunkObject;
        public Vector3Int gridIndex;
        public List<GameObject> plants;
    };
    private Stack<GameObject> cachedObjects = new Stack<GameObject>();
    private List<Chunk> loadedChunks = new List<Chunk>();

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        Vector3Int currentChunkGrid = new Vector3Int((int)transform.position.x / chunkSize, 0, (int)transform.position.z / chunkSize);

        // Remove far chunks
        for (int i = loadedChunks.Count - 1; i >= 0; i--) {
            var el = loadedChunks[i];
            Vector3 tempChunkWorldPos = el.gridIndex * chunkSize;

            if (!refresh && (tempChunkWorldPos - transform.position).magnitude <= loadingRadius * chunkSize) continue;

            el.chunkObject.SetActive(false);
            cachedObjects.Push(el.chunkObject);
            loadedChunks.RemoveAt(i);
        }
        refresh = false;

        for (int i = -loadingRadius; i < loadingRadius + 1; i++) {
            for (int j = -loadingRadius; j < loadingRadius + 1; j++) {
                Vector3Int relativeGridIndexes = new Vector3Int(i, 0, j);
                Vector3Int tempChunk = currentChunkGrid + relativeGridIndexes;
                Vector3 tempChunkWorldPos = tempChunk * chunkSize;

                // Load only in a circle around player
                if ((tempChunkWorldPos - transform.position).magnitude > loadingRadius * chunkSize) continue;

                // Skip already loaded chunks
                if (loadedChunks.Any(el => el.gridIndex == tempChunk)) continue;

                GameObject chunkObj = null;
                if (cachedObjects.Count() != 0) {
                    chunkObj = cachedObjects.Pop();
                    chunkObj.SetActive(true);
                } else {
                    chunkObj = new GameObject($"MeshFab_{loadedChunks.Count}", typeof(MeshFilter));
                    chunkObj.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Diffuse"));
                    chunkObj.AddComponent<MeshCollider>();
                }
                chunkObj.transform.position = tempChunkWorldPos + new Vector3(0, heightSize / 2, 0);

                MeshGenerator meshGenerator = new MeshGenerator(chunkObj, tempChunk);
                meshGenerator.marchingCubesShader = marchingCubesShader;
                meshGenerator.scale = new Vector3(chunkSize, heightSize, chunkSize);

                meshGenerator.doInterpolate = doInterpolate;
                meshGenerator.squishTerrain = squishTerrain;
                meshGenerator.closeLateralSurface = false;
                meshGenerator.dotsPerUnit = dotsPerUnit;
                meshGenerator.threshold = threshold;
                meshGenerator.perlinNoiseScale = perlinNoiseScale;

                meshGenerator.SetObjectMesh();

                // Set collision mesh
                chunkObj.GetComponent<MeshCollider>().sharedMesh = chunkObj.GetComponent<MeshFilter>().sharedMesh;

                loadedChunks.Add(new Chunk { gridIndex = tempChunk, chunkObject = chunkObj });

                // Generate plants on the newly loaded terrain
                GeneratePlants(tempChunkWorldPos);
            }
        }

        if (countTotalVertexes) {
            countTotalVertexes = false;

            ulong vertexes = 0;
            for (int i = 0; i < loadedChunks.Count; i++) {
                vertexes += (ulong)loadedChunks[i].chunkObject.GetComponent<MeshFilter>().mesh.vertices.Length;
            }
            Debug.Log($"Counted vertexes:  {vertexes}");
        }
    }

    private void GeneratePlants(Vector3 gridPosition) {
        for (int i = 0; i < 1; i++) {
            Vector3 rayOrigin = gridPosition;
            RaycastHit hit;
            rayOrigin.x += Random.value * chunkSize;
            rayOrigin.z += Random.value * chunkSize;
            rayOrigin.y = heightSize;

            if(Physics.Raycast(rayOrigin,Vector3.down,out hit, heightSize)) {
                Instantiate(plantPrefab, hit.point, Quaternion.identity);
            }
        }
    }
}
