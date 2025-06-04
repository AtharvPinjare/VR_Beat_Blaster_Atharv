using System.Diagnostics.Tracing;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    //Allows only one instance of the Sound Manager to exists for referencing from otehr classes which will play sound.
    private AudioSource source;

    private void Awake()
    {
        Instance = this;
        source = GetComponent<AudioSource>();   
    }

    public void PlaySoundOnce(AudioClip _sound) 
    {
        source.PlayOneShot(_sound);
    }
}
