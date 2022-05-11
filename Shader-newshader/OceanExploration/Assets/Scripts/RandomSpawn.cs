using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSpawn : MonoBehaviour


{
    public float itemXspread;
    public float itemYspread;
    public float itemZspread;
    public GameObject seaShell;
    // Start is called before the first frame update
    void Start()
    {
        Vector3 RandomSpawnPosition = new Vector3(Random.Range(1,itemXspread),Random.Range(1,itemYspread),Random.Range(1,itemZspread));
        for(int i = 0;i <=100000;i++){
            GameObject clone = Instantiate(seaShell,RandomSpawnPosition,Quaternion.identity);
        }
    }
    
}
