using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectsSource;

    [Header("Volume Settings")]
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1.0f;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.6f;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.8f;

    [Header("Audio Clips")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip playerShotClip;
    public AudioClip enemyShotClip;
    public AudioClip invaderHitClip;
    public AudioClip criticalHitClip;
    public AudioClip playerHitClip;
    public AudioClip ufoHumClip;
    public AudioClip ufoDestroyClip;
    public AudioClip levelWinClip;
    public AudioClip gameOverClip;
    public AudioClip focusChangeClip;
    public AudioClip warningSirenClip;
    public AudioClip shieldDeflectClip;
    public AudioClip depthChargeDiveClip;
    public AudioClip lowEnergyClip;
    public AudioClip[] marchBeats;

    private int currentBeatIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            EnsureAudioSources();
            EnsureProceduralClips();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        if (effectsSource == null)
        {
            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.loop = false;
            effectsSource.playOnAwake = false;
        }
        UpdateVolumes();
    }

    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    private void UpdateVolumes()
    {
        if (musicSource != null) musicSource.volume = masterVolume * musicVolume;
        if (effectsSource != null) effectsSource.volume = masterVolume * sfxVolume;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        EnsureAudioSources();
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    public void PlayEffect(AudioClip clip, float volumeScale = 1.0f)
    {
        if (clip == null) return;
        EnsureAudioSources();
        effectsSource.PlayOneShot(clip, volumeScale * masterVolume * sfxVolume);
    }

    public void PlayMarchBeat()
    {
        if (marchBeats != null && marchBeats.Length > 0)
        {
            PlayEffect(marchBeats[currentBeatIndex], 0.7f);
            currentBeatIndex = (currentBeatIndex + 1) % marchBeats.Length;
        }
    }

    private void EnsureProceduralClips()
    {
        if (playerShotClip == null) playerShotClip = RetroSoundGenerator.CreateLaserSound(880f, 220f, 0.12f);
        if (enemyShotClip == null) enemyShotClip = RetroSoundGenerator.CreateLaserSound(440f, 110f, 0.15f);
        if (invaderHitClip == null) invaderHitClip = RetroSoundGenerator.CreateExplosionSound(0.2f, 200f);
        if (criticalHitClip == null) criticalHitClip = RetroSoundGenerator.CreateHighExplosionSound(0.35f);
        if (playerHitClip == null) playerHitClip = RetroSoundGenerator.CreateExplosionSound(0.5f, 100f);
        if (ufoHumClip == null) ufoHumClip = RetroSoundGenerator.CreateUfoSound(0.6f);
        if (ufoDestroyClip == null) ufoDestroyClip = RetroSoundGenerator.CreateHighExplosionSound(0.4f);
        if (levelWinClip == null) levelWinClip = RetroSoundGenerator.CreateJingle(new float[] { 440, 554, 659, 880 }, 0.12f);
        if (gameOverClip == null) gameOverClip = RetroSoundGenerator.CreateJingle(new float[] { 440, 370, 311, 220 }, 0.2f);
        if (focusChangeClip == null) focusChangeClip = RetroSoundGenerator.CreateFocusChirp(1200f, 0.05f);
        if (warningSirenClip == null) warningSirenClip = RetroSoundGenerator.CreateSirenSound(600f, 900f, 0.25f);
        if (shieldDeflectClip == null) shieldDeflectClip = RetroSoundGenerator.CreateShieldDeflectSound(0.18f);
        if (depthChargeDiveClip == null) depthChargeDiveClip = RetroSoundGenerator.CreateDiveWhine(900f, 300f, 0.6f);
        if (lowEnergyClip == null) lowEnergyClip = RetroSoundGenerator.CreateLowEnergyBeep(0.12f);

        if (marchBeats == null || marchBeats.Length < 4)
        {
            marchBeats = new AudioClip[4];
            float[] freqs = { 110f, 98f, 87f, 78f };
            for (int i = 0; i < 4; i++)
            {
                marchBeats[i] = RetroSoundGenerator.CreateBeatSound(freqs[i], 0.08f);
            }
        }
    }
}

public static class RetroSoundGenerator
{
    private const int SampleRate = 44100;

