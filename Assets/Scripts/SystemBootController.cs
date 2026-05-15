using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using AeroOS.UI;

public class SystemBootController : MonoBehaviour
{
    [Header("UI")]
    public UIDocument uiDocument;
    public string nextScene = "AeroDesktopScene";

    [Header("Audio")]
    public AudioClip startupChime;
    public AudioClip glitchSound;
    public AudioClip crackSound;
    public AudioClip transitionSound;
    public AudioClip ambientHum;
    public AudioClip electricalAmbience;
    
    private AudioSource sfxSource;
    private AudioSource ambientSource;

    private VisualElement root;
    private VisualElement bgGlow;
    private VisualElement logo;
    private VisualElement loadingContainer;
    private VisualElement loadingBar;
    private VisualElement setupPanel;
    private VisualElement glitchOverlay;
    private TextField nameInput;
    private Button continueBtn;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement.Q<VisualElement>("root");
        bgGlow = root.Q<VisualElement>("bg-glow");
        logo = root.Q<VisualElement>("logo");
        loadingContainer = root.Q<VisualElement>("loading-container");
        loadingBar = root.Q<VisualElement>("loading-bar");
        setupPanel = root.Q<VisualElement>("setup-panel");
        glitchOverlay = root.Q<VisualElement>("glitch-overlay");
        nameInput = root.Q<TextField>("name-input");
        continueBtn = root.Q<Button>("continue-btn");

        if (continueBtn != null) continueBtn.clicked += OnContinueClicked;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.volume = 0.6f;
        
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = true;
        ambientSource.volume = 0.1f; // Very quiet ambient for Frutiger feel

        StartCoroutine(BootSequence());
    }

    private IEnumerator BootSequence()
    {
        // 1. Scene starts from black
        yield return new WaitForSeconds(0.4f);

        // 2. Soft blue glow appears
        if (bgGlow != null) bgGlow.AddToClassList("background-glow--visible");
        
        // Start ambient sounds (very subtle)
        if (ambientHum) {
            ambientSource.clip = ambientHum;
            ambientSource.Play();
        }
        
        yield return new WaitForSeconds(0.8f);

        // 3. AeroOS logo - Play startup chime
        if (logo != null) logo.AddToClassList("logo--visible");
        if (startupChime != null) sfxSource.PlayOneShot(startupChime, 0.7f);
        
        yield return new WaitForSeconds(1.0f);

        // 4. Loading animation
        if (loadingContainer != null) loadingContainer.AddToClassList("loading-container--visible");

        float elapsed = 0f;
        float duration = 4.0f;
        bool glitchTriggered = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            if (loadingBar != null) loadingBar.style.width = Length.Percent(progress * 100f);

            // 5. Glitch event at around 60%
            if (progress >= 0.6f && !glitchTriggered)
            {
                glitchTriggered = true;
                StartCoroutine(TriggerGlitch());
            }

            yield return null;
        }

        if (loadingBar != null) loadingBar.style.width = Length.Percent(100f);
        yield return new WaitForSeconds(1.0f);

        // 6. After loading completes: Setup panel
        if (logo != null) logo.RemoveFromClassList("logo--visible");
        if (loadingContainer != null) loadingContainer.RemoveFromClassList("loading-container--visible");
        
        yield return new WaitForSeconds(0.5f);
        
        if (setupPanel != null) setupPanel.AddToClassList("setup-panel--visible");
    }

    private IEnumerator TriggerGlitch()
    {
        if (glitchSound) sfxSource.PlayOneShot(glitchSound, 0.3f); // Lower volume
        if (crackSound) sfxSource.PlayOneShot(crackSound, 0.2f); // Lower volume

        // Logo distorts horizontally
        if (logo != null) logo.AddToClassList("glitch-distort");
        // Screen has RGB split and static
        if (glitchOverlay != null)
        {
            glitchOverlay.AddToClassList("glitch-rgb");
            glitchOverlay.AddToClassList("glitch-static");
            glitchOverlay.style.opacity = 0.25f; // More subtle glitch
        }

        yield return new WaitForSeconds(0.4f);

        if (logo != null) logo.RemoveFromClassList("glitch-distort");
        if (glitchOverlay != null)
        {
            glitchOverlay.RemoveFromClassList("glitch-rgb");
            glitchOverlay.RemoveFromClassList("glitch-static");
            glitchOverlay.style.opacity = 0f;
        }
    }

    private void OnContinueClicked()
    {
        string playerName = nameInput != null ? nameInput.value : "User";
        if (string.IsNullOrEmpty(playerName)) playerName = "User";
        
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();

        if (transitionSound) sfxSource.PlayOneShot(transitionSound);
        
        StartCoroutine(FadeOutAndLoad());
    }

    private IEnumerator FadeOutAndLoad()
    {
        if (setupPanel != null) setupPanel.RemoveFromClassList("setup-panel--visible");
        if (bgGlow != null) bgGlow.RemoveFromClassList("background-glow--visible");
        
        float fadeOut = 0;
        while (fadeOut < 1f) {
            fadeOut += Time.deltaTime * 0.7f;
            ambientSource.volume = 0.4f * (1f - fadeOut);
            yield return null;
        }
        
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(nextScene);
    }
}
