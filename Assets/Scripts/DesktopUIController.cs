using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

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
    private DocumentsAppController _documentsController;
    private TetrisController _tetrisController;
    private bool _hasPlayedDesktopAppearChime;
    private string _playerName;
    private string _stableClockText;
    private Coroutine _uiAnomalyRoutine;
    private bool _desktopAnomaliesPaused;
    private VisualElement _toast;
    private Label _toastTitle;
    private Label _toastBody;
    private VisualElement _fakeWindow;
    private Label _fakeWindowTitle;
    private Label _fakeWindowBody;
    private VisualElement _glassOverlay;
    private VisualElement _chatWindow;
    private Label _chatText;
    private Label _objectiveText;
    private Button _treeHotspot;
    private Button _cityHotspot;
    private Button _balloonHotspot;

    private readonly Dictionary<string, VisualElement> _iconMap = new Dictionary<string, VisualElement>();

    private void OnEnable()
    {
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            return;
        }

        _ = ProgressionManager.Instance;
        _ = AudioManager.Instance;

        _documentsController = GetComponent<DocumentsAppController>();
        _tetrisController = GetComponent<TetrisController>();
        _playerName = PlayerPrefs.GetString("PlayerName", "User");
        _root = _uiDocument.rootVisualElement;

        _startButton = _root.Q<VisualElement>("start-button");
        _startMenu = _root.Q<VisualElement>("start-menu");
        _shutdownButton = _root.Q<Button>(className: "start-menu-shutdown-btn");
        _clockLabel = _root.Q<Label>("tray-clock");
        _mainArea = _root.Q<VisualElement>("main-area");
        _startMenuUserName = _root.Q<Label>(className: "start-menu-user-name");
        _objectiveText = _root.Q<Label>("objective-text");
        _treeHotspot = _root.Q<Button>("tree-hotspot");
        _cityHotspot = _root.Q<Button>("city-hotspot");
        _balloonHotspot = _root.Q<Button>("balloon-hotspot");

        EnsureAnomalyUi();
        ApplyPersonalization();
        CacheDesktopIcons();

        if (_documentsController != null)
        {
            _documentsController.Initialize(_root);
        }

        if (_tetrisController != null)
        {
            _tetrisController.Initialize(_root);
        }

        RegisterUiEvents();
        RegisterHotspots();

        UpdateClock();
        InvokeRepeating(nameof(UpdateClock), 1f, 1f);
        StartCoroutine(PlayDesktopAppearChimeOnce());

        if (_uiAnomalyRoutine != null)
        {
            StopCoroutine(_uiAnomalyRoutine);
        }

        _uiAnomalyRoutine = StartCoroutine(UiAnomalyRoutine());
        ProgressionManager.Instance.ProgressionChanged += OnProgressionChanged;
        RefreshDesktopState();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(UpdateClock));
        if (_uiAnomalyRoutine != null)
        {
            StopCoroutine(_uiAnomalyRoutine);
            _uiAnomalyRoutine = null;
        }

        if (ProgressionManager.HasInstance)
        {
            ProgressionManager.Instance.ProgressionChanged -= OnProgressionChanged;
        }
    }

    private void Update()
    {
        bool shouldPauseDesktopAnomalies = IsDesktopAppWindowOpen();
        if (shouldPauseDesktopAnomalies == _desktopAnomaliesPaused)
        {
            return;
        }

        SetDesktopAnomaliesPaused(shouldPauseDesktopAnomalies);
    }

    private void RegisterUiEvents()
    {
        if (_startButton != null)
        {
            _startButton.RegisterCallback<ClickEvent>(OnStartButtonClicked);
            _startButton.RegisterCallback<ClickEvent>(_ => PlayClickSound());
        }

        if (_shutdownButton != null)
        {
            _shutdownButton.RegisterCallback<PointerOverEvent>(OnShutdownButtonHover);
            _shutdownButton.RegisterCallback<ClickEvent>(_ => PlayClickSound());
        }

        if (_mainArea != null)
        {
            _mainArea.RegisterCallback<ClickEvent>(OnMainAreaClicked);
            _mainArea.RegisterCallback<ClickEvent>(_ => PlayClickSound());
        }

        foreach (VisualElement icon in _iconMap.Values)
        {
            icon.RegisterCallback<ClickEvent>(evt =>
            {
                OnIconClicked(icon);
                PlayClickSound();
                evt.StopPropagation();
            });
        }
    }

    private void RegisterHotspots()
    {
        RegisterHotspot(_treeHotspot, LocationId.TreeScene, "TreeScene");
        RegisterHotspot(_cityHotspot, LocationId.CityScene, "CityScene");
        RegisterHotspot(_balloonHotspot, LocationId.BalloonScene, "BalloonScene");
    }

    private void RegisterHotspot(Button hotspot, LocationId locationId, string sceneName)
    {
        if (hotspot == null)
        {
            return;
        }

        hotspot.clicked += () =>
        {
            PlayClickSound();
            if (TryLoadScene(sceneName))
            {
                ProgressionManager.Instance.MarkLocationVisited(locationId);
            }
        };
    }

    private void CacheDesktopIcons()
    {
        _iconMap.Clear();
        _iconMap["Computer"] = _root.Q<VisualElement>("icon-wrapper-computer");
        _iconMap["Documents"] = _root.Q<VisualElement>("icon-wrapper-documents");
        _iconMap["Pictures"] = _root.Q<VisualElement>("icon-wrapper-pictures");
        _iconMap["Videos"] = _root.Q<VisualElement>("icon-wrapper-videos");
        _iconMap["Music"] = _root.Q<VisualElement>("icon-wrapper-music");
        _iconMap["Network"] = _root.Q<VisualElement>("icon-wrapper-network");
        _iconMap["Control Panel"] = _root.Q<VisualElement>("icon-wrapper-control-panel");
        _iconMap["Recycle Bin"] = _root.Q<VisualElement>("icon-wrapper-recycle-bin");
        _iconMap["Tetris"] = _root.Q<VisualElement>("icon-wrapper-tetris");
    }

    private void OnProgressionChanged()
    {
        RefreshDesktopState();
    }

    private void RefreshDesktopState()
    {
        ProgressionManager progression = ProgressionManager.Instance;

        if (_objectiveText != null)
        {
            _objectiveText.text = progression.GetCurrentObjectiveText();
        }

        UpdateIconState("Documents", true, progression.CurrentObjective == ObjectiveId.ReviewDocuments);
        UpdateIconState("Pictures", progression.HasKey(GameKey.DocumentsKey), progression.CurrentObjective == ObjectiveId.RecoverTreeMemory && !progression.HasKey(GameKey.PicturesKey));
        UpdateIconState("Music", progression.HasKey(GameKey.DocumentsKey), progression.CurrentObjective == ObjectiveId.RecoverTreeMemory && !progression.HasKey(GameKey.MusicKey));
        UpdateIconState("Computer", progression.HasKey(GameKey.PicturesKey) && progression.HasKey(GameKey.MusicKey), progression.CurrentObjective == ObjectiveId.AccessComputer);
        UpdateIconState("Control Panel", progression.HasKey(GameKey.ComputerKey), progression.CurrentObjective == ObjectiveId.ConfigureControlPanel);
        UpdateIconState("Network", progression.HasKey(GameKey.ControlPanelKey), progression.CurrentObjective == ObjectiveId.RepairNetwork);
        UpdateIconState("Videos", progression.HasKey(GameKey.NetworkKey), progression.CurrentObjective == ObjectiveId.RecoverVideoEvidence);
        UpdateIconState("Recycle Bin", progression.HasKey(GameKey.VideosKey), progression.CurrentObjective == ObjectiveId.SearchRecycleBin);
        UpdateIconState("Tetris", true, false);

        SetHotspotVisible(_treeHotspot, progression.IsLocationUnlocked(LocationId.TreeScene));
        SetHotspotVisible(_cityHotspot, progression.IsLocationUnlocked(LocationId.CityScene));
        SetHotspotVisible(_balloonHotspot, progression.IsLocationUnlocked(LocationId.BalloonScene));

        if (progression.HasUnseenObjectivePopup())
        {
            ShowSystemToast("Objective Updated", progression.GetObjectivePopupMessage());
            progression.AcknowledgeCurrentObjectivePopup();
        }
    }

    private void UpdateIconState(string iconName, bool isEnabled, bool isObjective)
    {
        if (!_iconMap.TryGetValue(iconName, out VisualElement icon) || icon == null)
        {
            return;
        }

        icon.EnableInClassList("desktop-icon-wrapper--locked", !isEnabled);
        icon.EnableInClassList("desktop-icon-wrapper--objective", isObjective);
        icon.SetEnabled(isEnabled || iconName == "Tetris");
    }

    private void SetHotspotVisible(VisualElement hotspot, bool isVisible)
    {
        if (hotspot == null)
        {
            return;
        }

        hotspot.EnableInClassList("hidden", !isVisible);
        hotspot.SetEnabled(isVisible);
    }

    private void PlayClickSound()
    {
        if (uiClickSound != null)
        {
            AudioManager.Instance.PlayUISFX(uiClickSound, 0.4f);
        }
    }

    private void OnShutdownButtonHover(PointerOverEvent evt)
    {
        if (_shutdownButton != null && _startMenu != null && !_startMenu.ClassListContains("hidden"))
        {
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

    private IEnumerator PlayDesktopAppearChimeOnce()
    {
        if (_hasPlayedDesktopAppearChime || desktopAppearChime == null)
        {
            yield break;
        }

        yield return null;

        AudioManager.Instance.PlayUISFX(desktopAppearChime, desktopAppearVolume);
        _hasPlayedDesktopAppearChime = true;

        yield return new WaitForSeconds(desktopAppearChime.length + 1.5f);

        if (ambientDesktopSound != null)
        {
            AudioManager.Instance.PlayAmbient(ambientDesktopSound, true, 0f);
            yield return StartCoroutine(FadeInAmbient());
        }
    }

    private IEnumerator FadeInAmbient()
    {
        float elapsed = 0f;
        while (elapsed < ambientFadeDuration)
        {
            elapsed += Time.deltaTime;
            AudioManager.Instance.SetAmbientVolume(Mathf.Lerp(0f, ambientVolume, elapsed / ambientFadeDuration));
            yield return null;
        }

        AudioManager.Instance.SetAmbientVolume(ambientVolume);
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
        if (_startMenu == null)
        {
            return;
        }

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

    private void OnMainAreaClicked(ClickEvent evt)
    {
        if (_startMenu != null && !_startMenu.ClassListContains("hidden"))
        {
            _startMenu.AddToClassList("hidden");
        }
    }

    private void OnIconClicked(VisualElement icon)
    {
        var allIcons = _root.Query<VisualElement>(className: "desktop-icon-wrapper").ToList();
        foreach (VisualElement other in allIcons)
        {
            other.RemoveFromClassList("desktop-icon-wrapper--selected");
        }

        Label label = icon.Q<Label>(className: "desktop-icon-label");
        string iconName = label != null ? label.text : "Unknown Icon";
        icon.AddToClassList("desktop-icon-wrapper--selected");

        switch (iconName)
        {
            case "Documents":
                if (!IsIconEnabled(iconName))
                {
                    ShowSystemToast("Recovery Incomplete", "This program is not available yet.");
                    break;
                }

                if (_documentsController != null)
                {
                    _documentsController.Show();
                }
                else
                {
                    ShowSystemToast("Module Missing", "Documents window is not wired into the desktop yet.");
                }
                break;
            case "Pictures":
                HandleProgramLaunch("PicturesMiniGame", IsIconEnabled(iconName));
                break;
            case "Music":
                HandleProgramLaunch("MusicMiniGame", IsIconEnabled(iconName));
                break;
            case "Computer":
                HandleProgramLaunch("ComputerMiniGame", IsIconEnabled(iconName));
                break;
            case "Control Panel":
                HandleProgramLaunch("ControlPanelMiniGame", IsIconEnabled(iconName));
                break;
            case "Network":
                HandleProgramLaunch("NetworkMiniGame", IsIconEnabled(iconName));
                break;
            case "Videos":
                HandleProgramLaunch("VideosMiniGame", IsIconEnabled(iconName));
                break;
            case "Recycle Bin":
                HandleProgramLaunch("RecycleBinMiniGame", IsIconEnabled(iconName));
                break;
            case "Tetris":
                if (_tetrisController != null)
                {
                    _tetrisController.Show();
                }
                break;
            default:
                Debug.Log($"Desktop Icon Clicked: {iconName}");
                break;
        }
    }

    private bool IsIconEnabled(string iconName)
    {
        return !_iconMap.TryGetValue(iconName, out VisualElement icon) || icon == null || !icon.ClassListContains("desktop-icon-wrapper--locked");
    }

    private void HandleProgramLaunch(string sceneName, bool canLaunch)
    {
        if (!canLaunch)
        {
            ShowSystemToast("Recovery Incomplete", "This program is not available yet.");
            return;
        }

        TryLoadScene(sceneName);
    }

    private bool TryLoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return true;
        }

        Debug.Log($"[Desktop] Scene '{sceneName}' is not available yet.");
        ShowSystemToast("Module Missing", $"Scene '{sceneName}' has not been added yet.");
        return false;
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
        if (_root == null)
        {
            return;
        }

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
            {
                _glassOverlay.style.backgroundImage = new StyleBackground(brokenGlassSprite);
            }

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
            {
                icon.style.backgroundImage = new StyleBackground(phantomChatIcon);
            }

            _chatText = new Label();
            _chatText.AddToClassList("phantom-chat-text");
            _chatText.pickingMode = PickingMode.Ignore;
            _chatWindow.Add(icon);
            _chatWindow.Add(_chatText);
            _root.Add(_chatWindow);
        }
    }

    private void ShowSystemToast(string title, string message)
    {
        if (_toast == null || _toastTitle == null || _toastBody == null)
        {
            return;
        }

        StopCoroutine(nameof(HideToastRoutine));
        _toastTitle.text = title;
        _toastBody.text = message;
        _toast.AddToClassList("desktop-toast--visible");
        StartCoroutine(HideToastRoutine());
    }

    private IEnumerator HideToastRoutine()
    {
        yield return new WaitForSeconds(4f);
        if (_toast != null)
        {
            _toast.RemoveFromClassList("desktop-toast--visible");
        }
    }

    private IEnumerator UiAnomalyRoutine()
    {
        while (isActiveAndEnabled)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(minUiAnomalyInterval, maxUiAnomalyInterval));
            if (_desktopAnomaliesPaused)
            {
                continue;
            }

            yield return StartCoroutine(TriggerUiAnomaly(UnityEngine.Random.Range(0, 8)));
        }
    }

    private bool IsDesktopAppWindowOpen()
    {
        return (_documentsController != null && _documentsController.IsWindowOpen)
            || (_tetrisController != null && _tetrisController.IsWindowOpen);
    }

    private void SetDesktopAnomaliesPaused(bool isPaused)
    {
        _desktopAnomaliesPaused = isPaused;

        if (isPaused)
        {
            if (_uiAnomalyRoutine != null)
            {
                StopCoroutine(_uiAnomalyRoutine);
                _uiAnomalyRoutine = null;
            }

            ClearDesktopAnomalyVisuals();
            return;
        }

        if (_uiAnomalyRoutine == null && isActiveAndEnabled)
        {
            _uiAnomalyRoutine = StartCoroutine(UiAnomalyRoutine());
        }
    }

    private void ClearDesktopAnomalyVisuals()
    {
        StopCoroutine(nameof(HideToastRoutine));
        if (_toast != null)
        {
            _toast.RemoveFromClassList("desktop-toast--visible");
        }

        if (_fakeWindow != null)
        {
            _fakeWindow.RemoveFromClassList("desktop-fake-window--visible");
        }

        if (_glassOverlay != null)
        {
            _glassOverlay.RemoveFromClassList("glass-overlay--active");
        }

        if (_chatWindow != null)
        {
            _chatWindow.RemoveFromClassList("phantom-chat--visible");
        }

        if (_clockLabel != null)
        {
            _clockLabel.RemoveFromClassList("tray-clock--glitch");
            _clockLabel.text = _stableClockText;
        }

        if (_mainArea != null)
        {
            _mainArea.RemoveFromClassList("desktop-main-area--glitch");
            _mainArea.style.translate = new Translate(0f, 0f, 0f);
        }

        foreach (VisualElement icon in _iconMap.Values)
        {
            if (icon == null)
            {
                continue;
            }

            icon.RemoveFromClassList("desktop-icon-wrapper--haunted");
            icon.style.translate = new Translate(0f, 0f, 0f);
        }

        ResetShutdownButton();
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
        if (_glassOverlay == null)
        {
            yield break;
        }

        if (glassCrackSound != null)
        {
            AudioManager.Instance.PlaySFX(glassCrackSound, 0.6f);
        }
        else
        {
            PlayUiAnomalyCue(0.3f);
        }

        _glassOverlay.AddToClassList("glass-overlay--active");
        yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 5f));
        _glassOverlay.RemoveFromClassList("glass-overlay--active");
    }

    private IEnumerator PhantomChatAnomaly()
    {
        if (_chatWindow == null)
        {
            yield break;
        }

        string[] messages = { "Are you still there?", "I remember you.", "Don't look behind.", "System error: Help." };
        _chatText.text = messages[UnityEngine.Random.Range(0, messages.Length)];
        _chatWindow.AddToClassList("phantom-chat--visible");
        PlayUiAnomalyCue(0.1f);
        yield return new WaitForSeconds(4f);
        _chatWindow.RemoveFromClassList("phantom-chat--visible");
    }

    private IEnumerator HauntedIconAnomaly()
    {
        List<VisualElement> icons = _root.Query<VisualElement>(className: "desktop-icon-wrapper").ToList();
        if (icons.Count == 0)
        {
            yield break;
        }

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
        if (_toast == null)
        {
            yield break;
        }

        _toastTitle.text = "AeroOS Security";
        _toastBody.text = _playerName + ", your deleted files are trying to return.";
        _toast.AddToClassList("desktop-toast--visible");
        PlayUiAnomalyCue(0.14f);
        yield return new WaitForSeconds(4f);
        _toast.RemoveFromClassList("desktop-toast--visible");
    }

    private IEnumerator ClockCorruptionAnomaly()
    {
        if (_clockLabel == null)
        {
            yield break;
        }

        string[] glitchTimes = { "7 MISSED", "03:33 AM", "CALL HOME", "--:--" };
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
        if (_mainArea == null)
        {
            yield break;
        }

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
        if (_fakeWindow == null)
        {
            yield break;
        }

        _fakeWindowTitle.text = "Session Recovery";
        _fakeWindowBody.text = "Recovered fragment for " + _playerName + ".\nDo you remember closing the last window?";
        _fakeWindow.AddToClassList("desktop-fake-window--visible");
        PlayUiAnomalyCue(0.15f);
        yield return new WaitForSeconds(3.5f);
        _fakeWindow.RemoveFromClassList("desktop-fake-window--visible");
    }

    private void PlayUiAnomalyCue(float volume)
    {
        if (uiAnomalyClip != null)
        {
            AudioManager.Instance.PlaySFX(uiAnomalyClip, volume);
        }
    }
}
