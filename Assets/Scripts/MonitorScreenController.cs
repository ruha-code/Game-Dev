using UnityEngine;

public class MonitorScreenController : MonoBehaviour
{
    public Renderer screenSurface;
    public Texture2D wallpaperTexture;
    public Texture2D creatureFaceTexture;
    public Light monitorLight;
    public Shader glitchScreenShader;
    
    [Header("Glitch Settings")]
    public float glitchIntensity = 0f;
    public float faceAlpha = 0f;
    public float scanlineStrength = 0.3f;
    [HideInInspector] public float brightness = 1f;
    public float contrast = 1f;
    public float chromaticOffset = 0f;
    public float screenTear = 0f;
    public float vignetteStrength = 0.3f;
    
    [Header("Internal")]
    public Material screenMaterial; // Use assigned material if available
    private Texture2D generatedFace;
    private float glitchTimer;
    private bool isGlitching;
    private float glitchDuration;
    private float nextGlitchTime;
    private bool ownsRuntimeMaterial;
    
    void Start()
    {
        ResolveReferences();
        if (screenSurface != null)
        {
            if (screenMaterial == null)
            {
                Shader shader = glitchScreenShader != null ? glitchScreenShader : Shader.Find("Monitor/GlitchScreen");
                if (shader != null)
                {
                    screenMaterial = new Material(shader);
                    ownsRuntimeMaterial = true;
                }
                else if (screenSurface.sharedMaterial != null)
                {
                    Shader fallbackShader = screenSurface.sharedMaterial.shader;
                    if (fallbackShader != null)
                    {
                        screenMaterial = new Material(screenSurface.sharedMaterial);
                        ownsRuntimeMaterial = true;
                    }
                }
            }

            if (screenMaterial != null)
            {
                screenSurface.material = screenMaterial;
            }
            else
            {
                Debug.LogWarning($"{nameof(MonitorScreenController)} on '{name}' could not create a runtime material. Screen glitch effects are disabled for this renderer.");
                return;
            }

            if (wallpaperTexture != null) screenMaterial.SetTexture("_MainTex", wallpaperTexture);
            if (creatureFaceTexture == null) { generatedFace = GenFace(); screenMaterial.SetTexture("_FaceTex", generatedFace); }
            else screenMaterial.SetTexture("_FaceTex", creatureFaceTexture);
            screenMaterial.SetFloat("_GlitchIntensity", 0f);
            screenMaterial.SetFloat("_FaceAlpha", 0f);
            screenMaterial.SetFloat("_ScanlineStrength", scanlineStrength);
            screenMaterial.SetFloat("_Brightness", brightness);
            screenMaterial.SetFloat("_Contrast", contrast);
            nextGlitchTime = Random.Range(2f, 5f);
        }
    }

    private void ResolveReferences()
    {
        if (screenSurface == null)
        {
            screenSurface = GetComponent<Renderer>();
            if (screenSurface == null)
            {
                screenSurface = GetComponentInChildren<Renderer>();
            }
        }

        if (monitorLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include);
            foreach (Light lightSource in lights)
            {
                if (lightSource == null)
                {
                    continue;
                }

                string candidateName = lightSource.name.ToLowerInvariant();
                if (candidateName.Contains("monitor"))
                {
                    monitorLight = lightSource;
                    break;
                }
            }
        }

        if (glitchScreenShader == null)
        {
            glitchScreenShader = Shader.Find("Monitor/GlitchScreen");
        }
    }

    void Update()
    {
        if (screenMaterial != null)
        {
            screenMaterial.SetFloat("_GlitchIntensity", glitchIntensity);
            screenMaterial.SetFloat("_FaceAlpha", faceAlpha);
            screenMaterial.SetFloat("_ChromaticOffset", chromaticOffset);
            screenMaterial.SetFloat("_ScreenTear", screenTear);
            screenMaterial.SetFloat("_Brightness", brightness);
            
            // Random glitch bursts
            glitchTimer += Time.deltaTime;
            if (glitchTimer >= nextGlitchTime && glitchIntensity < 0.3f)
            {
                glitchTimer = 0f;
                nextGlitchTime = Random.Range(1f, 4f);
                isGlitching = true;
                glitchDuration = Random.Range(0.1f, 0.3f);
                screenMaterial.SetFloat("_GlitchIntensity", Mathf.Max(glitchIntensity, 0.5f));
                screenMaterial.SetFloat("_ScreenTear", Mathf.Max(screenTear, 0.3f));
            }
            
            if (isGlitching)
            {
                glitchDuration -= Time.deltaTime;
                if (glitchDuration <= 0f)
                {
                    isGlitching = false;
                    if (glitchIntensity < 0.3f)
                    {
                        screenMaterial.SetFloat("_GlitchIntensity", glitchIntensity);
                        screenMaterial.SetFloat("_ScreenTear", screenTear);
                    }
                }
            }
        }
    }
    
    Texture2D GenFace()
    {
        int w = 256, h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w - 0.5f, ny = (float)y / h - 0.5f;
                Color c = Color.clear;
                float fd = Mathf.Sqrt(nx * nx * 2f + ny * ny * 3f);
                if (fd < 0.35f)
                {
                    float a = Mathf.Pow(1f - fd / 0.35f, 0.5f);
                    float n = Mathf.PerlinNoise(nx * 10f + 0.5f, ny * 10f + 0.5f) * 0.3f;
                    c = new Color(0.05f + n, 0.08f + n, 0.1f + n, a * 0.9f);
                    float le = Vector2.Distance(new Vector2(nx, ny), new Vector2(-0.12f, 0.05f));
                    float re = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.12f, 0.05f));
                    if (le < 0.06f || re < 0.06f)
                    {
                        float ea = Mathf.Pow(1f - Mathf.Min(le, re) / 0.06f, 0.3f);
                        c = Color.Lerp(c, new Color(0.3f, 0.8f, 1f, 1f), ea);
                    }
                    float md = Mathf.Abs(ny + 0.15f);
                    if (md < 0.03f && Mathf.Abs(nx) < 0.15f) c = new Color(0.02f, 0.02f, 0.03f, a);
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }
    
    void OnDestroy()
    {
        if (generatedFace != null) Destroy(generatedFace);
        if (ownsRuntimeMaterial && screenMaterial != null) Destroy(screenMaterial);
    }
}
