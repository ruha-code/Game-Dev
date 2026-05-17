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
        public Button Button;
        public Label WordLabel;
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
    private Label _anomalyLabel;
    private Label _recommendationLabel;
    private VisualElement _reconstructionContainer;
    private Label _finalMessageLabel;

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
        if (_window == null) return;

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
        _anomalyLabel = root.Q<Label>("documents-anomaly-label");
        _recommendationLabel = root.Q<Label>(className: "documents-recommendation");
        _reconstructionContainer = root.Q<VisualElement>("reconstruction-container");
        _finalMessageLabel = root.Q<Label>("final-reconstructed-message");

        _closeButton?.RegisterCallback<ClickEvent>(_ => { PlaySound(clickSound); Hide(); });
        _continueButton?.RegisterCallback<ClickEvent>(_ => { PlaySound(clickSound); ReturnToDesktop(); });

        SetupBlanks(root);
        SetupLies(root);
        SetupWordBank(root);
        RegisterWindowDragging();

        _maxProgress = _blanks.Count + _lies.Count;
        ResetPuzzleState();
    }

    public void Show()
    {
        if (_window == null) return;
        _window.RemoveFromClassList("hidden");
        _window.pickingMode = PickingMode.Position;
        _window.BringToFront();
        _isVisible = true;

        if (_anomalyRoutine != null) StopCoroutine(_anomalyRoutine);
        _anomalyRoutine = StartCoroutine(AnomalyRoutine());
    }

    public void Hide()
    {
        if (_window == null) return;
        _window.AddToClassList("hidden");
        _window.pickingMode = PickingMode.Ignore;
        _isVisible = false;
        if (_anomalyRoutine != null) { StopCoroutine(_anomalyRoutine); _anomalyRoutine = null; }
    }

    private void SetupBlanks(VisualElement root)
    {
        _blanks.Clear();
        RegisterBlank(root, "blank-colleague", "Личность", "КОЛЛЕГА");
        RegisterBlank(root, "blank-flash", "Событие", "ВСПЫШКА");
        RegisterBlank(root, "blank-inside", "Местоположение", "ВНУТРИ");
        RegisterBlank(root, "blank-exit", "Цель", "ВЫХОД");
        RegisterBlank(root, "blank-memory", "Состояние", "ПАМЯТЬ");
        RegisterBlank(root, "blank-reality", "Природа", "РЕАЛЬНОСТЬ");
        RegisterBlank(root, "blank-family", "Связь", "СЕМЬЯ");
    }

    private void RegisterBlank(VisualElement root, string id, string fieldName, string correctWord)
    {
        Button button = root.Q<Button>(id);
        if (button == null) return;

        BlankState blank = new BlankState
        {
            Id = id,
            FieldName = fieldName,
            CorrectWord = correctWord,
            Button = button,
            WordLabel = button.Q<Label>(className: "reconstruction-blank-label")
        };

        button.RegisterCallback<PointerEnterEvent>(_ => { blank.IsHovering = true; if (!blank.IsRestored) button.AddToClassList("reconstruction-blank--selected"); });
        button.RegisterCallback<PointerLeaveEvent>(_ => { blank.IsHovering = false; if (_selectedBlank != blank) button.RemoveFromClassList("reconstruction-blank--selected"); });
        button.RegisterCallback<ClickEvent>(_ => HandleBlankSelected(blank));
        _blanks.Add(blank);
    }

    private void SetupLies(VisualElement root)
    {
        _lies.Clear();
        RegisterLie(root, "lie-line-1", "AeroOS was created to protect human life.", "AeroOS was created to preserve consciousness without consent.");
        RegisterLie(root, "lie-line-2", "The missing engineers are safe.", "The missing engineers are still inside.");
        RegisterLie(root, "lie-line-3", "The tree is only a wallpaper decoration.", "The tree is an emotional containment layer.");
    }

    private void RegisterLie(VisualElement root, string id, string falseText, string truthText)
    {
        Label label = root.Q<Label>(id);
        if (label == null) return;
        LieState lie = new LieState { FalseText = falseText, TruthText = truthText, Label = label };
        label.RegisterCallback<PointerEnterEvent>(_ => { if (!lie.Exposed) label.AddToClassList("documents-lie-line--hover"); });
        label.RegisterCallback<PointerLeaveEvent>(_ => label.RemoveFromClassList("documents-lie-line--hover"));
        label.RegisterCallback<ClickEvent>(_ => ExposeLie(lie));
        _lies.Add(lie);
    }

    private void SetupWordBank(VisualElement root)
    {
        _wordButtons.Clear();
        VisualElement bank = root.Q<VisualElement>("word-bank");
        if (bank == null) return;

        foreach (Button button in bank.Query<Button>(className: "documents-word-button").ToList())
        {
            button.RegisterCallback<ClickEvent>(_ => HandleWordSelected(button));
            _wordButtons.Add(button);
        }
    }

    private void HandleBlankSelected(BlankState blank)
    {
        if (_isComplete || blank.IsRestored) return;
        PlaySound(clickSound);
        if (_selectedBlank != null && _selectedBlank != blank) _selectedBlank.Button.RemoveFromClassList("reconstruction-blank--selected");
        _selectedBlank = blank;
        blank.Button.AddToClassList("reconstruction-blank--selected");
        SetStatus("Status: Select a word from the bank to assign.");
        UpdateFocusPanel(blank);
        UpdateWordBankInteractivity();
    }

    private void HandleWordSelected(Button button)
    {
        if (_isComplete || _selectedBlank == null || button == null) return;
        PlaySound(clickSound);
        string word = button.text;

        if (string.Equals(word, _selectedBlank.CorrectWord, System.StringComparison.OrdinalIgnoreCase))
        {
            _selectedBlank.IsRestored = true;
            _selectedBlank.Button.RemoveFromClassList("reconstruction-blank--selected");
            _selectedBlank.Button.AddToClassList("reconstruction-blank--restored");
            _selectedBlank.WordLabel.text = _selectedBlank.CorrectWord;
            button.SetEnabled(false);
            button.AddToClassList("documents-word-button--used");

            _progress++;
            _systemTrust -= 5;
            PlaySound(successSound);
            SetStatus("Status: Fragment restored.");
            _selectedBlank = null;
            UpdateProgressUi();
            UpdateFocusPanel(null);
            UpdateWordBankInteractivity();
            CheckCompletion();
        }
        else
        {
            _systemTrust = Mathf.Max(0, _systemTrust - 10);
            PlaySound(errorSound);
            StartCoroutine(ShakeBlank(_selectedBlank));
            SetStatus("Status: Incorrect word assigned.");
            UpdateProgressUi();
        }
    }

    private IEnumerator ShakeBlank(BlankState blank)
    {
        blank.Button.AddToClassList("reconstruction-blank--error");
        for (int i = 0; i < 4; i++) { blank.Button.style.translate = new Translate(i % 2 == 0 ? -10f : 10f, 0f, 0f); yield return new WaitForSeconds(0.04f); }
        blank.Button.style.translate = new Translate(0f, 0f, 0f);
        blank.Button.RemoveFromClassList("reconstruction-blank--error");
    }

    private void ExposeLie(LieState lie)
    {
        if (_isComplete || lie.Exposed) return;
        lie.Exposed = true;
        lie.Label.text = lie.TruthText;
        lie.Label.AddToClassList("documents-lie-line--exposed");
        _progress++;
        _systemTrust = Mathf.Max(0, _systemTrust - 10);
        PlaySound(lieExposedSound);
        UpdateProgressUi();
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (_isComplete || _progress < _maxProgress) return;
        _isComplete = true;
        RevealFinalMessage();
        SetStatus("Status: Recovery complete.");
        ProgressionManager.Instance.UnlockKey(GameKey.DocumentsKey);
        _completionPopup?.RemoveFromClassList("hidden");
        PlaySound(completionSound);
    }

    private void RevealFinalMessage()
    {
        if (_reconstructionContainer == null || _finalMessageLabel == null) return;
        _reconstructionContainer.AddToClassList("hidden");
        _finalMessageLabel.RemoveFromClassList("hidden");
        _finalMessageLabel.text = "7 ноября. Мой КОЛЛЕГА исчез сразу после того, как в системе произошла ВСПЫШКА. Теперь я заперт ВНУТРИ AeroOS. Я пытаюсь найти ВЫХОД, но стены из пикселей не пускают меня. Моя ПАМЯТЬ стирается с каждым циклом. Кажется, РЕАЛЬНОСТЬ превратилась в код. Моя СЕМЬЯ ждет меня, но я всего лишь фрагмент данных.";
    }

    private void ResetPuzzleState()
    {
        _isComplete = false; _progress = 0; _systemTrust = 87; _selectedBlank = null;
        foreach (var b in _blanks) { b.IsRestored = false; b.WordLabel.text = b.Id.Contains("colleague") || b.Id.Contains("inside") || b.Id.Contains("memory") || b.Id.Contains("family") ? "[CORRUPTED]" : "[DATA LOST]"; b.Button.RemoveFromClassList("reconstruction-blank--selected"); b.Button.RemoveFromClassList("reconstruction-blank--restored"); }
        foreach (var l in _lies) { l.Exposed = false; l.Label.text = l.FalseText; l.Label.RemoveFromClassList("documents-lie-line--exposed"); }
        foreach (var btn in _wordButtons) { btn.SetEnabled(true); btn.RemoveFromClassList("documents-word-button--used"); }
        _reconstructionContainer?.RemoveFromClassList("hidden");
        _finalMessageLabel?.AddToClassList("hidden");
        UpdateProgressUi(); UpdateFocusPanel(null); UpdateWordBankInteractivity();
    }

    private void UpdateProgressUi()
    {
        if (_progressLabel != null) _progressLabel.text = $"Recovered Truth: {_progress} / {_maxProgress}";
        if (_trustLabel != null) _trustLabel.text = $"System Trust: {Mathf.Max(0, _systemTrust)}%";
    }

    private void UpdateFocusPanel(BlankState blank)
    {
        if (blank == null) { _focusFieldLabel.text = "Field: None"; _focusSummaryLabel.text = "Reconstruct the colleague's message."; return; }
        _focusFieldLabel.text = $"Target: {blank.FieldName}";
        _focusSummaryLabel.text = "Select the correct Russian word to restore this fragment.";
    }

    private void UpdateWordBankInteractivity()
    {
        bool canUse = _selectedBlank != null && !_isComplete;
        foreach (var btn in _wordButtons) btn.SetEnabled(canUse && !btn.ClassListContains("documents-word-button--used"));
    }

    private void SetStatus(string msg) { if (_statusLabel != null) _statusLabel.text = msg; }
    private void SetAnomalySignal(string msg, bool warn) { if (_anomalyLabel != null) { _anomalyLabel.text = msg; _anomalyLabel.EnableInClassList("documents-anomaly-label--warning", warn); } }
    private IEnumerator AnomalyRoutine() { while (_isVisible) { yield return new WaitForSeconds(Random.Range(10f, 15f)); TriggerRandomAnomaly(); } }

    private void TriggerRandomAnomaly()
    {
        switch (Random.Range(0, 3))
        {
            case 0: StartCoroutine(FlashTitle("SYSTEM ERROR: MEMORY LEAK")); break;
            case 1: StartCoroutine(ShiftWindow()); break;
            case 2: StartCoroutine(FlashStatus("Subject is still screaming.")); break;
        }
    }

    private IEnumerator FlashTitle(string text) { string orig = _titleLabel.text; _titleLabel.text = text; yield return new WaitForSeconds(0.4f); _titleLabel.text = orig; }
    private IEnumerator FlashStatus(string text) { string orig = _statusLabel.text; _statusLabel.text = text; yield return new WaitForSeconds(1f); _statusLabel.text = orig; }
    private IEnumerator ShiftWindow() { _window.AddToClassList("documents-window--glitch"); yield return new WaitForSeconds(0.1f); _window.RemoveFromClassList("documents-window--glitch"); }

    private void RegisterWindowDragging()
    {
        if (_window == null || _titleBar == null) return;
        _titleBar.RegisterCallback<PointerDownEvent>(OnTitleBarPointerDown);
        _titleBar.RegisterCallback<PointerMoveEvent>(OnTitleBarPointerMove);
        _titleBar.RegisterCallback<PointerUpEvent>(OnTitleBarPointerUp);
    }

    private void OnTitleBarPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0 || _window.parent == null) return;
        PrepareWindowForDragging();
        _dragPointerOffset = new Vector2(evt.position.x - _window.parent.worldBound.xMin - _window.resolvedStyle.left, evt.position.y - _window.parent.worldBound.yMin - _window.resolvedStyle.top);
        _isDraggingWindow = true;
        _titleBar.CapturePointer(evt.pointerId);
    }

    private void OnTitleBarPointerMove(PointerMoveEvent evt)
    {
        if (!_isDraggingWindow) return;
        float left = Mathf.Clamp(evt.position.x - _window.parent.worldBound.xMin - _dragPointerOffset.x, 0, _window.parent.worldBound.width - _window.resolvedStyle.width);
        float top = Mathf.Clamp(evt.position.y - _window.parent.worldBound.yMin - _dragPointerOffset.y, 0, _window.parent.worldBound.height - _window.resolvedStyle.height);
        _window.style.left = left; _window.style.top = top;
    }

    private void OnTitleBarPointerUp(PointerUpEvent evt) { if (_isDraggingWindow) { _isDraggingWindow = false; _titleBar.ReleasePointer(evt.pointerId); } }
    private void PrepareWindowForDragging() { if (_window.style.left.keyword == StyleKeyword.Null) { _window.style.left = _window.worldBound.xMin - _window.parent.worldBound.xMin; _window.style.top = _window.worldBound.yMin - _window.parent.worldBound.yMin; } _window.style.right = StyleKeyword.Auto; _window.style.bottom = StyleKeyword.Auto; _window.style.translate = new Translate(0, 0, 0); }
    private void ReturnToDesktop() { Hide(); if (SceneManager.GetActiveScene().name != DesktopSceneName) SceneManager.LoadScene(DesktopSceneName); }
    private void PlaySound(AudioClip clip) { if (clip != null && AudioManager.Instance != null) AudioManager.Instance.PlayUISFX(clip, 0.5f); }
}
