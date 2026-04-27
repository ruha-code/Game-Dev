using UnityEngine;

public class ProceduralAudioGenerator : MonoBehaviour
{
    public CinematicAudioController audioController;
    
    void Start()
    {
        if (audioController == null)
            audioController = FindFirstObjectByType<CinematicAudioController>();
        
        if (audioController != null)
        {
            audioController.ambientDrone = GenerateAmbientDrone();
            audioController.glitchStatic = GenerateGlitchStatic();
            audioController.creatureWhisper = GenerateCreatureWhisper();
            audioController.transitionWhoosh = GenerateTransitionWhoosh();
            audioController.heartbeat = GenerateHeartbeat();
            audioController.screenHum = GenerateScreenHum();
            audioController.teleportSound = GenerateTeleportSound();
            audioController.intenseGlitch = GenerateIntenseGlitch();
            
            UnityEngine.Debug.Log("All procedural audio clips generated");
        }
    }
    
    AudioClip GenerateAmbientDrone()
    {
        int sampleRate = 44100;
        int lengthSec = 10;
        AudioClip clip = AudioClip.Create("AmbientDrone", sampleRate * lengthSec, 2, sampleRate, true, GenerateAmbientData);
        return clip;
    }
    
    void GenerateAmbientData(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / 44100f;
            float sample = 0f;
            
            // Low drone
            sample += Mathf.Sin(t * 80f * Mathf.PI * 2f) * 0.3f;
            sample += Mathf.Sin(t * 120f * Mathf.PI * 2f) * 0.2f;
            sample += Mathf.Sin(t * 60f * Mathf.PI * 2f) * 0.25f;
            
            // Subtle modulation
            sample *= 0.8f + Mathf.Sin(t * 0.5f) * 0.2f;
            
            // Add subtle noise
            sample += (Random.value - 0.5f) * 0.02f;
            
            data[i] = Mathf.Clamp(sample, -1f, 1f) * 0.5f;
        }
    }
    
    AudioClip GenerateGlitchStatic()
    {
        int sampleRate = 44100;
        int lengthSec = 1;
        AudioClip clip = AudioClip.Create("GlitchStatic", sampleRate * lengthSec, 1, sampleRate, false);
        float[] data = new float[sampleRate * lengthSec];
        
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;
            
            // White noise bursts
            if (Mathf.Sin(t * 50f) > 0.7f)
                sample = (Random.value - 0.5f) * 0.8f;
            else
                sample = (Random.value - 0.5f) * 0.1f;
            
            // Frequency sweeps
            sample += Mathf.Sin(t * (200f + Mathf.Sin(t * 10f) * 100f) * Mathf.PI * 2f) * 0.2f;
            
            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
    
    AudioClip GenerateCreatureWhisper()
    {
        int sampleRate = 44100;
        int lengthSec = 2;
        AudioClip clip = AudioClip.Create("CreatureWhisper", sampleRate * lengthSec, 1, sampleRate, false);
        float[] data = new float[sampleRate * lengthSec];
        
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;
            
            // Filtered noise (whisper-like)
            float noise = (Random.value - 0.5f) * 0.3f;
            sample += noise * Mathf.Sin(t * 3f * Mathf.PI) * 0.5f;
            
            // Low frequency modulation
            sample += Mathf.Sin(t * 150f * Mathf.PI * 2f) * 0.1f * Mathf.Sin(t * 2f);
            
            // Envelope
            float envelope = Mathf.Sin(t / lengthSec * Mathf.PI);
            sample *= envelope;
            
            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
    
    AudioClip GenerateTransitionWhoosh()
    {
        int sampleRate = 44100;
        int lengthSec = 2;
        AudioClip clip = AudioClip.Create("TransitionWhoosh", sampleRate * lengthSec, 2, sampleRate, false);
        float[] data = new float[sampleRate * lengthSec * 2];
        
        for (int i = 0; i < data.Length / 2; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;
            
            // Rising frequency sweep
            float freq = 100f + t * 500f;
            sample += Mathf.Sin(t * freq * Mathf.PI * 2f) * 0.3f;
            
            // Noise component
            sample += (Random.value - 0.5f) * 0.2f * (t / lengthSec);
            
            // Envelope
            float envelope = Mathf.Sin(t / lengthSec * Mathf.PI);
            sample *= envelope;
            
            data[i * 2] = Mathf.Clamp(sample, -1f, 1f);
            data[i * 2 + 1] = Mathf.Clamp(sample * 0.9f, -1f, 1f);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
    
    AudioClip GenerateHeartbeat()
    {
        int sampleRate = 44100;
        int lengthSec = 2;
        AudioClip clip = AudioClip.Create("Heartbeat", sampleRate * lengthSec, 1, sampleRate, true);
        float[] data = new float[sampleRate * lengthSec];
        
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;
            
            // Two beats per cycle
            float beatPhase = (t * 1.2f) % 1f;
            
            if (beatPhase < 0.1f)
                sample = Mathf.Sin(beatPhase / 0.1f * Mathf.PI) * 0.8f;
            else if (beatPhase > 0.15f && beatPhase < 0.25f)
                sample = Mathf.Sin((beatPhase - 0.15f) / 0.1f * Mathf.PI) * 0.5f;
            
            // Low frequency
            sample *= Mathf.Sin(t * 60f * Mathf.PI * 2f) * 0.5f + 0.5f;
            
            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
    
    AudioClip GenerateScreenHum()
    {
        int sampleRate = 44100;
        int lengthSec = 5;
        AudioClip clip = AudioClip.Create("ScreenHum", sampleRate * lengthSec, 1, sampleRate, true);
        float[] data = new float[sampleRate * lengthSec];
        
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;
            
            // 60Hz hum
            sample += Mathf.Sin(t * 60f * Mathf.PI * 2f) * 0.1f;
            sample += Mathf.Sin(t * 120f * Mathf.PI * 2f) * 0.05f;
            
            // Subtle variation
            sample *= 0.9f + Mathf.Sin(t * 0.3f) * 0.1f;
            
            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
    
    AudioClip GenerateTeleportSound()
    {
        int sampleRate = 44100;
        int lengthSamples = (int)(sampleRate * 0.3f);
        AudioClip clip = AudioClip.Create("TeleportSound", lengthSamples, 1, sampleRate, false);
        float[] data = new float[lengthSamples];
        float lengthSec = 0.3f;
        
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;
            
            // Quick frequency sweep
            float freq = 2000f - t * 5000f;
            sample += Mathf.Sin(t * freq * Mathf.PI * 2f) * 0.5f;
            
            // Noise burst
            sample += (Random.value - 0.5f) * 0.3f * (1f - t / lengthSec);
            
            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
    
    float Tanh(float x) { float e2x = Mathf.Exp(2f * x); return (e2x - 1f) / (e2x + 1f); }
    
    AudioClip GenerateIntenseGlitch()
    {
        int sampleRate = 44100;
        int lengthSamples = (int)(sampleRate * 0.5f);
        AudioClip clip = AudioClip.Create("IntenseGlitch", lengthSamples, 1, sampleRate, false);
        float[] data = new float[lengthSamples];
        float lengthSec = 0.5f;
        
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;
            
            // Multiple frequency layers
            sample += Mathf.Sin(t * 800f * Mathf.PI * 2f) * 0.3f;
            sample += Mathf.Sin(t * 1200f * Mathf.PI * 2f) * 0.2f;
            sample += Mathf.Sin(t * 400f * Mathf.PI * 2f) * 0.25f;
            
            // Heavy noise
            sample += (Random.value - 0.5f) * 0.5f;
            
            // Distortion
            sample = Tanh(sample * 2f);
            
            data[i] = Mathf.Clamp(sample, -1f, 1f);
        }
        
        clip.SetData(data, 0);
        return clip;
    }
}
