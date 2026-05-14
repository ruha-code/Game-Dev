using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using AeroOS.UI;

public class DesktopArrivalController : MonoBehaviour
{
    [Header("UI")]
    public UIDocument uiDocument;
    
    [Header("Audio")]
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup ambientGroup;
    public AudioMixerGroup glitchGroup;
    public AudioClip startupChime;
public AudioClip typewriterClip;
    public AudioClip glitchClip;
    public AudioClip uiLandClip;
    public AudioClip popupClip;
    public AudioClip digitalAmbience;
    public AudioClip materializeClip;
    public List<AudioClip> uiLandClips = new List<AudioClip>();

    [Header("Icons")]
public Sprite myFilesIcon;
    public Sprite systemIcon;
    public Sprite logsIcon;
    public Sprite lab7Icon;
    public Sprite networkIcon;
    public Sprite recycleBinIcon;

    [Header("Scene References")]
    public DesktopController desktopController;
    public List<SpriteRenderer> skyLayers = new List<SpriteRenderer>();
    public List<SpriteRenderer> cityLayers = new List<SpriteRenderer>();
    public List<SpriteRenderer> natureLayers = new List<SpriteRenderer>();
    public List<SpriteRenderer> cloudLayers = new List<SpriteRenderer>();
    public List<SpriteRenderer> bubbleLayers = new List<SpriteRenderer>();
    public Light sunLight;

    private VisualElement root;
    private VisualElement backgroundDark;
    private Label identityText;
    private Label welcomeText;
    private VisualElement typewriterContainer;
    private VisualElement glitchOverlay;
    private VisualElement silhouette;
    private VisualElement taskbar;
    private VisualElement iconGrid;
    private VisualElement popupContainer;
    private VisualElement glassPopup;
    private Label popupTitle;
    private AeroAtmosphere atmosphere;

    private AudioSource sfxSource;
    private AudioSource ambientSource;
    private string playerName;

    private void Start()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement.Q<VisualElement>("root");
        backgroundDark = root.Q<VisualElement>("background-dark");

        identityText = root.Q<Label>("identity-text");
welcomeText = root.Q<Label>("welcome-text");
        typewriterContainer = root.Q<VisualElement>("typewriter-container");
        glitchOverlay = root.Q<VisualElement>("glitch-overlay");
        silhouette = root.Q<VisualElement>("silhouette");
        taskbar = root.Q<VisualElement>("taskbar");
        iconGrid = root.Q<VisualElement>("icon-grid");
        popupContainer = root.Q<VisualElement>("popup-container");
        glassPopup = root.Q<VisualElement>("glass-popup");
        popupTitle = root.Q<Label>("popup-title");
        atmosphere = root.Q<AeroAtmosphere>("atmosphere");

        // Hide all scene layers initially
        SetLayersAlpha(0);
        if (sunLight) sunLight.intensity = 0;
        if (desktopController) desktopController.enabled = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.outputAudioMixerGroup = sfxGroup;
        
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.outputAudioMixerGroup = ambientGroup;
        ambientSource.loop = true;

        playerName = PlayerPrefs.GetString("PlayerName", "User");
        popupTitle.text = "Welcome, " + playerName;

