using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;

public class DesktopUIController : MonoBehaviour
{
    [Header("Desktop Audio")]
    [SerializeField] private AudioClip desktopAppearChime;
    [SerializeField, Range(0f, 1f)] private float desktopAppearVolume = 0.8f;
    [SerializeField] private AudioClip uiAnomalyClip;
    [SerializeField] private float minUiAnomalyInterval = 12f;
    [SerializeField] private float maxUiAnomalyInterval = 24f;
    [SerializeField] private AudioClip ambientDesktopSound;
    [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.25f;
    [SerializeField] private float ambientFadeDuration = 4f;
    [SerializeField] private AudioClip uiClickSound;
    [SerializeField] private AudioClip glassCrackSound;
    [SerializeField] private Sprite brokenGlassSprite;
    [SerializeField] private Sprite phantomChatIcon;

    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement _startButton;
    private Button _shutdownButton;
    private VisualElement _startMenu;
    private Label _clockLabel;
    private VisualElement _mainArea;
    private Label _startMenuUserName;
    private TetrisController _tetrisController;
    private AudioSource _desktopAudioSource;
    private AudioSource _ambientAudioSource;
    private bool _hasPlayedDesktopAppearChime;
    private string _playerName;
    private string _stableClockText;
    private Coroutine _uiAnomalyRoutine;
    private VisualElement _toast;
    private Label _toastTitle;
    private Label _toastBody;
    private VisualElement _fakeWindow;
    private Label _fakeWindowTitle;
    private Label _fakeWindowBody;
    private VisualElement _glassOverlay;
    private VisualElement _chatWindow;
    private Label _chatText;

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null) return;

        _tetrisController = GetComponent<TetrisController>();
        _playerName = PlayerPrefs.GetString("PlayerName", "User");

        _root = _uiDocument.rootVisualElement;

        // Query elements
        _startButton = _root.Q<VisualElement>("start-button");
        _startMenu = _root.Q<VisualElement>("start-menu");
        _shutdownButton = _root.Q<Button>(className: "start-menu-shutdown-btn");
        _clockLabel = _root.Q<Label>("tray-clock");
        _mainArea = _root.Q<VisualElement>("main-area");
        _startMenuUserName = _root.Q<Label>(className: "start-menu-user-name");

        if (_desktopAudioSource == null)
        {
            _desktopAudioSource = gameObject.AddComponent<AudioSource>();
            _desktopAudioSource.playOnAwake = false;
            _desktopAudioSource.loop = false;
            _desktopAudioSource.spatialBlend = 0f;
        }

        if (_ambientAudioSource == null)
        {
            _ambientAudioSource = gameObject.AddComponent<AudioSource>();
            _ambientAudioSource.playOnAwake = false;
            _ambientAudioSource.loop = true;
            _ambientAudioSource.spatialBlend = 0f;
            _ambientAudioSource.volume = 0f;
        }

        EnsureAnomalyUi();
        ApplyPersonalization();

        // Initialize Tetris
        if (_tetrisController != null)
        {
            _tetrisController.Initialize(_root);
        }

        // Register events
        if (_startButton != null)
        {
            _startButton.RegisterCallback<ClickEvent>(OnStartButtonClicked);
            _startButton.RegisterCallback<ClickEvent>(evt => PlayClickSound());
        }

        if (_shutdownButton != null)
        {
            _shutdownButton.RegisterCallback<PointerOverEvent>(OnShutdownButtonHover);
            _shutdownButton.RegisterCallback<ClickEvent>(evt => PlayClickSound());
        }

        if (_mainArea != null)
        {
            _mainArea.RegisterCallback<ClickEvent>(OnMainAreaClicked);
            _mainArea.RegisterCallback<ClickEvent>(evt => PlayClickSound());
        }

        // Initialize clock
        UpdateClock();
        InvokeRepeating(nameof(UpdateClock), 1f, 1f);
        StartCoroutine(PlayDesktopAppearChimeOnce());
        if (_uiAnomalyRoutine != null) StopCoroutine(_uiAnomalyRoutine);
        _uiAnomalyRoutine = StartCoroutine(UiAnomalyRoutine());

