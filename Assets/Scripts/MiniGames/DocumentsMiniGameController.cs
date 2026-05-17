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
    public Image backgroundImage;

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
    private Texture2D _generatedWallpaper;
    private Sprite _wallpaperSprite;

    private void Start()
    {
        ApplyDesktopWallpaper();
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

    private void OnDestroy()
    {
        if (_wallpaperSprite != null)
        {
            Destroy(_wallpaperSprite);
        }

        if (_generatedWallpaper != null)
        {
            Destroy(_generatedWallpaper);
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

    private void ApplyDesktopWallpaper()
    {
        if (backgroundImage == null)
        {
            GameObject background = GameObject.Find("BackgroundGradient");
            if (background != null)
            {
                backgroundImage = background.GetComponent<Image>();
            }
        }

        if (backgroundImage == null)
        {
            return;
        }

        _generatedWallpaper = GenerateDesktopWallpaper();
        _wallpaperSprite = Sprite.Create(
            _generatedWallpaper,
            new Rect(0, 0, _generatedWallpaper.width, _generatedWallpaper.height),
            new Vector2(0.5f, 0.5f),
            100f);

        backgroundImage.sprite = _wallpaperSprite;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.preserveAspect = false;
        backgroundImage.color = new Color(1f, 1f, 1f, 0.42f);
    }

    private Texture2D GenerateDesktopWallpaper()
    {
        int width = 1024;
        int height = 1024;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / width;
                float ny = 1f - (float)y / height;
                Color color;

                if (ny > 0.35f)
                {
                    float skyT = (ny - 0.35f) / 0.65f;
                    color = Color.Lerp(new Color(0.5f, 0.8f, 1f), new Color(0.05f, 0.25f, 0.85f), skyT);

                    float sunDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.12f, 0.88f));
                    if (sunDist < 0.04f)
                    {
                        color = Color.Lerp(Color.white, color, sunDist / 0.04f);
                    }
                    else if (sunDist < 0.08f)
                    {
                        color = Color.Lerp(new Color(1f, 0.98f, 0.8f), color, (sunDist - 0.04f) / 0.04f);
                    }

                    float flareAngle = Mathf.Atan2(ny - 0.88f, nx - 0.12f);
                    float flareDist = sunDist;
                    if (Mathf.Abs(flareAngle) < 0.1f && flareDist < 0.3f && flareDist > 0.08f)
                    {
                        float flareIntensity = Mathf.Pow(1f - (flareDist - 0.08f) / 0.22f, 2f);
                        color = Color.Lerp(color, new Color(0.8f, 0.9f, 1f), flareIntensity * 0.3f);
                    }

                    float largeBubbleDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.5f, 0.78f));
                    if (largeBubbleDist < 0.15f)
                    {
                        float bubbleAlpha = 1f - largeBubbleDist / 0.15f;
                        float rim = Mathf.Pow(1f - Mathf.Abs(largeBubbleDist - 0.12f) / 0.03f, 0.5f);
                        rim = Mathf.Clamp01(rim);
                        Color bubbleColor = new Color(0.7f, 0.85f, 1f, 0.4f);
                        Color rimColor = new Color(0.9f, 0.95f, 1f, 0.8f);
                        color = Color.Lerp(color, rim > 0.5f ? rimColor : bubbleColor, bubbleAlpha * 0.6f);

                        float highlightDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.47f, 0.82f));
                        if (highlightDist < 0.03f)
                        {
                            color = Color.Lerp(color, Color.white, (1f - highlightDist / 0.03f) * 0.7f);
                        }
                    }

                    float smallBubbleDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.18f, 0.62f));
                    if (smallBubbleDist < 0.1f)
                    {
                        float bubbleAlpha = 1f - smallBubbleDist / 0.1f;
                        Color bubbleColor = new Color(0.7f, 0.85f, 1f, 0.4f);
                        color = Color.Lerp(color, bubbleColor, bubbleAlpha * 0.5f);

                        float highlightDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.16f, 0.65f));
                        if (highlightDist < 0.02f)
                        {
                            color = Color.Lerp(color, Color.white, (1f - highlightDist / 0.02f) * 0.6f);
                        }
                    }

                    float balloonX = 0.62f;
                    float balloonY = 0.48f;
                    float balloonDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(balloonX, balloonY));
                    if (balloonDist < 0.04f)
                    {
                        float balloonT = 1f - balloonDist / 0.04f;
                        color = Color.Lerp(color, new Color(0.9f, 0.3f, 0.2f), balloonT);
                    }

                    if (nx > balloonX - 0.01f && nx < balloonX + 0.01f && ny > balloonY - 0.06f && ny < balloonY - 0.03f)
                    {
                        color = new Color(0.5f, 0.3f, 0.1f);
                    }

                    if (nx > 0.58f && ny < 0.65f && ny > 0.35f)
                    {
                        float buildingSeed = Mathf.Sin(nx * 100f) * 0.5f + 0.5f;
                        float buildingHeight = 0.38f + buildingSeed * 0.25f;
                        if (ny < buildingHeight)
                        {
                            float windowPattern = (Mathf.Sin(nx * 200f) > 0.7f && Mathf.Sin(ny * 150f) > 0.7f) ? 1f : 0f;
                            Color buildingColor = Color.Lerp(new Color(0.6f, 0.65f, 0.7f), new Color(0.4f, 0.45f, 0.5f), buildingSeed);
                            if (windowPattern > 0.5f)
                            {
                                buildingColor = Color.Lerp(buildingColor, new Color(0.9f, 0.95f, 1f), 0.5f);
                            }

                            color = buildingColor;
                        }
                    }
                }
                else if (ny > 0.3f && ny <= 0.35f)
                {
                    float treeNoise = Mathf.Sin(nx * 40f + Mathf.Sin(nx * 15f) * 3f) * 0.5f + 0.5f;
                    color = Color.Lerp(new Color(0.15f, 0.45f, 0.1f), new Color(0.25f, 0.55f, 0.15f), treeNoise);
                }
                else
                {
                    float grassNoise = Mathf.PerlinNoise(nx * 30f, ny * 30f) * 0.2f;
                    float grassDetail = Mathf.Sin(nx * 100f + ny * 50f) * 0.05f;
                    color = new Color(0.18f + grassNoise + grassDetail, 0.55f + grassNoise, 0.08f);

                    float treeDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.82f, 0.15f));
                    if (treeDist < 0.06f && ny < 0.28f)
                    {
                        float treeAlpha = 1f - treeDist / 0.06f;
                        color = Color.Lerp(color, new Color(0.12f, 0.35f, 0.08f), treeAlpha);
                    }

                    if (nx > 0.815f && nx < 0.825f && ny > 0.05f && ny < 0.15f)
                    {
                        color = new Color(0.3f, 0.2f, 0.1f);
                    }

                    if (nx > 0.8f && nx < 0.84f && ny > 0.07f && ny < 0.09f)
                    {
                        color = new Color(0.4f, 0.25f, 0.15f);
                    }
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }
}
