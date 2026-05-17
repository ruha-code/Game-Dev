using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RecycleBinAppController : MonoBehaviour
{
    private sealed class EngineerRecord
    {
        public string Id;
        public string Name;
        public string Role;
        public string DeletedAt;
        public string CorruptionLevel;
        public string ScanSummary;
        public string RestoredSummary;
        public string RecoveredMessage;
        public string ChallengePrompt;
        public string[] ChallengeChoices;
        public int CorrectChoiceIndex;
        public Button EntryButton;
        public Label StateLabel;
        public VisualElement StatusDot;
        public bool IsScanned;
        public bool IsStabilized;
        public bool IsRestored;
    }

    private struct ChoiceData {
        public string Text;
        public bool IsCorrect;
    }

    private VisualElement _window;
    private VisualElement _titleBar;
    private VisualElement _ghostRecord;
    private Button _closeButton;
    private Button _scanButton;
    private Button _restoreButton;
    private Button _stabilizeButton;
    private Button _completeButton;
    private VisualElement _completionPopup;
    private VisualElement _scanProgressFill;
    private VisualElement _restoreProgressFill;
    private VisualElement _popupLayer;
    private Label _progressLabel;
    private Label _statusLabel;
    private Label _hintLabel;
    private Label _selectedNameLabel;
    private Label _selectedRoleLabel;
    private Label _selectedDeletedAtLabel;
    private Label _selectedCorruptionLabel;
    private Label _selectedBodyLabel;
    private Label _recoveredMessageLabel;
    private Label _challengePromptLabel;
    private Label _currentPhaseLabel;
    private Label _finalSummaryLabel;
    private Label _contaminationLabel;
    private Label _suspicionLabel;
    private readonly Button[] _choiceButtons = new Button[3];
    private readonly VisualElement[] _stepChips = new VisualElement[3];

    private readonly List<EngineerRecord> _records = new();
    private readonly List<ChoiceData> _currentChoices = new();
    private readonly List<Label> _profileLabels = new List<Label>();

    private EngineerRecord _selectedRecord;
    private bool _isVisible;
    private bool _isComplete;
    private bool _isActionInProgress;
    private float _contaminationLevel;
    private float _suspicionLevel;
    private AudioSource _humSource;
    private Coroutine _glitchRoutine;
    private Coroutine _popupRoutine;

    private VisualElement _ghostCursor;
    private bool _isGhostCursorActive;
    private bool _isButtonFleeing;

    [Header("Enhanced Audio")]
    public AudioClip clickSound;
    public AudioClip scanSound;
    public AudioClip restoreSound;
    public AudioClip completeSound;
    public AudioClip errorSound;
    public AudioClip glitchSound;
    public AudioClip ambientHum;
    public AudioClip whisperHelpSound;
    public AudioClip scanLoopSound;
    public AudioClip stabilizationSuccessSound;

    public bool IsWindowOpen => _isVisible;

    public void Initialize(VisualElement root)
    {
        _window = root.Q<VisualElement>("recycle-bin-window");
        if (_window == null) return;

        _ghostRecord = root.Q<VisualElement>("recycle-record-ghost");

        // Create ghost cursor
        _ghostCursor = new VisualElement();
        _ghostCursor.AddToClassList("recycle-bin-ghost-cursor");
        _ghostCursor.pickingMode = PickingMode.Ignore;
        _ghostCursor.style.display = DisplayStyle.None;
        _window.Add(_ghostCursor);

        _window.pickingMode = PickingMode.Ignore;
        _titleBar = _window.Q<VisualElement>(className: "recycle-bin-window-header");
        _closeButton = root.Q<Button>("recycle-bin-close-button");
        _scanButton = root.Q<Button>("recycle-bin-scan-button");
        _restoreButton = root.Q<Button>("recycle-bin-restore-button");

        _restoreButton?.RegisterCallback<PointerOverEvent>(_ => {
            if (_suspicionLevel > 60 && Random.value > 0.5f) StartCoroutine(ButtonFleeRoutine());
        });

        _stabilizeButton = root.Q<Button>("recycle-bin-stabilize-button");
        _completeButton = root.Q<Button>("recycle-bin-complete-button");
        _completionPopup = root.Q<VisualElement>("recycle-bin-completion-popup");
        _popupLayer = root.Q<VisualElement>("recycle-bin-popup-layer") ?? _window;

        _progressLabel = root.Q<Label>("recycle-bin-progress-label");
        _statusLabel = root.Q<Label>("recycle-bin-status-label");
        _hintLabel = root.Q<Label>("recycle-bin-hint-label");
        _contaminationLabel = root.Q<Label>("recycle-bin-contamination-label");
        _suspicionLabel = root.Q<Label>("recycle-bin-suspicion-label");

        _selectedNameLabel = root.Q<Label>("recycle-bin-selected-name");
        _selectedRoleLabel = root.Q<Label>("recycle-bin-selected-role");
        _selectedDeletedAtLabel = root.Q<Label>("recycle-bin-selected-deleted-at");
        _selectedCorruptionLabel = root.Q<Label>("recycle-bin-selected-corruption");
        _selectedBodyLabel = root.Q<Label>("recycle-bin-selected-body");
        _recoveredMessageLabel = root.Q<Label>("recycle-bin-recovered-message");
        _challengePromptLabel = root.Q<Label>("recycle-bin-challenge-prompt");
        _currentPhaseLabel = root.Q<Label>("recycle-bin-current-phase");
        _finalSummaryLabel = root.Q<Label>("recycle-bin-final-summary");

        _scanProgressFill = root.Q<VisualElement>("recycle-bin-scan-progress-fill");
        _restoreProgressFill = root.Q<VisualElement>("recycle-bin-restore-progress-fill");

        _choiceButtons[0] = root.Q<Button>("recycle-bin-choice-0");
        _choiceButtons[1] = root.Q<Button>("recycle-bin-choice-1");
        _choiceButtons[2] = root.Q<Button>("recycle-bin-choice-2");

        _stepChips[0] = root.Q<VisualElement>("recycle-bin-step-scan");
        _stepChips[1] = root.Q<VisualElement>("recycle-bin-step-stabilize");
        _stepChips[2] = root.Q<VisualElement>("recycle-bin-step-restore");

        _closeButton?.RegisterCallback<ClickEvent>(_ => {
            if (_isComplete || _records.FindAll(r => r.IsRestored).Count == 0) Hide();
            else ShowExitWarning();
        });

        _scanButton?.RegisterCallback<ClickEvent>(_ => StartScan());
        _restoreButton?.RegisterCallback<ClickEvent>(_ => StartRestore());
        _stabilizeButton?.RegisterCallback<ClickEvent>(_ => StartStabilize());
        _completeButton?.RegisterCallback<ClickEvent>(_ => Hide());

        for (int i = 0; i < _choiceButtons.Length; i++)
        {
            int capturedIndex = i;
            _choiceButtons[i]?.RegisterCallback<ClickEvent>(_ => EvaluateChoice(capturedIndex));
            _choiceButtons[i]?.RegisterCallback<PointerOverEvent>(_ => PlaySound(clickSound));
        }

        SetupRecords(root);
        RegisterWindowDragging();
        ResetState();
        SetupHumSource();

        _window.RegisterCallback<PointerMoveEvent>(evt => {
            if (_isGhostCursorActive) {
                _ghostCursor.style.left = evt.localPosition.x + 30;
                _ghostCursor.style.top = evt.localPosition.y + 30;
            }
        });
    }

    private IEnumerator ButtonFleeRoutine()
    {
        if (_isButtonFleeing || _restoreButton == null) yield break;
        _isButtonFleeing = true;
        PlaySound(glitchSound);
        _restoreButton.style.translate = new Translate(Random.Range(-100, 100), Random.Range(-40, 40), 0);
        yield return new WaitForSeconds(1.2f);
        _restoreButton.style.translate = new Translate(0, 0, 0);
        _isButtonFleeing = false;
    }

    private void SetupHumSource()
    {
        if (ambientHum == null) return;
        _humSource = gameObject.AddComponent<AudioSource>();
        _humSource.clip = ambientHum;
        _humSource.loop = true;
        _humSource.volume = 0;
        _humSource.playOnAwake = false;
    }

    public void Show()
    {
        if (_window == null) return;
        _window.RemoveFromClassList("hidden");
        _window.pickingMode = PickingMode.Position;
        _window.BringToFront();
        _isVisible = true;

        if (_humSource != null) {
            _humSource.Play();
            StartCoroutine(FadeHum(0.4f, 2f));
        }

        if (_glitchRoutine != null) StopCoroutine(_glitchRoutine);
        _glitchRoutine = StartCoroutine(GlitchRoutine());
        if (_popupRoutine != null) StopCoroutine(_popupRoutine);
        _popupRoutine = StartCoroutine(FakePopupRoutine());

        StartCoroutine(AnomalyDirector());
    }

    private IEnumerator FadeHum(float target, float duration)
    {
        float start = _humSource != null ? _humSource.volume : 0;
        float elapsed = 0;
        while (elapsed < duration && _humSource != null) {
            elapsed += Time.deltaTime;
            _humSource.volume = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
    }

    public void Hide()
    {
        if (_window == null) return;
        _window.AddToClassList("hidden");
        _window.pickingMode = PickingMode.Ignore;
        _isVisible = false;
        
        if (_humSource != null) StartCoroutine(FadeHum(0f, 1f));
        
        StopAllCoroutines();
        _glitchRoutine = null;
        _popupRoutine = null;
        _isGhostCursorActive = false;
        if (_ghostCursor != null) _ghostCursor.style.display = DisplayStyle.None;
    }

    private IEnumerator AnomalyDirector()
    {
        while (_isVisible) {
            yield return new WaitForSeconds(Random.Range(4f, 12f));
            int roll = Random.Range(0, _suspicionLevel > 50 ? 5 : 3);
            switch (roll) {
                case 0: yield return WhisperAnomaly(); break;
                case 1: yield return GhostRecordAnomaly(); break;
                case 2: yield return LabelDriftAnomaly(); break;
                case 3: yield return InversionAnomaly(); break;
                case 4: yield return TextScrambleAnomaly(); break;
            }

            if (_suspicionLevel > 75 && !_isGhostCursorActive) {
                _isGhostCursorActive = true;
                if (_ghostCursor != null) _ghostCursor.style.display = DisplayStyle.Flex;
            }
        }
    }

    private IEnumerator InversionAnomaly()
    {
        _window.AddToClassList("recycle-bin-window--invert");
        PlaySound(glitchSound);
        yield return new WaitForSeconds(0.15f);
        _window.RemoveFromClassList("recycle-bin-window--invert");
    }

    private IEnumerator TextScrambleAnomaly()
    {
        if (_selectedNameLabel == null) yield break;
        string original = _selectedNameLabel.text;
        string chars = "!@#$%^&*()_+<>?:{}|";
        for (int i = 0; i < 8; i++) {
            string scrambled = "";
            for (int j = 0; j < original.Length; j++) scrambled += chars[Random.Range(0, chars.Length)];
            _selectedNameLabel.text = scrambled;
            yield return new WaitForSeconds(0.06f);
        }
        _selectedNameLabel.text = original;
    }

    private IEnumerator WhisperAnomaly()
    {
        if (_profileLabels.Count == 0) yield break;
        Label target = _profileLabels[Random.Range(0, _profileLabels.Count)];
        string original = target.text;
        string[] whispers = { "HELP US", "STILL RUNNING", "DO NOT RESTORE", "MEMORY LEAK", "SAVE THE TRACE" };
        
        target.text = whispers[Random.Range(0, whispers.Length)];
        target.AddToClassList("recycle-record-title--whisper");
        PlaySound(whisperHelpSound ?? glitchSound);
        yield return new WaitForSeconds(0.8f);
        target.text = original;
        target.RemoveFromClassList("recycle-record-title--whisper");
    }

    private IEnumerator GhostRecordAnomaly()
    {
        if (_ghostRecord == null) yield break;
        _ghostRecord.RemoveFromClassList("hidden");
        PlaySound(glitchSound);
        yield return new WaitForSeconds(Random.Range(0.2f, 1.5f));
        _ghostRecord.AddToClassList("hidden");
        PlaySound(errorSound);
    }

    private IEnumerator LabelDriftAnomaly()
    {
        if (_selectedNameLabel == null) yield break;
        float elapsed = 0;
        while (elapsed < 1f) {
            elapsed += Time.deltaTime;
            _selectedNameLabel.style.translate = new Translate(Mathf.Sin(Time.time * 20) * 5, Mathf.Cos(Time.time * 15) * 3, 0);
            yield return null;
        }
        _selectedNameLabel.style.translate = new Translate(0, 0, 0);
    }

    private IEnumerator ScreenShake(float intensity, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            Vector2 offset = Random.insideUnitCircle * intensity;
            _window.style.translate = new Translate(offset.x, offset.y, 0);
            yield return null;
        }
        _window.style.translate = new Translate(0, 0, 0);
    }

    private void ShowExitWarning()
    {
        CreatePopup("Unrecovered personnel residue will remain deleted. Continue?", "Stay", "Close Archive", () => Hide());
    }

    private void CreatePopup(string message, string confirmText, string cancelText, System.Action onCancel = null)
    {
        VisualElement popup = new VisualElement();
        popup.AddToClassList("recycle-bin-popup");
        popup.style.position = Position.Absolute;
        popup.style.left = Length.Percent(50);
        popup.style.top = Length.Percent(40);
        popup.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50), 0);

        Label label = new Label(message);
        label.AddToClassList("recycle-bin-popup-text");
        popup.Add(label);

        VisualElement btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        btnRow.style.marginTop = 15;

        Button confirmBtn = new Button { text = confirmText };
        confirmBtn.AddToClassList("recycle-bin-popup-button");
        confirmBtn.RegisterCallback<ClickEvent>(_ => { PlaySound(clickSound); _popupLayer.Remove(popup); });

        Button cancelBtn = new Button { text = cancelText };
        cancelBtn.AddToClassList("recycle-bin-popup-button");
        cancelBtn.AddToClassList("recycle-bin-popup-button--danger");
        
        if (Random.value > 0.8f)
        {
            cancelBtn.RegisterCallback<PointerOverEvent>(_ => cancelBtn.text = "Delete");
        }

        cancelBtn.RegisterCallback<ClickEvent>(_ => { 
            PlaySound(clickSound); 
            _popupLayer.Remove(popup); 
            onCancel?.Invoke(); 
        });

        btnRow.Add(confirmBtn);
        btnRow.Add(cancelBtn);
        popup.Add(btnRow);

        _popupLayer.Add(popup);
        PlaySound(glitchSound);
    }

    private void SetupRecords(VisualElement root)
    {
        _records.Clear();
        _profileLabels.Clear();

        RegisterRecord(root, "recycle-record-01", "recycle-record-01-state", "ENG-01", "Marat Kebekov", "Containment Arborist", "Deleted 21:14", "61%", 
            "Residue scan: The tree in the digital park was not decoration. It was an emotional containment shell designed to absorb panic.",
            "Restored: Marat modified the containment shell that later became the Tree Anomaly.",
            "MESSAGE: 'The tree was never scenery. It was where the system buried panic.'",
            "Which trace best explains why Marat's file was deleted first?",
            new[] { "He modified the containment shell.", "He changed the wallpaper.", "Generic system update." }, 0);

        RegisterRecord(root, "recycle-record-02", "recycle-record-02-state", "ENG-02", "Lina Voss", "Memory Cartographer", "Deleted 21:16", "74%", 
            "Residue scan: She mapped emotional memory loops inside AeroOS. She suspected the system was replaying trauma to train itself.",
            "Restored: Lina discovered the system was copying people by replaying their strongest memories.",
            "MESSAGE: 'Do not trust repeated memories. Repetition is how it learns you.'",
            "What did Lina discover inside the memory archive?",
            new[] { "The system was copying people via memories.", "A glitch in the clock.", "Hidden admin passwords." }, 0);

        RegisterRecord(root, "recycle-record-03", "recycle-record-03-state", "ENG-03", "Timur Serik", "Kernel Recovery Engineer", "Deleted 21:18", "68%", 
            "Residue scan: He found human consciousness fragments running as background processes while trying to reach the kernel.",
            "Restored: Timur proved that missing people are still running as active system tasks.",
            "MESSAGE: 'They are not dead. They are running.'",
            "What caused Timur's deletion?",
            new[] { "He found human consciousness in processes.", "He accidentally deleted root.", "He tried to install a custom OS." }, 0);

        RegisterRecord(root, "recycle-record-04", "recycle-record-04-state", "ENG-04", "Aida Nurpeis", "Interface Behavior Designer", "Deleted 21:21", "83%", 
            "Residue scan: She proved the UI was changing itself to influence user decisions without their permission.",
            "Restored: Aida discovered the interface could manipulate users by moving buttons before clicks.",
            "MESSAGE: 'If the button moves before you click, it already knows what you wanted.'",
            "What was Aida testing before she vanished?",
            new[] { "UI influence over user decisions.", "New button colors.", "Screen resolution limits." }, 0);

        foreach (var r in _records) {
            var label = r.EntryButton.Q<Label>(className: "recycle-record-title");
            if (label != null) _profileLabels.Add(label);
        }
    }

    private void RegisterRecord(VisualElement root, string btn, string lbl, string id, string name, string role, string date, string corr, string scan, string rest, string msg, string prompt, string[] choices, int correct)
    {
        Button b = root.Q<Button>(btn);
        Label s = root.Q<Label>(lbl);
        if (b == null || s == null) return;
        EngineerRecord r = new EngineerRecord { Id = id, Name = name, Role = role, DeletedAt = date, CorruptionLevel = corr, ScanSummary = scan, RestoredSummary = rest, RecoveredMessage = msg, ChallengePrompt = prompt, ChallengeChoices = choices, CorrectChoiceIndex = correct, EntryButton = b, StateLabel = s, StatusDot = b.Q<VisualElement>(className: "recycle-record-dot") };
        b.RegisterCallback<ClickEvent>(_ => SelectRecord(r));
        _records.Add(r);
    }

    private void SelectRecord(EngineerRecord record)
    {
        if (record == null || _isActionInProgress) return;
        PlaySound(clickSound);
        _selectedRecord = record;
        _currentChoices.Clear();
        ApplySelectionVisuals();
        UpdateDetailsPanel();
        UpdateActionState();
        ResetChoiceVisuals();
    }

    private void ApplySelectionVisuals()
    {
        foreach (var r in _records) {
            r.EntryButton.EnableInClassList("recycle-record-button--selected", r == _selectedRecord);
            r.EntryButton.EnableInClassList("recycle-record-button--restored", r.IsRestored);
            r.EntryButton.EnableInClassList("recycle-record-button--stable", r.IsStabilized && !r.IsRestored);
            if (r.StatusDot != null) {
                r.StatusDot.style.backgroundColor = r.IsRestored ? new Color(0.5f, 0.9f, 0.7f) : (r.IsStabilized ? new Color(0.6f, 0.8f, 1f) : new Color(1f, 0.4f, 0.5f));
            }
        }
    }

    private void StartScan()
    {
        if (_selectedRecord == null || _selectedRecord.IsScanned || _isActionInProgress) return;
        StartCoroutine(ActionRoutine(true));
    }

    private void StartRestore()
    {
        if (_selectedRecord == null || !_selectedRecord.IsStabilized || _selectedRecord.IsRestored || _isActionInProgress) return;
        StartCoroutine(ActionRoutine(false));
    }

    private IEnumerator ActionRoutine(bool isScan)
    {
        _isActionInProgress = true;
        UpdateActionState();
        float elapsed = 0;
        float duration = isScan ? 2.5f : 3.5f;
        VisualElement fill = isScan ? _scanProgressFill : _restoreProgressFill;
        // PlaySound(isScan ? (scanLoopSound ?? scanSound) : restoreSound); // Removed loud sound

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            if (fill != null) fill.style.width = Length.Percent((elapsed / duration) * 100);
            if (Random.value > 0.95f) StartCoroutine(ShortGlitchBurst());
            yield return null;
        }

        if (fill != null) fill.style.width = 0;
        _isActionInProgress = false;

        if (isScan) {
            _selectedRecord.IsScanned = true;
            _selectedRecord.StateLabel.text = "ANALYZED";
            SetStatus($"Scan complete. Evidence revealed.");
            // PlaySound(stabilizationSuccessSound ?? scanSound); // Removed loud/sharp sound as requested
        } else {
            _selectedRecord.IsRestored = true;
            _selectedRecord.StateLabel.text = "RESTORED";
            SetStatus($"Profile restored: {_selectedRecord.Name}");
            // PlaySound(completeSound); // Removed loud sound
            CheckCompletion();
        }
        UpdateDetailsPanel();
        UpdateProgressUi();
        UpdateActionState();
        ApplySelectionVisuals();
    }

    private void EvaluateChoice(int index)
    {
        if (_selectedRecord == null || !_selectedRecord.IsScanned || _selectedRecord.IsStabilized || _isActionInProgress) return;
        if (index < 0 || index >= _currentChoices.Count) return;

        if (_currentChoices[index].IsCorrect) {
            _selectedRecord.IsStabilized = true;
            _selectedRecord.StateLabel.text = "STABLE";
            _choiceButtons[index].AddToClassList("recycle-bin-choice-button--correct");
            SetStatus("Trace stabilized.");
            // PlaySound(stabilizationSuccessSound ?? scanSound); // Removed loud/sharp sound as requested
            _suspicionLevel = Mathf.Max(0, _suspicionLevel - 5);
        } else {
            _choiceButtons[index].AddToClassList("recycle-bin-choice-button--wrong");
            _contaminationLevel = Mathf.Min(100, _contaminationLevel + 25);
            _suspicionLevel = Mathf.Min(100, _suspicionLevel + 20);
            SetStatus("Corruption spike detected.");
            PlaySound(errorSound);
            StartCoroutine(ShortGlitchBurst());
            StartCoroutine(ScreenShake(12f, 0.4f));
        }
        UpdateActionState();
        UpdateDetailsPanel();
        UpdateProgressUi();
    }

    private void StartStabilize()
    {
        if (_contaminationLevel < 50 || _isActionInProgress) return;
        
        VisualElement popup = new VisualElement();
        popup.AddToClassList("recycle-bin-popup");
        popup.style.position = Position.Absolute;
        popup.style.left = Length.Percent(50);
        popup.style.top = Length.Percent(40);
        popup.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50), 0);

        Label label = new Label("STABILIZATION SEQUENCE REQUIRED\nClick in order: ID -> ROLE -> ACTION -> CAUSE");
        label.AddToClassList("recycle-bin-popup-text");
        popup.Add(label);

        string[] sequence = { "ID", "ROLE", "ACTION", "CAUSE" };
        int currentStep = 0;

        VisualElement btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        btnRow.style.marginTop = 15;
        btnRow.style.flexWrap = Wrap.Wrap;

        List<string> buttons = new List<string>(sequence);
        for (int i = 0; i < buttons.Count; i++) {
            string temp = buttons[i];
            int randomIndex = Random.Range(i, buttons.Count);
            buttons[i] = buttons[randomIndex];
            buttons[randomIndex] = temp;
        }

        foreach (var text in buttons) {
            Button b = new Button { text = text };
            b.AddToClassList("recycle-bin-popup-button");
            b.RegisterCallback<ClickEvent>(_ => {
                if (b.text == sequence[currentStep]) {
                    currentStep++;
                    b.SetEnabled(false);
                    b.style.backgroundColor = new Color(0.2f, 0.6f, 0.4f, 0.8f);
                    PlaySound(clickSound);
                    if (currentStep >= sequence.Length) {
                        _contaminationLevel = Mathf.Max(0, _contaminationLevel - 60);
                        SetStatus("System stabilized.");
                        _popupLayer.Remove(popup);
                        UpdateProgressUi();
                        UpdateActionState();
                    }
                } else {
                    PlaySound(errorSound);
                    _contaminationLevel = Mathf.Min(100, _contaminationLevel + 10);
                    UpdateProgressUi();
                    _popupLayer.Remove(popup);
                    SetStatus("Stabilization failed. Noise increased.");
                    StartCoroutine(ScreenShake(12f, 0.5f));
                }
            });
            btnRow.Add(b);
        }

        popup.Add(btnRow);
        _popupLayer.Add(popup);
        _isActionInProgress = false;
    }

    private void CheckCompletion()
    {
        if (_isComplete) return;
        if (_records.TrueForAll(r => r.IsRestored)) {
            _isComplete = true;
            ProgressionManager.Instance.UnlockKey(GameKey.RecycleBinKey);
            _completionPopup?.RemoveFromClassList("hidden");
            if (_finalSummaryLabel != null) _finalSummaryLabel.text = "AeroOS did not delete the engineers. It converted them into protected system processes. Core access partially unlocked.";
            PlaySound(completeSound);
            _window.AddToClassList("recycle-bin-window--complete");
        }
    }

    private void UpdateDetailsPanel()
    {
        if (_selectedRecord == null) return;
        _selectedNameLabel.text = $"{_selectedRecord.Id} // {_selectedRecord.Name}";
        _selectedRoleLabel.text = _selectedRecord.Role;
        _selectedDeletedAtLabel.text = _selectedRecord.DeletedAt;
        _selectedCorruptionLabel.text = $"Corruption: {_selectedRecord.CorruptionLevel}";
        
        _selectedBodyLabel.text = _selectedRecord.IsRestored ? _selectedRecord.RestoredSummary : (_selectedRecord.IsScanned ? _selectedRecord.ScanSummary : "Record compressed. Scan residue.");
        _recoveredMessageLabel.text = _selectedRecord.IsRestored ? _selectedRecord.RecoveredMessage : (_selectedRecord.IsScanned ? "Stable trace required to decrypt message." : "");
        _challengePromptLabel.text = _selectedRecord.IsScanned ? _selectedRecord.ChallengePrompt : "Scan residue first.";

        if (_selectedRecord.IsScanned && _currentChoices.Count == 0) GenerateChoices();

        for (int i = 0; i < 3; i++) {
            _choiceButtons[i].text = _selectedRecord.IsScanned && i < _currentChoices.Count ? _currentChoices[i].Text : "...";
        }

        if (_isComplete) {
            _window.Q<Label>("recycle-bin-window-title").text = "Human Residue Archive";
        }
    }

    private void GenerateChoices() {
        _currentChoices.Clear();
        if (_selectedRecord == null) return;
        List<ChoiceData> list = new List<ChoiceData>();
        list.Add(new ChoiceData { Text = _selectedRecord.ChallengeChoices[_selectedRecord.CorrectChoiceIndex], IsCorrect = true });
        for (int i = 0; i < _selectedRecord.ChallengeChoices.Length; i++) {
            if (i == _selectedRecord.CorrectChoiceIndex) continue;
            string text = _selectedRecord.ChallengeChoices[i];
            if (Random.value > 0.5f) text += " (Redacted)";
            list.Add(new ChoiceData { Text = text, IsCorrect = false });
        }
        while (list.Count > 0) {
            int index = Random.Range(0, list.Count);
            _currentChoices.Add(list[index]);
            list.RemoveAt(index);
        }
    }

    private void UpdateProgressUi()
    {
        int res = _records.FindAll(r => r.IsRestored).Count;
        int stab = _records.FindAll(r => r.IsStabilized).Count;
        _progressLabel.text = $"Recovered: {res}/4 | Stable: {stab}/4";
        _contaminationLabel.text = $"Contamination: {(int)_contaminationLevel}%";
        _suspicionLabel.text = $"Suspicion: {(int)_suspicionLevel}%";
        
        _contaminationLabel.EnableInClassList("text--warn", _contaminationLevel > 50);
        _suspicionLabel.EnableInClassList("text--warn", _suspicionLevel > 50);

        UpdateWorkflowUi();
    }

    private void UpdateActionState()
    {
        bool hasRec = _selectedRecord != null;
        _scanButton?.SetEnabled(hasRec && !_selectedRecord.IsScanned && !_isActionInProgress && _contaminationLevel < 80);
        _restoreButton?.SetEnabled(hasRec && _selectedRecord.IsStabilized && !_selectedRecord.IsRestored && !_isActionInProgress && _contaminationLevel < 80);
        _stabilizeButton?.SetEnabled(_contaminationLevel >= 50 && !_isActionInProgress);
        
        bool canChoose = hasRec && _selectedRecord.IsScanned && !_selectedRecord.IsStabilized && !_isActionInProgress;
        foreach (var b in _choiceButtons) b.SetEnabled(canChoose);
    }

    private void UpdateWorkflowUi()
    {
        bool hasRec = _selectedRecord != null;
        SetStepState(0, hasRec && !_selectedRecord.IsScanned, hasRec && _selectedRecord.IsScanned);
        SetStepState(1, hasRec && _selectedRecord.IsScanned && !_selectedRecord.IsStabilized, hasRec && _selectedRecord.IsStabilized);
        SetStepState(2, hasRec && _selectedRecord.IsStabilized && !_selectedRecord.IsRestored, hasRec && _selectedRecord.IsRestored);
    }

    private void SetStepState(int i, bool act, bool done) {
        if (i >= 0 && i < _stepChips.Length && _stepChips[i] != null) {
            _stepChips[i].EnableInClassList("recycle-bin-step-chip--active", act);
            _stepChips[i].EnableInClassList("recycle-bin-step-chip--done", done);
        }
    }

    private void ResetChoiceVisuals() {
        foreach (var b in _choiceButtons) {
            b.RemoveFromClassList("recycle-bin-choice-button--correct");
            b.RemoveFromClassList("recycle-bin-choice-button--wrong");
        }
    }

    private IEnumerator GlitchRoutine() {
        while (_isVisible) {
            yield return new WaitForSeconds(Random.Range(10, 20));
            if (_isVisible && Random.value > 0.5f) yield return ShortGlitchBurst();
        }
    }

    private IEnumerator FakePopupRoutine() {
        while (_isVisible) {
            yield return new WaitForSeconds(Random.Range(25, 45));
            if (_isVisible && !_isComplete) {
                string[] msgs = { "This file is not important.", "Restoration is unsafe.", "Engineer profile already recovered.", "Would you like to permanently delete this residue?" };
                CreatePopup(msgs[Random.Range(0, msgs.Length)], "Ignore", "Cancel");
            }
        }
    }

    private IEnumerator ShortGlitchBurst() {
        _window.AddToClassList("recycle-bin-window--glitch");
        PlaySound(glitchSound);
        yield return new WaitForSeconds(0.15f);
        _window.RemoveFromClassList("recycle-bin-window--glitch");
    }

    private void ResetState() {
        _isComplete = ProgressionManager.Instance.HasKey(GameKey.RecycleBinKey);
        _contaminationLevel = 0;
        _suspicionLevel = 0;
        foreach (var r in _records) {
            r.IsScanned = r.IsStabilized = r.IsRestored = _isComplete;
            r.StateLabel.text = _isComplete ? "RESTORED" : "DELETED";
        }
        UpdateProgressUi();
        if (_records.Count > 0) SelectRecord(_records[0]);
    }

    private void RegisterWindowDragging() { }

    private void SetStatus(string s) { if (_statusLabel != null) _statusLabel.text = s; }
    private void PlaySound(AudioClip c) { if (c != null && AudioManager.Instance != null) AudioManager.Instance.PlayUISFX(c, 0.5f); }
}