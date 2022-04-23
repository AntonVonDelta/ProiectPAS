using System.Collections;
using System.Threading;
using UnityEngine;

public class LimitFPS : MonoBehaviour {
    public int FPS = 50;

    void Start() {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FPS;

        // ?? https://answers.unity.com/questions/1636093/material-depths-always-appears-blackdepth-shaders.html
        Camera cam = GetComponent<Camera>();
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
    }
}
