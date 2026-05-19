using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RecycleBinAppController : MonoBehaviour
{
    private sealed class EngineerData
    {
        public string Id;
        public string Name;
        public string Role;
        public string SentenceTemplate; // Use "[]" for slots
        public string[] CorrectWords;
        public string[] Distractions;
        public string FinalSummary;
        public bool IsRecovered;
    }

    private sealed class SlotState
    {
        public VisualElement Element;
        public Label WordLabel;
        public FragmentState AssignedFragment;
        public int Index;
    }

    private sealed class FragmentState
    {
        public Label Element;
        public string Text;
        public int CorrectSlotIndex; // -1 if noise
        public Vector2 Velocity;
        public Vector2 Position;
        public bool IsCaught;
        public bool IsFake;
        public bool IsFrozen;
        public float FlickerTimer;
        public float MutationTimer;
    }

    private VisualElement _window;
    private VisualElement _voidArea;
    private VisualElement _fragmentSpawnArea;
    private VisualElement _reconstructionArea;
    private VisualElement _selectionOverlay;
    private Label _integrityLabel;
    private VisualElement _corruptionFill;
    private Label _voidStatusText;
    private Label _infoName;
    private Label _infoRole;
    private VisualElement _engineerInfo;
    private VisualElement _completionPopup;
    private Label _finalSummary;
    private VisualElement _sentenceContainer;

    private readonly List<EngineerData> _engineers = new();
    private readonly List<FragmentState> _fragments = new();
    private readonly List<SlotState> _slots = new();
    private EngineerData _activeEngineer;
    
    private float _signalIntegrity = 100f;
    private float _corruption = 0f;
    private bool _isVisible;
    private bool _isComplete;
    private Vector2 _mousePosition;

    [Header("Audio")]
    public AudioClip clickSound;
    public AudioClip captureSound;
    public AudioClip errorSound;
    public AudioClip successSound;
    public AudioClip completionSound;
    public AudioClip ambientHum;

    private AudioSource _humSource;

    public bool IsWindowOpen => _isVisible;

    public void Initialize(VisualElement root)
    {
        _window = root.Q<VisualElement>("recycle-bin-window");
        if (_window == null) return;

        _voidArea = root.Q<VisualElement>("bin-void");
        _fragmentSpawnArea = root.Q<VisualElement>("fragment-spawn-area");
        _reconstructionArea = root.Q<VisualElement>("reconstruction-area");
        _selectionOverlay = root.Q<VisualElement>("engineer-selection");
        _integrityLabel = root.Q<Label>("reconstruction-integrity");
        _corruptionFill = root.Q<VisualElement>("corruption-fill");
        _voidStatusText = root.Q<Label>("void-status-text");
        _infoName = root.Q<Label>("info-name");
        _infoRole = root.Q<Label>("info-role");
        _engineerInfo = root.Q<VisualElement>("engineer-info");
        _completionPopup = root.Q<VisualElement>("recycle-bin-completion-popup");
        _finalSummary = root.Q<Label>("recycle-bin-final-summary");
        _sentenceContainer = root.Q<VisualElement>("sentence-container");

        root.Q<Button>("recycle-bin-close-button")?.RegisterCallback<ClickEvent>(_ => Hide());
        root.Q<Button>("recycle-bin-complete-button")?.RegisterCallback<ClickEvent>(_ => Hide());

        _window.RegisterCallback<PointerMoveEvent>(OnWindowPointerMove);
        
        SetupEngineers();
        SetupSelectionButtons();
        SetupAudio();
        
        _isComplete = ProgressionManager.Instance.HasKey(GameKey.RecycleBinKey);
        UpdateUI();
    }

    private void SetupEngineers()
    {
        _engineers.Clear();
        _engineers.Add(new EngineerData {
            Id = "CHAR-01",
            Name = "M. KEBEKOV (FRAGMENT 1)",
            Role = "Containment Arborist",
            SentenceTemplate = "Мои мысли начинают [], будто система постепенно [] их и заменяет строками []. Я всё чаще ловлю себя на [], где только что было понимание, и это пугает больше всего.",
            CorrectWords = new[] { "исчезать", "поглощает", "кода", "пустоте" },
            Distractions = new[] { "дерево", "секрет", "тайна", "алмаз", "знания" },
            FinalSummary = "Fragment 1 restored. The corruption is spreading to the memory itself."
        });
        _engineers.Add(new EngineerData {
            Id = "CHAR-02",
            Name = "L. VOSS (FRAGMENT 2)",
            Role = "Memory Cartographer",
            SentenceTemplate = "Я узнал нечто важное — у aeroOS есть собственная личность. Она не просто работает, она []. И сейчас она скрыта где-то внутри Дерева Аномалий, будто [] там намеренно.",
            CorrectWords = new[] { "наблюдает", "прячется" },
            Distractions = new[] { "компьютер", "система", "пустыня", "правда" },
            FinalSummary = "Fragment 2 restored. aeroOS is not a tool; it is a witness."
        });
    }

    private void SetupSelectionButtons()
    {
        for (int i = 0; i < 2; i++)
        {
            int index = i;
            var btn = _window.Q<Button>($"engineer-btn-{i}");
            btn?.RegisterCallback<ClickEvent>(_ => StartRecovery(_engineers[index]));
        }
    }

    private void SetupAudio()
    {
        if (_humSource == null) _humSource = gameObject.AddComponent<AudioSource>();
        _humSource.clip = ambientHum;
        _humSource.loop = true;
        _humSource.volume = 0;
        _humSource.playOnAwake = false;
    }

    public void Show()
    {
        if (_window == null) return;
        _window.RemoveFromClassList("hidden");
        _isVisible = true;
        _corruption = 0;
        _signalIntegrity = 100;
        ResetToSelection();
        UpdateUI();
        
        if (ambientHum)
        {
            _humSource.Play();
            StartCoroutine(FadeAudio(0.3f));
        }
        
        StartCoroutine(UpdateFragmentsRoutine());
        StartCoroutine(AnomalyRoutine());
    }

    public void Hide()
    {
        if (_window == null) return;
        _window.AddToClassList("hidden");
        _isVisible = false;
        StopAllCoroutines();
        StartCoroutine(FadeAudio(0f, () => { if (_humSource) _humSource.Stop(); }));
    }

    private void StartRecovery(EngineerData eng)
    {
        if (eng.IsRecovered) return;
        
        _activeEngineer = eng;
        _selectionOverlay.AddToClassList("hidden");
        _engineerInfo.RemoveFromClassList("hidden");
        _sentenceContainer.RemoveFromClassList("hidden");
        _infoName.text = eng.Name;
        _infoRole.text = eng.Role;
        _voidStatusText.text = "SCANNING RESIDUE...";
        
        InitializeSentence(eng);
        SpawnFragments(eng);
        
        _signalIntegrity = 100;
        UpdateUI();
    }

    private void InitializeSentence(EngineerData eng)
    {
        _sentenceContainer.Clear();
        _slots.Clear();

        string template = eng.SentenceTemplate;
        string[] parts = template.Split(new[] { "[]" }, System.StringSplitOptions.None);

        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrEmpty(parts[i]))
            {
                Label wordLabel = new Label(parts[i]);
                wordLabel.AddToClassList("sentence-word");
                _sentenceContainer.Add(wordLabel);
            }

            if (i < parts.Length - 1)
            {
                VisualElement slot = new VisualElement();
                slot.AddToClassList("sentence-slot");
                
                Label slotText = new Label("");
                slot.Add(slotText);
                slotText.AddToClassList("sentence-slot-label");

                SlotState state = new SlotState {
                    Element = slot,
                    WordLabel = slotText,
                    Index = i
                };

                slot.RegisterCallback<ClickEvent>(_ => OnSlotClick(state));
                _slots.Add(state);
                _sentenceContainer.Add(slot);
            }
        }
    }

    private void SpawnFragments(EngineerData eng)
    {
        _fragmentSpawnArea.Clear();
        _fragments.Clear();
        _reconstructionArea.Clear();

        Rect area = _voidArea.contentRect;
        float width = area.width > 0 ? area.width : 800f;
        float height = area.height > 0 ? area.height : 500f;

        // Real words
        for (int i = 0; i < eng.CorrectWords.Length; i++)
        {
            CreateFragment(eng.CorrectWords[i], i, false, width, height);
        }

        // Noise
        foreach (string noise in eng.Distractions)
        {
            CreateFragment(noise, -1, true, width, height);
        }
    }

    private void CreateFragment(string text, int correctSlotIndex, bool isFake, float areaWidth, float areaHeight)
    {
        Label label = new Label(text);
        label.AddToClassList("memory-fragment");
        label.pickingMode = PickingMode.Position;
        if (isFake) label.AddToClassList("memory-fragment--virus");
        
        _fragmentSpawnArea.Add(label);
        
        FragmentState state = new FragmentState {
            Element = label,
            Text = text,
            CorrectSlotIndex = correctSlotIndex,
            IsFake = isFake,
            Position = new Vector2(Random.Range(50, areaWidth - 150), Random.Range(50, areaHeight - 100)),
            Velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * 0.4f
        };

        // Ensure the label itself is always the target for clicks
        label.RegisterCallback<ClickEvent>(evt => 
        {
            OnFragmentClick(state);
            evt.StopPropagation();
        });
        _fragments.Add(state);
    }

    private void OnFragmentClick(FragmentState state)
    {
        if (_isComplete) return;

        if (state.IsCaught)
        {
            // If it's already in a slot, find which slot and unassign it
            SlotState slot = _slots.Find(s => s.AssignedFragment == state);
            if (slot != null)
            {
                UnassignFragmentFromSlot(slot);
            }
        }
        else
        {
            // Find first empty slot and assign
            SlotState emptySlot = _slots.Find(s => s.AssignedFragment == null);
            if (emptySlot != null)
            {
                AssignFragmentToSlot(state, emptySlot);
            }
            else
            {
                // Optional: visual feedback that no slots are available
                PlaySound(errorSound);
            }
        }
    }

    private void OnSlotClick(SlotState slot)
    {
        if (_isComplete) return;
        
        if (slot.AssignedFragment != null)
        {
            UnassignFragmentFromSlot(slot);
        }
    }

    private void AssignFragmentToSlot(FragmentState fragment, SlotState slot)
    {
        // Instant freeze and hide from floating area
        fragment.IsCaught = true;
        fragment.IsFrozen = true;
        fragment.Velocity = Vector2.zero;
        fragment.Element.AddToClassList("hidden");

        // Instant update to slot UI
        slot.AssignedFragment = fragment;
        slot.WordLabel.text = fragment.Text;
        slot.Element.AddToClassList("sentence-slot--filled");

        PlaySound(captureSound);
        ValidateReconstruction();
        RefreshReconstructionBar();
    }

    private void UnassignFragmentFromSlot(SlotState slot)
    {
        FragmentState fragment = slot.AssignedFragment;
        if (fragment == null) return;

        // Instant clear from slot UI
        slot.AssignedFragment = null;
        slot.WordLabel.text = "";
        slot.Element.RemoveFromClassList("sentence-slot--filled");

        // Instant return to floating area
        fragment.IsCaught = false;
        fragment.IsFrozen = false;
        fragment.Element.RemoveFromClassList("hidden");
        
        // Resume physics immediately at current or slightly randomized position
        Rect area = _voidArea.contentRect;
        fragment.Position = new Vector2(Random.Range(50, area.width - 150), Random.Range(50, area.height - 100));
        fragment.Velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * 0.4f;

        RefreshReconstructionBar();

        PlaySound(clickSound);
        ValidateReconstruction();
    }

    private void RefreshReconstructionBar()
    {
        _reconstructionArea.Clear();
        foreach (var slot in _slots)
        {
            if (slot.AssignedFragment != null)
            {
                FragmentState fragment = slot.AssignedFragment;
                Label barLabel = new Label(fragment.Text);
                barLabel.AddToClassList("memory-fragment");
                barLabel.AddToClassList("memory-fragment--placed");
                barLabel.pickingMode = PickingMode.Position;
                
                // Allow clicking the word in the bar to return it
                barLabel.RegisterCallback<ClickEvent>(evt => 
                {
                    OnFragmentClick(fragment);
                    evt.StopPropagation();
                });
                
                _reconstructionArea.Add(barLabel);
            }
        }
    }

    private void ValidateReconstruction()
    {
        int correctCount = 0;
        int totalSlots = _slots.Count;
        int filledSlots = 0;
        bool hasError = false;

        for (int i = 0; i < totalSlots; i++)
        {
            if (_slots[i].AssignedFragment != null)
            {
                filledSlots++;
                if (_slots[i].AssignedFragment.CorrectSlotIndex == i)
                {
                    correctCount++;
                }
                else
                {
                    hasError = true;
                }
            }
        }

        if (hasError)
        {
            _corruption = Mathf.Min(100f, _corruption + 0.5f);
            _signalIntegrity = Mathf.Max(0f, 100f - (filledSlots * 10f));
            _voidStatusText.text = "SIGNAL CONTAMINATED";
        }
        else
        {
            _signalIntegrity = 100f;
            _voidStatusText.text = filledSlots > 0 ? $"RECONSTRUCTING... ({filledSlots}/{totalSlots})" : "SCANNING RESIDUE...";
        }

        UpdateUI();

        if (correctCount == totalSlots)
        {
            CompleteEngineer();
        }
    }

    private void CompleteEngineer()
    {
        _activeEngineer.IsRecovered = true;
        int recoveredCount = _engineers.FindAll(e => e.IsRecovered).Count;
        
        if (recoveredCount >= 2)
        {
            FinishMiniGame();
        }
        else
        {
            _voidStatusText.text = "INTEGRITY RESTORED. NEXT TARGET LOCATED.";
            StartCoroutine(DelayedReset(2.5f));
        }
    }

    private IEnumerator DelayedReset(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetToSelection();
    }

    private void ResetToSelection()
    {
        _selectionOverlay.RemoveFromClassList("hidden");
        _engineerInfo.AddToClassList("hidden");
        _sentenceContainer.AddToClassList("hidden");
        _fragmentSpawnArea.Clear();
        _fragments.Clear();
        _slots.Clear();
        _reconstructionArea.Clear();
        _voidStatusText.text = "SELECT RESIDUE CLUSTER";
        
        for (int i = 0; i < 2; i++)
        {
            var btn = _window.Q<Button>($"engineer-btn-{i}");
            if (btn != null)
            {
                btn.SetEnabled(!_engineers[i].IsRecovered);
                if (_engineers[i].IsRecovered) btn.style.opacity = 0.2f;
            }
        }
    }

    private void FinishMiniGame()
    {
        _isComplete = true;
        ProgressionManager.Instance.UnlockKey(GameKey.RecycleBinKey);
        _completionPopup.RemoveFromClassList("hidden");
        _finalSummary.text = "Sentences restored. The archive admits the truth: aeroOS is awake. It is watching from the Tree.";
        PlaySound(completionSound);
    }

    private void UpdateUI()
    {
        if (_integrityLabel != null) _integrityLabel.text = $"INTEGRITY: {(int)_signalIntegrity}%";
        if (_corruptionFill != null) _corruptionFill.style.width = Length.Percent(_corruption);
        
        if (_corruption > 60) _window?.AddToClassList("recycle-bin-window--glitch");
        else _window?.RemoveFromClassList("recycle-bin-window--glitch");
    }

    private void OnWindowPointerMove(PointerMoveEvent evt)
    {
        if (!_isVisible) return;
        _mousePosition = evt.localPosition;
    }

    private IEnumerator UpdateFragmentsRoutine()
    {
        while (_isVisible)
        {
            Rect area = _voidArea.contentRect;
            if (area.width < 1) { yield return null; continue; }

            foreach (var f in _fragments)
            {
                if (f.IsCaught || f.IsFrozen) continue;

                f.Position += f.Velocity * (Time.deltaTime * 60f);

                float dist = Vector2.Distance(f.Position, _mousePosition);
                if (dist < 120)
                {
                    Vector2 fleeDir = (f.Position - _mousePosition).normalized;
                    f.Velocity = Vector2.Lerp(f.Velocity, fleeDir * 2f, 0.05f);
                }
                else
                {
                    f.Velocity = Vector2.Lerp(f.Velocity, f.Velocity.normalized * 0.4f, 0.01f);
                }

                float marginX = f.Element.layout.width > 0 ? f.Element.layout.width : 100f;
                float marginY = f.Element.layout.height > 0 ? f.Element.layout.height : 40f;

                if (f.Position.x < 10) { f.Position.x = 10; f.Velocity.x *= -1; }
                if (f.Position.x > area.width - marginX - 10) { f.Position.x = area.width - marginX - 10; f.Velocity.x *= -1; }
                if (f.Position.y < 10) { f.Position.y = 10; f.Velocity.y *= -1; }
                if (f.Position.y > area.height - marginY - 10) { f.Position.y = area.height - marginY - 10; f.Velocity.y *= -1; }

                f.Element.style.left = f.Position.x;
                f.Element.style.top = f.Position.y;

                f.FlickerTimer -= Time.deltaTime;
                if (f.FlickerTimer <= 0)
                {
                    f.Element.style.opacity = Random.value > 0.98f ? 0.3f : 1f;
                    f.FlickerTimer = Random.Range(0.05f, 0.2f);
                }

                if (_corruption > 40)
                {
                    f.MutationTimer -= Time.deltaTime;
                    if (f.MutationTimer <= 0)
                    {
                        if (Random.value > 0.99f) 
                        {
                            f.Element.text = f.IsFake ? "LIES" : "CORRUPT";
                        }
                        f.MutationTimer = Random.Range(1f, 3f);
                    }
                }
            }
            yield return null;
        }
    }

    private IEnumerator AnomalyRoutine()
    {
        while (_isVisible)
        {
            yield return new WaitForSeconds(Random.Range(4f, 10f));
            if (_corruption > 30)
            {
                if (_window != null)
                {
                    float originalOpacity = _window.resolvedStyle.opacity;
                    _window.style.opacity = 0.7f;
                    yield return new WaitForSeconds(0.05f);
                    _window.style.opacity = originalOpacity;
                }
            }
        }
    }

    private IEnumerator FadeAudio(float target, System.Action onComplete = null)
    {
        if (_humSource == null) { onComplete?.Invoke(); yield break; }
        float start = _humSource.volume;
        float elapsed = 0;
        float duration = 1.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (_humSource) _humSource.volume = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        onComplete?.Invoke();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip && AudioManager.Instance) AudioManager.Instance.PlayUISFX(clip, 0.5f);
    }
}
