using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour {
    public GameObject terrainChunkPrefab;

    [Header("Chunk parameters")]
    [Tooltip("The size of a cube")]
    public int chunkSize = 20;

    [Tooltip("The radius around the player where chunks are loaded")]
    public int loadingRadius = 2;

    [Header("Terrain generation params")]
    public ComputeShader marchingCubesShader;
    public int dotsPerUnit = 1;
    public bool doInterpolate = true;
    public bool squishTerrain = true;
    public float threshold = 0.5f;
    public float perlinNoiseScale = 0.26f;

    struct Chunk {
        public GameObject origin;
        public Vector3Int gridIndex;
    };

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

            if ((tempChunkWorldPos - transform.position).magnitude <= loadingRadius * chunkSize) continue;

            Destroy(el.origin);
            loadedChunks.RemoveAt(i);
        }

        for (int i = -loadingRadius; i < loadingRadius + 1; i++) {
            for (int j = -loadingRadius; j < loadingRadius + 1; j++) {
                Vector3Int relativeGridIndexes = new Vector3Int(i, 0, j);
                Vector3Int tempChunk = currentChunkGrid + relativeGridIndexes;
                Vector3 tempChunkWorldPos = tempChunk * chunkSize;

                // Load only in a circle around player
                if ((tempChunkWorldPos - transform.position).magnitude > loadingRadius * chunkSize) continue;

                // Skip already loaded chunks
                if (loadedChunks.Any(el => el.gridIndex == tempChunk)) continue;

                GameObject chunkObj = Instantiate(terrainChunkPrefab, tempChunkWorldPos, Quaternion.identity);
                MeshGenerator meshGenerator = new MeshGenerator(chunkObj, tempChunk);
                meshGenerator.marchingCubesShader = marchingCubesShader;
                meshGenerator.scale = Vector3.one * chunkSize;

                meshGenerator.doInterpolate = doInterpolate;
                meshGenerator.squishTerrain = squishTerrain;
                meshGenerator.closeLateralSurface = false;
                meshGenerator.dotsPerUnit = dotsPerUnit;
                meshGenerator.threshold = threshold;
                meshGenerator.perlinNoiseScale = perlinNoiseScale;

                meshGenerator.SetObjectMesh();

                loadedChunks.Add(new Chunk { gridIndex = tempChunk, origin = chunkObj });
            }
        }
    }
}
