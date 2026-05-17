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
    public AudioClip loginChime;
    public List<AudioClip> uiLandClips = new List<AudioClip>();
    [SerializeField, Range(0f, 1f)] private float typewriterVolume = 0.14f;
    [SerializeField] private float typewriterMinInterval = 0.045f;

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
    private AudioSource typewriterSource;
    private string playerName;
    private Label unreadLabel;

    private void Start()
    {
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

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
        unreadLabel = root.Q<Label>("unread-message");

        // Ensure we are covering everything at the start
        if (backgroundDark != null) backgroundDark.style.opacity = 1f;

        // Hide all scene layers initially
        SetLayersAlpha(0);
        if (sunLight) sunLight.intensity = 0;
        if (desktopController) desktopController.enabled = false;

        // Find the "real" Desktop UI and hide it initially
        DesktopUIController realUIController = Object.FindAnyObjectByType<DesktopUIController>();
        GameObject realUIGO = realUIController != null ? realUIController.gameObject : null;
        if (realUIGO != null) realUIGO.SetActive(false);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.outputAudioMixerGroup = sfxGroup;
        
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.outputAudioMixerGroup = ambientGroup;
        ambientSource.loop = true;

        typewriterSource = gameObject.AddComponent<AudioSource>();
        typewriterSource.outputAudioMixerGroup = sfxGroup;
        typewriterSource.playOnAwake = false;
        typewriterSource.loop = false;
        typewriterSource.spatialBlend = 0f;
        typewriterSource.volume = typewriterVolume;

        playerName = PlayerPrefs.GetString("PlayerName", "User");
        popupTitle.text = "Welcome back, " + playerName;
        if (identityText != null) identityText.text = "Identity Accepted: " + playerName;
        if (welcomeText != null) welcomeText.text = "Restoring your desktop, " + playerName + ".";
        if (unreadLabel != null) unreadLabel.text = playerName + ", you have 7 missed calls from 'Unknown'.";

        StartCoroutine(CinematicSequence(realUIGO));
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

    private IEnumerator CinematicSequence(GameObject realUIGO)
    {
        yield return new WaitForSeconds(0.35f);
        if (identityText != null)
        {
            identityText.AddToClassList("intro-text--visible");
        }
        if (startupChime) sfxSource.PlayOneShot(startupChime, 0.6f); // Removed loud sound as requested
        yield return new WaitForSeconds(1.1f);

        if (identityText != null)
        {
            identityText.RemoveFromClassList("intro-text--visible");
        }

        if (welcomeText != null)
        {
            welcomeText.AddToClassList("intro-text--visible");
        }
        yield return new WaitForSeconds(1.0f);

        string[] restoreLines = {
            "Restoring profile shell for " + playerName + "...",
            "Syncing deleted notifications and pinned memories...",
            "AeroOS remembers your last session."
        };

        foreach (string line in restoreLines)
        {
            yield return StartCoroutine(TypewriteLine(line));
            yield return new WaitForSeconds(0.25f);
        }

        if (welcomeText != null)
        {
            welcomeText.RemoveFromClassList("intro-text--visible");
        }
        yield return new WaitForSeconds(0.5f);
        
        if (digitalAmbience) {
            ambientSource.clip = digitalAmbience;
            ambientSource.Play();
        }

        // PHASE 1 — WALLPAPER MATERIALIZATION
        if (backgroundDark != null) backgroundDark.AddToClassList("background-dark--hidden");
        yield return new WaitForSeconds(1.0f);
        
        // 1. Sky
        if (materializeClip) sfxSource.PlayOneShot(materializeClip, 0.3f);
        yield return StartCoroutine(FadeLayer(skyLayers, 1f, 1.0f));
        // 2. Sunlight
        if (sunLight) yield return StartCoroutine(FadeLight(sunLight, 0.75f, 1.0f));
        // 3. Clouds
        if (materializeClip) sfxSource.PlayOneShot(materializeClip, 0.2f);
        yield return StartCoroutine(FadeLayer(cloudLayers, 1f, 0.8f));
        // 4. City
        if (materializeClip) sfxSource.PlayOneShot(materializeClip, 0.3f);
        yield return StartCoroutine(FadeLayer(cityLayers, 1f, 1.0f));
        // 5. Nature
        if (materializeClip) sfxSource.PlayOneShot(materializeClip, 0.3f);
        yield return StartCoroutine(FadeLayer(natureLayers, 1f, 1.0f));
        // 6. Bubbles
        if (materializeClip) sfxSource.PlayOneShot(materializeClip, 0.2f);
        yield return StartCoroutine(FadeLayer(bubbleLayers, 1f, 0.8f));

        // PHASE 2 — SYSTEM GLITCH
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(TriggerGlitch());
        yield return new WaitForSeconds(0.6f);

        // PHASE 3 — UI MATERIALIZATION
        if (loginChime) sfxSource.PlayOneShot(loginChime, 0.8f);
        taskbar.AddToClassList("taskbar--visible");
        yield return new WaitForSeconds(0.5f);

        string[] iconNames = { "My Files", "System", "Logs", "Lab 7", "Network", "Recycle Bin" };
        Sprite[] iconSprites = { myFilesIcon, systemIcon, logsIcon, lab7Icon, networkIcon, recycleBinIcon };

        for (int i = 0; i < iconNames.Length; i++)
        {
            CreateIcon(iconNames[i], iconSprites[i]);
            if (uiLandClips.Count > 0) 
            {
                AudioClip clip = uiLandClips[UnityEngine.Random.Range(0, uiLandClips.Count)];
                sfxSource.PlayOneShot(clip, 0.4f);
            }
            else if (uiLandClip) sfxSource.PlayOneShot(uiLandClip, 0.4f);
            yield return new WaitForSeconds(0.2f);
        }

        // PHASE 4 — FIRST SYSTEM MESSAGE
        yield return new WaitForSeconds(1.0f);
        popupContainer.style.display = DisplayStyle.Flex;
        yield return new WaitForEndOfFrame();
        
        glassPopup.AddToClassList("glass-popup--visible");
        if (popupClip) sfxSource.PlayOneShot(popupClip, 0.7f);

        yield return new WaitForSeconds(3.0f);

        // PHASE 5 — HANDOVER
        if (realUIGO != null) realUIGO.SetActive(true);
        
        float fade = 1f;
        while (fade > 0) {
            fade -= Time.deltaTime * 2f;
            root.style.opacity = fade;
            yield return null;
        }

        if (desktopController) desktopController.enabled = true;
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        gameObject.SetActive(false); 
    }

        private IEnumerator FlashNarrativeHint(string text, float duration)
        {
        Label hint = new Label(text);
        hint.style.position = Position.Absolute;
        hint.style.top = Length.Percent(45);
        hint.style.width = Length.Percent(100);
        hint.style.unityTextAlign = TextAnchor.MiddleCenter;
        hint.style.color = new Color(1, 0, 0, 0.4f);
        hint.style.fontSize = 60;
        hint.style.unityFontStyleAndWeight = FontStyle.Bold;
        root.Add(hint);

        yield return new WaitForSeconds(duration);
        root.Remove(hint);
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
        float nextTypewriterAt = 0f;
        for (int i = 0; i < text.Length; i++)
        {
            currentText += text[i];
            line.text = currentText;
            if (!char.IsWhiteSpace(text[i]) && typewriterClip != null && typewriterSource != null && Time.unscaledTime >= nextTypewriterAt)
            {
                typewriterSource.Stop();
                typewriterSource.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
                typewriterSource.PlayOneShot(typewriterClip, typewriterVolume);
                nextTypewriterAt = Time.unscaledTime + typewriterMinInterval;
            }
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
