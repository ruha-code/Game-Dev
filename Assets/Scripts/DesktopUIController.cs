using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class DesktopUIController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenuScene";

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

    [Header("Scene Transition")]
    [SerializeField] private float hotspotTransitionDuration = 1.1f;
    [SerializeField] private Color hotspotTransitionColor = new Color(0.01f, 0.03f, 0.05f, 1f);
    [SerializeField, Range(1f, 1.35f)] private float treeTransitionZoom = 1.12f;
    [SerializeField] private Vector2 treeTransitionPan = new Vector2(-90f, -42f);
    [SerializeField] private float treeTransitionGlitchLead = 0.28f;

    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement _startButton;
    private Button _shutdownButton;
    private VisualElement _startMenu;
    private Label _clockLabel;
    private VisualElement _clockExpandedPanel;
    private Label _clockPanelTime;
    private Label _clockPanelDay;
    private Label _clockPanelDate;
    private VisualElement _calendarGrid;
    private VisualElement _desktopBackground;
    private VisualElement _mainArea;
    private VisualElement _wallpaperHotspots;
    private Label _startMenuUserName;
    private DocumentsAppController _documentsController;
    private TetrisController _tetrisController;
    private RecycleBinAppController _recycleBinController;
    private bool _hasPlayedDesktopAppearChime;
    private string _playerName;
    private string _stableClockText;
    private Coroutine _uiAnomalyRoutine;
    private bool _desktopAnomaliesPaused;
    private VisualElement _toast;
    private Label _toastTitle;
    private Label _toastBody;
    private VisualElement _fakeWindow;
    private VisualElement _fakeWindowHeader;
    private VisualElement _fakeWindowIcon;
    private VisualElement _fakeWindowHeaderText;
    private Label _fakeWindowTitle;
    private Label _fakeWindowStatus;
    private VisualElement _fakeWindowProgressTrack;
    private VisualElement _fakeWindowProgressFill;
    private Label _fakeWindowBody;
    private VisualElement _fakeWindowModule;
    private VisualElement _fakeWindowActions;
    private Button _fakeWindowPrimaryButton;
    private Button _fakeWindowSecondaryButton;
    private VisualElement _glassOverlay;
    private VisualElement _sceneTransitionOverlay;
    private VisualElement _chatWindow;
    private Label _chatText;
    private Label _objectiveText;
    private Button _treeHotspot;
    private Button _cityHotspot;
    private Button _balloonHotspot;
    private Coroutine _sceneTransitionRoutine;
    private bool _isSceneTransitionInProgress;
    private float _lastShutdownRequestTime = -99f;
    private bool _desktopMemoryHintShown;
    private Coroutine _fakeWindowRoutine;

    private sealed class FakeProgramWindowData
    {
        public string Title;
        public string Status;
        public string Body;
        public string[] StatusFrames;
        public string[] BodyLines;
        public string ThemeClass;
        public string IconClass;
        public bool TriggerBackgroundGlitch;
        public bool TriggerGlassFlash;
        public float ProgressValue = 0.72f;
        public string[] DetailItems;
        public string PrimaryButtonText;
        public string SecondaryButtonText;
        public string FollowupToastTitle;
        public string FollowupToastBody;
    }

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
        ProgressionManager.Instance.LoadProgress();
        ProgressionManager.Instance.MarkPlayableSaveAvailable();
        _ = AudioManager.Instance;

        _documentsController = GetComponent<DocumentsAppController>();
        _tetrisController = GetComponent<TetrisController>();
        _recycleBinController = GetComponent<RecycleBinAppController>();
        if (_recycleBinController == null)
        {
            _recycleBinController = gameObject.AddComponent<RecycleBinAppController>();
        }
        _playerName = PlayerPrefs.GetString("PlayerName", "User");
        _root = _uiDocument.rootVisualElement;

        _startButton = _root.Q<VisualElement>("start-button");
        _startMenu = _root.Q<VisualElement>("start-menu");
        _shutdownButton = _root.Q<Button>(className: "start-menu-shutdown-btn");
        _clockLabel = _root.Q<Label>("tray-clock");
        _clockExpandedPanel = _root.Q<VisualElement>("clock-expanded-panel");
        _clockPanelTime = _root.Q<Label>("clock-panel-time");
        _clockPanelDay = _root.Q<Label>("clock-panel-day");
        _clockPanelDate = _root.Q<Label>("clock-panel-date");
        _calendarGrid = _root.Q<VisualElement>("calendar-grid");
        _desktopBackground = _root.Q<VisualElement>("background");

        _mainArea = _root.Q<VisualElement>("main-area");
        _wallpaperHotspots = _root.Q<VisualElement>("wallpaper-hotspots");
        _startMenuUserName = _root.Q<Label>(className: "start-menu-user-name");
        _objectiveText = _root.Q<Label>("objective-text");
        _treeHotspot = _root.Q<Button>("tree-hotspot");
        _cityHotspot = _root.Q<Button>("city-hotspot");
        _balloonHotspot = _root.Q<Button>("balloon-hotspot");

        EnsureAnomalyUi();
        ApplyPersonalization();
        CacheDesktopIcons();
        EnsureHotspotsAreClickable();

        if (_documentsController != null)
        {
            _documentsController.Initialize(_root);
        }

        if (_tetrisController != null)
        {
            _tetrisController.Initialize(_root);
        }

        if (_recycleBinController != null)
        {
            _recycleBinController.Initialize(_root);
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

        if (_clockLabel != null)
        {
            _clockLabel.RegisterCallback<ClickEvent>(OnClockClicked);
            _clockLabel.RegisterCallback<ClickEvent>(_ => PlayClickSound());
        }

        if (_clockExpandedPanel != null)
        {
            _clockExpandedPanel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        }

        if (_shutdownButton != null)
        {
            _shutdownButton.RegisterCallback<PointerOverEvent>(OnShutdownButtonHover);
            _shutdownButton.RegisterCallback<ClickEvent>(OnShutdownClicked);
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

        RegisterStartMenuShortcut("Documents", "Documents");
        RegisterStartMenuShortcut("Pictures", "Pictures");
        RegisterStartMenuShortcut("Music", "Music");
        RegisterStartMenuShortcut("Control Panel", "Control Panel");
        RegisterStartMenuShortcut("Internet Explorer", "Computer");
        RegisterStartMenuShortcut("E-mail", "Music");
    }

    private void RegisterHotspots()
    {
        RegisterHotspot(_treeHotspot, LocationId.TreeScene, "Park", smoothTransition: true);
        RegisterHotspot(_cityHotspot, LocationId.CityScene, "CityScene");
        RegisterHotspot(_balloonHotspot, LocationId.BalloonScene, "BalloonScene");
    }

    private void RegisterHotspot(Button hotspot, LocationId locationId, string sceneName, bool smoothTransition = false)
    {
        if (hotspot == null)
        {
            return;
        }

        hotspot.clicked += () =>
        {
            if (_isSceneTransitionInProgress)
            {
                return;
            }

            PlayClickSound();

            if (smoothTransition)
            {
                StartSceneTransition(sceneName, locationId);
                return;
            }

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

    private void EnsureHotspotsAreClickable()
    {
        if (_wallpaperHotspots == null)
        {
            return;
        }

        _wallpaperHotspots.pickingMode = PickingMode.Ignore;
        _wallpaperHotspots.BringToFront();

        _treeHotspot.pickingMode = PickingMode.Position;
        _cityHotspot.pickingMode = PickingMode.Position;
        _balloonHotspot.pickingMode = PickingMode.Position;
        _treeHotspot?.BringToFront();
        _cityHotspot?.BringToFront();
        _balloonHotspot?.BringToFront();
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
        UpdateIconState("Tetris", progression.HasKey(GameKey.DocumentsKey), progression.CurrentObjective == ObjectiveId.RecoverTreeMemory && !progression.TetrisRewardClaimed);
        UpdateIconState("Recycle Bin", progression.TetrisRewardClaimed, progression.CurrentObjective == ObjectiveId.SearchRecycleBin && !progression.HasKey(GameKey.RecycleBinKey));
        UpdateIconState("Computer", progression.HasKey(GameKey.ComputerKey), progression.CurrentObjective == ObjectiveId.AccessComputer && !progression.HasKey(GameKey.ComputerKey));
        UpdateIconState("Pictures", progression.HasKey(GameKey.DocumentsKey), false);
        UpdateIconState("Music", progression.HasKey(GameKey.DocumentsKey), false);
        UpdateIconState("Control Panel", progression.HasKey(GameKey.ComputerKey), false);
        UpdateIconState("Network", progression.HasKey(GameKey.ComputerKey), false);
        UpdateIconState("Videos", progression.HasKey(GameKey.ComputerKey), false);

        SetHotspotVisible(_treeHotspot, progression.IsLocationUnlocked(LocationId.TreeScene));
        SetHotspotVisible(_cityHotspot, progression.IsLocationUnlocked(LocationId.CityScene));
        SetHotspotVisible(_balloonHotspot, progression.IsLocationUnlocked(LocationId.BalloonScene));

        if (_treeHotspot != null)
        {
            bool treeIsObjective = progression.CurrentObjective == ObjectiveId.InvestigateTree && progression.IsLocationUnlocked(LocationId.TreeScene);
            _treeHotspot.text = treeIsObjective ? "Click Tree To Enter Park" : "Tree Anomaly";
            _treeHotspot.EnableInClassList("wallpaper-hotspot--objective", treeIsObjective);
            _treeHotspot.tooltip = treeIsObjective ? "The park trace is active here." : "An unstable trace in the wallpaper.";
        }

        if (progression.HasUnseenObjectivePopup())
        {
            ShowSystemToast("Objective Updated", progression.GetObjectivePopupMessage());
            progression.AcknowledgeCurrentObjectivePopup();
        }

        TryShowDesktopMemoryHint(progression);
    }

    private void UpdateIconState(string iconName, bool isEnabled, bool isObjective)
    {
        if (!_iconMap.TryGetValue(iconName, out VisualElement icon) || icon == null)
        {
            return;
        }

        icon.EnableInClassList("desktop-icon-wrapper--locked", !isEnabled);
        icon.EnableInClassList("desktop-icon-wrapper--objective", isObjective);
        icon.SetEnabled(true);
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
            _stableClockText = DateTime.Now.ToString("HH:mm");
            _clockLabel.text = _stableClockText;
        }

        if (_clockExpandedPanel != null && !_clockExpandedPanel.ClassListContains("hidden"))
        {
            DateTime now = DateTime.Now;
            if (_clockPanelTime != null) _clockPanelTime.text = now.ToString("HH:mm:ss");
            if (_clockPanelDay != null) _clockPanelDay.text = now.ToString("dddd");
            if (_clockPanelDate != null) _clockPanelDate.text = now.ToString("d MMMM yyyy");
        }
    }

    private void OnShutdownClicked(ClickEvent evt)
    {
        PlayClickSound();
        evt.StopPropagation();

        _lastShutdownRequestTime = Time.unscaledTime;
        ResetShutdownButton();
        if (_startMenu != null)
        {
            _startMenu.AddToClassList("hidden");
        }

        ShowSystemToast("Shutting Down", "AeroOS is closing the current session...");
        StartCoroutine(ReturnToMainMenuRoutine());
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

            if (_clockExpandedPanel != null && !_clockExpandedPanel.ClassListContains("hidden"))
            {
                _clockExpandedPanel.AddToClassList("hidden");
            }
        }
        else
        {
            _startMenu.AddToClassList("hidden");
        }

        evt.StopPropagation();
    }

    private void OnClockClicked(ClickEvent evt)
    {
        if (_clockExpandedPanel == null)
        {
            return;
        }

        bool isHidden = _clockExpandedPanel.ClassListContains("hidden");
        if (isHidden)
        {
            _clockExpandedPanel.RemoveFromClassList("hidden");
            PopulateCalendar();
            UpdateClock();

            if (_startMenu != null && !_startMenu.ClassListContains("hidden"))
            {
                _startMenu.AddToClassList("hidden");
            }
        }
        else
        {
            _clockExpandedPanel.AddToClassList("hidden");
        }

        evt.StopPropagation();
    }

    private void OnMainAreaClicked(ClickEvent evt)
    {
        if (_startMenu != null && !_startMenu.ClassListContains("hidden"))
        {
            _startMenu.AddToClassList("hidden");
        }

        if (_clockExpandedPanel != null && !_clockExpandedPanel.ClassListContains("hidden"))
        {
            _clockExpandedPanel.AddToClassList("hidden");
        }
    }

    private void PopulateCalendar()
    {
        if (_calendarGrid == null)
        {
            return;
        }

        _calendarGrid.Clear();

        DateTime now = DateTime.Now;
        DateTime firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
        int startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

        DateTime prevMonth = firstDayOfMonth.AddMonths(-1);
        int daysInPrevMonth = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
        for (int i = startDayOfWeek - 1; i >= 0; i--)
        {
            Label dayLabel = new Label((daysInPrevMonth - i).ToString());
            dayLabel.AddToClassList("calendar-day");
            dayLabel.AddToClassList("calendar-day--other-month");
            _calendarGrid.Add(dayLabel);
        }

        for (int i = 1; i <= daysInMonth; i++)
        {
            Label dayLabel = new Label(i.ToString());
            dayLabel.AddToClassList("calendar-day");
            if (i == now.Day)
            {
                dayLabel.AddToClassList("calendar-day--current");
            }

            _calendarGrid.Add(dayLabel);
        }

        int totalCells = _calendarGrid.childCount;
        int nextMonthDay = 1;
        while (totalCells % 7 != 0 || totalCells < 42)
        {
            Label dayLabel = new Label(nextMonthDay.ToString());
            dayLabel.AddToClassList("calendar-day");
            dayLabel.AddToClassList("calendar-day--other-month");
            _calendarGrid.Add(dayLabel);
            nextMonthDay++;
            totalCells++;
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
        HandleDesktopEntry(iconName);
    }

    private void HandleDesktopEntry(string entryName)
    {
        switch (entryName)
        {
            case "Documents":
                if (!IsIconEnabled(entryName))
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
                HandleProgramLaunch(entryName, "PicturesMiniGame", IsIconEnabled(entryName));
                break;
            case "Music":
                HandleProgramLaunch(entryName, "MusicMiniGame", IsIconEnabled(entryName));
                break;
            case "Computer":
                HandleProgramLaunch(entryName, "ComputerMiniGame", IsIconEnabled(entryName));
                break;
            case "Control Panel":
                HandleProgramLaunch(entryName, "ControlPanelMiniGame", IsIconEnabled(entryName));
                break;
            case "Network":
                HandleProgramLaunch(entryName, "NetworkMiniGame", IsIconEnabled(entryName));
                break;
            case "Videos":
                HandleProgramLaunch(entryName, "VideosMiniGame", IsIconEnabled(entryName));
                break;
            case "Recycle Bin":
                if (!IsIconEnabled(entryName))
                {
                    ShowSystemToast("Recovery Incomplete", "The Recycle Bin is still sealed behind the Tetris fragment.");
                    break;
                }

                if (_recycleBinController != null)
                {
                    _recycleBinController.Show();
                }
                break;
            case "Tetris":
                if (!IsIconEnabled(entryName))
                {
                    ShowSystemToast("Recovery Incomplete", "Documents must be restored before Tetris becomes usable.");
                    break;
                }

                if (_tetrisController != null)
                {
                    _tetrisController.Show();
                }
                break;
            default:
                Debug.Log($"Desktop Entry Clicked: {entryName}");
                break;
        }
    }

    private void RegisterStartMenuShortcut(string labelText, string desktopEntryName)
    {
        if (_root == null)
        {
            return;
        }

        List<Label> labels = _root.Query<Label>().ToList();
        foreach (Label label in labels)
        {
            if (label == null || label.text != labelText)
            {
                continue;
            }

            if (!label.ClassListContains("start-menu-right-item") && !label.ClassListContains("start-menu-item-label"))
            {
                continue;
            }

            label.RegisterCallback<ClickEvent>(evt =>
            {
                PlayClickSound();
                HandleDesktopEntry(desktopEntryName);
                if (_startMenu != null)
                {
                    _startMenu.AddToClassList("hidden");
                }
                evt.StopPropagation();
            });

            VisualElement clickableTarget = label.parent;
            if (clickableTarget != null)
            {
                clickableTarget.RegisterCallback<ClickEvent>(evt =>
                {
                    PlayClickSound();
                    HandleDesktopEntry(desktopEntryName);
                    if (_startMenu != null)
                    {
                        _startMenu.AddToClassList("hidden");
                    }
                    evt.StopPropagation();
                });
            }
        }
    }

    private bool IsIconEnabled(string iconName)
    {
        return !_iconMap.TryGetValue(iconName, out VisualElement icon) || icon == null || !icon.ClassListContains("desktop-icon-wrapper--locked");
    }

    private void HandleProgramLaunch(string iconName, string sceneName, bool canLaunch)
    {
        if (!canLaunch)
        {
            TriggerLockedProgramEasterEgg(iconName);
            return;
        }

        TryLoadScene(sceneName);
    }

    private void TriggerLockedProgramEasterEgg(string iconName)
    {
        FakeProgramWindowData data = null;

        switch (iconName)
        {
            case "Music":
                data = new FakeProgramWindowData
                {
                    Title = "Music Library",
                    Status = "Signal Recovery: Partial",
                    Body = "Playlist index restored.\nTrack 01: 'Last Voice Memo'\nTrack 02: metadata replaced with breathing static.",
                    StatusFrames = new[] { "Indexing cached tracks...", "Decoding analogue hiss...", "Signal Recovery: Partial" },
                    BodyLines = new[] { "Playlist index restored.", "Track 01: 'Last Voice Memo'", "Track 02: metadata replaced with breathing static." },
                    ThemeClass = "desktop-fake-window--echo",
                    IconClass = "desktop-fake-window-icon--music",
                    TriggerBackgroundGlitch = true,
                    ProgressValue = 0.58f,
                    DetailItems = new[] { "Track 01  | Last Voice Memo      | 03:33", "Track 02  | static_breathing     | 00:47", "Track 03  | [filename corrupted] | --:--" },
                    PrimaryButtonText = "Scan Tracks",
                    SecondaryButtonText = "Mute",
                    FollowupToastTitle = "Audio Trace",
                    FollowupToastBody = "AeroOS muted the damaged playlist before it could auto-play."
                };
                break;
            case "Computer":
                data = new FakeProgramWindowData
                {
                    Title = "My Computer",
                    Status = "Filesystem Integrity: Unstable",
                    Body = "Drive C: responds with one damaged sector.\nA hidden directory appears as /employees/final_session and vanishes before AeroOS can open it.",
                    StatusFrames = new[] { "Mounting local volumes...", "Repairing orphaned sectors...", "Filesystem Integrity: Unstable" },
                    BodyLines = new[] { "Drive C: responds with one damaged sector.", "A hidden directory appears as /employees/final_session", "It vanishes before AeroOS can open it." },
                    ThemeClass = "desktop-fake-window--critical",
                    IconClass = "desktop-fake-window-icon--computer",
                    TriggerBackgroundGlitch = true,
                    ProgressValue = 0.41f,
                    DetailItems = new[] { "C:\\  Healthy", "D:\\  Missing label", "/employees/final_session  ACCESS DENIED" },
                    PrimaryButtonText = "Retry Scan",
                    SecondaryButtonText = "Close",
                    FollowupToastTitle = "Disk Response",
                    FollowupToastBody = "The hidden directory dropped one clue, then hid itself again."
                };
                break;
            case "Network":
                data = new FakeProgramWindowData
                {
                    Title = "Network Connections",
                    Status = "External Access: Offline",
                    Body = "No external network detected.\nOne internal node keeps pinging from 'LAB-7/echo' with impossible round-trip times.",
                    StatusFrames = new[] { "Refreshing adapters...", "Loopback anomaly detected...", "External Access: Offline" },
                    BodyLines = new[] { "No external network detected.", "One internal node keeps pinging from 'LAB-7/echo'.", "Round-trip times exceed the clock itself." },
                    ThemeClass = "desktop-fake-window--echo",
                    IconClass = "desktop-fake-window-icon--network",
                    ProgressValue = 0.33f,
                    DetailItems = new[] { "Loopback      127.0.0.1      stable", "LAB-7/echo    0.0.0.0        impossible", "Gateway       unavailable    silent" },
                    PrimaryButtonText = "Trace Node",
                    SecondaryButtonText = "Ignore",
                    FollowupToastTitle = "Network Trace",
                    FollowupToastBody = "Trace cancelled. The internal ping moved before AeroOS could isolate it."
                };
                break;
            case "Control Panel":
                data = new FakeProgramWindowData
                {
                    Title = "Control Panel",
                    Status = "Administrative Lockout",
                    Body = "System settings are locked by policy.\nFlagged administrator override detected at 03:33.\nThe approval name has been scrubbed.",
                    StatusFrames = new[] { "Loading policy map...", "Reading override ledger...", "Administrative Lockout" },
                    BodyLines = new[] { "System settings are locked by policy.", "Flagged administrator override detected at 03:33.", "The approval name has been scrubbed." },
                    ThemeClass = "desktop-fake-window--warning",
                    IconClass = "desktop-fake-window-icon--control",
                    ProgressValue = 0.86f,
                    DetailItems = new[] { "Policy: ADMIN_OVERRIDE.lock", "Timestamp: 03:33", "Owner: [scrubbed by AeroOS]" },
                    PrimaryButtonText = "View Policy",
                    SecondaryButtonText = "Back",
                    FollowupToastTitle = "Policy Viewer",
                    FollowupToastBody = "The override record is there, but the owner field is blank."
                };
                break;
            case "Videos":
                data = new FakeProgramWindowData
                {
                    Title = "Video Archive",
                    Status = "Playback Blocked",
                    Body = "Recovered thumbnails show an office corridor.\nEvery clip ends one second before someone enters frame.\nOne filename keeps renaming itself to YOU_WERE_HERE.",
                    StatusFrames = new[] { "Collecting thumbnails...", "Buffering protected footage...", "Playback Blocked" },
                    BodyLines = new[] { "Recovered thumbnails show an office corridor.", "Every clip ends one second before someone enters frame.", "One filename keeps renaming itself to YOU_WERE_HERE." },
                    ThemeClass = "desktop-fake-window--critical",
                    IconClass = "desktop-fake-window-icon--videos",
                    TriggerGlassFlash = true,
                    ProgressValue = 0.67f,
                    DetailItems = new[] { "corridor_cam_01.mp4    protected", "elevator_lobby.avi     ends early", "YOU_WERE_HERE.mov      self-renaming" },
                    PrimaryButtonText = "Preview",
                    SecondaryButtonText = "Delete",
                    FollowupToastTitle = "Preview Error",
                    FollowupToastBody = "Video playback was denied. A single frame was marked as protected evidence."
                };
                break;
            case "Pictures":
                data = new FakeProgramWindowData
                {
                    Title = "Pictures",
                    Status = "Gallery Restored: 6/7",
                    Body = "Six family-safe wallpapers restored.\nOne extra image exists, but AeroOS refuses to preview it.\nIts capture date is tomorrow.",
                    StatusFrames = new[] { "Restoring gallery cache...", "Sorting capture dates...", "Gallery Restored: 6/7" },
                    BodyLines = new[] { "Six family-safe wallpapers restored.", "One extra image exists, but AeroOS refuses to preview it.", "Its capture date is tomorrow." },
                    ThemeClass = "desktop-fake-window--warning",
                    IconClass = "desktop-fake-window-icon--pictures",
                    ProgressValue = 0.74f,
                    DetailItems = new[] { "Wallpaper_01.jpg     restored", "Wallpaper_02.jpg     restored", "capture_tomorrow.png quarantined" },
                    PrimaryButtonText = "Open Gallery",
                    SecondaryButtonText = "Skip",
                    FollowupToastTitle = "Gallery Warning",
                    FollowupToastBody = "The hidden picture remains quarantined behind a broken timestamp."
                };
                break;
            default:
                ShowSystemToast("Recovery Incomplete", "This program is not available yet.");
                break;
        }

        if (data != null)
        {
            ShowFakeProgramWindow(data);
        }

        PlayUiAnomalyCue(0.1f);
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

    private void StartSceneTransition(string sceneName, LocationId locationId)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.Log($"[Desktop] Scene '{sceneName}' is not available yet.");
            ShowSystemToast("Module Missing", $"Scene '{sceneName}' has not been added yet.");
            return;
        }

        if (_sceneTransitionRoutine != null)
        {
            StopCoroutine(_sceneTransitionRoutine);
        }

        _sceneTransitionRoutine = StartCoroutine(TransitionToScene(sceneName, locationId));
    }

    private IEnumerator TransitionToScene(string sceneName, LocationId locationId)
    {
        _isSceneTransitionInProgress = true;
        SetDesktopAnomaliesPaused(true);
        ClearDesktopAnomalyVisuals();

        if (_sceneTransitionOverlay != null)
        {
            _sceneTransitionOverlay.style.display = DisplayStyle.Flex;
            _sceneTransitionOverlay.BringToFront();
        }

        float elapsed = 0f;
        while (elapsed < hotspotTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / hotspotTransitionDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            float zoomProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.7f));
            float glitchProgress = Mathf.Clamp01((progress - treeTransitionGlitchLead) / Mathf.Max(0.01f, 1f - treeTransitionGlitchLead));

            ApplyTreeTransitionVisuals(zoomProgress, glitchProgress);

            if (_sceneTransitionOverlay != null)
            {
                _sceneTransitionOverlay.style.opacity = easedProgress;
            }

            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.SetAmbientVolume(Mathf.Lerp(ambientVolume, 0f, easedProgress));
            }

            yield return null;
        }

        ProgressionManager.Instance.MarkLocationVisited(locationId);
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        if (loadOperation == null)
        {
            _isSceneTransitionInProgress = false;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    private void ApplyTreeTransitionVisuals(float zoomProgress, float glitchProgress)
    {
        if (_desktopBackground != null)
        {
            float scale = Mathf.Lerp(1f, treeTransitionZoom, zoomProgress);
            _desktopBackground.style.scale = new Scale(new Vector2(scale, scale));
            _desktopBackground.style.translate = new Translate(
                Mathf.Lerp(0f, treeTransitionPan.x, zoomProgress),
                Mathf.Lerp(0f, treeTransitionPan.y, zoomProgress),
                0f);
        }

        if (_mainArea != null)
        {
            bool shouldGlitch = glitchProgress > 0.12f;
            _mainArea.EnableInClassList("desktop-main-area--glitch", shouldGlitch);
            _mainArea.style.opacity = Mathf.Lerp(1f, 0.25f, zoomProgress);
            _mainArea.style.translate = new Translate(
                Mathf.Sin(Time.unscaledTime * 60f) * 10f * glitchProgress,
                Mathf.Cos(Time.unscaledTime * 48f) * 6f * glitchProgress,
                0f);
        }

        if (_wallpaperHotspots != null)
        {
            _wallpaperHotspots.style.scale = new Scale(new Vector2(Mathf.Lerp(1f, 1.05f, zoomProgress), Mathf.Lerp(1f, 1.05f, zoomProgress)));
            _wallpaperHotspots.style.opacity = Mathf.Lerp(1f, 0.78f, glitchProgress);
        }

        if (_treeHotspot != null)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 22f) * 0.04f * glitchProgress;
            _treeHotspot.style.scale = new Scale(new Vector2(pulse, pulse));
            _treeHotspot.style.opacity = Mathf.Lerp(1f, 0.55f, glitchProgress);
        }

        if (_objectiveText != null)
        {
            _objectiveText.style.opacity = Mathf.Lerp(1f, 0.35f, glitchProgress);
        }

        if (_clockLabel != null)
        {
            _clockLabel.EnableInClassList("tray-clock--glitch", glitchProgress > 0.2f);
        }

        if (_glassOverlay != null)
        {
            _glassOverlay.EnableInClassList("glass-overlay--active", glitchProgress > 0.78f);
            _glassOverlay.style.opacity = Mathf.Lerp(0f, 0.9f, Mathf.Clamp01((glitchProgress - 0.78f) / 0.22f));
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
            _fakeWindow.pickingMode = PickingMode.Position;
            _fakeWindowHeader = new VisualElement();
            _fakeWindowHeader.AddToClassList("desktop-fake-window-header");
            _fakeWindowIcon = new VisualElement();
            _fakeWindowIcon.AddToClassList("desktop-fake-window-icon");
            _fakeWindowHeaderText = new VisualElement();
            _fakeWindowHeaderText.AddToClassList("desktop-fake-window-header-text");
            _fakeWindowTitle = new Label();
            _fakeWindowTitle.AddToClassList("desktop-fake-window-title");
            _fakeWindowTitle.pickingMode = PickingMode.Position;
            _fakeWindowStatus = new Label();
            _fakeWindowStatus.AddToClassList("desktop-fake-window-status");
            _fakeWindowStatus.pickingMode = PickingMode.Position;
            _fakeWindowProgressTrack = new VisualElement();
            _fakeWindowProgressTrack.AddToClassList("desktop-fake-window-progress-track");
            _fakeWindowProgressFill = new VisualElement();
            _fakeWindowProgressFill.AddToClassList("desktop-fake-window-progress-fill");
            _fakeWindowProgressTrack.Add(_fakeWindowProgressFill);
            _fakeWindowBody = new Label();
            _fakeWindowBody.AddToClassList("desktop-fake-window-body");
            _fakeWindowBody.pickingMode = PickingMode.Position;
            _fakeWindowModule = new VisualElement();
            _fakeWindowModule.AddToClassList("desktop-fake-window-module");
            _fakeWindowActions = new VisualElement();
            _fakeWindowActions.AddToClassList("desktop-fake-window-actions");
            _fakeWindowPrimaryButton = new Button();
            _fakeWindowPrimaryButton.AddToClassList("desktop-fake-window-button");
            _fakeWindowSecondaryButton = new Button();
            _fakeWindowSecondaryButton.AddToClassList("desktop-fake-window-button");
            _fakeWindowSecondaryButton.AddToClassList("desktop-fake-window-button--secondary");
            _fakeWindowHeaderText.Add(_fakeWindowTitle);
            _fakeWindowHeaderText.Add(_fakeWindowStatus);
            _fakeWindowHeader.Add(_fakeWindowIcon);
            _fakeWindowHeader.Add(_fakeWindowHeaderText);
            _fakeWindow.Add(_fakeWindowHeader);
            _fakeWindow.Add(_fakeWindowProgressTrack);
            _fakeWindow.Add(_fakeWindowBody);
            _fakeWindow.Add(_fakeWindowModule);
            _fakeWindowActions.Add(_fakeWindowPrimaryButton);
            _fakeWindowActions.Add(_fakeWindowSecondaryButton);
            _fakeWindow.Add(_fakeWindowActions);
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

        if (_sceneTransitionOverlay == null)
        {
            _sceneTransitionOverlay = new VisualElement
            {
                name = "desktop-scene-transition-overlay",
                pickingMode = PickingMode.Ignore
            };
            _sceneTransitionOverlay.style.position = Position.Absolute;
            _sceneTransitionOverlay.style.left = 0f;
            _sceneTransitionOverlay.style.top = 0f;
            _sceneTransitionOverlay.style.right = 0f;
            _sceneTransitionOverlay.style.bottom = 0f;
            _sceneTransitionOverlay.style.backgroundColor = new StyleColor(hotspotTransitionColor);
            _sceneTransitionOverlay.style.opacity = 0f;
            _sceneTransitionOverlay.style.display = DisplayStyle.None;
            _root.Add(_sceneTransitionOverlay);
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

    private void ShowFakeWindow(string title, string body, float visibleDuration = 3.6f)
    {
        if (_fakeWindow == null || _fakeWindowTitle == null || _fakeWindowBody == null)
        {
            ShowSystemToast(title, body);
            return;
        }

        if (_fakeWindowRoutine != null)
        {
            StopCoroutine(_fakeWindowRoutine);
            _fakeWindowRoutine = null;
        }

        _fakeWindowTitle.text = title;
        if (_fakeWindowStatus != null)
        {
            _fakeWindowStatus.text = "Session Fragment";
        }
        _fakeWindowBody.text = body;
        if (_fakeWindowPrimaryButton != null)
        {
            _fakeWindowPrimaryButton.text = "Dismiss";
            _fakeWindowPrimaryButton.clicked -= OnFakeWindowButtonClicked;
            _fakeWindowPrimaryButton.clicked += OnFakeWindowButtonClicked;
        }
        if (_fakeWindowSecondaryButton != null)
        {
            _fakeWindowSecondaryButton.text = "Close";
            _fakeWindowSecondaryButton.clicked -= OnFakeWindowButtonClicked;
            _fakeWindowSecondaryButton.clicked += OnFakeWindowButtonClicked;
        }
        _fakeWindow.AddToClassList("desktop-fake-window--visible");
        _fakeWindowRoutine = StartCoroutine(HideFakeWindowRoutine(visibleDuration));
    }

    private void ShowFakeProgramWindow(FakeProgramWindowData data)
    {
        if (data == null)
        {
            return;
        }

        if (_fakeWindow == null || _fakeWindowTitle == null || _fakeWindowBody == null)
        {
            ShowSystemToast(data.Title, data.Body);
            return;
        }

        if (_fakeWindowRoutine != null)
        {
            StopCoroutine(_fakeWindowRoutine);
            _fakeWindowRoutine = null;
        }

        _fakeWindowTitle.text = data.Title;
        if (_fakeWindowStatus != null)
        {
            _fakeWindowStatus.text = string.Empty;
        }
        if (_fakeWindowIcon != null)
        {
            _fakeWindowIcon.RemoveFromClassList("desktop-fake-window-icon--music");
            _fakeWindowIcon.RemoveFromClassList("desktop-fake-window-icon--computer");
            _fakeWindowIcon.RemoveFromClassList("desktop-fake-window-icon--network");
            _fakeWindowIcon.RemoveFromClassList("desktop-fake-window-icon--control");
            _fakeWindowIcon.RemoveFromClassList("desktop-fake-window-icon--videos");
            _fakeWindowIcon.RemoveFromClassList("desktop-fake-window-icon--pictures");
            if (!string.IsNullOrWhiteSpace(data.IconClass))
            {
                _fakeWindowIcon.AddToClassList(data.IconClass);
            }
        }
        if (_fakeWindowProgressFill != null)
        {
            _fakeWindowProgressFill.style.width = Length.Percent(0f);
        }
        _fakeWindowBody.text = string.Empty;
        if (_fakeWindowModule != null)
        {
            _fakeWindowModule.Clear();
        }
        _fakeWindow.RemoveFromClassList("desktop-fake-window--warning");
        _fakeWindow.RemoveFromClassList("desktop-fake-window--critical");
        _fakeWindow.RemoveFromClassList("desktop-fake-window--echo");
        if (!string.IsNullOrWhiteSpace(data.ThemeClass))
        {
            _fakeWindow.AddToClassList(data.ThemeClass);
        }

        if (_fakeWindowPrimaryButton != null)
        {
            _fakeWindowPrimaryButton.text = string.IsNullOrWhiteSpace(data.PrimaryButtonText) ? "Open" : data.PrimaryButtonText;
            _fakeWindowPrimaryButton.userData = data;
            _fakeWindowPrimaryButton.clicked -= OnFakeWindowPrimaryButtonClicked;
            _fakeWindowPrimaryButton.clicked += OnFakeWindowPrimaryButtonClicked;
        }

        if (_fakeWindowSecondaryButton != null)
        {
            _fakeWindowSecondaryButton.text = string.IsNullOrWhiteSpace(data.SecondaryButtonText) ? "Close" : data.SecondaryButtonText;
            _fakeWindowSecondaryButton.userData = data;
            _fakeWindowSecondaryButton.clicked -= OnFakeWindowSecondaryButtonClicked;
            _fakeWindowSecondaryButton.clicked += OnFakeWindowSecondaryButtonClicked;
        }

        _fakeWindow.AddToClassList("desktop-fake-window--visible");
        if (_fakeWindowActions != null)
        {
            _fakeWindowActions.style.display = DisplayStyle.None;
        }
        _fakeWindowRoutine = StartCoroutine(PresentFakeProgramWindowRoutine(data));
    }

    private IEnumerator PresentFakeProgramWindowRoutine(FakeProgramWindowData data)
    {
        if (data.TriggerBackgroundGlitch && _mainArea != null)
        {
            _mainArea.AddToClassList("desktop-main-area--glitch");
        }

        if (data.TriggerGlassFlash && _glassOverlay != null)
        {
            _glassOverlay.AddToClassList("glass-overlay--active");
            _glassOverlay.style.opacity = 0.45f;
        }

        string[] statusFrames = data.StatusFrames != null && data.StatusFrames.Length > 0
            ? data.StatusFrames
            : new[] { data.Status };

        foreach (string frame in statusFrames)
        {
            if (_fakeWindowStatus != null)
            {
                _fakeWindowStatus.text = frame;
            }

            if (_fakeWindowProgressFill != null)
            {
                float frameProgress = (System.Array.IndexOf(statusFrames, frame) + 1f) / statusFrames.Length;
                _fakeWindowProgressFill.style.width = Length.Percent(Mathf.Lerp(18f, data.ProgressValue * 100f, frameProgress));
            }

            if (_fakeWindowTitle != null)
            {
                _fakeWindowTitle.text = UnityEngine.Random.value > 0.65f ? data.Title.ToUpperInvariant() : data.Title;
            }

            yield return new WaitForSeconds(0.32f);
        }

        if (_fakeWindowTitle != null)
        {
            _fakeWindowTitle.text = data.Title;
        }
        if (_fakeWindowProgressFill != null)
        {
            _fakeWindowProgressFill.style.width = Length.Percent(Mathf.Clamp01(data.ProgressValue) * 100f);
        }

        string[] bodyLines = data.BodyLines != null && data.BodyLines.Length > 0
            ? data.BodyLines
            : data.Body.Split('\n');

        string builtBody = string.Empty;
        foreach (string line in bodyLines)
        {
            builtBody = string.IsNullOrEmpty(builtBody) ? line : builtBody + "\n" + line;
            _fakeWindowBody.text = builtBody;
            yield return new WaitForSeconds(0.24f);
        }

        PopulateFakeWindowModule(data);

        if (_fakeWindowActions != null)
        {
            _fakeWindowActions.style.display = DisplayStyle.Flex;
        }

        yield return new WaitForSeconds(5.5f);

        if (_mainArea != null)
        {
            _mainArea.RemoveFromClassList("desktop-main-area--glitch");
        }

        if (_glassOverlay != null)
        {
            _glassOverlay.RemoveFromClassList("glass-overlay--active");
            _glassOverlay.style.opacity = 0f;
        }

        if (_fakeWindow != null)
        {
            _fakeWindow.RemoveFromClassList("desktop-fake-window--visible");
        }

        _fakeWindowRoutine = null;
    }

    private void PopulateFakeWindowModule(FakeProgramWindowData data)
    {
        if (_fakeWindowModule == null)
        {
            return;
        }

        _fakeWindowModule.Clear();
        if (data?.DetailItems == null || data.DetailItems.Length == 0)
        {
            _fakeWindowModule.style.display = DisplayStyle.None;
            return;
        }

        _fakeWindowModule.style.display = DisplayStyle.Flex;
        foreach (string item in data.DetailItems)
        {
            Label row = new Label(item);
            row.AddToClassList("desktop-fake-window-module-row");
            _fakeWindowModule.Add(row);
        }
    }

    private IEnumerator HideToastRoutine()
    {
        yield return new WaitForSeconds(4f);
        if (_toast != null)
        {
            _toast.RemoveFromClassList("desktop-toast--visible");
        }
    }

    private IEnumerator HideFakeWindowRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_fakeWindow != null)
        {
            _fakeWindow.RemoveFromClassList("desktop-fake-window--visible");
        }

        _fakeWindowRoutine = null;
    }

    private void OnFakeWindowButtonClicked()
    {
        if (_fakeWindowRoutine != null)
        {
            StopCoroutine(_fakeWindowRoutine);
            _fakeWindowRoutine = null;
        }

        if (_fakeWindow != null)
        {
            _fakeWindow.RemoveFromClassList("desktop-fake-window--visible");
            _fakeWindow.RemoveFromClassList("desktop-fake-window--warning");
            _fakeWindow.RemoveFromClassList("desktop-fake-window--critical");
            _fakeWindow.RemoveFromClassList("desktop-fake-window--echo");
        }

        if (_fakeWindowIcon != null)
        {
            _fakeWindowIcon.RemoveFromClassList("desktop-fake-window-icon--music");
            _fakeWindowIcon.RemoveFromClassList("desktop-fake-window-icon--computer");
            _fakeWindowIcon.RemoveFromClassList("desktop-fake-window-icon--network");
            _fakeWindowIcon.RemoveFromClassList("desktop-fake-window-icon--control");
            _fakeWindowIcon.RemoveFromClassList("desktop-fake-window-icon--videos");
            _fakeWindowIcon.RemoveFromClassList("desktop-fake-window-icon--pictures");
        }

        if (_fakeWindowActions != null)
        {
            _fakeWindowActions.style.display = DisplayStyle.None;
        }

        if (_fakeWindowModule != null)
        {
            _fakeWindowModule.Clear();
            _fakeWindowModule.style.display = DisplayStyle.None;
        }

        if (_mainArea != null)
        {
            _mainArea.RemoveFromClassList("desktop-main-area--glitch");
        }

        if (_glassOverlay != null)
        {
            _glassOverlay.RemoveFromClassList("glass-overlay--active");
            _glassOverlay.style.opacity = 0f;
        }
    }

    private void OnFakeWindowPrimaryButtonClicked()
    {
        HandleFakeWindowAction(_fakeWindowPrimaryButton);
    }

    private void OnFakeWindowSecondaryButtonClicked()
    {
        HandleFakeWindowAction(_fakeWindowSecondaryButton);
    }

    private void HandleFakeWindowAction(Button sourceButton)
    {
        PlayClickSound();

        if (sourceButton?.userData is FakeProgramWindowData data && !string.IsNullOrWhiteSpace(data.FollowupToastTitle))
        {
            ShowSystemToast(data.FollowupToastTitle, data.FollowupToastBody);
        }

        OnFakeWindowButtonClicked();
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
            || (_tetrisController != null && _tetrisController.IsWindowOpen)
            || (_recycleBinController != null && _recycleBinController.IsWindowOpen);
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
            _glassOverlay.style.opacity = 0f;
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
            _mainArea.style.opacity = 1f;
            _mainArea.style.translate = new Translate(0f, 0f, 0f);
        }

        if (_desktopBackground != null)
        {
            _desktopBackground.style.scale = new Scale(Vector2.one);
            _desktopBackground.style.translate = new Translate(0f, 0f, 0f);
        }

        if (_wallpaperHotspots != null)
        {
            _wallpaperHotspots.style.scale = new Scale(Vector2.one);
            _wallpaperHotspots.style.opacity = 1f;
        }

        if (_treeHotspot != null)
        {
            _treeHotspot.style.scale = new Scale(Vector2.one);
            _treeHotspot.style.opacity = 1f;
        }

        if (_objectiveText != null)
        {
            _objectiveText.style.opacity = 1f;
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
        ShowFakeWindow("Session Recovery", "Recovered fragment for " + _playerName + ".\nDo you remember closing the last window?");
        PlayUiAnomalyCue(0.15f);
        yield return new WaitForSeconds(3.5f);
    }

    private void PlayUiAnomalyCue(float volume)
    {
        if (uiAnomalyClip != null)
        {
            AudioManager.Instance.PlaySFX(uiAnomalyClip, volume);
        }
    }

    private void TryShowDesktopMemoryHint(ProgressionManager progression)
    {
        if (_desktopMemoryHintShown || progression == null)
        {
            return;
        }

        if (progression.HasKey(GameKey.DocumentsKey) && !progression.HasKey(GameKey.ComputerKey))
        {
            _desktopMemoryHintShown = true;
            ShowSystemToast("Wallpaper Memory", "The glass bubbles distort near Pictures and Music, like they are hiding warmer fragments.");
        }
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        yield return new WaitForSeconds(0.8f);

        if (!TryLoadScene(MainMenuSceneName))
        {
            ShowSystemToast("Shutdown Failed", "Main menu scene is missing from the build profile.");
        }
    }
}
