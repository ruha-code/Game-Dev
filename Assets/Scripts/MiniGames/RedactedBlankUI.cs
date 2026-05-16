using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class RedactedBlankUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum State { Redacted, Scanned, Restored }
    
    [Header("Settings")]
    public string correctWord;
    public string hintText;
    public float scanDuration = 1.8f;

    [Header("References")]
    public TMP_Text textComponent;
    public Image fillIndicator;
    public GameObject selectionHighlight;

    public event Action<RedactedBlankUI> OnSelected;
    public event Action<RedactedBlankUI> OnScanned;

    private State _currentState = State.Redacted;
    private float _scanTimer = 0f;
    private bool _isHovering = false;
    private bool _isScanned = false;

    public State CurrentState => _currentState;

    private void Start()
    {
        UpdateVisuals();
        if (fillIndicator) fillIndicator.fillAmount = 0;
        if (selectionHighlight) selectionHighlight.SetActive(false);
    }

    private void Update()
    {
        if (_currentState == State.Redacted && _isHovering)
        {
            _scanTimer += Time.deltaTime;
            if (fillIndicator) fillIndicator.fillAmount = Mathf.Clamp01(_scanTimer / scanDuration);

            if (_scanTimer >= scanDuration && !_isScanned)
            {
                _isScanned = true;
                _currentState = State.Scanned;
                UpdateVisuals();
                OnScanned?.Invoke(this);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_currentState == State.Restored) return;
        _isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        if (_currentState == State.Redacted)
        {
            _scanTimer = 0f;
            if (fillIndicator) fillIndicator.fillAmount = 0;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_currentState == State.Restored) return;
        OnSelected?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        if (selectionHighlight) selectionHighlight.SetActive(selected);
    }

    public bool Restore(string word)
    {
        if (word.Equals(correctWord, StringComparison.OrdinalIgnoreCase))
        {
            _currentState = State.Restored;
            textComponent.text = word.ToUpper();
            textComponent.color = new Color(0.2f, 0.8f, 0.2f); // Soft green
            UpdateVisuals();
            return true;
        }
        return false;
    }

    private void UpdateVisuals()
    {
        switch (_currentState)
        {
            case State.Redacted:
                textComponent.text = "[███████]";
                break;
            case State.Scanned:
                textComponent.text = hintText;
                break;
            case State.Restored:
                if (fillIndicator) fillIndicator.gameObject.SetActive(false);
                break;
        }
    }

    public void PlayShake()
    {
        StartCoroutine(ShakeRoutine());
    }

    private System.Collections.IEnumerator ShakeRoutine()
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;
        float duration = 0.3f;
        float magnitude = 5f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = originalPos;
    }
}