    public static AudioClip CreateLaserSound(float startFreq, float endFreq, float duration)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float phase = (float)i / SampleRate * freq;
            float wave = (phase % 1.0f < 0.5f) ? 0.6f : -0.6f; // square wave
            float env = 1.0f - t;
            samples[i] = wave * env;
        }
        AudioClip clip = AudioClip.Create("Laser", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip CreateExplosionSound(float duration, float startFilter)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random rnd = new System.Random(42);
        float last = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float alpha = Mathf.Clamp01(startFilter * (1f - t) / SampleRate);
            last = Mathf.Lerp(last, white, alpha);
            float env = Mathf.Pow(1.0f - t, 1.5f);
            samples[i] = last * env * 0.8f;
        }
        AudioClip clip = AudioClip.Create("Explosion", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip CreateHighExplosionSound(float duration)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random rnd = new System.Random(123);
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float tone = Mathf.Sin(2f * Mathf.PI * (800f * (1f - t)) * i / SampleRate);
            float env = 1f - t;
            samples[i] = (noise * 0.5f + tone * 0.5f) * env * 0.8f;
        }
        AudioClip clip = AudioClip.Create("HighExplosion", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip CreateUfoSound(float duration)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float lfo = Mathf.Sin(2f * Mathf.PI * 8f * t) * 100f;
            float freq = 500f + lfo;
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
            samples[i] = wave * 0.4f;
        }
        AudioClip clip = AudioClip.Create("UfoHum", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip CreateBeatSound(float freq, float duration)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float phase = (float)i / SampleRate * freq;
            float wave = (phase % 1.0f < 0.5f) ? 0.8f : -0.8f;
            float env = Mathf.Exp(-t * 8f);
            samples[i] = wave * env;
        }
        AudioClip clip = AudioClip.Create("MarchBeat", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip CreateJingle(float[] notes, float noteDuration)
    {
        int totalSamples = Mathf.CeilToInt(SampleRate * noteDuration * notes.Length);
        float[] samples = new float[totalSamples];
        int noteSampleCount = Mathf.CeilToInt(SampleRate * noteDuration);

        for (int n = 0; n < notes.Length; n++)
        {
            float freq = notes[n];
            for (int i = 0; i < noteSampleCount; i++)
            {
                int sampleIndex = n * noteSampleCount + i;
                if (sampleIndex >= totalSamples) break;
                float t = (float)i / noteSampleCount;
                float wave = Mathf.Sin(2f * Mathf.PI * freq * i / SampleRate);
                float env = 1f - (t * 0.3f);
                samples[sampleIndex] = wave * env * 0.5f;
            }
        }
        AudioClip clip = AudioClip.Create("Jingle", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip CreateFocusChirp(float freq, float duration)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float f = freq + (t * 400f);
            float phase = (float)i / SampleRate * f;
            float wave = Mathf.Sin(2f * Mathf.PI * phase);
            float env = Mathf.Sin(t * Mathf.PI);
            samples[i] = wave * env * 0.45f;
        }
        AudioClip clip = AudioClip.Create("FocusChirp", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip CreateSirenSound(float startFreq, float peakFreq, float duration)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.PingPong(t * 2f, 1f) * (peakFreq - startFreq) + startFreq;
            float phase = (float)i / SampleRate * freq;
            float wave = (phase % 1.0f < 0.5f) ? 0.5f : -0.5f;
            float env = 1f - (t * 0.2f);
            samples[i] = wave * env * 0.5f;
        }
        AudioClip clip = AudioClip.Create("WarningSiren", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip CreateShieldDeflectSound(float duration)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random rnd = new System.Random(88);
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float sine = Mathf.Sin(2f * Mathf.PI * (1400f - t * 800f) * i / SampleRate);
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float env = Mathf.Exp(-t * 12f);
            samples[i] = (sine * 0.7f + noise * 0.3f) * env * 0.65f;
        }
        AudioClip clip = AudioClip.Create("ShieldDeflect", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip CreateDiveWhine(float startFreq, float endFreq, float duration)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t * t);
            float phase = (float)i / SampleRate * freq;
            float wave = Mathf.Sin(2f * Mathf.PI * phase) + 0.3f * Mathf.Sin(4f * Mathf.PI * phase);
            float env = Mathf.Sin(t * Mathf.PI);
            samples[i] = wave * env * 0.4f;
        }
        AudioClip clip = AudioClip.Create("DiveWhine", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip CreateLowEnergyBeep(float duration)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float phase = (float)i / SampleRate * 350f;
            float wave = (phase % 1.0f < 0.5f) ? 0.4f : -0.4f;
            float env = 1f - t;
            samples[i] = wave * env * 0.4f;
        }
        AudioClip clip = AudioClip.Create("LowEnergyBeep", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
