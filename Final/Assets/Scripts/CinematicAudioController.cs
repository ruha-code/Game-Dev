using UnityEngine;

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
    private float heartbeatInterval = 0.8f;
    
    void Start()
    {
        cinematic = GetComponent<CinematicIntroController>();
        
        // Setup audio sources if not assigned
        if (ambientSource == null) ambientSource = CreateSource("Ambient", 0.5f, true);
        if (glitchSource == null) glitchSource = CreateSource("Glitch", 0.3f, false);
        if (creatureSource == null) creatureSource = CreateSource("Creature", 0.4f, false);
        if (transitionSource == null) transitionSource = CreateSource("Transition", 0.6f, false);
        if (heartbeatSource == null) heartbeatSource = CreateSource("Heartbeat", 0.3f, false);
        
        // Start ambient drone
        if (ambientSource != null && ambientDrone != null)
        {
            ambientSource.clip = ambientDrone;
            ambientSource.loop = true;
            ambientSource.Play();
        }
        
        // Start subtle screen hum
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
    }
    
    void Update()
    {
        if (cinematic == null) return;
        
        float t = Time.timeSinceLevelLoad;
        
        // Ambient drone volume based on phase
        if (ambientSource != null)
        {
            if (t < 3f)
                ambientSource.volume = Mathf.Lerp(0f, 0.4f, t / 3f);
            else if (t < 8f)
                ambientSource.volume = 0.4f;
            else if (t < 12f)
                ambientSource.volume = Mathf.Lerp(0.4f, 0.6f, (t - 8f) / 4f);
            else if (t < 16f)
                ambientSource.volume = Mathf.Lerp(0.6f, 0.3f, (t - 12f) / 4f);
            else
                ambientSource.volume = Mathf.Lerp(0.3f, 0f, (t - 16f) / 2f);
        }
        
        // Heartbeat during creature appearance (8-12 seconds)
        if (heartbeatSource != null && heartbeat != null)
        {
            if (t >= 8f && t < 12f)
            {
                float timeSinceLastBeat = Time.time - lastHeartbeatTime;
                if (timeSinceLastBeat >= heartbeatInterval)
                {
                    lastHeartbeatTime = Time.time;
                    heartbeatSource.PlayOneShot(heartbeat, Mathf.Lerp(0.3f, 0.6f, (t - 8f) / 4f));
                }
            }
        }
        
        // Glitch sounds during glitch phase (10-12 seconds)
        if (t >= 10f && t < 12f && glitchSource != null)
        {
            if (Time.time - lastGlitchSoundTime > Random.Range(0.3f, 0.8f))
            {
                lastGlitchSoundTime = Time.time;
                PlayGlitchSound();
            }
        }
        
        // Teleport sounds during heavy glitch (10-12 seconds)
        if (t >= 10f && t < 12f && creatureSource != null && teleportSound != null)
        {
            if (Time.time - lastTeleportTime > 0.4f)
            {
                lastTeleportTime = Time.time;
                creatureSource.PlayOneShot(teleportSound, 0.4f);
            }
        }
        
        // Creature whispers during eye contact (8-10 seconds)
        if (t >= 8f && t < 10f && creatureSource != null && creatureWhisper != null)
        {
            if (Time.time - lastWhisperTime > Random.Range(1.5f, 3f))
            {
                lastWhisperTime = Time.time;
                creatureSource.PlayOneShot(creatureWhisper, 0.3f);
            }
        }
        
        // Intense glitch during pull (12-14 seconds) - ramping up
        if (t >= 12f && t < 14f && glitchSource != null && intenseGlitch != null)
        {
            float pullProgress = (t - 12f) / 2f;
            if (Time.time - lastGlitchSoundTime > Mathf.Lerp(0.15f, 0.05f, pullProgress))
            {
                lastGlitchSoundTime = Time.time;
                glitchSource.PlayOneShot(intenseGlitch, Mathf.Lerp(0.5f, 0.8f, pullProgress));
            }
            
            // Rising tone effect
            if (glitchSource != null && ambientDrone != null && !glitchSource.isPlaying)
            {
                glitchSource.pitch = Mathf.Lerp(1f, 2f, pullProgress);
                glitchSource.PlayOneShot(intenseGlitch, pullProgress * 0.6f);
            }
        }
        
        // Final suction sound (14-16 seconds)
        if (t >= 14f && t < 16f && transitionSource != null && transitionWhoosh != null)
        {
            float suctionProgress = (t - 14f) / 2f;
            if (!transitionSource.isPlaying)
            {
                transitionSource.clip = transitionWhoosh;
                transitionSource.pitch = Mathf.Lerp(0.5f, 1.5f, suctionProgress);
                transitionSource.volume = Mathf.Lerp(0.3f, 0.8f, suctionProgress);
                transitionSource.Play();
            }
        }
        
        // Transition whoosh and white flash (16-18 seconds)
        if (t >= 16f && t < 18f && transitionSource != null && transitionWhoosh != null)
        {
            if (!transitionSource.isPlaying)
            {
                transitionSource.clip = transitionWhoosh;
                transitionSource.pitch = 1.5f;
                transitionSource.volume = 0.8f;
                transitionSource.Play();
            }
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
