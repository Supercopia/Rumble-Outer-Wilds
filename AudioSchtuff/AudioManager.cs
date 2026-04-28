using System.Collections;
using AudioSchtuff;
using MelonLoader;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using UnityEngine;

public static class AudioManager
{
    private static WaveOutEvent globalWaveOut;
    private static MixingSampleProvider globalMixer;

    // Store coroutine *tokens* instead of IEnumerators
    private static Dictionary<ClipData, object> fadeTokens = new Dictionary<ClipData, object>();

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
            provider = new MonoToStereoSampleProvider(provider);
        if (provider.WaveFormat.SampleRate != globalMixer.WaveFormat.SampleRate)
            provider = new WdlResamplingSampleProvider(provider, globalMixer.WaveFormat.SampleRate);

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
            clipData.Reader.Volume = Mathf.Clamp01(volume);
    }

    // --- Fade functions --- 
    public static void FadeIn(ClipData clipData, float time, float minVolume = 0f, float maxVolume = 1f, bool stopOnEnd = false)
    {
        StartFade(clipData, minVolume, maxVolume, time, stopOnEnd);
    }

    public static void FadeOut(ClipData clipData, float time, float minVolume = 0f, float maxVolume = 1f, bool stopOnEnd = false)
    {
        StartFade(clipData, maxVolume, minVolume, time, stopOnEnd);
    }

    private static void StartFade(ClipData clipData, float from, float to, float duration, bool stopOnEnd)
    {
        if (clipData == null) return;

        // Cancel any existing fade on this clip
        if (fadeTokens.TryGetValue(clipData, out object existingToken))
        {
            MelonCoroutines.Stop(existingToken);
            fadeTokens.Remove(clipData);
        }

        IEnumerator routine = FadeRoutine(clipData, from, to, duration, stopOnEnd);
        object token = MelonCoroutines.Start(routine);
        fadeTokens[clipData] = token;
    }

    private static IEnumerator FadeRoutine(ClipData clipData, float startVolume, float targetVolume, float duration, bool stopOnEnd)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ChangeVolume(clipData, Mathf.Lerp(startVolume, targetVolume, t));
            yield return null;
        }

        ChangeVolume(clipData, targetVolume);

        if (stopOnEnd)
        {
            // Directly clean up without calling StopPlayback (avoids recursion)
            if (clipData.MixerInput != null && globalMixer != null)
                globalMixer.RemoveMixerInput(clipData.MixerInput);
            clipData.Reader?.Dispose();

            // Remove our own token from the dictionary
            if (fadeTokens.ContainsKey(clipData))
                fadeTokens.Remove(clipData);
        }
        else
        {
            // Normal fade ended, remove the token
            if (fadeTokens.ContainsKey(clipData))
                fadeTokens.Remove(clipData);
        }
    }

    public static void StopPlayback(ClipData clipData)
    {
        if (clipData == null) return;

        // Cancel any active fade and remove its token
        if (fadeTokens.TryGetValue(clipData, out object token))
        {
            MelonCoroutines.Stop(token);
            fadeTokens.Remove(clipData);
        }

        if (clipData.MixerInput != null && globalMixer != null)
            globalMixer.RemoveMixerInput(clipData.MixerInput);

        clipData.Reader?.Dispose();
    }
}