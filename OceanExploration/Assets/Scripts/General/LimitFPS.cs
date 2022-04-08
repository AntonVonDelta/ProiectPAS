using System.Collections;
using System.Threading;
using UnityEngine;

public class LimitFPS : MonoBehaviour {
    public int FPS = 50;

    void Start() {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FPS;
    }
}