        // Setup desktop icons
        var icons = _root.Query<VisualElement>(className: "desktop-icon-wrapper").ToList();
        foreach (var icon in icons)
        {
            icon.RegisterCallback<ClickEvent>(evt => {
                OnIconClicked(icon);
                PlayClickSound();
            });
        }
    }

    private void PlayClickSound()
    {
        if (uiClickSound != null && _desktopAudioSource != null)
        {
            _desktopAudioSource.PlayOneShot(uiClickSound, 0.4f);
        }
    }

    private void OnShutdownButtonHover(PointerOverEvent evt)
    {
        // Runaway button anomaly logic: only if start menu is visible
        if (_shutdownButton != null && !_startMenu.ClassListContains("hidden"))
        {
            // Occasionally "run away"
            if (UnityEngine.Random.value < 0.3f) 
            {
                float offsetX = UnityEngine.Random.Range(-50f, 50f);
                float offsetY = UnityEngine.Random.Range(-30f, 30f);
                _shutdownButton.style.translate = new Translate(offsetX, offsetY, 0);
                PlayUiAnomalyCue(0.05f);
            }
        }
    }

    private void ResetShutdownButton()
    {
        if (_shutdownButton != null)
        {
            _shutdownButton.style.translate = new Translate(0, 0, 0);
        }
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(UpdateClock));
        if (_uiAnomalyRoutine != null)
        {
            StopCoroutine(_uiAnomalyRoutine);
            _uiAnomalyRoutine = null;
        }
    }

    private IEnumerator PlayDesktopAppearChimeOnce()
    {
        if (_hasPlayedDesktopAppearChime || desktopAppearChime == null || _desktopAudioSource == null)
        {
            yield break;
        }

        // Wait one frame so the chime lines up with the desktop becoming visible.
        yield return null;

        _desktopAudioSource.PlayOneShot(desktopAppearChime, desktopAppearVolume);
        _hasPlayedDesktopAppearChime = true;

        // Wait for chime to finish (approximate) + extra 1.5s delay
        yield return new WaitForSeconds(desktopAppearChime.length + 1.5f);

        if (ambientDesktopSound != null && _ambientAudioSource != null)
        {
            _ambientAudioSource.clip = ambientDesktopSound;
            _ambientAudioSource.Play();
            yield return StartCoroutine(FadeInAmbient());
        }
    }

    private IEnumerator FadeInAmbient()
    {
        float elapsed = 0f;
        while (elapsed < ambientFadeDuration)
        {
            elapsed += Time.deltaTime;
            _ambientAudioSource.volume = Mathf.Lerp(0f, ambientVolume, elapsed / ambientFadeDuration);
            yield return null;
        }
        _ambientAudioSource.volume = ambientVolume;
    }

    private void UpdateClock()
    {
        if (_clockLabel != null)
        {
            _stableClockText = DateTime.Now.ToString("h:mm tt");
            _clockLabel.text = _stableClockText;
        }
    }

    private void OnStartButtonClicked(ClickEvent evt)
    {
        if (_startMenu != null)
        {
            bool isHidden = _startMenu.ClassListContains("hidden");
            if (isHidden)
            {
                _startMenu.RemoveFromClassList("hidden");
                ResetShutdownButton();
            }
            else
            {
                _startMenu.AddToClassList("hidden");
            }
            evt.StopPropagation();
        }
    }

    private void OnMainAreaClicked(ClickEvent evt)
    {
        // Close start menu when clicking background
        if (_startMenu != null && !_startMenu.ClassListContains("hidden"))
        {
            _startMenu.AddToClassList("hidden");
        }
    }

    private void OnIconClicked(VisualElement icon)
    {
        // Deselect others
        var allIcons = _root.Query<VisualElement>(className: "desktop-icon-wrapper").ToList();
        foreach (var other in allIcons)
        {
            other.RemoveFromClassList("desktop-icon-wrapper--selected");
        }

        var label = icon.Q<Label>(className: "desktop-icon-label");
        string iconName = label != null ? label.text : "Unknown Icon";
        Debug.Log($"Desktop Icon Clicked: {iconName}");
        
        // Add selected visual feedback
        icon.AddToClassList("desktop-icon-wrapper--selected");

        if (iconName == "Tetris")
        {
            if (_tetrisController != null)
            {
                _tetrisController.Show();
            }
        }
    }

    private void ApplyPersonalization()
    {
        if (_startMenuUserName != null)
        {
            _startMenuUserName.text = _playerName;
        }
    }

    private void EnsureAnomalyUi()
    {
        if (_root == null) return;

        if (_toast == null)
        {
            _toast = new VisualElement();
            _toast.AddToClassList("desktop-toast");
            _toast.pickingMode = PickingMode.Ignore;
            _toastTitle = new Label();
            _toastTitle.AddToClassList("desktop-toast-title");
            _toastTitle.pickingMode = PickingMode.Ignore;
            _toastBody = new Label();
            _toastBody.AddToClassList("desktop-toast-body");
            _toastBody.pickingMode = PickingMode.Ignore;
            _toast.Add(_toastTitle);
            _toast.Add(_toastBody);
            _root.Add(_toast);
        }

        if (_fakeWindow == null)
        {
            _fakeWindow = new VisualElement();
            _fakeWindow.AddToClassList("desktop-fake-window");
            _fakeWindow.pickingMode = PickingMode.Ignore;
            _fakeWindowTitle = new Label();
            _fakeWindowTitle.AddToClassList("desktop-fake-window-title");
            _fakeWindowTitle.pickingMode = PickingMode.Ignore;
            _fakeWindowBody = new Label();
            _fakeWindowBody.AddToClassList("desktop-fake-window-body");
            _fakeWindowBody.pickingMode = PickingMode.Ignore;
            _fakeWindow.Add(_fakeWindowTitle);
            _fakeWindow.Add(_fakeWindowBody);
            _root.Add(_fakeWindow);
        }

        if (_glassOverlay == null)
        {
            _glassOverlay = new VisualElement();
            _glassOverlay.AddToClassList("glass-overlay");
            _glassOverlay.pickingMode = PickingMode.Ignore;
            if (brokenGlassSprite != null)
                _glassOverlay.style.backgroundImage = new StyleBackground(brokenGlassSprite);
            _root.Add(_glassOverlay);
        }

        if (_chatWindow == null)
        {
            _chatWindow = new VisualElement();
            _chatWindow.AddToClassList("phantom-chat");
            _chatWindow.pickingMode = PickingMode.Ignore;
            var icon = new VisualElement();
            icon.AddToClassList("phantom-chat-icon");
            icon.pickingMode = PickingMode.Ignore;
            if (phantomChatIcon != null)
                icon.style.backgroundImage = new StyleBackground(phantomChatIcon);
            _chatText = new Label();
            _chatText.AddToClassList("phantom-chat-text");
            _chatText.pickingMode = PickingMode.Ignore;
            _chatWindow.Add(icon);
            _chatWindow.Add(_chatText);
            _root.Add(_chatWindow);
        }
    }

    private IEnumerator UiAnomalyRoutine()
    {
        while (isActiveAndEnabled)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(minUiAnomalyInterval, maxUiAnomalyInterval));
            yield return StartCoroutine(TriggerUiAnomaly(UnityEngine.Random.Range(0, 7)));
        }
    }

    private IEnumerator TriggerUiAnomaly(int type)
    {
        switch (type)
        {
            case 0: yield return StartCoroutine(HauntedIconAnomaly()); break;
            case 1: yield return StartCoroutine(FalseNotificationAnomaly()); break;
            case 2: yield return StartCoroutine(ClockCorruptionAnomaly()); break;
            case 3: yield return StartCoroutine(LayoutShiftAnomaly()); break;
            case 4: yield return StartCoroutine(FakeWindowAnomaly()); break;
            case 5: yield return StartCoroutine(GlassCrackingAnomaly()); break;
            case 6: yield return StartCoroutine(PhantomChatAnomaly()); break;
            case 7: yield return StartCoroutine(WindowGhostingAnomaly()); break;
            }
            }

            private IEnumerator WindowGhostingAnomaly()
            {
            // Simulate a window "ghosting" or trailing across the screen
            PlayUiAnomalyCue(0.12f);
        
            VisualElement ghost = new VisualElement();
            ghost.AddToClassList("ghost-window");
            ghost.pickingMode = PickingMode.Ignore;
            _root.Add(ghost);
        
            float startX = UnityEngine.Random.value > 0.5f ? -400f : Screen.width + 100f;
            float targetX = startX < 0 ? Screen.width + 100f : -400f;
            float y = UnityEngine.Random.Range(100f, Screen.height - 300f);
        
            float duration = 1.5f;
            float elapsed = 0f;
        
            while (elapsed < duration)
            {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentX = Mathf.Lerp(startX, targetX, t);
            ghost.style.left = currentX;
            ghost.style.top = y;
            ghost.style.opacity = Mathf.Sin(t * Mathf.PI);
            yield return null;
            }
        
            _root.Remove(ghost);
            }

            private IEnumerator GlassCrackingAnomaly()
            {
            if (_glassOverlay == null) yield break;

            if (glassCrackSound != null && _desktopAudioSource != null)
            _desktopAudioSource.PlayOneShot(glassCrackSound, 0.6f);
            else
            PlayUiAnomalyCue(0.3f);

            _glassOverlay.AddToClassList("glass-overlay--active");
            yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 5f));
            _glassOverlay.RemoveFromClassList("glass-overlay--active");
            }

    private IEnumerator PhantomChatAnomaly()
    {
        if (_chatWindow == null) yield break;
        string[] messages = { "Are you still there?", "I remember you.", "Don't look behind.", "System error: Help." };
        _chatText.text = messages[UnityEngine.Random.Range(0, messages.Length)];
        _chatWindow.AddToClassList("phantom-chat--visible");
        PlayUiAnomalyCue(0.1f);
        yield return new WaitForSeconds(4f);
        _chatWindow.RemoveFromClassList("phantom-chat--visible");
    }

    private IEnumerator HauntedIconAnomaly()
    {
        var icons = _root.Query<VisualElement>(className: "desktop-icon-wrapper").ToList();
        if (icons.Count == 0) yield break;

        VisualElement icon = icons[UnityEngine.Random.Range(0, icons.Count)];
        Label label = icon.Q<Label>(className: "desktop-icon-label");
        string originalLabel = label != null ? label.text : string.Empty;

        PlayUiAnomalyCue(0.18f);
        icon.AddToClassList("desktop-icon-wrapper--haunted");
        if (label != null)
        {
            label.text = _playerName == "User" ? "OPEN ME" : _playerName.ToUpperInvariant();
        }

        for (int i = 0; i < 6; i++)
        {
            icon.style.translate = new Translate(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(-5f, 5f), 0f);
            yield return new WaitForSeconds(0.08f);
        }

        icon.style.translate = new Translate(0f, 0f, 0f);
        icon.RemoveFromClassList("desktop-icon-wrapper--haunted");
        if (label != null)
        {
            label.text = originalLabel;
        }
    }

    private IEnumerator FalseNotificationAnomaly()
    {
        if (_toast == null) yield break;

        _toastTitle.text = "AeroOS Security";
        _toastBody.text = _playerName + ", your deleted files are trying to return.";
        _toast.AddToClassList("desktop-toast--visible");
        PlayUiAnomalyCue(0.14f);
        yield return new WaitForSeconds(4f);
        _toast.RemoveFromClassList("desktop-toast--visible");
    }

    private IEnumerator ClockCorruptionAnomaly()
    {
        if (_clockLabel == null) yield break;

        string[] glitchTimes = {
            "7 MISSED",
            "03:33 AM",
            "CALL HOME",
            "--:--"
        };

        _clockLabel.AddToClassList("tray-clock--glitch");
        PlayUiAnomalyCue(0.12f);

        for (int i = 0; i < glitchTimes.Length; i++)
        {
            _clockLabel.text = glitchTimes[i];
            yield return new WaitForSeconds(0.28f);
        }

        _clockLabel.RemoveFromClassList("tray-clock--glitch");
        _clockLabel.text = _stableClockText;
    }

    private IEnumerator LayoutShiftAnomaly()
    {
        if (_mainArea == null) yield break;

        PlayUiAnomalyCue(0.1f);
        _mainArea.AddToClassList("desktop-main-area--glitch");
        for (int i = 0; i < 5; i++)
        {
            _mainArea.style.translate = new Translate(UnityEngine.Random.Range(-14f, 14f), UnityEngine.Random.Range(-10f, 10f), 0f);
            yield return new WaitForSeconds(0.07f);
        }

        _mainArea.style.translate = new Translate(0f, 0f, 0f);
        _mainArea.RemoveFromClassList("desktop-main-area--glitch");
    }

    private IEnumerator FakeWindowAnomaly()
    {
        if (_fakeWindow == null) yield break;

        _fakeWindowTitle.text = "Session Recovery";
        _fakeWindowBody.text = "Recovered fragment for " + _playerName + ".\nDo you remember closing the last window?";
        _fakeWindow.AddToClassList("desktop-fake-window--visible");
        PlayUiAnomalyCue(0.15f);
        yield return new WaitForSeconds(3.5f);
        _fakeWindow.RemoveFromClassList("desktop-fake-window--visible");
    }

    private void PlayUiAnomalyCue(float volume)
    {
        if (uiAnomalyClip != null && _desktopAudioSource != null)
        {
            _desktopAudioSource.PlayOneShot(uiAnomalyClip, volume);
        }
    }
}
