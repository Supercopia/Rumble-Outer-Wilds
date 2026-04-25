using System;
using AudioSchtuff;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class MusicEmitter: MonoBehaviour
{
    public MusicEmitter(IntPtr ptr) : base(ptr) {}

    public string musicFileName; //TODO set
    private AudioManager.ClipData clipData;
    private float maxVolume = 1f;


    void Start()
    {
        clipData = AudioManager.PlaySoundIfFileExists(Path.Combine(Main.folderPath,musicFileName) ,0,true);
    }

    public void SetVolume(float volume)
    {
        if (clipData != null && Math.Abs(clipData.Reader.Volume - volume) > 0.01)
        {
           AudioManager.ChangeVolume(clipData, Mathf.Clamp(volume, 0, maxVolume));
        }
    }
}