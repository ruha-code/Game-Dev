using UnityEngine;
using System.Collections;

public class CinematicAudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource ambientSource;
    public AudioSource glitchSource;
    public AudioSource creatureSource;
    public AudioSource transitionSource;
    public AudioSource heartbeatSource;
    
    [Header("Audio Clips")]
    public AudioClip ambientDrone;
    public AudioClip glitchStatic;
    public AudioClip creatureWhisper;
    public AudioClip transitionWhoosh;
    public AudioClip heartbeat;
    public AudioClip screenHum;
    public AudioClip teleportSound;
    public AudioClip intenseGlitch;
    
    private CinematicIntroController cinematic;
    private float lastTeleportTime;
    private float lastWhisperTime;
    private float lastGlitchSoundTime;
    private float lastHeartbeatTime;
    private bool isSilenced;
    private bool hasPlayedTransitionWhoosh;

    void Start()
    {
        cinematic = GetComponent<CinematicIntroController>();
        
        if (ambientSource == null) ambientSource = CreateSource("Ambient", 0.5f, true);
        if (glitchSource == null) glitchSource = CreateSource("Glitch", 0.3f, false);
        if (creatureSource == null) creatureSource = CreateSource("Creature", 0.4f, false);
        if (transitionSource == null) transitionSource = CreateSource("Transition", 0.6f, false);
        if (heartbeatSource == null) heartbeatSource = CreateSource("Heartbeat", 0.3f, false);
        
        if (ambientSource != null && ambientDrone != null)
        {
            ambientSource.clip = ambientDrone;
            ambientSource.loop = true;
            ambientSource.Play();
        }
        
        if (screenHum != null)
        {
            AudioSource humSource = CreateSource("ScreenHum", 0.08f, true);
            humSource.clip = screenHum;
            humSource.Play();
        }
        
        lastTeleportTime = Time.time;
        lastWhisperTime = Time.time;
        lastGlitchSoundTime = Time.time;
        lastHeartbeatTime = Time.time;
        hasPlayedTransitionWhoosh = false;
    }

    public void Silence()
    {
        if (isSilenced) return;
        isSilenced = true;
        
        if (ambientSource != null) StartCoroutine(FadeOutSource(ambientSource, 1f));
        if (glitchSource != null) glitchSource.Stop();
        if (creatureSource != null) creatureSource.Stop();
        if (transitionSource != null) transitionSource.Stop();
        if (heartbeatSource != null) heartbeatSource.Stop();
    }

    private IEnumerator FadeOutSource(AudioSource source, float duration)
    {
        float startVol = source.volume;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        source.volume = 0;
        source.Stop();
    }

    void Update()
    {
        if (cinematic == null || isSilenced) return;
        
        float t = cinematic.Timeline;

        if (ambientSource != null)
        {
            if (t < cinematic.T2)
                ambientSource.volume = Mathf.Lerp(0f, 0.4f, t / cinematic.T2);
            else if (t < cinematic.T3)
                ambientSource.volume = 0.4f;
            else if (t < cinematic.T5)
                ambientSource.volume = Mathf.Lerp(0.4f, 0.6f, (t - cinematic.T3) / (cinematic.T5 - cinematic.T3));
            else if (t < cinematic.T8)
                ambientSource.volume = Mathf.Lerp(0.6f, 0.3f, (t - cinematic.T5) / (cinematic.T8 - cinematic.T5));
            else
                ambientSource.volume = Mathf.Lerp(0.3f, 0f, (t - cinematic.T8) / 2f);
        }
        
        if (heartbeatSource != null && heartbeat != null)
        {
            if (t >= cinematic.T4 && t < cinematic.T6)
            {
                float timeSinceLastBeat = Time.time - lastHeartbeatTime;
                float intensity = (t - cinematic.T4) / (cinematic.T6 - cinematic.T4);
                float currentInterval = Mathf.Lerp(0.8f, 0.4f, intensity);
                
                if (timeSinceLastBeat >= currentInterval)
                {
                    lastHeartbeatTime = Time.time;
                    heartbeatSource.PlayOneShot(heartbeat, Mathf.Lerp(0.3f, 0.7f, intensity));
                }
            }
        }
        
        if (t >= cinematic.T6 && t < cinematic.T7 && glitchSource != null)
        {
            float intensity = (t - cinematic.T6) / (cinematic.T7 - cinematic.T6);
            if (Time.time - lastGlitchSoundTime > Random.Range(0.1f, 0.5f) / (1f + intensity))
            {
                lastGlitchSoundTime = Time.time;
                PlayGlitchSound();
            }
        }
        
        if (t >= cinematic.T6 && t < cinematic.T7 && creatureSource != null && teleportSound != null)
        {
            if (Time.time - lastTeleportTime > Random.Range(0.2f, 0.6f))
            {
                lastTeleportTime = Time.time;
                creatureSource.PlayOneShot(teleportSound, 0.4f);
            }
        }
        
        if (t >= cinematic.T5 && t < cinematic.T6 && creatureSource != null && creatureWhisper != null)
        {
            if (Time.time - lastWhisperTime > Random.Range(1f, 2.5f))
            {
                lastWhisperTime = Time.time;
                creatureSource.PlayOneShot(creatureWhisper, 0.5f);
            }
        }
        
        if (t >= cinematic.T7 && t < cinematic.T8 && glitchSource != null && intenseGlitch != null)
        {
            float pullProgress = (t - cinematic.T7) / (cinematic.T8 - cinematic.T7);
            if (Time.time - lastGlitchSoundTime > Mathf.Lerp(0.15f, 0.03f, pullProgress))
            {
                lastGlitchSoundTime = Time.time;
                glitchSource.PlayOneShot(intenseGlitch, Mathf.Lerp(0.5f, 1.0f, pullProgress));
            }
        }
        
        if (t >= cinematic.T8 && transitionSource != null && transitionWhoosh != null)
        {
            if (!hasPlayedTransitionWhoosh)
            {
                hasPlayedTransitionWhoosh = true;
                transitionSource.clip = transitionWhoosh;
                transitionSource.Play();
            }
            float progress = (t - cinematic.T8) / 1.5f; 
            transitionSource.pitch = Mathf.Lerp(1.0f, 2.5f, progress);
            transitionSource.volume = Mathf.Lerp(0.6f, 1.0f, progress);
        }
    }

    void PlayGlitchSound()
    {
        if (glitchSource == null) return;
        if (glitchStatic != null)
            glitchSource.PlayOneShot(glitchStatic, Random.Range(0.2f, 0.4f));
    }
    
    AudioSource CreateSource(string name, float volume, bool loop)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        AudioSource source = go.AddComponent<AudioSource>();
        source.volume = volume;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        return source;
    }
}
