using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SystemBootController : MonoBehaviour
{
    [Header("Timing")]
    public float whiteFlashDuration = 0.5f;
    public float fadeToDigitalDuration = 1.5f;
    public float stabilizationDuration = 2f;
    public float loadingDuration = 4f;
    public float logoDuration = 2f;
    public float glitchDuration = 0.3f;
    public float transitionDuration = 1.5f;
    
    [Header("UI")]
    public Canvas mainCanvas;
    public Image whiteFlashOverlay;
    public Image backgroundOverlay;
    public Image loadingBar;
    public Image loadingBarFill;
    public Text logoText;
    public Text statusText;
    
    [Header("Effects")]
    public ParticleSystem floatingParticles;
    public Light ambientLight;
    public UnityEngine.UI.RawImage wallpaperImage;
    public Texture2D wallpaperTexture;
    
    [Header("Settings")]
    public string nextScene = "AeroDesktopScene";
    
    private float timeline;
    private float t1, t2, t3, t4, t5, t6, t7;
    private float cameraFloatOffset;
    private float glitchTimer;
    private bool glitchActive;
    
    void Start()
    {
        LoadWallpaper();
        SetupScene();
        CalculateTimings();
        StartCoroutine(RunBootSequence());
    }
    
    void LoadWallpaper()
    {
        wallpaperTexture = Resources.Load<Texture2D>("Image/Wallpaper");
    }
    
    void SetupScene()
    {
        if (mainCanvas == null)
        {
            GameObject canvasObj = new GameObject("BootCanvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        if (whiteFlashOverlay == null)
            whiteFlashOverlay = CreateOverlay("WhiteFlash", Color.white);
        
        if (backgroundOverlay == null)
            backgroundOverlay = CreateOverlay("Background", new Color(0.02f, 0.05f, 0.15f, 0f));
        
        if (loadingBar == null)
            loadingBar = CreateLoadingBar();
        
        if (logoText == null)
            logoText = CreateLogoText();
        
        if (statusText == null)
            statusText = CreateStatusText();
        
        if (floatingParticles == null)
            CreateParticles();
        
        if (ambientLight == null)
        {
            GameObject lightObj = new GameObject("AmbientLight");
            ambientLight = lightObj.AddComponent<Light>();
            ambientLight.type = LightType.Directional;
            ambientLight.color = new Color(0.3f, 0.6f, 1f);
            ambientLight.intensity = 0.5f;
        }
        
        CreateWallpaperImage();
        
        Camera.main.backgroundColor = Color.black;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
    }
    
    void CreateWallpaperImage()
    {
        GameObject imgObj = new GameObject("WallpaperImage");
        imgObj.transform.SetParent(mainCanvas.transform);
        wallpaperImage = imgObj.AddComponent<UnityEngine.UI.RawImage>();
        wallpaperImage.color = new Color(1f, 1f, 1f, 0f);
        
        if (wallpaperTexture != null)
        {
            wallpaperImage.texture = wallpaperTexture;
        }
        
        RectTransform rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }
    
    Image CreateOverlay(string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(mainCanvas.transform);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return img;
    }
    
    Image CreateLoadingBar()
    {
        GameObject container = new GameObject("LoadingBarContainer");
        container.transform.SetParent(mainCanvas.transform);
        Image containerImg = container.AddComponent<Image>();
        containerImg.color = new Color(0.1f, 0.2f, 0.4f, 0f);
        RectTransform containerRt = container.GetComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.3f, 0.45f);
        containerRt.anchorMax = new Vector2(0.7f, 0.5f);
        containerRt.sizeDelta = Vector2.zero;
        
        GameObject fillObj = new GameObject("LoadingBarFill");
        fillObj.transform.SetParent(container.transform);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.7f, 1f, 0f);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.sizeDelta = Vector2.zero;
        
        return containerImg;
    }
    
    Text CreateLogoText()
    {
        GameObject obj = new GameObject("LogoText");
        obj.transform.SetParent(mainCanvas.transform);
        Text txt = obj.AddComponent<Text>();
        txt.text = "AeroOS";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 72;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(0.4f, 0.8f, 1f, 0f);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.3f, 0.55f);
        rt.anchorMax = new Vector2(0.7f, 0.65f);
        rt.sizeDelta = Vector2.zero;
        return txt;
    }
    
    Text CreateStatusText()
    {
        GameObject obj = new GameObject("StatusText");
        obj.transform.SetParent(mainCanvas.transform);
        Text txt = obj.AddComponent<Text>();
        txt.text = "Initializing...";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(0.5f, 0.7f, 0.9f, 0f);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.3f, 0.4f);
        rt.anchorMax = new Vector2(0.7f, 0.45f);
        rt.sizeDelta = Vector2.zero;
        return txt;
    }
    
    void CreateParticles()
    {
        GameObject particleObj = new GameObject("FloatingParticles");
        floatingParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = floatingParticles.main;
        main.startColor = new Color(0.3f, 0.7f, 1f, 0.6f);
        main.startSize = 0.05f;
        main.startLifetime = 8f;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        
        var emission = floatingParticles.emission;
        emission.rateOverTime = 5f;
        
        var shape = floatingParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(10f, 10f, 10f);
        
        var renderer = floatingParticles.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.SetColor("_Color", new Color(0.3f, 0.7f, 1f, 0.5f));
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", new Color(0.2f, 0.5f, 0.8f, 1f));
    }
    
    void CalculateTimings()
    {
        t1 = whiteFlashDuration;
        t2 = t1 + fadeToDigitalDuration;
        t3 = t2 + stabilizationDuration;
        t4 = t3 + loadingDuration;
        t5 = t4 + logoDuration;
        t6 = t5 + glitchDuration;
        t7 = t6 + transitionDuration;
    }
    
    IEnumerator RunBootSequence()
    {
        timeline = 0f;
        while (timeline < t7)
        {
            float dt = Time.deltaTime;
            timeline += dt;
            
            UpdateWhiteFlash(timeline);
            UpdateBackground(timeline);
            UpdateCamera(timeline, dt);
            UpdateParticles(timeline);
            UpdateLoading(timeline);
            UpdateLogo(timeline);
            UpdateGlitch(timeline, dt);
            
            if (timeline >= t7)
            {
                yield return StartCoroutine(TransitionToDesktop());
                break;
            }
            
            yield return null;
        }
    }
    
    void UpdateWhiteFlash(float t)
    {
        if (t < t1)
        {
            whiteFlashOverlay.color = Color.white;
        }
        else if (t < t1 + 0.3f)
        {
            float p = (t - t1) / 0.3f;
            whiteFlashOverlay.color = new Color(1f, 1f, 1f, 1f - p);
        }
        else
        {
            whiteFlashOverlay.color = new Color(1f, 1f, 1f, 0f);
        }
    }
    
    void UpdateBackground(float t)
    {
        if (wallpaperImage == null) return;
        
        if (t < t1)
        {
            wallpaperImage.color = new Color(1f, 1f, 1f, 0f);
        }
        else if (t < t2)
        {
            float p = (t - t1) / fadeToDigitalDuration;
            float easedP = p * p * (3f - 2f * p);
            wallpaperImage.color = new Color(1f, 1f, 1f, easedP);
        }
        else
        {
            wallpaperImage.color = new Color(1f, 1f, 1f, 1f);
        }
    }
    
    void UpdateCamera(float t, float dt)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        if (t < t2)
        {
            float shakeAmount = Mathf.Lerp(0.1f, 0.01f, (t - t1) / fadeToDigitalDuration);
            cam.transform.position += new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0f
            );
        }
        else if (t < t3)
        {
            float p = (t - t2) / stabilizationDuration;
            cameraFloatOffset = Mathf.Sin(t * 0.5f) * 0.02f * (1f - p);
            cam.transform.position = new Vector3(0f, cameraFloatOffset, -10f);
        }
        else
        {
            cameraFloatOffset = Mathf.Sin(t * 0.3f) * 0.01f;
            cam.transform.position = new Vector3(0f, cameraFloatOffset, -10f);
        }
    }
    
    void UpdateParticles(float t)
    {
        if (floatingParticles == null) return;
        
        if (t < t1)
        {
            floatingParticles.Stop();
        }
        else if (t < t2)
        {
            float p = (t - t1) / fadeToDigitalDuration;
            var emission = floatingParticles.emission;
            emission.rateOverTime = p * 5f;
            if (!floatingParticles.isPlaying && p > 0.1f)
                floatingParticles.Play();
        }
        else
        {
            var main = floatingParticles.main;
            main.startColor = new Color(0.3f, 0.7f, 1f, 0.6f);
        }
    }
    
    void UpdateLoading(float t)
    {
        if (loadingBar == null || loadingBarFill == null) return;
        
        RectTransform fillRt = loadingBarFill.GetComponent<RectTransform>();
        
        if (t < t3)
        {
            loadingBar.color = new Color(0.1f, 0.2f, 0.4f, 0f);
            loadingBarFill.color = new Color(0.2f, 0.7f, 1f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
        }
        else if (t < t4)
        {
            float p = (t - t3) / loadingDuration;
            loadingBar.color = new Color(0.1f, 0.2f, 0.4f, 0.8f);
            loadingBarFill.color = new Color(0.2f, 0.7f, 1f, 0.9f);
            fillRt.anchorMax = new Vector2(p, 1f);
            
            if (statusText != null)
            {
                statusText.color = new Color(0.5f, 0.7f, 0.9f, 0.8f);
                if (p < 0.3f) statusText.text = "Initializing...";
                else if (p < 0.6f) statusText.text = "Loading modules...";
                else if (p < 0.9f) statusText.text = "Preparing environment...";
                else statusText.text = "Almost ready...";
            }
        }
        else
        {
            loadingBar.color = new Color(0.1f, 0.2f, 0.4f, 0.8f);
            loadingBarFill.color = new Color(0.2f, 0.7f, 1f, 0.9f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            if (statusText != null)
            {
                statusText.text = "Complete";
                statusText.color = new Color(0.4f, 0.9f, 0.6f, 0.8f);
            }
        }
    }
    
    void UpdateLogo(float t)
    {
        if (logoText == null) return;
        
        if (t < t3)
        {
            logoText.color = new Color(0.4f, 0.8f, 1f, 0f);
        }
        else if (t < t4)
        {
            float p = (t - t3) / loadingDuration;
            float alpha = p > 0.5f ? (p - 0.5f) / 0.5f : 0f;
            logoText.color = new Color(0.4f, 0.8f, 1f, alpha);
        }
        else if (t < t5)
        {
            float pulse = Mathf.Sin(t * 2f) * 0.1f + 0.9f;
            logoText.color = new Color(0.4f * pulse, 0.8f * pulse, 1f, 1f);
        }
        else
        {
            logoText.color = new Color(0.4f, 0.8f, 1f, 1f);
        }
    }
    
    void UpdateGlitch(float t, float dt)
    {
        if (t < t5) return;
        
        glitchTimer += dt;
        
        if (glitchTimer > 0.1f && Random.value < 0.3f)
        {
            glitchActive = true;
            glitchTimer = 0f;
            
            if (wallpaperImage != null)
            {
                wallpaperImage.color = new Color(
                    1f + Random.value * 0.1f,
                    1f + Random.value * 0.1f,
                    1f + Random.value * 0.1f,
                    1f
                );
            }
            
            if (logoText != null)
            {
                logoText.color = new Color(
                    0.4f + Random.value * 0.2f,
                    0.8f,
                    1f,
                    0.8f + Random.value * 0.2f
                );
            }
            
            Invoke("ResetGlitch", 0.05f);
        }
    }
    
    void ResetGlitch()
    {
        glitchActive = false;
        if (wallpaperImage != null)
            wallpaperImage.color = new Color(1f, 1f, 1f, 1f);
        if (logoText != null)
            logoText.color = new Color(0.4f, 0.8f, 1f, 1f);
    }
    
    IEnumerator TransitionToDesktop()
    {
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / transitionDuration;
            
            if (wallpaperImage != null)
                wallpaperImage.color = new Color(1f, 1f, 1f, 1f - p);
            
            if (logoText != null)
                logoText.color = new Color(0.4f, 0.8f, 1f, 1f - p);
            
            if (loadingBar != null)
                loadingBar.color = new Color(0.1f, 0.2f, 0.4f, 0.8f * (1f - p));
            
            if (loadingBarFill != null)
                loadingBarFill.color = new Color(0.2f, 0.7f, 1f, 0.9f * (1f - p));
            
            yield return null;
        }
        
        if (!string.IsNullOrEmpty(nextScene))
            SceneManager.LoadScene(nextScene);
    }
}
