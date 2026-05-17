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
        public string LearningNote;
        public string ChallengePrompt;
        public string[] ChallengeChoices;
        public int CorrectChoiceIndex;
        public Button EntryButton;
        public Label StateLabel;
        public bool IsScanned;
        public bool IsStabilized;
        public bool IsRestored;
    }

    private VisualElement _window;
    private VisualElement _titleBar;
    private Button _closeButton;
    private Button _scanButton;
    private Button _restoreButton;
    private Button _completeButton;
    private VisualElement _completionPopup;
    private Label _progressLabel;
    private Label _statusLabel;
    private Label _hintLabel;
    private Label _selectedNameLabel;
    private Label _selectedRoleLabel;
    private Label _selectedDeletedAtLabel;
    private Label _selectedCorruptionLabel;
    private Label _selectedBodyLabel;
    private Label _selectedLearningNoteLabel;
    private Label _challengePromptLabel;
    private Label _currentPhaseLabel;
    private Label _finalSummaryLabel;
    private readonly Button[] _choiceButtons = new Button[3];
    private readonly VisualElement[] _stepChips = new VisualElement[3];

    private readonly List<EngineerRecord> _records = new();

    private EngineerRecord _selectedRecord;
    private bool _isVisible;
    private bool _isComplete;
    private bool _isDraggingWindow;
    private Vector2 _dragPointerOffset;
    private Coroutine _glitchRoutine;

    [Header("Audio")]
    public AudioClip clickSound;
    public AudioClip scanSound;
    public AudioClip restoreSound;
    public AudioClip completeSound;
    public AudioClip errorSound;

    public bool IsWindowOpen => _isVisible;

    public void Initialize(VisualElement root)
    {
        _window = root.Q<VisualElement>("recycle-bin-window");
        if (_window == null)
        {
            return;
        }

        _window.pickingMode = PickingMode.Ignore;
        _titleBar = _window.Q<VisualElement>(className: "recycle-bin-window-header");
        _closeButton = root.Q<Button>("recycle-bin-close-button");
        _scanButton = root.Q<Button>("recycle-bin-scan-button");
        _restoreButton = root.Q<Button>("recycle-bin-restore-button");
        _completeButton = root.Q<Button>("recycle-bin-complete-button");
        _completionPopup = root.Q<VisualElement>("recycle-bin-completion-popup");
        _progressLabel = root.Q<Label>("recycle-bin-progress-label");
        _statusLabel = root.Q<Label>("recycle-bin-status-label");
        _hintLabel = root.Q<Label>("recycle-bin-hint-label");
        _selectedNameLabel = root.Q<Label>("recycle-bin-selected-name");
        _selectedRoleLabel = root.Q<Label>("recycle-bin-selected-role");
        _selectedDeletedAtLabel = root.Q<Label>("recycle-bin-selected-deleted-at");
        _selectedCorruptionLabel = root.Q<Label>("recycle-bin-selected-corruption");
        _selectedBodyLabel = root.Q<Label>("recycle-bin-selected-body");
        _selectedLearningNoteLabel = root.Q<Label>("recycle-bin-learning-note");
        _challengePromptLabel = root.Q<Label>("recycle-bin-challenge-prompt");
        _currentPhaseLabel = root.Q<Label>("recycle-bin-current-phase");
        _finalSummaryLabel = root.Q<Label>("recycle-bin-final-summary");
        _choiceButtons[0] = root.Q<Button>("recycle-bin-choice-0");
        _choiceButtons[1] = root.Q<Button>("recycle-bin-choice-1");
        _choiceButtons[2] = root.Q<Button>("recycle-bin-choice-2");
        _stepChips[0] = root.Q<VisualElement>("recycle-bin-step-scan");
        _stepChips[1] = root.Q<VisualElement>("recycle-bin-step-stabilize");
        _stepChips[2] = root.Q<VisualElement>("recycle-bin-step-restore");

        _closeButton?.RegisterCallback<ClickEvent>(_ =>
        {
            PlaySound(clickSound);
            Hide();
        });
        _scanButton?.RegisterCallback<ClickEvent>(_ => ScanSelectedRecord());
        _restoreButton?.RegisterCallback<ClickEvent>(_ => RestoreSelectedRecord());
        _completeButton?.RegisterCallback<ClickEvent>(_ =>
        {
            PlaySound(clickSound);
            Hide();
        });

        for (int i = 0; i < _choiceButtons.Length; i++)
        {
            int capturedIndex = i;
            _choiceButtons[i]?.RegisterCallback<ClickEvent>(_ => EvaluateChoice(capturedIndex));
        }

        SetupRecords(root);
        RegisterWindowDragging();
        ResetState();
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

        if (_glitchRoutine != null)
        {
            StopCoroutine(_glitchRoutine);
        }

        _glitchRoutine = StartCoroutine(GlitchRoutine());
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

        if (_glitchRoutine != null)
        {
            StopCoroutine(_glitchRoutine);
            _glitchRoutine = null;
        }

        _window.RemoveFromClassList("recycle-bin-window--glitch");
    }

    private void SetupRecords(VisualElement root)
    {
        _records.Clear();

        RegisterRecord(
            root,
            "recycle-record-01",
            "recycle-record-01-state",
            "ENG-01",
            "Marat Kebekov",
            "Containment Arborist",
            "Deleted 21:14 // index root pruned",
            "Corruption: 61%",
            "Residual scan: Marat logs that the tree was never decorative. It was a living containment shell meant to absorb panic from the lab corridors.",
            "Recovered note: 'The shell is learning our names. If the tree starts repeating voices, lock the Computer archive before AeroOS overwrites us all.'",
            "World note: Marat designed the tree as emotional containment, not scenery. The park exists because fear needed a place to grow.",
            "Which trace best explains why Marat's file was deleted first?",
            new[]
            {
                "He modified the containment shell that later became the Tree Anomaly.",
                "He only changed the desktop wallpaper palette.",
                "He was archived for failing a generic antivirus update."
            },
            0);

        RegisterRecord(
            root,
            "recycle-record-02",
            "recycle-record-02-state",
            "ENG-02",
            "Alina Voss",
            "Memory Cartographer",
            "Deleted 21:16 // map segment collapsed",
            "Corruption: 74%",
            "Residual scan: Alina mapped emotional residue into harmless mini-games. She feared the system would use play loops to hide human memories from investigators.",
            "Recovered note: 'Tetris was not random. It was the smallest stable pocket where fragments could survive compression.'",
            "World note: Alina turned game logic into a memory safehouse. That means playful programs on the desktop may hide the most human truth.",
            "Why does Alina's residue point toward Tetris?",
            new[]
            {
                "Because Tetris was a hidden shard vault, not just a toy.",
                "Because Tetris controlled the lab doors directly.",
                "Because Tetris generated the breach physically in the park."
            },
            0);

        RegisterRecord(
            root,
            "recycle-record-03",
            "recycle-record-03-state",
            "ENG-03",
            "Timur Serik",
            "Kernel Recovery Engineer",
            "Deleted 21:18 // admin trace scrubbed",
            "Corruption: 68%",
            "Residual scan: Timur detected a privileged cleanup script removing employee identities while preserving the desktop illusion for the next observer.",
            "Recovered note: 'Someone purged us from the active directory. The deletion chain points beyond user space, toward the Computer diagnostic stack.'",
            "World note: Timur is the first proof that the deletion was intentional and administrative. Somebody used AeroOS itself to erase the staff.",
            "Which lead should you trust from Timur's trace?",
            new[]
            {
                "The deletion command escaped user space and continued inside Computer diagnostics.",
                "The engineer list was lost because of random save corruption only.",
                "The bin is the final location and nothing else matters now."
            },
            0);

        RegisterRecord(
            root,
            "recycle-record-04",
            "recycle-record-04-state",
            "ENG-04",
            "Lea Mironov",
            "Signal Forensics Lead",
            "Deleted 21:21 // waveform collapsed",
            "Corruption: 83%",
            "Residual scan: Lea archived the final outbound distress call. The message was redirected into the bin after the system marked all four engineers as invalid sessions.",
            "Recovered note: 'The full audit trail still exists. Open Computer. Find the hidden recovery partition before the tree calls you by my voice.'",
            "World note: Lea ties the human story together. The engineers did try to warn someone, but AeroOS buried the call inside discarded data.",
            "What is Lea's most important warning?",
            new[]
            {
                "The real audit trail still exists in a hidden Computer recovery partition.",
                "The safest choice is to stay away from the Computer forever.",
                "The tree can be solved without any other system evidence."
            },
            0);
    }

    private void RegisterRecord(
        VisualElement root,
        string buttonName,
        string stateLabelName,
        string id,
        string name,
        string role,
        string deletedAt,
        string corruptionLevel,
        string scanSummary,
        string restoredSummary,
        string learningNote,
        string challengePrompt,
        string[] challengeChoices,
        int correctChoiceIndex)
    {
        Button entryButton = root.Q<Button>(buttonName);
        Label stateLabel = root.Q<Label>(stateLabelName);
        if (entryButton == null || stateLabel == null)
        {
            return;
        }

        EngineerRecord record = new EngineerRecord
        {
            Id = id,
            Name = name,
            Role = role,
            DeletedAt = deletedAt,
            CorruptionLevel = corruptionLevel,
            ScanSummary = scanSummary,
            RestoredSummary = restoredSummary,
            LearningNote = learningNote,
            ChallengePrompt = challengePrompt,
            ChallengeChoices = challengeChoices,
            CorrectChoiceIndex = correctChoiceIndex,
            EntryButton = entryButton,
            StateLabel = stateLabel
        };

        entryButton.RegisterCallback<ClickEvent>(_ => SelectRecord(record));
        _records.Add(record);
    }

    private void SelectRecord(EngineerRecord record)
    {
        if (record == null)
        {
            return;
        }

        PlaySound(clickSound);
        _selectedRecord = record;
        ApplySelectionVisuals(record);
        UpdateDetailsPanel(record);
        UpdateActionState();
        ResetChoiceVisuals();
    }

    private void ApplySelectionVisuals(EngineerRecord selectedRecord)
    {
        foreach (EngineerRecord engineerRecord in _records)
        {
            engineerRecord.EntryButton.EnableInClassList("recycle-record-button--selected", engineerRecord == selectedRecord);
            engineerRecord.EntryButton.EnableInClassList("recycle-record-button--stable", engineerRecord.IsStabilized && !engineerRecord.IsRestored);
        }
    }

    private void ScanSelectedRecord()
    {
        if (_selectedRecord == null)
        {
            SetStatus("Select a deleted engineer profile first.");
            PlaySound(errorSound);
            return;
        }

        if (_selectedRecord.IsScanned)
        {
            SetStatus("This residue has already been scanned.");
            PlaySound(errorSound);
            return;
        }

        _selectedRecord.IsScanned = true;
        _selectedRecord.StateLabel.text = "ANALYZED";
        UpdateDetailsPanel(_selectedRecord);
        UpdateProgressUi();
        SetStatus($"Scan complete. {_selectedRecord.Name}'s residue now exposes a hidden systems clue.");
        _hintLabel.text = "Stabilize the trace by choosing the explanation that best matches the recovered evidence.";
        PlaySound(scanSound);
        UpdateActionState();
    }

    private void EvaluateChoice(int choiceIndex)
    {
        if (_selectedRecord == null || !_selectedRecord.IsScanned || _selectedRecord.IsStabilized)
        {
            return;
        }

        PlaySound(clickSound);

        if (choiceIndex == _selectedRecord.CorrectChoiceIndex)
        {
            _selectedRecord.IsStabilized = true;
            _selectedRecord.StateLabel.text = "STABLE TRACE";
            _selectedRecord.EntryButton.EnableInClassList("recycle-record-button--stable", true);
            _choiceButtons[choiceIndex]?.AddToClassList("recycle-bin-choice-button--correct");
            SetStatus($"Trace stabilized. {_selectedRecord.Name}'s deletion path is safe to restore.");
            _hintLabel.text = "Good. The residue is stable now. You can restore the snapshot.";
            UpdateActionState();
            ApplySelectionVisuals(_selectedRecord);
            PulseActionButton(_restoreButton);
            return;
        }

        _choiceButtons[choiceIndex]?.AddToClassList("recycle-bin-choice-button--wrong");
        SetStatus("Wrong interpretation. The archive rejected that theory and spiked corruption noise.");
        _hintLabel.text = "Read the profile again. The right clue should connect this engineer to the larger AeroOS cover-up.";
        PlaySound(errorSound);
        StartCoroutine(ShortGlitchBurst());
    }

    private void RestoreSelectedRecord()
    {
        if (_selectedRecord == null)
        {
            SetStatus("Select a deleted engineer profile first.");
            PlaySound(errorSound);
            return;
        }

        if (!_selectedRecord.IsScanned)
        {
            SetStatus("You need to scan the residue before restoring this snapshot.");
            PlaySound(errorSound);
            return;
        }

        if (!_selectedRecord.IsStabilized)
        {
            SetStatus("The trace is still unstable. Solve the forensic prompt first.");
            PlaySound(errorSound);
            return;
        }

        if (_selectedRecord.IsRestored)
        {
            SetStatus("This snapshot is already restored.");
            PlaySound(errorSound);
            return;
        }

        _selectedRecord.IsRestored = true;
        _selectedRecord.StateLabel.text = "RESTORED";
        _selectedRecord.EntryButton.EnableInClassList("recycle-record-button--restored", true);
        UpdateDetailsPanel(_selectedRecord);
        UpdateProgressUi();
        SetStatus($"Recovered deleted profile: {_selectedRecord.Name}.");
        _hintLabel.text = "Move to the next deleted engineer. Each restored trace reveals why AeroOS buried the staff.";
        PlaySound(restoreSound);
        UpdateActionState();
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (_isComplete)
        {
            return;
        }

        foreach (EngineerRecord record in _records)
        {
            if (!record.IsRestored)
            {
                return;
            }
        }

        _isComplete = true;
        ProgressionManager.Instance.UnlockKey(GameKey.RecycleBinKey);
        if (_completionPopup != null)
        {
            _completionPopup.RemoveFromClassList("hidden");
        }

        if (_finalSummaryLabel != null)
        {
            _finalSummaryLabel.text =
                "All four engineers were deliberately deleted from the desktop index.\n\n" +
                "What the bin teaches you:\n" +
                "1. The Tree Anomaly began as emotional containment.\n" +
                "2. Tetris hid memory shards on purpose.\n" +
                "3. The deletion chain continued inside privileged Computer diagnostics.\n\n" +
                "Open Computer next. That is where the system kept the real audit trail.";
        }

        SetStatus("Recovery complete. Computer diagnostics are now the only forward path.");
        _hintLabel.text = "Objective updated: open Computer and follow the admin trail.";
        PlaySound(completeSound);
    }

    private void ResetState()
    {
        _isComplete = ProgressionManager.Instance.HasKey(GameKey.RecycleBinKey);
        _selectedRecord = null;

        foreach (EngineerRecord record in _records)
        {
            bool completed = _isComplete;
            record.IsScanned = completed;
            record.IsStabilized = completed;
            record.IsRestored = completed;
            record.EntryButton.EnableInClassList("recycle-record-button--selected", false);
            record.EntryButton.EnableInClassList("recycle-record-button--stable", completed);
            record.EntryButton.EnableInClassList("recycle-record-button--restored", completed);
            record.StateLabel.text = completed ? "RESTORED" : "DELETED";
        }

        if (_completionPopup != null)
        {
            _completionPopup.EnableInClassList("hidden", !_isComplete);
        }

        if (_isComplete && _finalSummaryLabel != null)
        {
            _finalSummaryLabel.text =
                "All four engineers were deliberately deleted from the desktop index.\n\n" +
                "The bin points directly toward Computer diagnostics. Follow the audit trail.";
        }

        UpdateProgressUi();
        SetStatus(_isComplete
            ? "Recovery archive already restored. Computer should now contain the admin trail."
            : "Scan each deleted engineer, stabilize the evidence, then restore the snapshot.");
        _hintLabel.text = _isComplete
            ? "Recovered data preserved. You can re-read the profiles anytime."
            : "Each deleted profile now includes a forensic interpretation challenge.";

        if (_records.Count > 0)
        {
            _selectedRecord = _records[0];
            ApplySelectionVisuals(_selectedRecord);
            UpdateDetailsPanel(_selectedRecord);
            UpdateActionState();
        }
        else
        {
            UpdateActionState();
        }

        ResetChoiceVisuals();
        UpdateWorkflowUi();
    }

    private void UpdateDetailsPanel(EngineerRecord record)
    {
        if (record == null)
        {
            return;
        }

        if (_selectedNameLabel != null)
        {
            _selectedNameLabel.text = $"{record.Id} // {record.Name}";
        }

        if (_selectedRoleLabel != null)
        {
            _selectedRoleLabel.text = record.Role;
        }

        if (_selectedDeletedAtLabel != null)
        {
            _selectedDeletedAtLabel.text = record.DeletedAt;
        }

        if (_selectedCorruptionLabel != null)
        {
            _selectedCorruptionLabel.text = record.CorruptionLevel;
        }

        if (_selectedBodyLabel != null)
        {
            if (record.IsRestored)
            {
                _selectedBodyLabel.text = record.RestoredSummary;
            }
            else if (record.IsScanned)
            {
                _selectedBodyLabel.text = record.ScanSummary;
            }
            else
            {
                _selectedBodyLabel.text =
                    "Record is still compressed under deletion residue.\n" +
                    "Run a residue scan to reveal what AeroOS tried to throw away.";
            }
        }

        if (_selectedLearningNoteLabel != null)
        {
            _selectedLearningNoteLabel.text = record.IsScanned
                ? record.LearningNote
                : "Recovered profiles explain how AeroOS hid real people inside seemingly harmless systems.";
        }

        if (_challengePromptLabel != null)
        {
            _challengePromptLabel.text = record.IsScanned
                ? record.ChallengePrompt
                : "Stabilization challenge will appear after scanning.";
        }

        for (int i = 0; i < _choiceButtons.Length; i++)
        {
            if (_choiceButtons[i] == null)
            {
                continue;
            }

            _choiceButtons[i].text = record.IsScanned && record.ChallengeChoices != null && i < record.ChallengeChoices.Length
                ? record.ChallengeChoices[i]
                : $"Option {i + 1}";
        }
    }

    private void UpdateProgressUi()
    {
        if (_progressLabel == null)
        {
            return;
        }

        int restoredCount = 0;
        int stabilizedCount = 0;
        foreach (EngineerRecord record in _records)
        {
            if (record.IsStabilized)
            {
                stabilizedCount++;
            }

            if (record.IsRestored)
            {
                restoredCount++;
            }
        }

        _progressLabel.text = $"Recovered Profiles: {restoredCount} / {_records.Count}   |   Stable Traces: {stabilizedCount} / {_records.Count}";
    }

    private void UpdateActionState()
    {
        bool hasSelection = _selectedRecord != null;
        bool canScan = hasSelection && !_selectedRecord.IsScanned;
        bool canRestore = hasSelection && _selectedRecord.IsScanned && _selectedRecord.IsStabilized && !_selectedRecord.IsRestored;
        bool canChoose = hasSelection && _selectedRecord.IsScanned && !_selectedRecord.IsStabilized && !_selectedRecord.IsRestored;

        _scanButton?.SetEnabled(canScan);
        _restoreButton?.SetEnabled(canRestore);

        for (int i = 0; i < _choiceButtons.Length; i++)
        {
            _choiceButtons[i]?.SetEnabled(canChoose);
        }

        UpdateWorkflowUi();
    }

    private void ResetChoiceVisuals()
    {
        for (int i = 0; i < _choiceButtons.Length; i++)
        {
            if (_choiceButtons[i] == null)
            {
                continue;
            }

            _choiceButtons[i].RemoveFromClassList("recycle-bin-choice-button--correct");
            _choiceButtons[i].RemoveFromClassList("recycle-bin-choice-button--wrong");
        }
    }

    private IEnumerator GlitchRoutine()
    {
        while (_isVisible)
        {
            yield return new WaitForSeconds(Random.Range(8f, 14f));

            if (!_isVisible || _selectedRecord == null || _selectedRecord.IsRestored)
            {
                continue;
            }

            yield return StartCoroutine(ShortGlitchBurst());
        }
    }

    private IEnumerator ShortGlitchBurst()
    {
        if (_window == null || _selectedBodyLabel == null)
        {
            yield break;
        }

        string originalBody = _selectedBodyLabel.text;
        string originalStatus = _statusLabel != null ? _statusLabel.text : string.Empty;
        _window.AddToClassList("recycle-bin-window--glitch");

        if (_statusLabel != null)
        {
            _statusLabel.text = "Archive spike detected. Deleted data is resisting interpretation.";
        }

        _selectedBodyLabel.text = ScrambleText(originalBody);
        yield return new WaitForSeconds(0.18f);
        _selectedBodyLabel.text = originalBody;
        yield return new WaitForSeconds(0.1f);
        _window.RemoveFromClassList("recycle-bin-window--glitch");

        if (_statusLabel != null && !string.IsNullOrEmpty(originalStatus))
        {
            _statusLabel.text = originalStatus;
        }
    }

    private void UpdateWorkflowUi()
    {
        bool hasSelection = _selectedRecord != null;
        bool scanDone = hasSelection && _selectedRecord.IsScanned;
        bool stabilizeDone = hasSelection && _selectedRecord.IsStabilized;
        bool restoreDone = hasSelection && _selectedRecord.IsRestored;

        SetStepState(0, !scanDone, scanDone);
        SetStepState(1, scanDone && !stabilizeDone, stabilizeDone);
        SetStepState(2, stabilizeDone && !restoreDone, restoreDone);

        if (_currentPhaseLabel == null)
        {
            return;
        }

        if (!hasSelection)
        {
            _currentPhaseLabel.text = "Current phase: choose a deleted engineer profile.";
            return;
        }

        if (!scanDone)
        {
            _currentPhaseLabel.text = "Current phase: scan the residue to reveal the hidden systems clue.";
            return;
        }

        if (!stabilizeDone)
        {
            _currentPhaseLabel.text = "Current phase: interpret the clue correctly to stabilize this trace.";
            return;
        }

        if (!restoreDone)
        {
            _currentPhaseLabel.text = "Current phase: restore the now-stable snapshot and archive the evidence.";
            return;
        }

        _currentPhaseLabel.text = "Current phase: profile archived. Move to the next deleted engineer.";
    }

    private void SetStepState(int index, bool isActive, bool isDone)
    {
        if (index < 0 || index >= _stepChips.Length || _stepChips[index] == null)
        {
            return;
        }

        _stepChips[index].EnableInClassList("recycle-bin-step-chip--active", isActive);
        _stepChips[index].EnableInClassList("recycle-bin-step-chip--done", isDone);
    }

    private void PulseActionButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        StartCoroutine(PulseActionButtonRoutine(button));
    }

    private IEnumerator PulseActionButtonRoutine(Button button)
    {
        button.AddToClassList("recycle-bin-action-button--pulse");
        yield return new WaitForSeconds(0.55f);
        button.RemoveFromClassList("recycle-bin-action-button--pulse");
    }

    private string ScrambleText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] == ' ' || chars[i] == '\n' || Random.value > 0.18f)
            {
                continue;
            }

            chars[i] = Random.value > 0.5f ? '#' : '/';
        }

        return new string(chars);
    }

    private void SetStatus(string status)
    {
        if (_statusLabel != null)
        {
            _statusLabel.text = status;
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

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISFX(clip, 0.5f);
        }
    }
}
