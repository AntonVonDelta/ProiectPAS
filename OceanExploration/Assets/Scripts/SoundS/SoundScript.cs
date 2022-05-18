using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundScript : MonoBehaviour {
    public string MasterVolume = "MasterVolume";

    public AudioMixer audioMixer;

    private void Start() {
        audioMixer.SetFloat(MasterVolume, 20 * Mathf.Log10(1));
    }

    public void SetVolume(float volume) {
        audioMixer.SetFloat(MasterVolume, 20 * Mathf.Log10(volume));
    }
}