        StartCoroutine(CinematicSequence());
    }

    private void SetLayersAlpha(float alpha)
    {
        foreach (var sr in skyLayers) SetAlpha(sr, alpha);
        foreach (var sr in cityLayers) SetAlpha(sr, alpha);
        foreach (var sr in natureLayers) SetAlpha(sr, alpha);
        foreach (var sr in cloudLayers) SetAlpha(sr, alpha);
        foreach (var sr in bubbleLayers) SetAlpha(sr, alpha);
    }

    private void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr) {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    private IEnumerator CinematicSequence()
    {
        // PHASE 1 — IDENTITY ACCEPTED
        yield return new WaitForSeconds(0.5f);
        identityText.AddToClassList("intro-text--visible");
        if (startupChime) sfxSource.PlayOneShot(startupChime, 0.6f);
        
        yield return new WaitForSeconds(1.5f);
        identityText.RemoveFromClassList("intro-text--visible");
        yield return new WaitForSeconds(0.5f);
        
        welcomeText.text = "Welcome, " + playerName;
        welcomeText.AddToClassList("intro-text--visible");
        
        yield return new WaitForSeconds(1.5f);
        welcomeText.RemoveFromClassList("intro-text--visible");
        yield return new WaitForSeconds(0.5f);

        // PHASE 2 — WORLD GENERATION
        if (digitalAmbience) {
            ambientSource.clip = digitalAmbience;
            ambientSource.Play();
        }

        string[] lines = {
            "Loading Personal Environment...",
            "Synchronizing Memory...",
            "Building Interface...",
            "Preparing Desktop...",
            "Verifying Reality..."
        };

        foreach (var line in lines)
        {
            yield return StartCoroutine(TypewriteLine(line));
            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(0.5f);

        // PHASE 3 — WALLPAPER CREATION
        typewriterContainer.style.display = DisplayStyle.None;
        if (backgroundDark != null) backgroundDark.AddToClassList("background-dark--hidden");
        
        // 1. Sky
        if (materializeClip) sfxSource.PlayOneShot(materializeClip, 0.3f);
        yield return StartCoroutine(FadeLayer(skyLayers, 1f, 1.0f));
        // 2. Sunlight
        if (sunLight) yield return StartCoroutine(FadeLight(sunLight, 1.2f, 1.0f));
        // 3. Clouds
        if (materializeClip) sfxSource.PlayOneShot(materializeClip, 0.2f);
        yield return StartCoroutine(FadeLayer(cloudLayers, 1f, 0.8f));
        // 4. City
        if (materializeClip) sfxSource.PlayOneShot(materializeClip, 0.3f);
        yield return StartCoroutine(FadeLayer(cityLayers, 1f, 1.0f));
        // 5. Trees & 6. Grass
        if (materializeClip) sfxSource.PlayOneShot(materializeClip, 0.3f);
        yield return StartCoroutine(FadeLayer(natureLayers, 1f, 1.0f));
        // 7. Water (If separate, but usually in nature/city)
        // 8. Floating bubbles
        if (materializeClip) sfxSource.PlayOneShot(materializeClip, 0.2f);
        yield return StartCoroutine(FadeLayer(bubbleLayers, 1f, 0.8f));

        // PHASE 4 — SYSTEM GLITCH
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(TriggerGlitch());
        yield return new WaitForSeconds(0.6f);

        // PHASE 5 — UI MATERIALIZATION
        taskbar.AddToClassList("taskbar--visible");
        yield return new WaitForSeconds(0.5f);

        string[] iconNames = { "My Files", "System", "Logs", "Lab 7", "Network", "Recycle Bin" };
        Sprite[] iconSprites = { myFilesIcon, systemIcon, logsIcon, lab7Icon, networkIcon, recycleBinIcon };

        for (int i = 0; i < iconNames.Length; i++)
        {
            CreateIcon(iconNames[i], iconSprites[i]);
            if (uiLandClips.Count > 0) 
            {
                AudioClip clip = uiLandClips[Random.Range(0, uiLandClips.Count)];
                sfxSource.PlayOneShot(clip, 0.4f);
            }
            else if (uiLandClip) sfxSource.PlayOneShot(uiLandClip, 0.4f);
            yield return new WaitForSeconds(0.2f);
        }

        // PHASE 6 — FIRST SYSTEM MESSAGE
        yield return new WaitForSeconds(1.0f);
        popupContainer.style.display = DisplayStyle.Flex;
        yield return new WaitForEndOfFrame();
        glassPopup.AddToClassList("glass-popup--visible");
        if (popupClip) sfxSource.PlayOneShot(popupClip, 0.7f);

        yield return new WaitForSeconds(3.0f);

        // PHASE 7 — HAND CONTROL
        if (desktopController) desktopController.enabled = true;
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        root.pickingMode = PickingMode.Ignore;
    }

    private IEnumerator FadeLayer(List<SpriteRenderer> srs, float targetAlpha, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            // Add a "shimmer/materialize" flicker
            float alpha = progress;
            if (progress < 0.9f && Random.value > 0.8f) alpha *= 0.5f; 
            
            foreach (var sr in srs) SetAlpha(sr, alpha * targetAlpha);
            yield return null;
        }
        foreach (var sr in srs) SetAlpha(sr, targetAlpha);
    }

    private IEnumerator FadeLight(Light light, float targetIntensity, float duration)
    {
        float start = light.intensity;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            light.intensity = Mathf.Lerp(start, targetIntensity, elapsed / duration);
            yield return null;
        }
    }

    private IEnumerator TypewriteLine(string text)
    {
        Label line = new Label();
        line.AddToClassList("typewriter-line");
        typewriterContainer.Add(line);

        string currentText = "";
        for (int i = 0; i < text.Length; i++)
        {
            currentText += text[i];
            line.text = currentText;
            if (typewriterClip) sfxSource.PlayOneShot(typewriterClip, 0.1f);
            yield return new WaitForSeconds(0.03f);
        }
    }

    private IEnumerator TriggerGlitch()
    {
        if (glitchClip) sfxSource.PlayOneShot(glitchClip, 0.5f);
        glitchOverlay.style.opacity = 0.5f;
        glitchOverlay.AddToClassList("glitch-rgb");
        silhouette.style.opacity = 0.15f;
        
        yield return new WaitForSeconds(0.05f);
        glitchOverlay.style.opacity = 0.8f;
        yield return new WaitForSeconds(0.05f);
        glitchOverlay.style.opacity = 0.3f;
        silhouette.style.opacity = 0.05f;
        yield return new WaitForSeconds(0.4f);
        
        glitchOverlay.style.opacity = 0f;
        silhouette.style.opacity = 0f;
        glitchOverlay.RemoveFromClassList("glitch-rgb");
    }

    private void CreateIcon(string name, Sprite sprite)
    {
        VisualElement icon = new VisualElement();
        icon.AddToClassList("desktop-icon");
        
        VisualElement img = new VisualElement();
        img.AddToClassList("icon-image");
        img.style.backgroundImage = new StyleBackground(sprite);
        icon.Add(img);

        Label lbl = new Label(name);
        lbl.AddToClassList("icon-label");
        icon.Add(lbl);

        iconGrid.Add(icon);
        icon.schedule.Execute(() => icon.AddToClassList("desktop-icon--visible")).StartingIn(10);
    }
}
