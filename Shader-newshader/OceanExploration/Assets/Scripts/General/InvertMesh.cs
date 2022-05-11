using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InvertMesh : MonoBehaviour {
    // Start is called before the first frame update
    void Start() {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.triangles = mesh.triangles.Reverse().ToArray();
    }

    // Update is called once per frame
    void Update() {

    }
}
