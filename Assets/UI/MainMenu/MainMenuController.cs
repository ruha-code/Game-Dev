using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using AeroOS.UI;
using System.Collections.Generic;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    private const string MasterVolumePrefKey = "Settings.MasterVolume";
    private const string FullscreenPrefKey = "Settings.Fullscreen";

    private VisualElement root;
    private VisualElement container;
    private VisualElement title;
    private VisualElement subtitle;
    private VisualElement fadeOverlay;
    private AeroActiveBackground activeBackground;
    private AeroAtmosphere atmosphere;
    private List<VisualElement> menuItems = new List<VisualElement>();

    // Anomaly elements
    private VisualElement shadowElement;
    private VisualElement ghostElement;
    private VisualElement popupElement;
    private Label popupLabel;
    private VisualElement loadElement;
    private VisualElement loadFill;
    private Label clockLabel;
    private Label masterVolumeValueLabel;
    private Label fullscreenValueLabel;
    private VisualElement eyeLeft;
    private VisualElement eyeRight;
    private VisualElement reflectionElement;

    [Header("Audio")]
    public AudioClip ambientMusic;
    public AudioClip hoverTick;
    public AudioClip clickGlass;
    public AudioClip anomalyHum;
    public AudioClip anomalyWhisper;
    private AudioSource audioSource;
    private AudioSource sfxSource;
    private AudioSource anomalySource;

    [Header("Anomaly Assets")]
    public Texture2D ghostTexture;
    public Texture2D shadowTexture;
    public Texture2D bedroomTexture;
    public Texture2D normalBackground;

    private float inactivityTime;
    private float anomalyTimer;
    private bool isTransitioning;
    private int lastAnomalyIndex = -1;
    private bool anomaliesEnabled = false;
    private bool suppressSettingsCallbacks;

    private struct AnomalyWeight
    {
        public int index;
        public int weight;
    }
    private List<AnomalyWeight> anomalyWeights = new List<AnomalyWeight>();

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        container = root.Q<VisualElement>("root");
        title = root.Q<VisualElement>("title");
        subtitle = root.Q<VisualElement>("subtitle");
        fadeOverlay = root.Q<VisualElement>("fade-overlay");
        activeBackground = root.Q<AeroActiveBackground>();
        atmosphere = root.Q<AeroAtmosphere>();

        // Find anomaly elements
        shadowElement = root.Q<VisualElement>("player-shadow");
        ghostElement = root.Q<VisualElement>("digital-ghost");
        popupElement = root.Q<VisualElement>("system-popup");
        popupLabel = root.Q<Label>("popup-text");
        loadElement = root.Q<VisualElement>("fake-load");
        loadFill = root.Q<VisualElement>("load-bar-fill");
        clockLabel = root.Q<Label>("clock-text");
        masterVolumeValueLabel = root.Q<Label>("master-vol-value");
        fullscreenValueLabel = root.Q<Label>("fullscreen-value");
        eyeLeft = root.Q<VisualElement>("eye-left");
        eyeRight = root.Q<VisualElement>("eye-right");
        reflectionElement = root.Q<VisualElement>("glass-reflection");

        if (ghostTexture != null) ghostElement.style.backgroundImage = ghostTexture;
        if (shadowTexture != null) shadowElement.style.backgroundImage = shadowTexture;

        SetupAudio();
        SetupMenu();
        SetupOverlays();
        SetupAnomalyWeights();
        CheckSave();
        
        // Ensure cursor is visible in main menu
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        StopAllCoroutines(); 
        StartCoroutine(TitleGlowBreathing());
        StartCoroutine(ClockUpdate());
        
        anomalyTimer = 4f; 
        CancelInvoke("EnableAnomalies");
        Invoke("EnableAnomalies", 4f); 
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        CancelInvoke();
        UnregisterMenuCallbacks();
    }

    private void UnregisterMenuCallbacks()
    {
        var menuBox = root?.Q<VisualElement>("menu");
        if (menuBox != null)
        {
            foreach (var item in menuBox.Children())
            {
                item.UnregisterCallback<ClickEvent>(OnMenuItemClickedEvent);
                item.UnregisterCallback<MouseEnterEvent>(OnHoverEvent);
            }
        }
    }

    private void OnMenuItemClickedEvent(ClickEvent evt) => OnMenuItemClicked(evt.currentTarget as VisualElement);
    private void OnHoverEvent(MouseEnterEvent evt) => OnHover(evt.currentTarget as VisualElement);

    private void SetupMenu()
    {
        menuItems.Clear();
        var menuBox = root.Q<VisualElement>("menu");
        if (menuBox != null)
        {
            foreach (var item in menuBox.Children())
            {
                if (item.ClassListContains("menu-item"))
                {
                    menuItems.Add(item);
                    item.RegisterCallback<ClickEvent>(OnMenuItemClickedEvent);
                    item.RegisterCallback<MouseEnterEvent>(OnHoverEvent);
                }
            }
        }
    }

    private void SetupAnomalyWeights()
    {
        anomalyWeights.Clear();
        // Rebalanced weights: prioritize visible and audible distortions
        int[] weights = { 10, 10, 10, 15, 12, 12, 8, 8, 10, 15, 10, 10, 8, 15, 25, 12, 25, 12, 12, 25 };
        for (int i = 0; i < weights.Length; i++)
        {
            anomalyWeights.Add(new AnomalyWeight { index = i, weight = weights[i] });
        }
    }

    private void SetupAudio()
    {
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = ambientMusic; audioSource.loop = true; audioSource.volume = 0.35f;
        if (!audioSource.isPlaying) audioSource.Play();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (anomalySource == null) anomalySource = gameObject.AddComponent<AudioSource>();
        anomalySource.volume = 0.9f;
    }

    private void EnableAnomalies()
    {
        anomaliesEnabled = true;
        // Force the very first anomaly to be a highly visible one (System Message)
        TriggerAnomalySpecific(14); 
    }

    private void TriggerAnomalySpecific(int index)
    {
        if (isTransitioning) return;
        lastAnomalyIndex = index;
        Debug.Log($"[AeroOS] Forced Initial Anomaly: Index {index}");
        StartCoroutine(GetAnomalyCoroutine(index));
        anomalyTimer = Random.Range(4f, 10f);
    }

    private void Update()
    {
        if (isTransitioning) return;

        inactivityTime += Time.unscaledDeltaTime;
        
        if (anomaliesEnabled)
        {
            anomalyTimer -= Time.unscaledDeltaTime;
            if (anomalyTimer <= 0)
            {
                TriggerRandomAnomaly();
                anomalyTimer = Random.Range(4f, 10f); // Frequency: every 4-10 seconds
            }
        }

        if (Keyboard.current != null && (Keyboard.current.anyKey.wasPressedThisFrame || Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f))
            inactivityTime = 0;
    }

    private void TriggerRandomAnomaly()
    {
        if (anomalyWeights.Count == 0) return;
        
        int totalWeight = 0;
        foreach (var aw in anomalyWeights) totalWeight += aw.weight;

        int rnd = Random.Range(0, totalWeight);
        int current = 0;
        int selectedIndex = -1;

        foreach (var aw in anomalyWeights)
        {
            current += aw.weight;
            if (rnd < current)
            {
                selectedIndex = aw.index;
                break;
            }
        }

        if (selectedIndex == lastAnomalyIndex && anomalyWeights.Count > 1)
        {
            // Avoid recursion, just pick next one
            selectedIndex = (selectedIndex + 1) % anomalyWeights.Count;
        }

        lastAnomalyIndex = selectedIndex;
        Debug.Log($"[AeroOS] System Anomaly Triggered: Index {selectedIndex}");
        StartCoroutine(GetAnomalyCoroutine(selectedIndex));
    }

    private IEnumerator GetAnomalyCoroutine(int index)
    {
        switch (index)
        {
            case 0: yield return Anomaly_ContinueExists(); break;
            case 1: yield return Anomaly_QuitChanges(); break;
            case 2: yield return Anomaly_NewGameChanges(); break;
            case 3: yield return Anomaly_CursorPossession(); break;
            case 4: yield return Anomaly_CursorClicks(); break;
            case 5: yield return Anomaly_ButtonGhostPress(); break;
            case 6: yield return Anomaly_GhostBehindPanel(); break;
            case 7: yield return Anomaly_GhostInWater(); break;
            case 8: yield return Anomaly_RoomAppears(); break;
            case 9: yield return Anomaly_LogoEyes(); break;
            case 10: yield return Anomaly_ScreenFreeze(); break;
            case 11: yield return Anomaly_ClockError(); break;
            case 12: yield return Anomaly_GlassReflection(); break;
            case 13: yield return Anomaly_PlayerShadow(); break;
            case 14: yield return Anomaly_SystemMessage(); break;
            case 15: yield return Anomaly_ScreenBreathing(); break;
            case 16: yield return Anomaly_AudioWhisper(); break;
            case 17: yield return Anomaly_FalseLoad(); break;
            case 18: yield return Anomaly_SkyCorruption(); break;
            case 19: yield return Anomaly_SystemKnows(); break;
        }
    }

    #region Anomalies Implementation

    private IEnumerator Anomaly_ContinueExists()
    {
        var continueBtn = root.Q<VisualElement>("continue-item");
        if (continueBtn == null || PlayerPrefs.HasKey("SaveExists")) yield break;
        continueBtn.SetEnabled(true);
        var label = continueBtn.Q<Label>();
        string original = label.text;
        label.text = "Resume Session";
        continueBtn.RemoveFromClassList("menu-item-disabled");
        yield return new WaitForSecondsRealtime(2f);
        if (anomalyHum != null) PlaySFX(anomalyHum);
        label.text = original;
        continueBtn.SetEnabled(false);
        continueBtn.AddToClassList("menu-item-disabled");
    }

    private IEnumerator Anomaly_QuitChanges()
    {
        var btn = root.Q<VisualElement>("quit-item");
        var label = btn?.Q<Label>();
        if (label == null) yield break;
        string original = label.text;
        label.text = "Leave?";
        yield return new WaitForSecondsRealtime(2f);
        label.text = original;
    }

    private IEnumerator Anomaly_NewGameChanges()
    {
        var btn = root.Q<VisualElement>("new-game-item");
        var label = btn?.Q<Label>();
        if (label == null) yield break;
        string original = label.text;
        label.text = "Start Again";
        yield return new WaitForSecondsRealtime(2f);
        label.text = original;
    }

    private IEnumerator Anomaly_CursorPossession()
    {
        Vector2 startPos = Mouse.current.position.ReadValue();
        Vector2 offset = new Vector2(Random.Range(-150, 150), Random.Range(-150, 150));
        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 0.4f;
            Vector2 currentPos = Vector2.Lerp(startPos, startPos + offset, t);
            Mouse.current.WarpCursorPosition(currentPos);
            yield return null;
        }
    }

    private IEnumerator Anomaly_CursorClicks()
    {
        if (menuItems.Count == 0) yield break;
        var target = menuItems[Random.Range(0, menuItems.Count)];
        Vector2 startPos = Mouse.current.position.ReadValue();
        Vector2 targetPos = target.worldBound.center;
        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 1.2f;
            Mouse.current.WarpCursorPosition(Vector2.Lerp(startPos, targetPos, t));
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.3f);
        target.AddToClassList("menu-item-active");
        if (clickGlass != null) PlaySFX(clickGlass);
        yield return new WaitForSecondsRealtime(0.5f);
        target.RemoveFromClassList("menu-item-active");
        t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 1.2f;
            Mouse.current.WarpCursorPosition(Vector2.Lerp(targetPos, startPos, t));
            yield return null;
        }
    }

    private IEnumerator Anomaly_ButtonGhostPress()
    {
        if (menuItems.Count == 0) yield break;
        var target = menuItems[Random.Range(0, menuItems.Count)];
        target.AddToClassList("menu-item-active");
        if (clickGlass != null) PlaySFX(clickGlass);
        yield return new WaitForSecondsRealtime(0.5f);
        target.RemoveFromClassList("menu-item-active");
    }

    private IEnumerator Anomaly_GhostBehindPanel()
    {
        if (ghostElement == null) yield break;
        ghostElement.style.display = DisplayStyle.Flex;
        ghostElement.style.opacity = 0;
        float t = 0;
        while (t < 1f) { t += Time.unscaledDeltaTime; ghostElement.style.opacity = t * 0.5f; yield return null; }
        yield return new WaitForSecondsRealtime(1.5f);
        ghostElement.style.display = DisplayStyle.None;
    }

    private IEnumerator Anomaly_GhostInWater()
    {
        if (activeBackground == null) yield break;
        activeBackground.style.unityBackgroundImageTintColor = new Color(0.6f, 0.9f, 1f, 0.5f);
        yield return new WaitForSecondsRealtime(1.5f);
        activeBackground.style.unityBackgroundImageTintColor = Color.white;
    }

    private IEnumerator Anomaly_RoomAppears()
    {
        if (container == null || bedroomTexture == null) yield break;
        var original = container.style.backgroundImage;
        container.style.backgroundImage = bedroomTexture;
        if (anomalyHum != null) PlaySFX(anomalyHum);
        yield return new WaitForSecondsRealtime(2f);
        container.style.backgroundImage = original;
    }

    private IEnumerator Anomaly_LogoEyes()
    {
        if (eyeLeft == null || eyeRight == null) yield break;
        eyeLeft.style.display = DisplayStyle.Flex;
        eyeRight.style.display = DisplayStyle.Flex;
        eyeLeft.style.opacity = 1; eyeRight.style.opacity = 1;
        yield return new WaitForSecondsRealtime(1.2f);
        eyeLeft.AddToClassList("logo-eye-blink"); eyeRight.AddToClassList("logo-eye-blink");
        yield return new WaitForSecondsRealtime(0.15f);
        eyeLeft.RemoveFromClassList("logo-eye-blink"); eyeRight.RemoveFromClassList("logo-eye-blink");
        yield return new WaitForSecondsRealtime(0.8f);
        eyeLeft.style.display = DisplayStyle.None; eyeRight.style.display = DisplayStyle.None;
    }

    private IEnumerator Anomaly_ScreenFreeze()
    {
        float originalScale = Time.timeScale;
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(2.5f);
        Time.timeScale = originalScale;
        if (anomalyHum != null) PlaySFX(anomalyHum);
    }

    private IEnumerator Anomaly_ClockError()
    {
        if (clockLabel == null) yield break;
        string original = clockLabel.text;
        clockLabel.text = "21:11";
        clockLabel.style.color = new Color(1, 0.2f, 0.2f, 1);
        yield return new WaitForSecondsRealtime(2f);
        clockLabel.text = original;
        clockLabel.style.color = new StyleColor(StyleKeyword.Null);
    }

    private IEnumerator Anomaly_GlassReflection()
    {
        if (reflectionElement == null) yield break;
        reflectionElement.style.opacity = 0.4f;
        yield return new WaitForSecondsRealtime(1f);
        reflectionElement.style.opacity = 0f;
    }

    private IEnumerator Anomaly_PlayerShadow()
    {
        if (shadowElement == null) yield break;
        shadowElement.style.left = -400f;
        shadowElement.style.opacity = 0.5f;
        float t = 0;
        while (t < 1.5f) { t += Time.unscaledDeltaTime; shadowElement.style.left = -400f + (t / 1.5f) * 2800f; yield return null; }
        shadowElement.style.opacity = 0f;
    }

    private IEnumerator Anomaly_SystemMessage()
    {
        if (popupElement == null) yield break;
        popupLabel.text = Random.value > 0.5f ? "Scanning..." : "Memory restored...";
        popupElement.style.display = DisplayStyle.Flex; popupElement.style.opacity = 1;
        yield return new WaitForSecondsRealtime(2.5f);
        popupElement.style.display = DisplayStyle.None;
    }

    private IEnumerator Anomaly_ScreenBreathing()
    {
        float t = 0;
        while (t < 3f) { t += Time.unscaledDeltaTime; float s = 1f + Mathf.Sin(t * Mathf.PI / 1.5f) * 0.04f; container.style.scale = new Scale(new Vector2(s, s)); yield return null; }
        container.style.scale = new Scale(Vector2.one);
    }

    private IEnumerator Anomaly_AudioWhisper()
    {
        if (anomalyWhisper != null && anomalySource != null)
        {
            anomalySource.panStereo = -1f;
            anomalySource.volume = 0.9f;
            anomalySource.PlayOneShot(anomalyWhisper);
            float t = 0;
            while (t < 1.2f) { t += Time.unscaledDeltaTime * 0.6f; anomalySource.panStereo = Mathf.Lerp(-1f, 1f, t); yield return null; }
        }
    }

    private IEnumerator Anomaly_FalseLoad()
    {
        if (loadElement == null) yield break;
        loadElement.style.display = DisplayStyle.Flex; loadElement.style.opacity = 1;
        float p = 0;
        while (p < 1f) { p += Time.unscaledDeltaTime * 0.3f; loadFill.style.width = Length.Percent(p * 100f); yield return null; }
        yield return new WaitForSecondsRealtime(0.7f);
        loadElement.style.display = DisplayStyle.None;
    }

    private IEnumerator Anomaly_SkyCorruption()
    {
        if (atmosphere != null) { atmosphere.brightnessBoost = 3f; yield return new WaitForSecondsRealtime(1.2f); atmosphere.brightnessBoost = 0f; }
    }

    private IEnumerator Anomaly_SystemKnows()
    {
        if (popupElement == null) yield break;
        popupLabel.text = Random.value > 0.5f ? "User detected." : "Connection established.";
        popupElement.style.display = DisplayStyle.Flex; popupElement.style.opacity = 1;
        yield return new WaitForSecondsRealtime(2.5f);
        popupElement.style.display = DisplayStyle.None;
    }

    #endregion

    private void SetupOverlays()
    {
        var settingsPanel = root.Q<VisualElement>("settings-panel");
        var settingsItem = root.Q<VisualElement>("settings-item");
        if (settingsItem != null) settingsItem.RegisterCallback<ClickEvent>(evt => ShowPanel(settingsPanel));
        var settingsClose = root.Q<Button>("settings-close");
        if (settingsClose != null) settingsClose.clicked += () => HidePanel(settingsPanel);
        var creditsPanel = root.Q<VisualElement>("credits-panel");
        var creditsItem = root.Q<VisualElement>("credits-item");
        if (creditsItem != null) creditsItem.RegisterCallback<ClickEvent>(evt => ShowPanel(creditsPanel));
        var creditsClose = root.Q<Button>("credits-close");
        if (creditsClose != null) creditsClose.clicked += () => HidePanel(creditsPanel);

        var masterSlider = root.Q<Slider>("master-vol");
        if (masterSlider != null)
        {
            masterSlider.RegisterValueChangedCallback(evt =>
            {
                if (suppressSettingsCallbacks)
                {
                    return;
                }

                ApplyMasterVolume(evt.newValue, true);
            });
        }

        var fsToggle = root.Q<Toggle>("fullscreen-toggle");
        if (fsToggle != null)
        {
            fsToggle.RegisterValueChangedCallback(evt =>
            {
                if (suppressSettingsCallbacks)
                {
                    return;
                }

                ApplyFullscreenSetting(evt.newValue, true);
            });
        }

        InitializeSettingsUi(masterSlider, fsToggle);
    }

    private void CheckSave()
    {
        bool hasSave = PlayerPrefs.HasKey("SaveExists");
        var continueBtn = root.Q<VisualElement>("continue-item");
        if (continueBtn == null)
        {
            return;
        }

        continueBtn.SetEnabled(hasSave);
        continueBtn.EnableInClassList("menu-item-disabled", !hasSave);

        if (!hasSave)
        {
            StartCoroutine(DisabledContinuePulse(continueBtn));
        }
        else
        {
            continueBtn.style.opacity = 1f;
        }
    }

    private IEnumerator ClockUpdate()
    {
        while (true)
        {
            if (clockLabel != null) clockLabel.text = System.DateTime.Now.ToString("h:mm tt");
            yield return new WaitForSecondsRealtime(30f);
        }
    }

    private void OnHover(VisualElement item)
    {
        if (isTransitioning) return;
        inactivityTime = 0; PlaySFX(hoverTick);
        var sweep = item.Q<AeroHighlightSweep>(); if (sweep != null) sweep.Animate();
    }

    private void OnMenuItemClicked(VisualElement clickedItem)
    {
        if (clickedItem == null || isTransitioning) return;
        PlaySFX(clickGlass);
        if (activeBackground != null) clickedItem.Insert(0, activeBackground);
        foreach (var item in menuItems)
        {
            item.RemoveFromClassList("menu-item-active");
            var chevron = item.Q<VisualElement>(className: "menu-chevron");
            if (chevron != null) chevron.style.display = DisplayStyle.None;
        }
        clickedItem.AddToClassList("menu-item-active");
        var activeChevron = clickedItem.Q<VisualElement>(className: "menu-chevron");
        if (activeChevron != null) activeChevron.style.display = DisplayStyle.Flex;

        switch (clickedItem.name)
        {
            case "new-game-item":
                StartNewGame();
                break;
            case "continue-item":
                ContinueGame();
                break;
            case "quit-item":
                QuitGame();
                break;
        }
    }

    private void ShowPanel(VisualElement panel)
    {
        if (panel == null) return;
        panel.style.display = DisplayStyle.Flex;
        panel.schedule.Execute(() => panel.style.opacity = 1).StartingIn(10);
    }

    private void HidePanel(VisualElement panel)
    {
        if (panel == null) return;
        panel.style.opacity = 0;
        panel.schedule.Execute(() => panel.style.display = DisplayStyle.None).StartingIn(400);
    }

    private void StartNewGame()
    {
        if (isTransitioning) return;
        ProgressionManager.Instance.ResetProgress();
        isTransitioning = true; anomaliesEnabled = false;
        root.pickingMode = PickingMode.Ignore;

        StopAllCoroutines(); 
        StartCoroutine(TransitionSequence());
    }

    private void ContinueGame()
    {
        if (isTransitioning)
        {
            return;
        }

        if (!PlayerPrefs.HasKey("SaveExists"))
        {
            return;
        }

        ProgressionManager.Instance.LoadProgress();
        isTransitioning = true;
        anomaliesEnabled = false;
        root.pickingMode = PickingMode.Ignore;

        StopAllCoroutines();
        StartCoroutine(ContinueTransitionSequence());
    }

    private IEnumerator ContinueTransitionSequence()
    {
        if (atmosphere != null)
        {
            atmosphere.StopAtmosphere();
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.style.display = DisplayStyle.Flex;
            fadeOverlay.pickingMode = PickingMode.Position;
            fadeOverlay.style.backgroundColor = Color.black;
        }

        float elapsed = 0f;
        const float duration = 0.9f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (fadeOverlay != null)
            {
                fadeOverlay.style.opacity = t;
            }

            if (container != null)
            {
                container.style.opacity = 1f - t;
            }

            yield return null;
        }

        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("AeroDesktopScene");
    }

    private void QuitGame()
    {
        if (isTransitioning)
        {
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator TransitionSequence()
    {
        if (atmosphere != null) atmosphere.StopAtmosphere();
        if (anomalyHum != null) PlaySFX(anomalyHum);
        
        float elapsed = 0; float duration = 2.2f;
        if (fadeOverlay != null) { 
            fadeOverlay.style.display = DisplayStyle.Flex; 
            fadeOverlay.pickingMode = PickingMode.Position;
            fadeOverlay.style.backgroundColor = Color.black; 
        }
        
        var loadingContainer = fadeOverlay?.Q<VisualElement>("loading-container");
        var loadingLogo = fadeOverlay?.Q<VisualElement>("loading-logo");
        var loadingSpinner = fadeOverlay?.Q<VisualElement>(className: "loading-spinner");

        // Phase 1: Sudden Glitch & Audio Spike
        float glitchTimer = 0;
        if (clickGlass != null) PlaySFX(clickGlass);
        
        while (glitchTimer < 0.7f)
        {
            glitchTimer += Time.unscaledDeltaTime;
            if (container != null)
            {
                // Jitter
                float intensity = glitchTimer * 40f;
                container.style.translate = new Translate(Random.Range(-intensity, intensity), Random.Range(-intensity/2, intensity/2), 0);
                
                // Color Distortion (Abberation feel)
                if (Random.value > 0.8f)
                    container.style.unityBackgroundImageTintColor = new Color(1f, 0.4f, 0.4f, 0.9f);
                else if (Random.value > 0.8f)
                    container.style.unityBackgroundImageTintColor = new Color(0.4f, 1f, 1f, 0.9f);
                else
                    container.style.unityBackgroundImageTintColor = Color.white;
                
                // Scale pumping
                float s = 1f + Random.Range(-0.02f, 0.05f);
                container.style.scale = new Scale(new Vector2(s, s));
            }
            
            // Momentary "Flash"
            if (Random.value > 0.95f && fadeOverlay != null)
            {
                fadeOverlay.style.opacity = 0.5f;
                fadeOverlay.style.backgroundColor = Color.white;
            }
            else if (fadeOverlay != null)
            {
                fadeOverlay.style.opacity = 0f;
                fadeOverlay.style.backgroundColor = Color.black;
            }
            
            yield return new WaitForSecondsRealtime(0.02f);
        }

        // Reset container before dissolve
        if (container != null)
        {
            container.style.translate = new Translate(0, 0, 0);
            container.style.rotate = new Rotate(0);
            container.style.unityBackgroundImageTintColor = Color.white;
        }

        // Phase 2: Cinematic Dissolve & Fade to Black
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = t * t * (3 - 2 * t); // Smoothstep
            
            if (fadeOverlay != null) {
                fadeOverlay.style.backgroundColor = Color.black;
                fadeOverlay.style.opacity = easedT;
            }
            
            if (container != null) { 
                container.style.opacity = 1f - easedT; 
                // Zooming out and sinking effect
                float zoom = 1f - (easedT * 0.15f);
                container.style.scale = new Scale(new Vector2(zoom, zoom)); 
                container.style.translate = new Translate(0, easedT * 150f, 0);
                container.style.rotate = new Rotate(new Angle(easedT * 5f));
            }
            
            if (atmosphere != null) atmosphere.brightnessBoost = easedT * 4f;
            
            yield return null;
        }

        if (fadeOverlay != null) fadeOverlay.style.opacity = 1;
        if (container != null) container.style.opacity = 0;
        yield return new WaitForSecondsRealtime(0.5f);

        // Phase 3: Loading System Initialization
        if (loadingContainer != null) loadingContainer.style.opacity = 1;
        float spinnerRot = 0; float pulseTime = 0;
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("StoryIntroScene");
        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = false;
            while (asyncLoad.progress < 0.9f)
            {
                pulseTime += Time.unscaledDeltaTime;
                if (loadingSpinner != null) { 
                    spinnerRot += 350f * Time.unscaledDeltaTime; 
                    loadingSpinner.style.rotate = new Rotate(new Angle(spinnerRot)); 
                }
                if (loadingLogo != null) { 
                    float p = 0.85f + 0.15f * Mathf.PingPong(pulseTime * 1.5f, 1f); 
                    loadingLogo.style.scale = new Scale(new Vector2(p, p)); 
                    loadingLogo.style.opacity = 0.6f + 0.4f * p; 
                }
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.0f); // Minimum wait for "feeling"
            asyncLoad.allowSceneActivation = true;
        }
    }

    private IEnumerator TitleGlowBreathing()
    {
        while (true)
        {
            float t = 0;
            while (t < 5f)
            {
                if (isTransitioning) yield break;
                t += Time.unscaledDeltaTime;
                if (subtitle != null) subtitle.style.opacity = 0.6f + 0.3f * Mathf.PingPong(t * 0.8f, 1);
                yield return new WaitForSecondsRealtime(0.05f);
            }
        }
    }

    private IEnumerator DisabledContinuePulse(VisualElement btn)
    {
        while (true)
        {
            yield return new WaitForSeconds(15f);
            float t = 0;
            while (t < 1f) { t += Time.deltaTime; btn.style.opacity = 0.35f + 0.15f * Mathf.Sin(t * Mathf.PI); yield return null; }
            btn.style.opacity = 0.35f;
        }
    }

    private void PlaySFX(AudioClip clip) { if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip); }

    private void InitializeSettingsUi(Slider masterSlider, Toggle fullscreenToggle)
    {
        suppressSettingsCallbacks = true;

        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumePrefKey, AudioListener.volume > 0f ? AudioListener.volume : 1f));
        bool savedFullscreen = PlayerPrefs.GetInt(FullscreenPrefKey, Screen.fullScreen ? 1 : 0) == 1;

        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(savedVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetValueWithoutNotify(savedFullscreen);
        }

        ApplyMasterVolume(savedVolume, false);
        ApplyFullscreenSetting(savedFullscreen, false);

        suppressSettingsCallbacks = false;
    }

    private void ApplyMasterVolume(float value, bool save)
    {
        float clamped = Mathf.Clamp01(value);
        AudioListener.volume = clamped;

        if (masterVolumeValueLabel != null)
        {
            masterVolumeValueLabel.text = Mathf.RoundToInt(clamped * 100f) + "%";
        }

        if (save)
        {
            PlayerPrefs.SetFloat(MasterVolumePrefKey, clamped);
            PlayerPrefs.Save();
        }
    }

    private void ApplyFullscreenSetting(bool isFullscreen, bool save)
    {
        Screen.fullScreen = isFullscreen;

        if (fullscreenValueLabel != null)
        {
            fullscreenValueLabel.text = isFullscreen ? "Enabled" : "Windowed";
        }

        if (save)
        {
            PlayerPrefs.SetInt(FullscreenPrefKey, isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
