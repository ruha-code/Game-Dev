using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class DocumentsMiniGameController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text progressText;
    public TMP_Text trustText;
    public TMP_Text statusText;
    public GameObject completionPopup;
    public Button continueButton;
    public Button fakeCloseButton;

    [Header("Puzzle Elements")]
    public List<RedactedBlankUI> redactedBlanks;
    public List<LieLineUI> lieLines;
    public List<Button> wordButtons;

    [Header("Audio")]
    public AudioClip clickSound;
    public AudioClip hoverSound;
    public AudioClip successSound;
    public AudioClip errorSound;
    public AudioClip lieExposedSound;
    public AudioClip completionSound;

    private int _progress = 0;
    private int _maxProgress = 6;
    private int _systemTrust = 87;
    private RedactedBlankUI _selectedBlank;
    private bool _isComplete = false;

    private void Start()
    {
        _maxProgress = redactedBlanks.Count + lieLines.Count;
        UpdateUI();
        completionPopup.SetActive(false);

        foreach (var blank in redactedBlanks)
        {
            blank.OnSelected += HandleBlankSelected;
            blank.OnScanned += HandleBlankScanned;
        }

        foreach (var lie in lieLines)
        {
            lie.OnLieExposed += HandleLieExposed;
        }

        foreach (var btn in wordButtons)
        {
            string word = btn.GetComponentInChildren<TMP_Text>().text;
            btn.onClick.AddListener(() => HandleWordSelected(word));
        }

        if (fakeCloseButton)
        {
            fakeCloseButton.onClick.AddListener(() => SetStatus("Document cannot be closed during recovery."));
        }

        if (continueButton)
        {
            continueButton.onClick.AddListener(ReturnToDesktop);
        }
    }

    private void HandleBlankSelected(RedactedBlankUI blank)
    {
        if (_isComplete) return;
        PlaySound(clickSound);

        if (_selectedBlank != null) _selectedBlank.SetSelected(false);
        _selectedBlank = blank;
        _selectedBlank.SetSelected(true);
        
        SetStatus("Selected blank. Choose word from bank.");
    }

    private void HandleBlankScanned(RedactedBlankUI blank)
    {
        PlaySound(hoverSound);
        SetStatus("Scan complete. Hint available.");
        Debug.Log($"[AeroDocs] Blank scanned: {blank.correctWord}");
    }

    private void HandleWordSelected(string word)
    {
        if (_isComplete || _selectedBlank == null) return;
        PlaySound(clickSound);

        if (_selectedBlank.CurrentState == RedactedBlankUI.State.Redacted)
        {
            SetStatus("Scan required before restoration.");
            _selectedBlank.PlayShake();
            return;
        }

        if (_selectedBlank.Restore(word))
        {
            _progress++;
            _systemTrust -= 7;
            _selectedBlank.SetSelected(false);
            _selectedBlank = null;
            
            UpdateProgressStatus();
            UpdateUI();
            PlaySound(successSound);
            Debug.Log($"[AeroDocs] Correct word restored: {word}");
            
            CheckCompletion();
        }
        else
        {
            _systemTrust -= 3;
            _selectedBlank.PlayShake();
            SetStatus("Incorrect restoration attempt.");
            PlaySound(errorSound);
            UpdateUI();
            Debug.Log($"[AeroDocs] Wrong word attempted: {word}");
        }
    }

    private void HandleLieExposed(LieLineUI lie)
    {
        if (_isComplete) return;
        
        _progress++;
        _systemTrust -= 13;
        
        SetStatus(_progress == 1 ? "Correction unauthorized." : "Containment statement rejected.");
        PlaySound(lieExposedSound);
        UpdateUI();
        Debug.Log($"[AeroDocs] Lie exposed: {lie.truthText}");
        
        CheckCompletion();
    }

    private void UpdateProgressStatus()
    {
        string[] statuses = {
            "Recovery fragment accepted.",
            "User curiosity increasing.",
            "Please stop reading.",
            "Recovery cannot be reversed."
        };
        int idx = Mathf.Clamp(_progress - 1, 0, statuses.Length - 1);
        SetStatus(statuses[idx]);
    }

    private void UpdateUI()
    {
        progressText.text = $"Recovered Truth: {_progress} / {_maxProgress}";
        
        if (_systemTrust < 0) _systemTrust = 0;
        
        if (_isComplete)
        {
            trustText.text = "System Trust: watching";
        }
        else if (_systemTrust < 40)
        {
            trustText.text = "System Trust: unstable";
        }
        else
        {
            trustText.text = $"System Trust: {_systemTrust}%";
        }
    }

    private void SetStatus(string message)
    {
        statusText.text = message;
    }

    private void CheckCompletion()
    {
        if (_progress >= _maxProgress)
        {
            _isComplete = true;
            UpdateUI();
            StartCoroutine(CompletionRoutine());
        }
    }

    private System.Collections.IEnumerator CompletionRoutine()
    {
        SetStatus("Recovery complete. Wallpaper anomaly detected.");
        yield return new WaitForSeconds(1.5f);
        
        PlaySound(completionSound);
        completionPopup.SetActive(true);
        Debug.Log("[AeroDocs] Mini-game completed.");
    }

    private void ReturnToDesktop()
    {
        Debug.Log("[AeroDocs] Returning to AeroDesktopScene.");
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.UnlockKey(GameKey.DocumentsKey);
            Debug.Log("[AeroDocs] DocumentsKey unlocked via ProgressionManager.");
        }
        else
        {
            Debug.LogWarning("[AeroDocs] ProgressionManager not found. Key not unlocked.");
        }

        SceneManager.LoadScene("AeroDesktopScene");
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISFX(clip);
        }
    }
}
