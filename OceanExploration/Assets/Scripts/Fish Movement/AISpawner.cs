using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class AIObjects
{
    public string AIGroupName { get { return m_aiGroupName; } }
    public GameObject objectPrefab { get { return m_prefab; } }
    public int maxAI { get { return m_maxAI; } }
    // public int maxAI { get; set; }
    public int spawnRate { get { return m_spawnRate; } }
    public int spawnAmount { get { return m_maxSpawnAmount; } }
    public bool randomizeStats { get { return m_randomizeStats; } }
    public bool enableSpawner { get { return m_enableSpawner; } }

    [Header("AI Group Stats")]
    [SerializeField]
    private string m_aiGroupName;
    [SerializeField]
    private GameObject m_prefab;
    [SerializeField]
    [Range(0f, 200f)]
    private int m_maxAI;
    [SerializeField]
    [Range(0f, 20f)]
    private int m_spawnRate;
    [SerializeField]
    [Range(0f, 200f)]
    private int m_maxSpawnAmount;

    [Header("Main Settings")]
    [SerializeField]
    private bool m_enableSpawner;
    [SerializeField]
    private bool m_randomizeStats;

    public AIObjects(string name, GameObject prefab, int maxAI, int spawnRate, int spawnAmount, bool randomizeStats)
    {
        this.m_aiGroupName = name;
        this.m_prefab = prefab;
        this.m_maxAI = maxAI;
        this.m_spawnRate = spawnRate;
        this.m_maxSpawnAmount = spawnAmount;
        this.m_randomizeStats = randomizeStats;
    }

    public void setValues(int maxAI, int spawnRate, int spawnAmount)
    {
        this.m_maxAI = maxAI;
        this.m_spawnRate = spawnRate;
        this.m_maxSpawnAmount = spawnAmount;
    }
}

public class AISpawner : MonoBehaviour
{   
    public List<Transform> Waypoints = new List<Transform>();

    public float spawnTimer { get { return m_SpawnTimer; } }
    public Vector3 spawnArea { get { return m_SpawnArea; } }

    [Header("Global Stats")]
    [Range(0f, 600f)]
    [SerializeField]
    private float m_SpawnTimer;
    [SerializeField]
    private Color m_SpawnColor = new Color(1.000f, 0.000f, 0.000f, 0.300f);
    [SerializeField]
    private Vector3 m_SpawnArea = new Vector3 (20f, 10f, 20f);

    //public Transform[] WaypointsLinq;

    [Header("AI Group Settings")]
    public AIObjects[] AIObject = new AIObjects[5];

    //Empty Game Object to keep out AI in
    //private GameObject m_AIGroupSpawn;

    // Start is called before the first frame update
    void Start()
    {
        GetWaypoints();
        RandomizeGroups();
        CreateAIGroups();
        InvokeRepeating("SpawnNPC", 0.5f, spawnTimer);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnNPC()
    {
        for (int i = 0; i < AIObject.Count(); i++)
        {
            //check to make sure spawner is enabled
            if (AIObject[i].enableSpawner && AIObject[i].objectPrefab != null)
            {
                GameObject tempGroup = GameObject.Find(AIObject[i].AIGroupName);
                if (tempGroup.GetComponentInChildren<Transform>().childCount < AIObject[i].maxAI)
                {
                    //spawn random number of NPCs
                    for (int y = 0; y < Random.Range(0, AIObject[i].spawnAmount); y++)
                    {
                        //rotation
                        Quaternion randomRotation = Quaternion.Euler(Random.Range(-20, 20), Random.Range(0, 360), 0);
                        //create spawned gameobject
                        GameObject tempSpawn;
                        tempSpawn = Instantiate(AIObject[i].objectPrefab, RandomPosition(), randomRotation);
                        //put as child of group
                        tempSpawn.transform.parent = tempGroup.transform;
                        //add the AIMove
                        tempSpawn.AddComponent<AIMove>();
                    }
                }
            }
        }
    }

    public Vector3 RandomPosition()
    {
        Vector3 randomPosition = new Vector3(
            Random.Range(-spawnArea.x, spawnArea.x),
            Random.Range(-spawnArea.y, spawnArea.y),
            Random.Range(-spawnArea.z, spawnArea.z)
        );
        randomPosition = transform.TransformPoint(randomPosition * .5f);
        return randomPosition;
    }

    public Vector3 RandomWaypoint()
    {
        int randomWP = Random.Range(0, (Waypoints.Count - 1));
        Vector3 randomWaypoint = Waypoints[randomWP].transform.position;
        return randomWaypoint;
    }

    //Method for putting random values in the AI Group setting
    void RandomizeGroups()
    {
        for (int i = 0; i < AIObject.Count(); i++)
        {
            if (AIObject[i].randomizeStats)
            {
                //AIObject[i].maxAI = random.Range(1, 30);
                //AIObject[i] = new AIObjects(AIObject[i].AIGroupName, AIObject[i].objectPrefab, Random.Range(1, 30), Random.Range(1, 20), Random.Range(1, 10), AIObject[i].randomizeStats);
                AIObject[i].setValues(Random.Range(1, 30), Random.Range(1, 20), Random.Range(1, 10));
            }
        }
    }

    void CreateAIGroups()
    {
        for (int i = 0; i < AIObject.Count(); i++)
        {
            GameObject AIGroupSpawn;

            if (AIObject[i].AIGroupName != null) 
            {
                AIGroupSpawn = new GameObject(AIObject[i].AIGroupName);
                AIGroupSpawn.transform.parent = this.gameObject.transform;
            }
        }
    }

    // void GetWaypointsLinq()
    // {
    //     WaypointsLinq = this.transform.GetComponentsInChildren<Transform>().Where(c => c.gameObject.tag == "waypoints").ToArray();
    // }

    void GetWaypoints()
    {
        Transform[] wpList = transform.GetComponentsInChildren<Transform>();
        for (int i = 0; i < wpList.Length; i++)
        {
            if (wpList[i].tag == "waypoints")
            {
                Waypoints.Add(wpList[i]);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = m_SpawnColor;
        Gizmos.DrawCube(transform.position, spawnArea);
    }
}
