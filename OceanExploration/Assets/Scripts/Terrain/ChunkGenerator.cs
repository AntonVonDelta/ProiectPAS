using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour {
    public static string ChunkTag = "ChunkTag";

    [Header("Chunk parameters")]
    [Tooltip("The size of the base")]
    public int chunkSize = 10;

    [Tooltip("The height of a chunk")]
    public int heightSize = 30;

    [Tooltip("The radius around the player where chunks are loaded")]
    public int loadingRadius = 6;
    public bool refresh = false;
    public bool countTotalVertexes = false;

    [Header("Terrain generation params")]
    public ComputeShader marchingCubesShader;
    public int dotsPerUnit = 2;
    public bool doInterpolate = true;
    public bool squishTerrain = true;
    public float threshold = 0.5f;
    public float perlinNoiseScale = 0.26f;

    [Header("Vegetation generation params")]
    public GameObject plantPrefab;
    public int plantsPerChunk = 2;
    public int plantsLoadingRadius = 1;

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

            // Destroy all plants
            if (el.plants != null) {
                foreach (GameObject plant in el.plants) Destroy(plant);
            }
            // Preserve chunk gameobject
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
                int loadedChunkIndex = loadedChunks.FindIndex(el => el.gridIndex == tempChunk);
                if (loadedChunkIndex != -1) {
                    // Load plants if near 'em
                    if ((tempChunkWorldPos - transform.position).magnitude <= plantsLoadingRadius * chunkSize) {
                        Chunk loadedChunk = loadedChunks[loadedChunkIndex];
                        if (loadedChunk.plants == null) {
                            loadedChunk.plants = GeneratePlants(tempChunkWorldPos);
                            loadedChunks[loadedChunkIndex] = loadedChunk;
                        }
                    }

                    continue;
                }

                GameObject chunkObj = null;
                if (cachedObjects.Count() != 0) {
                    chunkObj = cachedObjects.Pop();
                    chunkObj.SetActive(true);
                } else {
                    chunkObj = new GameObject($"MeshFab_{loadedChunks.Count}", typeof(MeshFilter));
                    chunkObj.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Diffuse"));
                    chunkObj.AddComponent<MeshCollider>();
                    chunkObj.tag = ChunkTag;
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

                // Register newly created chunk
                loadedChunks.Add(new Chunk { gridIndex = tempChunk, chunkObject = chunkObj, plants = null });
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

    private List<GameObject> GeneratePlants(Vector3 gridPosition) {
        List<GameObject> result = new List<GameObject>();

        for (int i = 0; i < plantsPerChunk; i++) {
            Vector3 rayOrigin = gridPosition;
            RaycastHit hit;
            rayOrigin.x += Random.value * chunkSize;
            rayOrigin.z += Random.value * chunkSize;
            rayOrigin.y = heightSize;

            if (Physics.Raycast(rayOrigin, Vector3.down + Random.insideUnitSphere.normalized / 2, out hit, heightSize)) {
                GameObject obj = Instantiate(plantPrefab, hit.point, Quaternion.identity);
                result.Add(obj);
            }
        }
        return result;
    }
}
