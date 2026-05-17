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
    public AudioClip typewriterClip;
    public AudioClip glitchSound;
    public AudioClip crackSound;
    public AudioClip transitionSound;
    public AudioClip ambientHum;
    public AudioClip electricalAmbience;
    [SerializeField, Range(0f, 1f)] private float typewriterVolume = 0.18f;
    [SerializeField] private float typewriterMinInterval = 0.045f;
    
    private AudioSource sfxSource;
    private AudioSource ambientSource;
    private AudioSource typewriterSource;

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

        // Hide cursor during initial boot sequence
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

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
        if (nameInput != null)
        {
            nameInput.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    OnContinueClicked();
                }
            });
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.volume = 0.6f;
        
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = true;
        ambientSource.volume = 0.1f; // Very quiet ambient for Frutiger feel

        typewriterSource = gameObject.AddComponent<AudioSource>();
        typewriterSource.playOnAwake = false;
        typewriterSource.loop = false;
        typewriterSource.spatialBlend = 0f;
        typewriterSource.volume = typewriterVolume;

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
        
        if (setupPanel != null) {
            setupPanel.AddToClassList("setup-panel--visible");
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
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
        Debug.Log("Continue button clicked or Enter pressed.");
        string playerName = nameInput != null ? nameInput.value : "User";
        if (string.IsNullOrEmpty(playerName)) playerName = "User";
        
        Debug.Log($"Saving PlayerName: {playerName}");
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();

        if (transitionSound) sfxSource.PlayOneShot(transitionSound);
        
        StartCoroutine(PlayNarrativeAndLoad(playerName));
    }

    private IEnumerator PlayNarrativeAndLoad(string playerName)
    {
        // 1. Hide Setup Panel
        if (setupPanel != null) setupPanel.RemoveFromClassList("setup-panel--visible");
        yield return new WaitForSeconds(0.8f);

        // 2. Play Narrative Sequence
        string[] narrativeLines = {
            "Connecting to Aether Dynamics Mainframe...",
            "Recovering Session Data for Lab 7...",
            "Warning: 4 Engineer Accounts remain 'FLAGGED'...",
            "Synchronizing Memory Cores...",
            "Reconstructing Personal Environment...",
            "User '" + playerName + "' detected as Last Active Employee.",
            "Finalizing Reality Simulation..."
        };

        // We use the logo or a dedicated label for this
        Label narrativeLabel = root.Q<Label>("logo");
        if (narrativeLabel != null)
        {
            narrativeLabel.style.fontSize = 24;
            narrativeLabel.style.color = new Color(0, 1, 1, 0.8f);
            narrativeLabel.AddToClassList("logo--visible");

            foreach (string line in narrativeLines)
            {
                narrativeLabel.text = "";
                float nextTypewriterAt = 0f;
                foreach (char c in line)
                {
                    narrativeLabel.text += c;
                    if (typewriterClip != null && typewriterSource != null && !char.IsWhiteSpace(c) && Time.unscaledTime >= nextTypewriterAt)
                    {
                        typewriterSource.Stop();
                        typewriterSource.pitch = Random.Range(0.97f, 1.03f);
                        typewriterSource.PlayOneShot(typewriterClip, typewriterVolume);
                        nextTypewriterAt = Time.unscaledTime + typewriterMinInterval;
                    }
                    yield return new WaitForSeconds(0.04f);
                }
                yield return new WaitForSeconds(1.2f);
            }

            narrativeLabel.text = "System Status: READY";
            yield return new WaitForSeconds(1.0f);
            narrativeLabel.RemoveFromClassList("logo--visible");
        }

        // 3. Final Transition
        yield return StartCoroutine(FadeOutAndLoad());
    }

    private IEnumerator FadeOutAndLoad()
    {
        Debug.Log("Starting FadeOutAndLoad sequence.");
        if (bgGlow != null) bgGlow.RemoveFromClassList("background-glow--visible");
        
        VisualElement fadeOverlay = root.Q<VisualElement>("fade-overlay");

        float fade = 0;
        while (fade < 1f) {
            fade += Time.deltaTime * 0.8f;
            if (fadeOverlay != null) fadeOverlay.style.opacity = fade;
            ambientSource.volume = 0.1f * (1f - fade);
            yield return null;
        }
        
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(nextScene);
    }
}
