using UnityEngine;
using System.Collections;

public class CinematicIntroController : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera;
    
    [Header("Monitor")]
    public BootScreenController screenController;
    public MonitorScreenController monitorScreen;
    public Light monitorLight;
    
    [Header("Lighting")]
    public Light[] ambientLights;
    
    [Header("Creature")]
    public Shader glitchCreatureShader;
    
    [Header("Timing")]
    public float blackScreenDuration = 0.5f;
    public float fadeInDuration = 1f;
    public float enterRoomDuration = 2f;
    public float focusDuration = 2f;
    public float entityAppearDuration = 1.5f;
    public float entityLookDuration = 1.5f;
    public float glitchDuration = 1.5f;
    public float pullInDuration = 1.5f;
    public float transitionDuration = 1.5f;
    
    [Header("Audio")]
    public CinematicAudioController audioController;
    
    [Header("Settings")]
    public string nextScene = "AeroDesktopScene";
    
    private Vector3[] cameraPath;
    private float[] pathDistances;
    private float totalPathLength;
    private Vector3 currentLookTarget;
    private float lookTransitionSpeed = 3f;
    private Vector3 monitorPosition;
    
    private float timeline;
    private float blackOverlayAlpha = 1f;
    private float whiteFlashAlpha;
    private GameObject creatureRoot;
    private Material creatureMaterial;
    private bool creatureSpawned;
    private float glitchIntensity;
    private float shakeAmount;
    private bool transitionStarted;
    
    private float t1, t2, t3, t4, t5, t6, t7, t8;
    
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = Color.black;
        
        GameObject screenSurface = GameObject.Find("Boot Screen Surface");
        monitorPosition = screenSurface != null ? screenSurface.transform.position : new Vector3(0f, 1.35f, 2.24f);
        
        if (monitorLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include);
            foreach (Light l in lights)
            {
                if (l.name.ToLower().Contains("monitor"))
                {
                    monitorLight = l;
                    break;
                }
            }
        }
        
        if (monitorScreen == null)
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            foreach (Renderer r in renderers)
            {
                monitorScreen = r.GetComponent<MonitorScreenController>();
                if (monitorScreen != null) break;
            }
        }
        
        if (audioController == null)
            audioController = gameObject.AddComponent<CinematicAudioController>();
        
        // Add procedural audio generator
        ProceduralAudioGenerator audioGen = gameObject.GetComponent<ProceduralAudioGenerator>();
        if (audioGen == null)
            audioGen = gameObject.AddComponent<ProceduralAudioGenerator>();
        audioGen.audioController = audioController;
        
        BuildCameraPath();
        currentLookTarget = new Vector3(0f, 1.5f, 10f);
        
        mainCamera.transform.position = cameraPath[0];
        mainCamera.transform.LookAt(currentLookTarget);
        
        t1 = blackScreenDuration;
        t2 = t1 + fadeInDuration;
        t3 = t2 + enterRoomDuration;
        t4 = t3 + focusDuration;
        t5 = t4 + entityAppearDuration;
        t6 = t5 + entityLookDuration;
        t7 = t6 + glitchDuration;
        t8 = t7 + pullInDuration;
        
        if (monitorLight != null)
            monitorLight.intensity = 0f;
        
        StartCoroutine(RunCinematic());
    }
    
    IEnumerator RunCinematic()
    {
        timeline = 0f;
        while (timeline < t8 + transitionDuration)
        {
            float dt = Time.deltaTime;
            timeline += dt;
            UpdateCamera(timeline, dt);
            UpdateLighting(timeline);
            UpdateCreature(timeline, dt);
            UpdateGlitchEffects(timeline, dt);
            UpdateOverlay(timeline);
            if (timeline >= t8 + transitionDuration && !transitionStarted)
            {
                transitionStarted = true;
                yield return StartCoroutine(TransitionToNextScene());
            }
            yield return null;
        }
    }
    
    void BuildCameraPath()
    {
        cameraPath = new Vector3[]
        {
            new Vector3(0f, 1.5f, -6f),
            new Vector3(0f, 1.5f, -3f),
            new Vector3(0f, 1.5f, -0.5f),
            new Vector3(0f, 1.5f, 0.5f),
            new Vector3(0f, 1.5f, 1.2f),
            new Vector3(0f, 1.4f, 1.8f),
            monitorPosition + new Vector3(0f, 0f, -0.1f),
        };
        pathDistances = new float[cameraPath.Length];
        pathDistances[0] = 0f;
        totalPathLength = 0f;
        for (int i = 1; i < cameraPath.Length; i++)
        {
            totalPathLength += Vector3.Distance(cameraPath[i - 1], cameraPath[i]);
            pathDistances[i] = totalPathLength;
        }
    }
    
    Vector3 GetPositionOnPath(float t)
    {
        float totalDuration = t8 + transitionDuration;
        float normalizedTime = Mathf.Clamp01(t / totalDuration);
        float speedProfile;
        if (normalizedTime < 0.15f)
            speedProfile = normalizedTime / 0.15f * 0.3f;
        else if (normalizedTime < 0.7f)
            speedProfile = 0.3f + (normalizedTime - 0.15f) / 0.55f * 0.4f;
        else
            speedProfile = 0.7f + (normalizedTime - 0.7f) / 0.3f * 0.3f;
        float targetDistance = speedProfile * totalPathLength;
        for (int i = 1; i < cameraPath.Length; i++)
        {
            if (targetDistance <= pathDistances[i])
            {
                float segStart = pathDistances[i - 1];
                float segLen = pathDistances[i] - segStart;
                float segT = segLen > 0 ? (targetDistance - segStart) / segLen : 0f;
                float smoothT = segT * segT * (3f - 2f * segT);
                return Vector3.Lerp(cameraPath[i - 1], cameraPath[i], smoothT);
            }
        }
        return cameraPath[cameraPath.Length - 1];
    }
    
    Vector3 GetLookTarget(float t)
    {
        if (t < t3) return new Vector3(0f, 1.5f, 10f);
        if (t < t5) return monitorPosition;
        if (t < t6) return new Vector3(0.51f, 1.0f, 1.24f);
        return monitorPosition;
    }
    
    void UpdateCamera(float t, float dt)
    {
        if (t >= t8)
        {
            float endProgress = (t - t8) / transitionDuration;
            float accelerateT = endProgress * endProgress * endProgress;
            mainCamera.transform.position = Vector3.Lerp(
                monitorPosition + new Vector3(0f, 0f, -0.3f),
                monitorPosition + new Vector3(0f, 0f, 5f),
                accelerateT
            );
            if (endProgress < 0.2f)
                mainCamera.fieldOfView = Mathf.Lerp(15f, 3f, endProgress / 0.2f);
            else if (endProgress < 0.5f)
                mainCamera.fieldOfView = Mathf.Lerp(3f, 80f, (endProgress - 0.2f) / 0.3f);
            else
                mainCamera.fieldOfView = Mathf.Lerp(80f, 120f, (endProgress - 0.5f) / 0.5f);
            float spiralSpeed = endProgress < 0.3f ? 720f : 180f;
            mainCamera.transform.Rotate(Vector3.forward, spiralSpeed * dt * endProgress);
            Vector3 lookTarget = mainCamera.transform.position + Vector3.forward * 20f;
            mainCamera.transform.LookAt(lookTarget);
            if (endProgress < 0.4f)
            {
                float distortionStrength = (1f - endProgress / 0.4f) * 0.05f;
                mainCamera.transform.position += new Vector3(
                    Mathf.Sin(Time.time * 60f) * distortionStrength,
                    Mathf.Cos(Time.time * 45f) * distortionStrength,
                    Mathf.Sin(Time.time * 30f) * distortionStrength * 0.5f
                );
            }
            return;
        }
        Vector3 targetPos = GetPositionOnPath(t);
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, 1f - Mathf.Exp(-8f * dt));
        Vector3 desiredLook = GetLookTarget(t);
        currentLookTarget = Vector3.Lerp(currentLookTarget, desiredLook, 1f - Mathf.Exp(-lookTransitionSpeed * dt));
        currentLookTarget.y = Mathf.Lerp(currentLookTarget.y, 1.5f, 0.3f);
        mainCamera.transform.LookAt(currentLookTarget);
        if (t < t2) mainCamera.fieldOfView = 60f;
        else if (t < t3) mainCamera.fieldOfView = Mathf.Lerp(60f, 55f, (t - t2) / (t3 - t2));
        else if (t < t7) mainCamera.fieldOfView = 55f;
        else if (t < t8) mainCamera.fieldOfView = Mathf.Lerp(55f, 30f, (t - t7) / (t8 - t7));
    }
    
    void UpdateLighting(float t)
    {
        if (monitorLight == null) return;
        if (t < t2) monitorLight.intensity = 0f;
        else if (t < t3) monitorLight.intensity = Mathf.Lerp(0f, 0.5f, (t - t2) / (t3 - t2));
        else if (t < t4) monitorLight.intensity = Mathf.Lerp(0.5f, 2f, (t - t3) / (t4 - t3));
        else if (t < t6) monitorLight.intensity = 2f;
        else if (t < t7) { float p = Mathf.Sin(t * 20f) * 0.5f + 0.5f; monitorLight.intensity = 2f + p * 3f * glitchIntensity; }
        else if (t < t8) monitorLight.intensity = Mathf.Lerp(2f, 8f, (t - t7) / (t8 - t7));
        else monitorLight.intensity = Mathf.Lerp(8f, 0f, (t - t8) / transitionDuration);
        if (ambientLights != null)
        {
            float ai = t < t3 ? 0.1f : 0.05f;
            foreach (Light l in ambientLights) if (l != null && l != monitorLight) l.intensity = ai;
        }
    }
    
    private Vector3 creatureSeatPos;
    private Vector3[] teleportPositions;
    private float teleportTimer;
    private float teleportInterval = 0.3f;
    private int currentTeleportIndex;
    private bool creatureLookingAtPlayer;
    private float lookAtPlayerTimer;
    private float lookAtPlayerDuration = 1.5f;
    private float eyeContactStart;
    
    void UpdateCreature(float t, float dt)
    {
        if (t < t4) { if (creatureRoot != null) creatureRoot.SetActive(false); return; }
        if (!creatureSpawned) { SpawnCreature(); creatureSpawned = true; }
        if (creatureRoot == null || creatureMaterial == null) return;
        creatureRoot.SetActive(true);
        
        creatureSeatPos = new Vector3(0.5f, 0.48f, 1.2f);
        
        if (t < t5)
        {
            float p = (t - t4) / entityAppearDuration;
            float fl = Mathf.PerlinNoise(t * 10f, 0f);
            float alpha = p * (0.5f + 0.5f * fl);
            creatureMaterial.SetFloat("_FlickerIntensity", 0.8f);
            creatureMaterial.SetColor("_MainColor", new Color(0.1f, 0.1f, 0.1f, alpha));
            creatureRoot.transform.position = creatureSeatPos;
            creatureRoot.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 1f));
            creatureRoot.transform.localScale = Vector3.one;
        }
        else if (t < t6)
        {
            float p = (t - t5) / entityLookDuration;
            
            creatureMaterial.SetFloat("_FlickerIntensity", 0.6f);
            creatureMaterial.SetColor("_MainColor", new Color(0.1f, 0.1f, 0.1f, 0.7f));
            creatureRoot.transform.localScale = Vector3.one;
            
            // Monitor screen shows creature face during eye contact with glitch effects
            if (monitorScreen != null)
            {
                if (p > 0.3f)
                {
                    monitorScreen.faceAlpha = Mathf.Lerp(0f, 0.9f, (p - 0.3f) / 0.7f);
                    monitorScreen.glitchIntensity = 0.2f + p * 0.3f;
                    monitorScreen.chromaticOffset = p * 0.02f;
                    
                    // Random face flicker
                    if (Random.value < 0.1f)
                        monitorScreen.faceAlpha = 0f;
                }
                else
                {
                    monitorScreen.faceAlpha = 0f;
                    monitorScreen.glitchIntensity = p * 0.3f;
                }
            }
            
            if (p < 0.3f)
            {
                creatureRoot.transform.position = creatureSeatPos;
                creatureRoot.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 1f));
            }
            else if (!creatureLookingAtPlayer)
            {
                creatureLookingAtPlayer = true;
                eyeContactStart = t;
                creatureRoot.transform.position = creatureSeatPos;
            }
            else
            {
                float contactTime = t - eyeContactStart;
                
                // Enhanced glitch movement during eye contact
                creatureRoot.transform.position = creatureSeatPos + new Vector3(
                    Mathf.Sin(contactTime * 15f) * 0.02f + (Random.value - 0.5f) * 0.01f,
                    Mathf.Sin(contactTime * 20f) * 0.01f,
                    (Random.value - 0.5f) * 0.01f
                );
                
                Transform head = creatureRoot.transform.Find("Head");
                if (head != null)
                {
                    Vector3 toCamera = mainCamera.transform.position - head.position;
                    head.rotation = Quaternion.LookRotation(toCamera);
                    
                    if (contactTime > 0.5f && contactTime < 1.5f)
                    {
                        float jitter = Mathf.Sin(t * 25f) * 3f;
                        head.localRotation *= Quaternion.Euler(jitter, Mathf.Sin(t * 18f) * 2f, Mathf.Sin(t * 30f) * 1f);
                        
                        // Random head snap
                        if (Random.value < 0.05f)
                        {
                            head.localRotation *= Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(-10f, 10f), 0f);
                        }
                    }
                }
                
                creatureMaterial.SetFloat("_FlickerIntensity", 0.5f + Mathf.Sin(t * 12f) * 0.3f);
                float alphaFlicker = 0.5f + Mathf.PerlinNoise(t * 8f, 0f) * 0.4f;
                creatureMaterial.SetColor("_MainColor", new Color(0.1f, 0.1f, 0.1f, alphaFlicker));
                
                // Random scale glitch
                if (Random.value < 0.03f)
                {
                    creatureRoot.transform.localScale = new Vector3(
                        Random.Range(0.9f, 1.1f),
                        Random.Range(0.95f, 1.05f),
                        Random.Range(0.9f, 1.1f)
                    );
                }
            }
        }
        else if (t < t7)
        {
            float p = (t - t6) / glitchDuration;
            glitchIntensity = p;
            
            creatureLookingAtPlayer = false;
            
            // Monitor screen heavy glitch with face flickering
            if (monitorScreen != null)
            {
                monitorScreen.glitchIntensity = 0.3f + p * 0.7f;
                monitorScreen.faceAlpha = Mathf.PerlinNoise(t * 5f, 0f) > 0.5f ? 0.6f + p * 0.4f : 0f;
                monitorScreen.chromaticOffset = p * 0.05f;
                monitorScreen.screenTear = p * 0.5f;
            }
            
            creatureMaterial.SetFloat("_FlickerIntensity", 1f);
            creatureMaterial.SetFloat("_Distortion", 0.5f + p * 0.5f);
            
            teleportTimer += dt;
            if (teleportTimer >= teleportInterval)
            {
                teleportTimer = 0f;
                teleportInterval = Random.Range(0.15f, 0.5f);
                
                if (teleportPositions == null)
                {
                    teleportPositions = new Vector3[]
                    {
                        creatureSeatPos,
                        creatureSeatPos + new Vector3(1.5f, 0f, 0f),
                        creatureSeatPos + new Vector3(-1f, 0f, 0.5f),
                        creatureSeatPos + new Vector3(0f, 0f, -1f),
                        creatureSeatPos + new Vector3(0.8f, 0f, -0.5f),
                    };
                }
                
                currentTeleportIndex = (currentTeleportIndex + 1) % teleportPositions.Length;
                creatureRoot.transform.position = teleportPositions[currentTeleportIndex];
                
                float flashAlpha = Random.value > 0.5f ? 1f : 0.1f;
                creatureMaterial.SetColor("_MainColor", new Color(0.1f, 0.1f, 0.1f, flashAlpha));
            }
            
            if (Random.value < 0.05f)
            {
                creatureRoot.transform.localScale = new Vector3(
                    Random.Range(0.7f, 1.3f),
                    Random.Range(0.8f, 1.2f),
                    Random.Range(0.7f, 1.3f)
                );
            }
            else
            {
                creatureRoot.transform.localScale = Vector3.Lerp(creatureRoot.transform.localScale, Vector3.one, dt * 10f);
            }
            
            Transform head = creatureRoot.transform.Find("Head");
            if (head != null && Random.value < 0.1f)
            {
                Vector3 toCamera = mainCamera.transform.position - head.position;
                head.rotation = Quaternion.LookRotation(toCamera);
            }
        }
        else if (t < t8)
        {
            float p = (t - t7) / pullInDuration;
            
            // Switch screen to pull mode with intense effects
            if (screenController != null)
                screenController.SetMode(BootScreenController.ScreenMode.Pull, p);
            
            // Monitor screen: intense glitch then white out
            if (monitorScreen != null)
            {
                if (p < 0.6f)
                {
                    monitorScreen.glitchIntensity = 1f;
                    monitorScreen.faceAlpha = Mathf.Lerp(0.8f, 0f, p / 0.6f);
                    monitorScreen.chromaticOffset = 0.05f * (1f - p);
                    monitorScreen.screenTear = 0.5f + p * 0.5f;
                }
                else if (p < 0.85f)
                {
                    // Screen tearing and color distortion
                    monitorScreen.glitchIntensity = Mathf.Lerp(1f, 0.5f, (p - 0.6f) / 0.25f);
                    monitorScreen.faceAlpha = 0f;
                    monitorScreen.chromaticOffset = Mathf.Lerp(0.02f, 0.08f, (p - 0.6f) / 0.25f);
                    monitorScreen.screenTear = 1f;
                }
                else
                {
                    // White out transition
                    monitorScreen.glitchIntensity = Mathf.Lerp(0.5f, 0f, (p - 0.85f) / 0.15f);
                    monitorScreen.faceAlpha = 0f;
                    monitorScreen.chromaticOffset = 0f;
                    monitorScreen.screenTear = Mathf.Lerp(1f, 0f, (p - 0.85f) / 0.15f);
                    monitorScreen.brightness = Mathf.Lerp(1f, 3f, (p - 0.85f) / 0.15f);
                }
            }
            
            // Enhanced dissolve effect with acceleration
            creatureMaterial.SetFloat("_FlickerIntensity", 1f);
            creatureMaterial.SetFloat("_Distortion", 1f);
            creatureMaterial.SetFloat("_DissolveAmount", Mathf.Pow(p, 1.5f) * 0.9f);
            creatureMaterial.SetFloat("_ParticleDensity", 0.8f + p * 0.2f);
            creatureMaterial.SetFloat("_ParticleSpeed", 5f + p * 10f);
            creatureMaterial.SetFloat("_GlitchBlocks", 0.5f + p * 0.5f);
            
            // Heavy particle emission during dissolve
            if (creatureParticles != null)
            {
                var em = creatureParticles.emission;
                em.enabled = true;
                em.rateOverTime = 100f + Mathf.Pow(p, 2f) * 400f;
            }
            
            // Accelerated stretch and movement
            float stretchP = Mathf.Pow(p, 1.3f);
            creatureRoot.transform.localScale = new Vector3(
                1f - stretchP * 0.7f,
                1f + stretchP * 3f,
                1f - stretchP * 0.7f
            );
            
            Vector3 startPos = teleportPositions != null && currentTeleportIndex >= 0 ? teleportPositions[currentTeleportIndex] : creatureSeatPos;
            
            // Creature accelerates into monitor
            float moveP = Mathf.Pow(p, 2f);
            creatureRoot.transform.position = Vector3.Lerp(startPos, monitorPosition + new Vector3(0f, 0f, 0.3f), moveP);
            
            // Random glitch jumps near the end
            if (p > 0.7f && Random.value < 0.2f)
            {
                creatureRoot.transform.position += new Vector3(
                    (Random.value - 0.5f) * 0.2f * (1f - p),
                    (Random.value - 0.5f) * 0.1f * (1f - p),
                    (Random.value - 0.5f) * 0.1f * (1f - p)
                );
            }
            
            creatureMaterial.SetColor("_MainColor", new Color(0.05f, 0.08f, 0.1f, 1f - Mathf.Pow(p, 1.5f)));
        }
        else creatureRoot.SetActive(false);
    }
    
    void UpdateGlitchEffects(float t, float dt)
    {
        if (t >= t6 && t < t7)
        {
            shakeAmount = glitchIntensity * 0.03f;
            mainCamera.transform.position += new Vector3(Random.Range(-shakeAmount, shakeAmount), Random.Range(-shakeAmount, shakeAmount), 0f);
        }
        else if (t >= t7 && t < t8)
        {
            // Intense screen distortion during pull-in
            float pullProgress = (t - t7) / (t8 - t7);
            shakeAmount = pullProgress * 0.08f;
            mainCamera.transform.position += new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount * 0.5f, shakeAmount * 0.5f)
            );
            
            // Random FOV spikes
            if (Random.value < 0.1f)
            {
                mainCamera.fieldOfView += Random.Range(-10f, 10f) * pullProgress;
            }
        }
        else shakeAmount = 0f;
    }
    
    void UpdateOverlay(float t)
    {
        if (t < t2) { blackOverlayAlpha = 1f; whiteFlashAlpha = 0f; }
        else if (t < t2 + fadeInDuration * 0.5f) { float p = (t - t2) / (fadeInDuration * 0.5f); blackOverlayAlpha = 1f - p; whiteFlashAlpha = 0f; }
        else if (t >= t8)
        {
            float progress = (t - t8) / transitionDuration;
            blackOverlayAlpha = 0f;
            if (progress < 0.1f)
                whiteFlashAlpha = 0.5f + progress / 0.1f * 0.5f;
            else if (progress < 0.3f)
                whiteFlashAlpha = 1f;
            else if (progress < 0.5f)
                whiteFlashAlpha = Random.value > 0.4f ? 1f : 0.6f;
            else if (progress < 0.8f)
            {
                whiteFlashAlpha = Mathf.Lerp(1f, 0f, (progress - 0.5f) / 0.3f);
                if (Random.value < 0.1f) whiteFlashAlpha = Mathf.Max(whiteFlashAlpha, 0.5f);
            }
            else
                whiteFlashAlpha = Mathf.Lerp(0.3f, 0f, (progress - 0.8f) / 0.2f);
        }
        else { blackOverlayAlpha = 0f; whiteFlashAlpha = 0f; }
        if (t >= t6 && t < t7 && Random.value < 0.1f) blackOverlayAlpha = Mathf.Max(blackOverlayAlpha, 0.3f);
    }
    
    private ParticleSystem creatureParticles;
    
    void SpawnCreature()
    {
        creatureRoot = new GameObject("GlitchEntity");
        creatureRoot.transform.position = creatureSeatPos;
        creatureRoot.transform.SetParent(GameObject.Find("BootScene_Setup")?.transform);
        creatureLookingAtPlayer = false;
        teleportTimer = 0f;
        currentTeleportIndex = 0;
        
        // Create particle system for breakup effects
        GameObject particleObj = new GameObject("CreatureParticles");
        particleObj.transform.SetParent(creatureRoot.transform);
        particleObj.transform.localPosition = Vector3.zero;
        creatureParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = creatureParticles.main;
        main.startColor = new Color(0.3f, 0.6f, 0.8f, 0.6f);
        main.startSize = 0.02f;
        main.startLifetime = 1.5f;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = creatureParticles.emission;
        emission.enabled = false;
        
        var shape = creatureParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        
        var renderer = creatureParticles.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.SetColor("_Color", new Color(0.3f, 0.6f, 0.8f, 0.5f));
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", new Color(0.2f, 0.5f, 0.7f, 1f));
        
        // Try enhanced shader first, fallback to original
        Shader enhancedShader = Shader.Find("GlitchCreature/EnhancedEntity");
        if (enhancedShader != null)
        {
            creatureMaterial = new Material(enhancedShader);
            creatureMaterial.SetColor("_MainColor", new Color(0.05f, 0.08f, 0.1f, 0f));
            creatureMaterial.SetFloat("_NoiseScale", 5f);
            creatureMaterial.SetFloat("_NoiseSpeed", 2f);
            creatureMaterial.SetFloat("_FlickerIntensity", 0.5f);
            creatureMaterial.SetFloat("_Distortion", 0.3f);
            creatureMaterial.SetFloat("_ParticleDensity", 0.3f);
            creatureMaterial.SetFloat("_ParticleSpeed", 1f);
            creatureMaterial.SetFloat("_GlitchBlocks", 0.2f);
            creatureMaterial.SetFloat("_DissolveAmount", 0f);
        }
        else if (glitchCreatureShader != null)
        {
            creatureMaterial = new Material(glitchCreatureShader);
            creatureMaterial.SetColor("_MainColor", new Color(0.1f, 0.1f, 0.1f, 0f));
            creatureMaterial.SetFloat("_NoiseScale", 5f);
            creatureMaterial.SetFloat("_NoiseSpeed", 2f);
            creatureMaterial.SetFloat("_FlickerIntensity", 0.5f);
            creatureMaterial.SetFloat("_Distortion", 0.3f);
        }
        else
        {
            creatureMaterial = new Material(Shader.Find("Standard"));
            creatureMaterial.SetColor("_Color", new Color(0.1f, 0.1f, 0.1f, 0.5f));
            creatureMaterial.SetFloat("_Mode", 3);
            creatureMaterial.EnableKeyword("_ALPHABLEND_ON");
        }
        
        CreateBodyPart("Torso", PrimitiveType.Capsule, new Vector3(0f, 0.6f, 0f), new Vector3(0.25f, 0.35f, 0.15f));
        GameObject head = CreateBodyPart("Head", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), new Vector3(0.12f, 0.15f, 0.12f));
        head.transform.SetParent(creatureRoot.transform);
        CreateBodyPart("LeftArm", PrimitiveType.Capsule, new Vector3(-0.25f, 0.55f, 0f), new Vector3(0.06f, 0.3f, 0.06f));
        CreateBodyPart("RightArm", PrimitiveType.Capsule, new Vector3(0.25f, 0.55f, 0f), new Vector3(0.06f, 0.3f, 0.06f));
        CreateBodyPart("LeftLeg", PrimitiveType.Capsule, new Vector3(-0.1f, 0.15f, 0f), new Vector3(0.08f, 0.3f, 0.08f));
        CreateBodyPart("RightLeg", PrimitiveType.Capsule, new Vector3(0.1f, 0.15f, 0f), new Vector3(0.08f, 0.3f, 0.08f));
    }
    
    GameObject CreateBodyPart(string name, PrimitiveType type, Vector3 lp, Vector3 ls)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(creatureRoot.transform);
        part.transform.localPosition = lp;
        part.transform.localScale = ls;
        part.transform.localRotation = Quaternion.identity;
        Renderer rend = part.GetComponent<Renderer>();
        if (rend != null) { rend.material = creatureMaterial; rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; rend.receiveShadows = false; }
        Destroy(part.GetComponent<Collider>());
        return part;
    }
    
    IEnumerator TransitionToNextScene()
    {
        float elapsed = 0f;
        while (elapsed < transitionDuration) { elapsed += Time.deltaTime; yield return null; }
        if (!string.IsNullOrEmpty(nextScene)) UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
    }
    
    void OnGUI()
    {
        if (blackOverlayAlpha > 0.001f) { Color c = GUI.color; GUI.color = new Color(0f, 0f, 0f, blackOverlayAlpha); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = c; }
        if (whiteFlashAlpha > 0.001f) { Color c = GUI.color; GUI.color = new Color(1f, 1f, 1f, whiteFlashAlpha); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = c; }
    }
    
    void OnDestroy()
    {
        if (creatureRoot != null) Destroy(creatureRoot);
        if (creatureMaterial != null) Destroy(creatureMaterial);
    }
}
