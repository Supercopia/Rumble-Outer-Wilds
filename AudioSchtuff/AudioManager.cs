using System.IO;
using MelonLoader;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using UnityEngine;

namespace AudioSchtuff;

public static class AudioManager
{
    private static WaveOutEvent globalWaveOut;
    private static MixingSampleProvider globalMixer;

    public class ClipData
    {
        public AudioFileReader Reader { get; set; }
        public ISampleProvider MixerInput { get; set; }
    }

    public static void Initialize()
    {
        if (globalWaveOut != null) return;

        globalMixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
        {
            ReadFully = true
        };

        globalWaveOut = new WaveOutEvent();
        globalWaveOut.Init(globalMixer);
        globalWaveOut.Play();
    }

    public static ClipData PlaySoundIfFileExists(string soundFilePath, float volume = 1.0f, bool loop = false)
    {
        if (!File.Exists(soundFilePath))
        {
            MelonLogger.Error($"Audio file not found: {soundFilePath}");
            return null;
        }

        Initialize();

        var reader = new AudioFileReader(soundFilePath)
        {
            Volume = Mathf.Clamp01(volume) 
        };

        ISampleProvider provider = new LoopingSampleProvider(reader, loop);

        if (provider.WaveFormat.Channels == 1)
        {
            provider = new MonoToStereoSampleProvider(provider);
        }
        if (provider.WaveFormat.SampleRate != globalMixer.WaveFormat.SampleRate)
        {
            provider = new WdlResamplingSampleProvider(provider, globalMixer.WaveFormat.SampleRate);
        }

        var clipData = new ClipData
        {
            Reader = reader,
            MixerInput = provider
        };

        globalMixer.AddMixerInput(clipData.MixerInput);
    
        return clipData;
    }

    public static void ChangeVolume(ClipData clipData, float volume)
    {
        if (clipData?.Reader != null)
        {
            clipData.Reader.Volume = Mathf.Clamp01(volume);
        }
    }

    public static void StopPlayback(ClipData clipData)
    {
        if (clipData == null) return;
        
        if (clipData.MixerInput != null && globalMixer != null)
        {
            globalMixer.RemoveMixerInput(clipData.MixerInput);
        }
        
        clipData.Reader?.Dispose();
    }
}