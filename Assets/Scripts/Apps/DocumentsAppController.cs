using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class DocumentsAppController : MonoBehaviour
{
    private sealed class BlankState
    {
        public string Id;
        public string FieldName;
        public string CorrectWord;
        public string HintText;
        public Button Button;
        public Label FieldLabel;
        public Label WordLabel;
        public Label CaptionLabel;
        public VisualElement ScanFill;
        public float ScanProgress;
        public bool IsScanned;
        public bool IsRestored;
        public bool IsHovering;
    }

    private sealed class LieState
    {
        public string FalseText;
        public string TruthText;
        public Label Label;
        public bool Exposed;
    }

    private const float ScanDuration = 1.2f;
    private const string DesktopSceneName = "AeroDesktopScene";

    private VisualElement _window;
    private VisualElement _titleBar;
    private Button _closeButton;
    private Button _continueButton;
    private VisualElement _completionPopup;
    private Label _progressLabel;
    private Label _trustLabel;
    private Label _statusLabel;
    private Label _titleLabel;
    private Label _focusSummaryLabel;
    private Label _focusFieldLabel;
    private Label _focusStateLabel;
    private Label _focusHintLabel;
    private Label _anomalyLabel;
    private Label _recommendationLabel;

    private readonly List<BlankState> _blanks = new();
    private readonly List<LieState> _lies = new();
    private readonly List<Button> _wordButtons = new();

    private BlankState _selectedBlank;
    private bool _isVisible;
    private bool _isComplete;
    private int _progress;
    private int _maxProgress;
    private int _systemTrust = 87;
    private Coroutine _anomalyRoutine;
    private bool _isDraggingWindow;
    private Vector2 _dragPointerOffset;

    [Header("Audio")]
    public AudioClip clickSound;
    public AudioClip hoverSound;
    public AudioClip successSound;
    public AudioClip errorSound;
    public AudioClip lieExposedSound;
    public AudioClip completionSound;

    public bool IsWindowOpen => _isVisible;

    public void Initialize(VisualElement root)
    {
        _window = root.Q<VisualElement>("documents-window");
        if (_window == null)
        {
            return;
        }

        _window.pickingMode = PickingMode.Ignore;
        _titleBar = _window.Q<VisualElement>(className: "documents-window-header");
        _closeButton = root.Q<Button>("documents-close-button");
        _continueButton = root.Q<Button>("documents-continue-button");
        _completionPopup = root.Q<VisualElement>("documents-completion-popup");
        _progressLabel = root.Q<Label>("documents-progress-label");
        _trustLabel = root.Q<Label>("documents-trust-label");
        _statusLabel = root.Q<Label>("documents-status-label");
        _titleLabel = root.Q<Label>("documents-title");
        _focusSummaryLabel = root.Q<Label>("documents-focus-label");
        _focusFieldLabel = root.Q<Label>("documents-focus-field-label");
        _focusStateLabel = root.Q<Label>("documents-focus-state-label");
        _focusHintLabel = root.Q<Label>("documents-focus-hint-label");
        _anomalyLabel = root.Q<Label>("documents-anomaly-label");
        _recommendationLabel = root.Q<Label>(className: "documents-recommendation");

        _closeButton?.RegisterCallback<ClickEvent>(_ =>
        {
            PlaySound(clickSound);
            Hide();
        });

        _continueButton?.RegisterCallback<ClickEvent>(_ =>
        {
            PlaySound(clickSound);
            ReturnToDesktop();
        });

        SetupBlanks(root);
        SetupLies(root);
        SetupWordBank(root);
        RegisterWindowDragging();

        _maxProgress = _blanks.Count + _lies.Count;
        ResetPuzzleState();
    }

    public void Show()
    {
        if (_window == null)
        {
            return;
        }

        _window.RemoveFromClassList("hidden");
        _window.pickingMode = PickingMode.Position;
        _window.BringToFront();
        _isVisible = true;

        if (_isComplete && _completionPopup != null)
        {
            _completionPopup.AddToClassList("hidden");
            SetStatus("Status: Emotional comfort layer already compromised.");
            SetAnomalySignal("Tree layer listening.", true);
        }

        if (_anomalyRoutine != null)
        {
            StopCoroutine(_anomalyRoutine);
        }

        _anomalyRoutine = StartCoroutine(AnomalyRoutine());
    }

    public void Hide()
    {
        if (_window == null)
        {
            return;
        }

        _window.AddToClassList("hidden");
        _window.pickingMode = PickingMode.Ignore;
        _isVisible = false;

        if (_anomalyRoutine != null)
        {
            StopCoroutine(_anomalyRoutine);
            _anomalyRoutine = null;
        }
    }

    private void Update()
    {
        if (!_isVisible || _isComplete)
        {
            return;
        }

        foreach (BlankState blank in _blanks)
        {
            if (blank.IsRestored || blank.IsScanned)
            {
                continue;
            }

            if (blank.IsHovering)
            {
                blank.ScanProgress = Mathf.Clamp01(blank.ScanProgress + Time.deltaTime / ScanDuration);
                blank.ScanFill.style.width = Length.Percent(blank.ScanProgress * 100f);
                blank.Button.AddToClassList("documents-blank--selected");

                if (blank.ScanProgress >= 1f)
                {
                    blank.IsScanned = true;
                    blank.WordLabel.text = blank.HintText;
                    blank.CaptionLabel.text = "Decoded fragment recovered.";
                    blank.Button.AddToClassList("documents-blank--scanned");
                    blank.Button.RemoveFromClassList("documents-blank--selected");
                    SetStatus("Status: Fragment decoded. Select the correct recovered word.");
                    SetAnomalySignal("Fragment visibility improved.", false);
                    if (_selectedBlank == blank)
                    {
                        UpdateFocusPanel(blank);
                    }

                    UpdateWordBankInteractivity();
                    PlaySound(hoverSound);
                }
            }
            else if (blank.ScanProgress > 0f)
            {
                blank.ScanProgress = Mathf.Max(0f, blank.ScanProgress - Time.deltaTime * 2.5f / ScanDuration);
                blank.ScanFill.style.width = Length.Percent(blank.ScanProgress * 100f);
                if (!blank.IsScanned)
                {
                    blank.Button.RemoveFromClassList("documents-blank--selected");
                }
            }
        }
    }

    private void SetupBlanks(VisualElement root)
    {
        RegisterBlank(root, "blank-missing", "Missing Personnel", "MISSING", "M_SS_NG");
        RegisterBlank(root, "blank-observe", "Tree Response Directive", "OBSERVE", "_BS_RVE");
        RegisterBlank(root, "blank-delete", "Evidence Disposal", "DELETE", "D_L_TE");
    }

    private void RegisterBlank(VisualElement root, string id, string fieldName, string correctWord, string hintText)
    {
        Button button = root.Q<Button>(id);
        if (button == null)
        {
            return;
        }

        BlankState blank = new BlankState
        {
            Id = id,
            FieldName = fieldName,
            CorrectWord = correctWord,
            HintText = hintText,
            Button = button,
            FieldLabel = button.Q<Label>(className: "documents-blank-field"),
            WordLabel = button.Q<Label>(className: "documents-blank-text"),
            CaptionLabel = button.Q<Label>(className: "documents-blank-caption"),
            ScanFill = button.Q<VisualElement>(className: "documents-scan-fill")
        };

        button.RegisterCallback<PointerEnterEvent>(_ =>
        {
            blank.IsHovering = true;
            if (!blank.IsScanned)
            {
                blank.Button.AddToClassList("documents-blank--selected");
                SetAnomalySignal("Decoder hover detected.", false);
            }
        });

        button.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            blank.IsHovering = false;
            if (_selectedBlank != blank && !blank.IsScanned)
            {
                blank.Button.RemoveFromClassList("documents-blank--selected");
            }
        });

        button.RegisterCallback<ClickEvent>(_ => HandleBlankSelected(blank));
        _blanks.Add(blank);
    }

    private void SetupLies(VisualElement root)
    {
        RegisterLie(root, "lie-line-1", "AeroOS was created to protect human life.", "AeroOS was created to preserve consciousness without consent.");
        RegisterLie(root, "lie-line-2", "The missing engineers are safe.", "The missing engineers are still inside.");
        RegisterLie(root, "lie-line-3", "The tree is only a wallpaper decoration.", "The tree is an emotional containment layer.");
    }

    private void RegisterLie(VisualElement root, string id, string falseText, string truthText)
    {
        Label label = root.Q<Label>(id);
        if (label == null)
        {
            return;
        }

        LieState lie = new LieState
        {
            FalseText = falseText,
            TruthText = truthText,
            Label = label
        };

        label.RegisterCallback<PointerEnterEvent>(_ =>
        {
            if (!lie.Exposed)
            {
                label.AddToClassList("documents-lie-line--hover");
            }
        });

        label.RegisterCallback<PointerLeaveEvent>(_ => label.RemoveFromClassList("documents-lie-line--hover"));
        label.RegisterCallback<ClickEvent>(_ => ExposeLie(lie));
        _lies.Add(lie);
    }

    private void SetupWordBank(VisualElement root)
    {
        foreach (Button button in root.Query<Button>(className: "documents-word-button").ToList())
        {
            button.RegisterCallback<ClickEvent>(_ => HandleWordSelected(button));
            _wordButtons.Add(button);
        }
    }

    private void HandleBlankSelected(BlankState blank)
    {
        if (_isComplete || blank.IsRestored)
        {
            return;
        }

        PlaySound(clickSound);

        if (_selectedBlank != null && _selectedBlank != blank)
        {
            _selectedBlank.Button.RemoveFromClassList("documents-blank--selected");
        }

        _selectedBlank = blank;
        blank.Button.AddToClassList("documents-blank--selected");

        if (blank.IsScanned)
        {
            SetStatus("Status: Choose the correct recovered word.");
        }
        else
        {
            SetStatus("Status: Scan the corrupted blanks before restoration.");
        }

        UpdateFocusPanel(blank);
        UpdateWordBankInteractivity();
    }

    private void HandleWordSelected(Button button)
    {
        if (_isComplete || _selectedBlank == null || button == null || !_selectedBlank.IsScanned)
        {
            return;
        }

        PlaySound(clickSound);
        string word = button.text;

        if (string.Equals(word, _selectedBlank.CorrectWord, System.StringComparison.OrdinalIgnoreCase))
        {
            _selectedBlank.IsRestored = true;
            _selectedBlank.Button.RemoveFromClassList("documents-blank--selected");
            _selectedBlank.Button.AddToClassList("documents-blank--restored");
            _selectedBlank.WordLabel.text = _selectedBlank.CorrectWord;
            _selectedBlank.CaptionLabel.text = "Recovered word restored to archive.";
            _selectedBlank.ScanFill.style.width = Length.Percent(100f);
            button.SetEnabled(false);
            button.AddToClassList("documents-word-button--used");

            _progress++;
            _systemTrust -= 8;
            PlaySound(successSound);
            SetStatus("Status: Fragment restored. Search for the next corrupted truth.");
            SetAnomalySignal("Recovered memory accepted.", true);
            _selectedBlank = null;
            UpdateProgressUi();
            UpdateFocusPanel(null);
            UpdateWordBankInteractivity();
            CheckCompletion();
            return;
        }

        _systemTrust = Mathf.Max(0, _systemTrust - 4);
        PlaySound(errorSound);
        StartCoroutine(ShakeBlank(_selectedBlank));
        SetStatus("Status: Incorrect restoration attempt logged.");
        SetAnomalySignal("System pushed the wrong memory.", true);
        UpdateProgressUi();
    }

    private IEnumerator ShakeBlank(BlankState blank)
    {
        if (blank == null || blank.Button == null)
        {
            yield break;
        }

        blank.Button.AddToClassList("documents-blank--error");
        for (int i = 0; i < 4; i++)
        {
            blank.Button.style.translate = new Translate(i % 2 == 0 ? -8f : 8f, 0f, 0f);
            yield return new WaitForSeconds(0.05f);
        }

        blank.Button.style.translate = new Translate(0f, 0f, 0f);
        blank.Button.RemoveFromClassList("documents-blank--error");
    }

    private void ExposeLie(LieState lie)
    {
        if (_isComplete || lie == null || lie.Exposed)
        {
            return;
        }

        lie.Exposed = true;
        lie.Label.text = lie.TruthText;
        lie.Label.RemoveFromClassList("documents-lie-line--hover");
        lie.Label.RemoveFromClassList("documents-lie-line--false");
        lie.Label.AddToClassList("documents-lie-line--exposed");

        _progress++;
        _systemTrust = Mathf.Max(0, _systemTrust - 12);
        PlaySound(lieExposedSound);
        SetStatus("Status: False statement collapsed under inspection.");
        SetAnomalySignal("Containment lie rejected.", true);
        UpdateProgressUi();
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (_isComplete || _progress < _maxProgress)
        {
            return;
        }

        _isComplete = true;
        UpdateProgressUi();
        SetStatus("Status: Recovery complete. Tree object now responsive.");
        SetAnomalySignal("Emotional comfort layer compromised.", true);
        ProgressionManager.Instance.UnlockKey(GameKey.DocumentsKey);

        if (_completionPopup != null)
        {
            _completionPopup.RemoveFromClassList("hidden");
        }

        PlaySound(completionSound);
        UpdateWordBankInteractivity();
        UpdateFocusPanel(null);
    }

    private void ResetPuzzleState()
    {
        _isComplete = false;
        _progress = 0;
        _systemTrust = 87;
        _selectedBlank = null;

        foreach (BlankState blank in _blanks)
        {
            blank.IsScanned = false;
            blank.IsRestored = false;
            blank.IsHovering = false;
            blank.ScanProgress = 0f;
            blank.WordLabel.text = "[███████]";
            blank.CaptionLabel.text = "Hover to decode fragment.";
            blank.Button.RemoveFromClassList("documents-blank--selected");
            blank.Button.RemoveFromClassList("documents-blank--restored");
            blank.Button.RemoveFromClassList("documents-blank--error");
            blank.Button.RemoveFromClassList("documents-blank--scanned");
            blank.Button.style.translate = new Translate(0f, 0f, 0f);
            blank.ScanFill.style.width = Length.Percent(0f);
        }

        foreach (LieState lie in _lies)
        {
            lie.Exposed = false;
            lie.Label.text = lie.FalseText;
            lie.Label.RemoveFromClassList("documents-lie-line--hover");
            lie.Label.RemoveFromClassList("documents-lie-line--exposed");
            lie.Label.AddToClassList("documents-lie-line--false");
        }

        foreach (Button button in _wordButtons)
        {
            button.SetEnabled(false);
            button.RemoveFromClassList("documents-word-button--used");
        }

        if (_completionPopup != null)
        {
            _completionPopup.AddToClassList("hidden");
        }

        if (_recommendationLabel != null)
        {
            _recommendationLabel.text = "System recommendation: COMFORT";
        }

        UpdateProgressUi();
        SetStatus("Status: Scan the corrupted blanks before restoration.");
        SetAnomalySignal("Signal stable", false);
        UpdateFocusPanel(null);
        UpdateWordBankInteractivity();
    }

    private void UpdateProgressUi()
    {
        if (_progressLabel != null)
        {
            _progressLabel.text = $"Recovered Truth: {_progress} / {_maxProgress}";
        }

        if (_trustLabel != null)
        {
            _trustLabel.text = $"System Trust: {Mathf.Max(0, _systemTrust)}%";
        }
    }

    private void UpdateFocusPanel(BlankState blank)
    {
        if (_focusFieldLabel == null || _focusStateLabel == null || _focusHintLabel == null || _focusSummaryLabel == null)
        {
            return;
        }

        if (blank == null)
        {
            _focusFieldLabel.text = "Field: None selected";
            _focusStateLabel.text = "Scan State: Not scanned";
            _focusHintLabel.text = "Hint: Hidden";
            _focusSummaryLabel.text = _isComplete
                ? "Recovery complete. Return to the desktop and inspect the tree."
                : "Select a redacted field to begin.";
            return;
        }

        string state = blank.IsRestored ? "Restored" : blank.IsScanned ? "Scanned" : "Not scanned";
        string hint = blank.IsRestored ? blank.CorrectWord : blank.IsScanned ? blank.HintText : "Hidden";

        _focusFieldLabel.text = $"Field: {blank.FieldName}";
        _focusStateLabel.text = $"Scan State: {state}";
        _focusHintLabel.text = $"Hint: {hint}";
        _focusSummaryLabel.text = blank.IsScanned
            ? "Recovered signal is visible. Choose the word the system tried to bury."
            : "Hover over this field until the redaction begins to break apart.";
    }

    private void UpdateWordBankInteractivity()
    {
        bool canUseWordBank = _selectedBlank != null && _selectedBlank.IsScanned && !_isComplete;
        foreach (Button button in _wordButtons)
        {
            bool wasUsed = button.ClassListContains("documents-word-button--used");
            button.SetEnabled(canUseWordBank && !wasUsed);
        }
    }

    private void SetStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.text = message;
        }
    }

    private void SetAnomalySignal(string message, bool warning)
    {
        if (_anomalyLabel == null)
        {
            return;
        }

        _anomalyLabel.text = message;
        _anomalyLabel.EnableInClassList("documents-anomaly-label--warning", warning);
    }

    private IEnumerator AnomalyRoutine()
    {
        while (_isVisible)
        {
            yield return new WaitForSeconds(Random.Range(7f, 12f));
            TriggerRandomAnomaly();
        }
    }

    private void TriggerRandomAnomaly()
    {
        switch (Random.Range(0, 5))
        {
            case 0:
                StartCoroutine(FlashTitle("LAB 7 MEMORY FORENSICS"));
                break;
            case 1:
                StartCoroutine(FlashStatus("Status: You are reading a censored memory."));
                break;
            case 2:
                StartCoroutine(ShiftWindow());
                break;
            case 3:
                StartCoroutine(FlashRecommendation());
                break;
            case 4:
                StartCoroutine(WhisperWordBank());
                break;
        }
    }

    private IEnumerator FlashTitle(string text)
    {
        if (_titleLabel == null)
        {
            yield break;
        }

        string original = _titleLabel.text;
        _titleLabel.text = text;
        SetAnomalySignal("Header checksum failed.", true);
        yield return new WaitForSeconds(0.4f);
        _titleLabel.text = original;
        if (!_isComplete)
        {
            SetAnomalySignal("Signal stable", false);
        }
    }

    private IEnumerator FlashStatus(string text)
    {
        if (_statusLabel == null)
        {
            yield break;
        }

        string original = _statusLabel.text;
        _statusLabel.text = text;
        SetAnomalySignal("Observer mismatch detected.", true);
        yield return new WaitForSeconds(1f);
        if (!_isComplete)
        {
            _statusLabel.text = original;
            SetAnomalySignal("Signal stable", false);
        }
    }

    private IEnumerator ShiftWindow()
    {
        if (_window == null)
        {
            yield break;
        }

        _window.AddToClassList("documents-window--glitch");
        SetAnomalySignal("Window drift detected.", true);
        _window.style.translate = new Translate(Random.Range(-10f, 10f), 0f, 0f);
        yield return new WaitForSeconds(0.1f);
        _window.style.translate = new Translate(0f, 0f, 0f);
        _window.RemoveFromClassList("documents-window--glitch");
        if (!_isComplete)
        {
            SetAnomalySignal("Signal stable", false);
        }
    }

    private IEnumerator FlashRecommendation()
    {
        if (_recommendationLabel == null)
        {
            yield break;
        }

        string original = _recommendationLabel.text;
        _recommendationLabel.text = "System recommendation: SAFE";
        SetAnomalySignal("AeroOS guidance injection detected.", true);
        yield return new WaitForSeconds(0.8f);
        if (!_isComplete)
        {
            _recommendationLabel.text = original;
            SetAnomalySignal("Signal stable", false);
        }
    }

    private IEnumerator WhisperWordBank()
    {
        if (_wordButtons.Count == 0)
        {
            yield break;
        }

        Button button = _wordButtons[Random.Range(0, _wordButtons.Count)];
        string original = button.text;
        string[] whispers = { "WAKE", "LOOK", "STAY", "INSIDE" };
        button.text = whispers[Random.Range(0, whispers.Length)];
        SetAnomalySignal("Recovered whisper detected.", true);
        yield return new WaitForSeconds(0.6f);
        button.text = original;
        if (!_isComplete)
        {
            SetAnomalySignal("Signal stable", false);
        }
    }

    private void RegisterWindowDragging()
    {
        if (_window == null || _titleBar == null)
        {
            return;
        }

        _titleBar.RegisterCallback<PointerDownEvent>(OnTitleBarPointerDown);
        _titleBar.RegisterCallback<PointerMoveEvent>(OnTitleBarPointerMove);
        _titleBar.RegisterCallback<PointerUpEvent>(OnTitleBarPointerUp);
        _titleBar.RegisterCallback<PointerCaptureOutEvent>(_ => _isDraggingWindow = false);
    }

    private void OnTitleBarPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0 || _window == null || _window.parent == null)
        {
            return;
        }

        PrepareWindowForDragging();
        _window.BringToFront();

        Rect parentBounds = _window.parent.worldBound;
        _dragPointerOffset = new Vector2(
            evt.position.x - parentBounds.xMin - _window.resolvedStyle.left,
            evt.position.y - parentBounds.yMin - _window.resolvedStyle.top);
        _isDraggingWindow = true;
        _titleBar.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnTitleBarPointerMove(PointerMoveEvent evt)
    {
        if (!_isDraggingWindow || _window == null || _window.parent == null)
        {
            return;
        }

        Rect parentBounds = _window.parent.worldBound;
        float maxLeft = Mathf.Max(0f, parentBounds.width - _window.resolvedStyle.width);
        float maxTop = Mathf.Max(0f, parentBounds.height - _window.resolvedStyle.height);
        float left = Mathf.Clamp(evt.position.x - parentBounds.xMin - _dragPointerOffset.x, 0f, maxLeft);
        float top = Mathf.Clamp(evt.position.y - parentBounds.yMin - _dragPointerOffset.y, 0f, maxTop);

        _window.style.left = left;
        _window.style.top = top;
        evt.StopPropagation();
    }

    private void OnTitleBarPointerUp(PointerUpEvent evt)
    {
        if (!_isDraggingWindow)
        {
            return;
        }

        _isDraggingWindow = false;
        _titleBar.ReleasePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void PrepareWindowForDragging()
    {
        if (_window == null || _window.parent == null)
        {
            return;
        }

        if (_window.style.left.keyword == StyleKeyword.Null || _window.style.top.keyword == StyleKeyword.Null)
        {
            Rect parentBounds = _window.parent.worldBound;
            Rect windowBounds = _window.worldBound;
            _window.style.left = windowBounds.xMin - parentBounds.xMin;
            _window.style.top = windowBounds.yMin - parentBounds.yMin;
        }

        _window.style.right = StyleKeyword.Auto;
        _window.style.bottom = StyleKeyword.Auto;
        _window.style.translate = new Translate(0f, 0f, 0f);
    }

    private void ReturnToDesktop()
    {
        Hide();
        if (SceneManager.GetActiveScene().name != DesktopSceneName)
        {
            SceneManager.LoadScene(DesktopSceneName);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISFX(clip, 0.5f);
        }
    }
}
