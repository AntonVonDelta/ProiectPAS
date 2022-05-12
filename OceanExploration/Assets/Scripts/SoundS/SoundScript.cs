using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundScript : MonoBehaviour {
    public const string MasterVolume = "MasterVolume";

    public AudioMixer audioMixer;

    public void SetVolume(float volume) {
        audioMixer.SetFloat(MasterVolume, 20 * Mathf.Log10(volume));
    }
}
