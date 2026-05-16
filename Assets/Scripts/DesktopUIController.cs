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

    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement _startButton;
    private VisualElement _startMenu;
    private Label _clockLabel;
    private VisualElement _mainArea;
    private Label _startMenuUserName;
    private TetrisController _tetrisController;
    private AudioSource _desktopAudioSource;
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
        }

        if (_mainArea != null)
        {
            _mainArea.RegisterCallback<ClickEvent>(OnMainAreaClicked);
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
            icon.RegisterCallback<ClickEvent>(evt => OnIconClicked(icon));
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
            _toastTitle = new Label();
            _toastTitle.AddToClassList("desktop-toast-title");
            _toastBody = new Label();
            _toastBody.AddToClassList("desktop-toast-body");
            _toast.Add(_toastTitle);
            _toast.Add(_toastBody);
            _root.Add(_toast);
        }

        if (_fakeWindow == null)
        {
            _fakeWindow = new VisualElement();
            _fakeWindow.AddToClassList("desktop-fake-window");
            _fakeWindowTitle = new Label();
            _fakeWindowTitle.AddToClassList("desktop-fake-window-title");
            _fakeWindowBody = new Label();
            _fakeWindowBody.AddToClassList("desktop-fake-window-body");
            _fakeWindow.Add(_fakeWindowTitle);
            _fakeWindow.Add(_fakeWindowBody);
            _root.Add(_fakeWindow);
        }
    }

    private IEnumerator UiAnomalyRoutine()
    {
        while (isActiveAndEnabled)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(minUiAnomalyInterval, maxUiAnomalyInterval));
            yield return StartCoroutine(TriggerUiAnomaly(UnityEngine.Random.Range(0, 5)));
        }
    }

    private IEnumerator TriggerUiAnomaly(int type)
    {
        switch (type)
        {
            case 0:
                yield return StartCoroutine(HauntedIconAnomaly());
                break;
            case 1:
                yield return StartCoroutine(FalseNotificationAnomaly());
                break;
            case 2:
                yield return StartCoroutine(ClockCorruptionAnomaly());
                break;
            case 3:
                yield return StartCoroutine(LayoutShiftAnomaly());
                break;
            default:
                yield return StartCoroutine(FakeWindowAnomaly());
                break;
        }
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
