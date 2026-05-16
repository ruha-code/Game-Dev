using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class LieLineUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    public string falseText;
    public string truthText;

    [Header("References")]
    public TMP_Text textComponent;

    public event Action<LieLineUI> OnLieExposed;
    private bool _isExposed = false;

    private void Start()
    {
        if (textComponent) textComponent.text = falseText;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isExposed) return;

        _isExposed = true;
        ExposeTruth();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isExposed) return;
        // Subtle hint: red underline or slight flicker
        textComponent.fontStyle = FontStyles.Underline;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isExposed) return;
        textComponent.fontStyle = FontStyles.Normal;
    }

    private void ExposeTruth()
    {
        StartCoroutine(ExposeRoutine());
    }

    private System.Collections.IEnumerator ExposeRoutine()
    {
        // Flicker effect
        float elapsed = 0f;
        float duration = 0.5f;
        Color originalColor = textComponent.color;

        while (elapsed < duration)
        {
            textComponent.color = (UnityEngine.Random.value > 0.5f) ? Color.red : originalColor;
            elapsed += Time.deltaTime;
            yield return null;
        }

        textComponent.text = truthText;
        textComponent.color = originalColor;
        textComponent.fontStyle = FontStyles.Normal;
        
        OnLieExposed?.Invoke(this);
    }
}
