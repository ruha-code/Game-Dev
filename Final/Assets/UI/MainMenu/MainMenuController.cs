using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using AeroOS.UI;
using System.Collections.Generic;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    private VisualElement root;
    private VisualElement container;
    private VisualElement title;
    private VisualElement subtitle;
    private AeroActiveBackground activeBackground;
    private AeroAtmosphere atmosphere;
    private List<VisualElement> menuItems = new List<VisualElement>();

    [Header("Audio")]
    public AudioClip ambientMusic;
    public AudioClip hoverTick;
    public AudioClip clickGlass;
    private AudioSource audioSource;
    private AudioSource sfxSource;

    private float inactivityTime;
    private float anomalyCooldown;
    private bool isTransitioning;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        container = root.Q<VisualElement>("root");
        title = root.Q<VisualElement>("title");
        subtitle = root.Q<VisualElement>("subtitle");
        activeBackground = root.Q<AeroActiveBackground>();
        atmosphere = root.Q<AeroAtmosphere>();

        SetupAudio();
        SetupMenu();
        SetupOverlays();
        CheckSave();
        
        StartCoroutine(TitleGlowBreathing());
        anomalyCooldown = Random.Range(20f, 40f);
    }

    private void SetupAudio()
    {
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = ambientMusic;
        audioSource.loop = true;
        audioSource.volume = 0.35f;
        if (!audioSource.isPlaying) audioSource.Play();

        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
    }

    private void SetupMenu()
    {
        var menuBox = root.Q<VisualElement>("menu");
        if (menuBox != null)
        {
            foreach (var item in menuBox.Children())
            {
                if (item.ClassListContains("menu-item"))
                {
                    menuItems.Add(item);
                    item.RegisterCallback<ClickEvent>(evt => OnMenuItemClicked(item));
                    item.RegisterCallback<MouseEnterEvent>(evt => OnHover(item));
                }
            }
        }

        var newGameBtn = root.Q<VisualElement>("new-game-item");
        if (newGameBtn != null) newGameBtn.RegisterCallback<ClickEvent>(evt => StartNewGame());
    }

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
        if (masterSlider != null) masterSlider.RegisterValueChangedCallback(evt => AudioListener.volume = evt.newValue);
        
        var fsToggle = root.Q<Toggle>("fullscreen-toggle");
        if (fsToggle != null) fsToggle.RegisterValueChangedCallback(evt => Screen.fullScreen = evt.newValue);
    }

    private void CheckSave()
    {
        bool hasSave = PlayerPrefs.HasKey("SaveExists");
        var continueBtn = root.Q<VisualElement>("continue-item");
        if (continueBtn != null && !hasSave)
        {
            continueBtn.SetEnabled(false);
            continueBtn.AddToClassList("menu-item-disabled");
            StartCoroutine(DisabledContinuePulse(continueBtn));
        }
    }

    private void Update()
    {
        if (isTransitioning) return;

        inactivityTime += Time.deltaTime;
        anomalyCooldown -= Time.deltaTime;

        if (Keyboard.current.anyKey.wasPressedThisFrame || Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f)
            inactivityTime = 0;

        if (anomalyCooldown <= 0)
        {
            TriggerAnomaly();
            anomalyCooldown = Random.Range(30f, 60f);
        }
    }

    private void OnHover(VisualElement item)
    {
        if (isTransitioning) return;
        inactivityTime = 0;
        PlaySFX(hoverTick);
        var sweep = item.Q<AeroHighlightSweep>();
        if (sweep != null) sweep.Animate();
    }

    private void OnMenuItemClicked(VisualElement clickedItem)
    {
        if (isTransitioning) return;
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
        StartCoroutine(TransitionSequence());
    }

    private IEnumerator TransitionSequence()
    {
        isTransitioning = true;
        
        float t = 0;
        var menu = root.Q<VisualElement>("menu");
        var footer = root.Q<VisualElement>("footer");
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float alpha = 1f - (t / 0.3f);
            if (menu != null) menu.style.opacity = alpha;
            if (footer != null) footer.style.opacity = alpha;
            yield return null;
        }
        if (menu != null) menu.style.opacity = 0;
        if (footer != null) footer.style.opacity = 0;

        t = 0;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            if (atmosphere != null) atmosphere.brightnessBoost = (t / 0.4f) * 0.5f;
            yield return null;
        }

        t = 0;
        if (container != null)
        {
            container.style.backgroundColor = Color.white;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                container.style.opacity = t / 0.3f;
                yield return null;
            }
        }
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("BootScene");
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
    }

    private IEnumerator TitleGlowBreathing()
    {
        while (true)
        {
            float t = 0;
            while (t < 5f)
            {
                t += Time.deltaTime;
                float p = Mathf.PingPong(t * 0.4f, 1);
                if (title != null) title.style.textShadow = new TextShadow { color = new Color(0.47f, 0.82f, 1f, 0.4f * p), blurRadius = 30 * p };
                if (subtitle != null) subtitle.style.opacity = 0.6f + 0.3f * Mathf.PingPong(t * 0.8f, 1);
                yield return null;
            }
        }
    }

    private IEnumerator DisabledContinuePulse(VisualElement btn)
    {
        while (true)
        {
            yield return new WaitForSeconds(15f);
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime;
                btn.style.opacity = 0.35f + 0.15f * Mathf.Sin(t * Mathf.PI);
                yield return null;
            }
            btn.style.opacity = 0.35f;
        }
    }

    private void TriggerAnomaly()
    {
        int type = Random.Range(0, 5);
        switch (type)
        {
            case 0: if (atmosphere != null) atmosphere.TriggerParticleAnomaly(); break;
            case 1: if (title != null) StartCoroutine(AnomalyShift(title, 1)); break;
            case 2: if (menuItems.Count > 0) StartCoroutine(AnomalyFlicker(menuItems[Random.Range(0, menuItems.Count)])); break;
            case 3: if (container != null) StartCoroutine(AnomalyShift(container, 2)); break;
            case 4: StartCoroutine(AnomalyDip()); break;
        }
    }

    private IEnumerator AnomalyShift(VisualElement el, int pixels)
    {
        el.style.translate = new Translate(pixels, 0, 0);
        yield return new WaitForSeconds(0.1f);
        el.style.translate = new Translate(0, 0, 0);
    }

    private IEnumerator AnomalyFlicker(VisualElement el)
    {
        el.style.opacity = 0.2f;
        yield return new WaitForSeconds(0.05f);
        el.style.opacity = 1f;
    }

    private IEnumerator AnomalyDip()
    {
        if (container != null)
        {
            container.style.unityBackgroundImageTintColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            yield return new WaitForSeconds(0.15f);
            container.style.unityBackgroundImageTintColor = Color.white;
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }
}